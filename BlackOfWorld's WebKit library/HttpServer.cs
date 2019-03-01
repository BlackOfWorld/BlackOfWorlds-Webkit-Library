using BlackOfWorld.Webkit.Models;
using BlackOfWorld.Webkit.Toolkit;
using BlackOfWorld.Webkit.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
namespace BlackOfWorld.Webkit
{
    public class HttpServer
    {
        /// <summary>
        /// This is for advanced people who knows about HttpListener a lot
        /// </summary>
        public HttpListener httpServer { get; internal set; }
        /// <summary>
        /// This is the config, pretty self-explanatory
        /// </summary>
        public ServerConfig httpServerConfig = null;
        private readonly StaticFileManager sFM = new StaticFileManager();

        #region Events
        public delegate string OnDataReceive(object sender, DataReceiveArgs args);
        public event OnDataReceive OnDataReceiveEvent;
        public delegate bool OnDataStaticSent(object sender, DataReceiveArgs args);
        public event OnDataStaticSent OnDataStaticSentEvent;
        public delegate bool OnFirewallBan(object sender, FirewallBanArgs args);
        public event OnFirewallBan OnFirewallBanEvent;
        #endregion
        /// <summary>
        /// Currents status of HttpServer
        /// </summary>
        public CurrentServerStatus CurrentStatus = CurrentServerStatus.NotListening;
        private RateLimit rl;
        private Firewall fr;
        private byte[] rateLimited;
        private bool _lock = false;
        internal static string mainFolderPath;
        public HttpServer()
        {
            httpServer = new HttpListener();
            mainFolderPath = Path.Combine(System.AppContext.BaseDirectory, "WebkitLibrary");
            if (!Directory.Exists(mainFolderPath))
                Directory.CreateDirectory(mainFolderPath);
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var ex = (Exception)args.ExceptionObject;
                Tools.EnablePrint = true;
                Tools.ConsolePrint("Internal unhandled exception happened! Please submit logs and source code to issue tracker so I can fix it.\nMessage: " + ex.Message + "\nStack trace:\n" + ex.StackTrace + "\n\nThis program will terminate.\n");
                if (Debugger.IsAttached)
                    Debugger.Break();
                Environment.Exit(1);
            };
            OnFirewallBanEvent += (sender, args) => true;
            OnDataStaticSentEvent += (sender, args) => true;
            OnDataReceiveEvent += (sender, args) => "<h1>This is an example page</h1>";
        }
        /// <summary>
        /// Starts the server
        /// </summary>
        /// <returns>Any errors that could have happened</returns>
        public WebkitErrors Start()
        {
            if (httpServerConfig == null)
                return WebkitErrors.ConfigEmpty;
            Tools.EnablePrint = httpServerConfig.EnableConsolePrint;
            rl = new RateLimit(httpServerConfig.RateLimitPacketAmount, httpServerConfig.RateLimitWaitTime, httpServerConfig.RateLimitSecLimit);
            fr = new Firewall(httpServerConfig.FirewallPacketBan, httpServerConfig.FirewallPacketInterval, OnFirewallBanEvent);
            sFM.Start(httpServerConfig);
            rateLimited = Encoding.UTF8.GetBytes("You are being rate limited! Please wait " + httpServerConfig.RateLimitWaitTime + " seconds. (pro tip: every time you try to refresh the page, the timer will be set to max)");
            string[] blackPrefixes = new[] { "http://*.com", "http://*:", "https://*.com", "https://*:", "http://+.com", "http://+:", "https://+.com", "https://+:" }; // no racism lol, also the only way to prevent any false detection
            try
            {
                foreach (var prefix in httpServerConfig.Prefixes)
                {
                    if (blackPrefixes.Any((x) => prefix.StartsWith(x)))
                    {
                        Tools.ConsolePrint($"Due to RFC 7230, we can't let you use \"{prefix}\" as it's classified as unsafe.\n");
                        continue;
                    }
                    if (prefix.StartsWith("https://") && httpServerConfig.SSLCertificate == null)
                    {
                        Tools.ConsolePrint($"As you don't have SSLCertificate setup, we can't let you use https. Skipping \"{prefix}\"\n");
                        continue;
                    }
                    if (!prefix.EndsWith("/"))
                    {
                        Tools.ConsolePrint($"Prefixes must end with '/'. Skipping \"{prefix}\"\n");
                        continue;
                    }
                    httpServer.Prefixes.Add(prefix);
                    Tools.ConsolePrint($"Adding \"{prefix}\" to prefix list\n");
                }

                if (httpServer.Prefixes.Count <= 0)
                {
                    bool hmm = Tools.EnablePrint;
                    Tools.EnablePrint = true;
                    Tools.ConsolePrint("Oops! Looks like no you don't have any valid prefixes! Server will not start.\n");
                    Tools.EnablePrint = hmm;
                    return WebkitErrors.NotRunning;
                }
                Tools.ConsolePrint("Starting the server!\n");
                if (httpServerConfig.EnforceProtectionPolicy)
                    httpServer.ExtendedProtectionPolicy = new System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy(System.Security.Authentication.ExtendedProtection.PolicyEnforcement.Always);
                httpServer.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
                httpServer.Start();

                Thread listenThread = new Thread(ServerThread) { Name = "Webkit server" };
                listenThread.Start();
                CurrentStatus = CurrentServerStatus.Listening;
            }
            catch (Exception)
            { CurrentStatus = CurrentServerStatus.InternalExceptionHappened; return WebkitErrors.UnknownError; }
            Tools.ConsolePrint("Server started!\n");
            return WebkitErrors.Ok;
        }

