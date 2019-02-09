using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BlackOfWorld.Webkit.Models;

namespace BlackOfWorld.Webkit.Toolkit
{
    internal class Firewall
    {
        internal MemoryCache mCache = new MemoryCache(new MemoryCacheOptions());
        internal List<IPAddress> firewallBannedIPs = new List<IPAddress>();
        int max = 0;
        private HttpServer.OnFirewallBan banEvent;
        TimeSpan timeSpan;
        
        private CancellationTokenSource cts = new CancellationTokenSource();
        private bool running = false;
        public Firewall(int max, int interval, HttpServer.OnFirewallBan banEvent)
        {
            this.max = max;
            this.banEvent = banEvent;
            timeSpan = interval <= 0 ? TimeSpan.MaxValue : TimeSpan.FromSeconds(interval);
            string[] IPs = Tools.ReadListFromFile("firewallBanIp.WKL");
            foreach (var ip in IPs) firewallBannedIPs.Add(IPAddress.Parse(ip));
            running = true;
            Task.Run(() => SaveList(cts.Token),cts.Token);
        }
        internal void Stop()
        {
            cts.Cancel();
            running = false;
        }
        private Task SaveList(CancellationToken cs)
        {
            while (running)
            {
                cs.WaitHandle.WaitOne(900000);
                Tools.SaveListToFile("firewallBanIp.WKL", firewallBannedIPs.ToArray());
            }
            return Task.CompletedTask;
        }
        internal bool AccessPermitted(IPAddress ip)
        {
            if (max == 0) return true;
            if (firewallBannedIPs.Contains(ip))  return false;
            mCache.TryGetValue(ip, out int packets);
            packets = packets + 1;
            if (packets >= max && Tools.FireEvent(banEvent, this, new FirewallBanArgs() { Ip = ip, banDate = DateTime.Now }))
            {
                Tools.ConsolePrint($"\"{ip.ToString()}\" has been firewall banned for sending too many packets!");
                firewallBannedIPs.Add(ip);
                mCache.Remove(ip);
                return false;
            }
            mCache.Set(ip, packets, timeSpan);
            return true;
        }
    }
}
