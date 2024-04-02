using System;

namespace TDHost
{
    class Program
    {
        static void Main(string[] args)
        {
            HostServer server = new HostServer();
            server.Start();
        }
    }
}
