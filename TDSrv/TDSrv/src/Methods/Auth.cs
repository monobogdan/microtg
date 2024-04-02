using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace TDSrv
{
    public enum AuthError
    {
        PhoneNumberMissing,
        AuthCodeMismatch
    }

    public sealed class MethodAuth
    {

        /*public static string BeginAuthSequence(Dictionary<string, string> args)
        {
            if(args.ContainsKey("phone"))
            {
                SyncClient client = new SyncClient("1234");
                client.BeginAuthorization(args["phone"]);
                client.Start();

                client.WaitUntilReady();

                return HttpGenericResponse.OK.ToString();
            }

            return AuthError.PhoneNumberMissing.ToString();
        }

        public static string EndAuthSequence(Dictionary<string, string> args)
        {
            if (args.ContainsKey("code"))
            {
                SyncClient client = new SyncClient("1234");
                client.BeginAuthorization(args["code"]);
                client.EndAuthorization(args["code"]);
                client.Start();

                client.WaitUntilReady();

                return HttpGenericResponse.OK.ToString();
            }

            return AuthError.AuthCodeMismatch.ToString();
        }*/
    }
}
