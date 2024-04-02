using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;

namespace TDSrv
{
    class Program
    {
        static void Main(string[] args)
        {
            HttpServer.Instance.CheckAuthorizationState();
            HttpServer.Instance.Start();
        }
    }
}
