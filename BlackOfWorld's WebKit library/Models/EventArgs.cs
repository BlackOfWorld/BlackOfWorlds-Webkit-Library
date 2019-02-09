using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace BlackOfWorld.Webkit.Models
{
    public class FirewallBanArgs : EventArgs
    {
        public IPAddress Ip;
        public DateTime banDate;
    }
    public class DataReceiveArgs : EventArgs
    {
        public IPEndPoint RemoteIP;
        public string UserAgent;
        public Uri Referer;
        public string URL;
        public string ContentType;
        public CookieCollection Cookies;
        public bool Local;
        public string HostName;
        public string Method;
    }
}
