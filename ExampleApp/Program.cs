using System;
using BlackOfWorld.Webkit;
using BlackOfWorld.Webkit.Models;
namespace ExampleApp
{
    class Program
    {
        private static HttpServer hServer;

        static void Main(string[] args)
        {
            Console.Title = "An example app of BlackOfWorld's WebKit Library";
            hServer = new HttpServer
            {
                httpServerConfig = new ServerConfig()
                {
                    Prefixes = new[] { "http://127.0.0.1:4819/" },
                    RateLimitSecLimit = 0,
                    RateLimitPacketAmount = 2,
                    RateLimitWaitTime = 60,
                    FirewallPacketBan = 0,
                    FirewallPacketInterval = 0
                }
            };
            hServer.OnDataReceiveEvent += new HttpServer.OnDataReceive(HServer_OnDataReceiveEvent);
            hServer.OnDataStaticSentEvent += new HttpServer.OnDataStaticSent(HServer_OnDataStaticSentEvent);
            hServer.OnFirewallBanEvent += (sender, banArgs) => true;
            hServer.Start();
            for (int i = 0; i < 2; i++)
            {
                Console.ReadLine();
            }
            hServer.Stop(false);
            for (;;)
            {
                Console.ReadLine();
            }
        }

        private static bool HServer_OnDataStaticSentEvent(object sender, DataReceiveArgs args)
        {
            return true;
        }

        private static string HServer_OnDataReceiveEvent(object sender, DataReceiveArgs args)
        {
            Console.WriteLine($"{args.Method}: {args.RemoteIP} -> {args.URL}");
            return "ok boss";
        }
    }
}