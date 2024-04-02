using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace TDSrv
{
    public delegate void JSONEventHandler(string raw, JObject obj);

    public sealed class Client
    {
        public int InstanceId
        {
            get;
            private set;
        }
        
        private bool workerState;
        private Dictionary<string, JSONEventHandler> handlers;

        public void AttachEventHandler(string ev, JSONEventHandler handler)
        {
            handlers.Add(ev, handler);
        }

        public Client()
        {
            InstanceId = NativeInterface.CreateClientID();

            handlers = new Dictionary<string, JSONEventHandler>();
            NativeInterface.Send(InstanceId, "{\"@type\": \"setLogVerbosityLevel\", \"new_verbosity_level\": 1, \"@extra\": 1.01234 }");
        }
        
        public void Start()
        {
            new Thread(() =>
            {
                while (!workerState)
                {
                    string recv = NativeInterface.Receive(10.0d);

                    if (recv != null)
                    {
                        JObject json = JObject.Parse(recv);

                        string type = json["@type"].ToString();

                        if (!handlers.ContainsKey(type))
                        {
                            //Console.WriteLine("Unknown event type: {0}", type);
                            continue;
                        }

                        handlers[type](recv, json);
                    }
                }
            }).Start();
        }

        public void Close()
        {
            workerState = true;

            NativeInterface.Send(InstanceId, "{\"@type\": \"close\" }");
            Console.WriteLine("Closed client {0}", InstanceId);
        }
    }
}
