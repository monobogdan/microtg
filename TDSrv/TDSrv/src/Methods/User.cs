using System;
using System.Collections.Generic;
using System.Text;

namespace TDSrv.Methods
{
    public sealed class Users
    {

        public static string QueryUserInfo(Dictionary<string, string> args)
        {
            if(args.ContainsKey("user_id"))
            {
                long userId = long.Parse(args["user_id"]);
                StringBuilder output = new StringBuilder();

                User user = HttpServer.Instance.Client.QueryUser(userId);
                output.AppendLine("Name=" + user.Name);
                output.AppendLine("ID=" + user.ID);
                output.AppendLine("IsBot=" + user.IsBot);
                output.AppendLine("PhoneNumber=" + user.PhoneNumber);

                return output.ToString();
            }

            return HttpGenericResponse.OK.ToString();
        }
    }
}
