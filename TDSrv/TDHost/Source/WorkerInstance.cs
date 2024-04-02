using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO.Pipes;

namespace TDHost
{
    public sealed class WorkerInstance
    {
        public const string ExecutableName = "tdsrv.dll";

        public string Parameters
        {
            get; set;
        }

        public string Input
        {
            get; set;
        }

        private Process process;
        private NamedPipeServerStream serverPipe;

        public WorkerInstance()
        {
            serverPipe = new NamedPipeServerStream("ServerPipe", PipeDirection.InOut);
        }

        public string Invoke()
        {
            try
            {

                Process process = new Process();
                process.StartInfo.FileName = "dotnet";
                process.StartInfo.Arguments = string.Format("{0} {1}", ExecutableName, Parameters);
                process.Start();
                process.WaitForExit();

                return process.StandardOutput.ReadToEnd();
            }
            catch (Exception e)
            {
                throw new ArgumentException("Failed to start worker process: {0}", e.Message);
            }
        }
    }
}
