using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace BlackOfWorld.Webkit.Toolkit
{
    internal class RateLimit
    {
        int max = 0;
        int time = 0;
        int amount = 0;
        internal MemoryCache mCache = new MemoryCache(new MemoryCacheOptions());
        public RateLimit(int max, int time, int amount)
        {
            this.max = max;
            this.time = time;
            this.amount = amount;
        }
        public bool IsAllowed(IPAddress ip)
        {
            if (max <= 0 || time <= 0 || amount <= 0)
                return true;
            mCache.TryGetValue(ip, out int packets);
            if (packets > max) { Tools.ConsolePrint($"\"{ip.ToString()}\" got rate limited!"); packets = -1; }
            if (packets == -1) { mCache.Set(ip, -1, TimeSpan.FromSeconds(time)); return false; }
            packets = packets + 1;
            mCache.Set(ip, packets, TimeSpan.FromSeconds(amount));
            return true;
        }
    }
}
