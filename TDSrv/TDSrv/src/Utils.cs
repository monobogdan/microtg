using System;
using System.Collections.Generic;
using System.Text;

namespace TDSrv
{
    public sealed class Utils
    {

        public static string Format(string fmt, params object[] args)
        {
            string ret = fmt;

            for (int i = 0; i < args.Length; i++)
            {
                ret = ret.Replace("{" + i + "}", args[i].ToString());
            }

            return ret;
        }

        public static string UrlEncodingToUTF(string url)
        {
            string ret = "";

            for(int i = 0; i < url.Length; i++)
            {
                if(url[i] == '%' && i + 2 < url.Length)
                {
                    string hex = url[i + 1].ToString() + url[i + 2];

                    try
                    {
                        ret += "\\u" + "00" + hex;
                    }
                    catch (FormatException e)
                    {
                        
                    }

                    i += 2;
                    continue;
                }

                ret += url[i];
            }

            return ret;
        }

        public static void WriteDataset()
        {

        }
    }
}