        private void ServerThread()
        {
            while (httpServer.IsListening)
            {
                if (_lock) return;
                var context = httpServer.BeginGetContext(ServerWork, null);
                context.AsyncWaitHandle.WaitOne();
                context.AsyncWaitHandle.Dispose();
            }
        }

        private void ServerWork(IAsyncResult result)
        {
            HttpListenerContext context;

            try
            {
                context = httpServer.EndGetContext(result);
            }
            catch (Exception) { return; }

            HttpListenerRequest request = context.Request;

            using (HttpListenerResponse response = context.Response)
            {
                var remoteIP = request.RemoteEndPoint.Address;

                if ((!request.IsLocal && httpServerConfig.LocalOnly) || Array.Exists(httpServerConfig.BlacklistedIPs, (e) => e == remoteIP.ToString()))
                    return;
                if (!fr.AccessPermitted(remoteIP))
                {
                    response.StatusCode = 403;
                    WriteToRemote(request, response, new byte[0]);
                    return;
                }
                if (!rl.IsAllowed(remoteIP))
                {
                    response.AddHeader("Retry-After", httpServerConfig.RateLimitWaitTime.ToString());
                    response.StatusCode = 429;
                    WriteToRemote(request, response, rateLimited);
                    return;
                }

                string item = request.RawUrl;

                if (item == "/")
                    item = "/index.html";

                byte[] buffer = sFM.GetStaticItem(item);
                DataReceiveArgs eventArgs = new DataReceiveArgs()
                {
                    ContentType = request.ContentType,
                    Cookies = request.Cookies,
                    HostName = request.UserHostName,
                    Local = request.IsLocal,
                    Referer = request.UrlReferrer,
                    RemoteIP = request.RemoteEndPoint,
                    URL = request.RawUrl,
                    UserAgent = request.UserAgent,
                    Method = request.HttpMethod
                };
                string output = Tools.FireEvent(OnDataReceiveEvent, this, eventArgs);

                if (output == "NO_EXECUTE")
                    return;
                if (buffer.Length > 1 && Tools.FireEvent(OnDataStaticSentEvent, this, eventArgs))
                {
                    WriteToRemote(request, response, buffer);
                    return;
                }
                buffer = Encoding.UTF8.GetBytes(output);
                WriteToRemote(request, response, buffer);
            }
        }

        internal void WriteToRemote(HttpListenerRequest request, HttpListenerResponse response, byte[] buffer)
        {
            if (request.HttpMethod == "HEAD")
                return;
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = buffer.Length;
            using (Stream dataSent = response.OutputStream)
            {
                dataSent.Write(buffer, 0, buffer.Length);
            }
        }
        /// <summary>
        /// Stops the server
        /// </summary>
        /// <param name="immediately">If this parameters is true, the server will be down as soon as possible</param>
        /// <returns>Any errors that could have happened</returns>
        public WebkitErrors Stop(bool immediately = false)
        {
            if (!httpServer.IsListening) return WebkitErrors.NotRunning;
            Tools.ConsolePrint("Stopping the server!\n");
            _lock = true;
            fr.Stop();
            Thread.Sleep(1000);
            _lock = false;
            if (immediately)
                httpServer.Abort();
            else
                httpServer.Stop();
            CurrentStatus = CurrentServerStatus.NotListening;
            Tools.ConsolePrint("Server stopped!\n");
            return WebkitErrors.Ok;
        }
    }
}