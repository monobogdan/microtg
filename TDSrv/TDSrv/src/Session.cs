using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace TDSrv
{
    public sealed class Session
    {
        public static string TDLibDirectory = "tdlib/";
        public static string SessionFile = "sessionkey.txt";

        private static Session session;

        public static Session Instance
        {
            get
            {
                if (session == null)
                    session = new Session();

                return session;
            }
        }

        public string AccessKey
        {
            get;
            private set;
        }
        
        public void LoadState()
        {
            if(AccessKey == null)
                AccessKey = File.ReadAllText(SessionFile);
        }

        public void WriteKey(string phoneNumber, string responseCode)
        {
            MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.ASCII.GetBytes(string.Format("{0}/{1}/{2}", phoneNumber, responseCode, new Random().Next())));
            string hashStr = "";

            for(int i = 0; i < hash.Length; i++)
                hashStr += string.Format(hash[i].ToString("x2"));

            File.WriteAllText(SessionFile, hashStr);
            AccessKey = hashStr;
        }
    }
}
