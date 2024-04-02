using System;
using System.Collections.Generic;
using System.Text;

namespace TDHost
{
    public sealed class Log
    {

        public static void WriteLine(string fmt, params object[] args)
        {
            string str = string.Format(fmt, args);
            Console.WriteLine(str);
        }
    }
}
