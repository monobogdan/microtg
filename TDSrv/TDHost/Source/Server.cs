using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;

namespace TDHost
{
    public enum HostErrors
    {
        MethodRequired,
        NotValidError,
        InternalException
    }

    public sealed class HostServer
    {
        private const int NetworkStateSampleRate = 8;
        private HttpListener listener;

        public HostServer()
        {
            Log.WriteLine("Initializing server...");

            listener = new HttpListener();
            listener.Prefixes.Add("http://127.0.0.1:8080/");
            listener.IgnoreWriteExceptions = true;
        }

        private void RefuseWithError(HttpListenerContext ctx, HostErrors error)
        {
            ctx.Response.ContentType = "text/html";
            ctx.Response.KeepAlive = false;
            ctx.Response.StatusCode = 200;

            StreamWriter writer = new StreamWriter(ctx.Response.OutputStream);
            writer.WriteLine(error.ToString());
            writer.Flush();
            ctx.Response.Close();
        }

        private void HandleRequest(IAsyncResult res)
        {
            HttpListenerContext ctx = null;

            try
            {
                ctx = listener.EndGetContext(res);
                string method = ctx.Request.Url.LocalPath;

                if(method == "/")
                {
                    RefuseWithError(ctx, HostErrors.MethodRequired);

                    return;
                }

                WorkerInstance worker = new WorkerInstance();
                worker.Invoke();
            }
            catch (WebException e)
            {
                Log.WriteLine("WebException: {0}", e.Message);

                if (ctx != null)
                    ctx.Response.Close();
            }
        }

        public void Start()
        {
            listener.Start();
            Log.WriteLine("Server successfuly started. IsListening: {0}", listener.IsListening);

            while(listener.IsListening)
            {
                listener.BeginGetContext(HandleRequest, null);

                System.Threading.Thread.Sleep(1000 / NetworkStateSampleRate);
            }
        }
    }
}
