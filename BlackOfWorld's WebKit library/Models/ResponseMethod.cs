using System;
using System.Collections.Generic;
using System.Text;

namespace BlackOfWorld.Webkit.Models
{
    public class ResponseMethod
    {
        public ResponseMethod(string response)
        {
            this.response = response;
        }
        public bool cancelExecution = false;
        public int Status = 200;
        public string response;
    }
}
