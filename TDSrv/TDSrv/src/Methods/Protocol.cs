using System;
using System.Collections.Generic;
using System.Text;

namespace TDSrv.Methods
{
    public enum CredintialsResponse
    {
        ServerNotReady,
        NotValidKey
    }

    public static class Protocol
    {

        public static string CheckCredentials(Dictionary<string, string> args)
        {
            if(args.ContainsKey("auth_key"))
            {
                if (Session.Instance.AccessKey == null)
                    return CredintialsResponse.ServerNotReady.ToString();

                if (Session.Instance.AccessKey != args["auth_key"])
                    return CredintialsResponse.NotValidKey.ToString();

                return HttpGenericResponse.OK.ToString();
            }

            return HttpGenericResponse.InternalException.ToString();
        }
    }
}
