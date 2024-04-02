using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using System.Net;
using TDSrv.Methods;

namespace TDSrv
{
    public enum HttpGenericResponse
    {
        OK,
        MethodRequired,
        UnknownMethod,
        InternalException
    }

    public delegate string HttpMethodHandler(Dictionary<string, string> queryString);

    public sealed class HttpServer
    {
        private static HttpServer instance;

        public static HttpServer Instance
        {
            get
            {
                if (instance == null)
                    instance = new HttpServer();

                return instance;
            }
        }

        public SyncClient Client
        {
            get;
            private set;
        }

        private HttpListener listener;
        private List<HttpMethodHandler> methods;
        private ScheduledRestart restartManager;

        private void AddMethod(HttpMethodHandler info)
        {
            if(info != null)
            {
                methods.Add(info);
                Console.WriteLine("Registered method: {0}", info.Method.Name);
            }
        }

        private void PrepareMethods()
        {
            AddMethod(Chats.QueryChats);
            AddMethod(Chats.QueryMessages);
            AddMethod(Chats.SendMessage);
            AddMethod(Users.QueryUserInfo);
            AddMethod(Protocol.CheckCredentials);
        }

        private void PrepareState()
        {
            // We should fetch dialog list due to TDLib nature of preloading-everything
            Client.QueryChats(50);
        }

        public HttpServer()
        {
            listener = new HttpListener();
            listener.Prefixes.Add("http://+:13377/");

            Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            methods = new List<HttpMethodHandler>();
            PrepareMethods();
        }

        private void SendResponse(string str, HttpListenerContext ctx, bool useASCIIEncoding)
        {
            Encoding enc = Encoding.UTF8;
            str += "\n";
            byte[] buf = enc.GetBytes(str);
            ctx.Response.ContentType = "text/plain; charset=UTF-8";
            ctx.Response.ContentLength64 = buf.Length;

            if (str != null)
                ctx.Response.OutputStream.Write(buf);
            
            ctx.Response.Close();
        }

        private void SendFileResponse(string mimeType, string fileName, HttpListenerContext ctx)
        {
            try
            {
                using (Stream strm = File.OpenRead(fileName))
                {
                    ctx.Response.ContentType = mimeType;
                    ctx.Response.ContentLength64 = strm.Length;
                    strm.CopyTo(ctx.Response.OutputStream);

                    ctx.Response.Close();
                }
            }
            catch (IOException e)
            {
                SendResponse(HttpGenericResponse.InternalException.ToString(), ctx, true);
            }
        }

        private void HandleRequest(HttpListenerContext ctx)
        {
            string method = ctx.Request.Url.LocalPath.Substring(1).ToLower();

            if(method.StartsWith("photos/"))
            {
                // Process photo request
                string[] url = ctx.Request.Url.LocalPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

                if(url.Length != 2)
                {
                    SendResponse(HttpGenericResponse.InternalException.ToString(), ctx, true);

                    return;
                }

                string path = string.Format("tdlib/profile_photos/{0}.jpg", url[1]);

                if(File.Exists(string.Format(path)))
                {
                    SendFileResponse("image/jpeg", path, ctx);
                }
                else
                {
                    SendResponse(HttpGenericResponse.InternalException.ToString(), ctx, true);
                }
            }
            else
            {
                if (method.Length < 0)
                {
                    SendResponse(HttpGenericResponse.MethodRequired.ToString(), ctx, true);
                    return;
                }

                foreach (HttpMethodHandler handler in methods)
                {
                    if (method == handler.Method.Name.ToLower())
                    {
                        string result = "";
                        bool useASCII = false;

                        if (ctx.Request.Url.Query.Length > 0)
                        {
                            string[] args = ctx.Request.Url.Query.Substring(1).Split('&', StringSplitOptions.RemoveEmptyEntries);
                            Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();

                            foreach (string arg in args)
                            {
                                if (arg.IndexOf('=') >= 0)
                                    keyValuePairs.Add(arg.Substring(0, arg.IndexOf('=')), arg.Substring(arg.IndexOf('=') + 1));
                                else
                                    keyValuePairs.Add(arg.Substring(0, arg.IndexOf('=')), "");
                            }

                            useASCII = keyValuePairs.ContainsKey("ascii");

                            if (!keyValuePairs.ContainsKey("auth_key"))
                            {
                                SendResponse(CredintialsResponse.NotValidKey.ToString(), ctx, true);

                                return;
                            }

                            result = handler(keyValuePairs);

                            if (result == null || result.Length < 1)
                            {
                                Console.WriteLine("Suspicious <null> result from {0}", handler.Method.Name);
                            }
                        }

                        SendResponse(result, ctx, useASCII);

                        return;
                    }
                }

                SendResponse(HttpGenericResponse.UnknownMethod.ToString(), ctx, true);
            }
        }

        public void CheckAuthorizationState()
        {
            Client = new SyncClient("test");
            Client.Start();
            Client.WaitUntilReady();

            PrepareState();
        }

        public void Start()
        {
            listener.Start();

            while(listener.IsListening)
            {
                HandleRequest(listener.GetContext());
            }
        }
    }
}
