using System.Net;

namespace BlackOfWorld.Webkit.Models
{
    public class ServerConfig
    {
        /// <summary>
        /// This can be used for debug purposes
        /// </summary>
        public bool     EnableConsolePrint       = true;
        public bool     EnforceProtectionPolicy  = true;
        /// <summary>
        /// This will prevent any non-local IPs from connecting
        /// </summary>
        public bool     LocalOnly                = false;
        /// <summary>
        /// This will cache static items to access static items faster in the browser 
        /// </summary>
        public bool     CacheStaticFiles         = true;
        /// <summary>
        /// Minifies the content of static items to make packet size smaller
        /// </summary>
        public bool     MinifyCachedStaticFiles = true;
        /// <summary>
        /// If user passes this limit in your chosen interval, he will be banned (make this zero or less to disable)
        /// </summary>
        public int      FirewallPacketBan = 1000;
        /// <summary>
        /// If user passes your chosen packet limit in this interval, he will be banned (make this zero or less to disable)
        /// </summary>
        public int      FirewallPacketInterval   = 60;
        /// <summary>
        /// If user passes this limit in your chosen interval, he will be rate limited (make this zero or less to disable)
        /// </summary>
        public int      RateLimitPacketAmount    = 30;
        /// <summary>
        /// If user passes this limit in your chosen interval, he will be banned (make this zero or less to disable)
        /// </summary>
        public int      RateLimitSecLimit        = 5;
        /// <summary>
        /// If user got rate limited, his temporary timeout will be this
        /// </summary>
        public int      RateLimitWaitTime        = 20;
        /// <summary>
        /// If you have any static files, put your folder path here to be indexed
        /// </summary>
        public string   StaticFileLocation       = "";
        /// <summary>
        /// Put your prefixes (URLs) here
        /// </summary>
        public string[] Prefixes                 = new string[0];
        /// <summary>
        /// Put hated people here to prevent them to connecting
        /// </summary>
        public string[] BlacklistedIPs           = new string[0];
    }
}
