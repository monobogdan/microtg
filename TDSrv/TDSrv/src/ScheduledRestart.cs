using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using System.Diagnostics;

namespace TDSrv
{
    public sealed class ScheduledRestart
    {
        public int Interval
        {
            get;
            private set;
        }

        private Timer timer;

        public ScheduledRestart(int interval)
        {
            Interval = interval;

            timer = new Timer(Interval * 1000);
            timer.Elapsed += OnElapsed;
        }

        public void Start()
        {
            timer.Start();
        }

        private void OnElapsed(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine("Restarting...");

            Process process = new Process();
            Process currProcess = Process.GetCurrentProcess();
            process.StartInfo.FileName = currProcess.MainModule.FileName;
            process.StartInfo.Arguments = Environment.CommandLine;

            process.Start();
            Environment.Exit(1);
        }
    }
}
