﻿using BlackOfWorld.Webkit.Models;
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
using System.Collections;
using System.Net.NetworkInformation;

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
        public delegate ResponseMethod OnDataReceive(object sender, DataReceiveArgs args);
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
        private Hashtable respStatus = new Hashtable();
        public HttpServer()
        {
            httpServer = new HttpListener();
            mainFolderPath = Path.Combine(System.AppContext.BaseDirectory, "WebkitLibrary");
            if (!Directory.Exists(mainFolderPath))
                Directory.CreateDirectory(mainFolderPath);
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            OnFirewallBanEvent += (sender, args) => true;
            OnDataStaticSentEvent += (sender, args) => true;
            OnDataReceiveEvent += (sender, args) => new ResponseMethod("<h1>This is an example page</h1>");
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            var ex = (Exception)args.ExceptionObject;
            if (!ex.StackTrace.Split('\n')[0].Substring(6).StartsWith("BlackOfWorld.Webkit")) return;
            Tools.EnablePrint = true;
            Tools.ConsolePrint("Internal unhandled exception happened! Please submit logs and source code to issue tracker so I can fix it.\nMessage: " + ex.Message + "\nStack trace:\n" + ex.StackTrace + "\n\nThis program will terminate.\n");
            if (Debugger.IsAttached) { Debugger.Break(); return; }
            Environment.Exit(1);
        }
        private bool isPortAvalaible(int myPort)
        {
            var unavailablePorts = new List<int>();
            var properties = IPGlobalProperties.GetIPGlobalProperties();

            // Active connections
            var connections = properties.GetActiveTcpConnections();
            unavailablePorts.AddRange(connections.Select(con => con.LocalEndPoint.Port));

            // Active tcp listners
            var endPointsTcp = properties.GetActiveTcpListeners();
            unavailablePorts.AddRange(endPointsTcp.Select(con => con.Port));

            // Active udp listeners
            var endPointsUdp = properties.GetActiveUdpListeners();
            unavailablePorts.AddRange(endPointsUdp.Select(con => con.Port));

            foreach (int p in unavailablePorts)
            {
                if (p == myPort) return false;
            }
            return true;
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
            InitStatusCodes();
            rateLimited = Encoding.UTF8.GetBytes("You are being rate limited! Please wait " + httpServerConfig.RateLimitWaitTime + " seconds. (pro tip: every time you try to refresh the page, the timer will be set to max)");
            string[] blackPrefixes = new[] { "http://*.com", "http://*:", "https://*.com", "https://*:", "http://+.com", "http://+:", "https://+.com", "https://+:" }; // no racism lol, also the only way to prevent any false detection
            try
            {
                foreach (var _prefix in httpServerConfig.Prefixes)
                {
                    var prefix = _prefix;
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
                    var port = new Uri(prefix, UriKind.Absolute).Port;
                    if (!isPortAvalaible(port))
                    {
                        Tools.ConsolePrint($"Oops! Port {port} is unavailable for prefix \"{prefix}\"!\n");
                        continue;
                    }
                    if (!prefix.EndsWith("/"))
                    {
                        prefix += "/";
                        Tools.ConsolePrint($"Prefixes must end with '/'. Fixing it \"{prefix}\" for you! <3\n"); ;
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
        private void InitStatusCodes()
        {
            //should never be used, but for the same of completion, whatever
            respStatus.Add(100, "Continue");
            respStatus.Add(101, "Switching Protocols");


            respStatus.Add(200, "OK");
            respStatus.Add(201, "Created");
            respStatus.Add(202, "Accepted");
            respStatus.Add(203, "Non-Authoritative Information");
            respStatus.Add(204, "No Content");
            respStatus.Add(205, "Reset Content");
            respStatus.Add(206, "Partial Content");

            respStatus.Add(300, "Multiple Choices");
            respStatus.Add(301, "Moved Permanently");
            respStatus.Add(302, "Found");
            respStatus.Add(303, "See other");
            respStatus.Add(304, "Not Modified");
            respStatus.Add(305, "Use Proxy");
            respStatus.Add(307, "Temporary Redirect");
            respStatus.Add(308, "Permanent Redirect");

            respStatus.Add(400, "Bad Request");
            respStatus.Add(401, "Unauthorized");
            respStatus.Add(402, "Payment Required");
            respStatus.Add(403, "Forbidden");
            respStatus.Add(404, "Not Found");
            respStatus.Add(405, "Method Not Allowed");
            respStatus.Add(406, "Not Acceptable");
            respStatus.Add(407, "Proxy Authentication Required");
            respStatus.Add(408, "Request Timeout");
            respStatus.Add(409, "Conflict");
            respStatus.Add(410, "Gone");
            respStatus.Add(411, "Length Required");
            respStatus.Add(412, "Precondition Failed");
            respStatus.Add(413, "Request Entity Too Large");
            respStatus.Add(414, "Request-URI Too Long");
            respStatus.Add(415, "Unsupported Media Type");
            respStatus.Add(416, "Requested Range Not Satisfiable");
            respStatus.Add(417, "Expectation Failed");
            respStatus.Add(418, "I'm a teapot");
            respStatus.Add(420, "Enhance Your Calm");
            respStatus.Add(426, "Upgrade Required");
            respStatus.Add(428, "Precondition Required");
            respStatus.Add(429, "Too Many Requests");
            respStatus.Add(431, "Request Header Fields Too Large");

            respStatus.Add(500, "Internal Server Error");
            respStatus.Add(501, "Not Implemented");
            respStatus.Add(502, "Bad Gateway");
            respStatus.Add(503, "Service Unavailable");
            respStatus.Add(504, "Gateway Timeout");
            respStatus.Add(505, "HTTP Version Not Supported");
            respStatus.Add(506, "Variant Also Negotiates");
            respStatus.Add(510, "Not Extended");
            respStatus.Add(511, "Network Authentication Required");
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

                response.Headers["Server"] = httpServerConfig.ServerHeader;

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
                    Method = request.HttpMethod,
                    Headers = request.Headers.AllKeys
                };
                ResponseMethod output = Tools.FireEvent(OnDataReceiveEvent, this, eventArgs);

                if (output.cancelExecution && output.Status != 200)
                {
                    
                    string errorString = $"<html><head><title>{output.Status} {respStatus[output.Status]}</title></head><body bgcolor=\"white\"><center><h1>{output.Status} {respStatus[output.Status]}</h1></center><hr><center>nginx/1.10.3</center></body></html>";
                    return;
                }
                if (output.cancelExecution)
                    return;
                if (buffer.Length > 1 && Tools.FireEvent(OnDataStaticSentEvent, this, eventArgs))
                {
                    WriteToRemote(request, response, buffer);
                    return;
                }
                buffer = Encoding.UTF8.GetBytes(output.response);
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
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            Tools.ConsolePrint("Server stopped!\n");
            return WebkitErrors.Ok;
        }
    }
}