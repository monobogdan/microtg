using System;
using System.Collections.Generic;
using System.Text;

namespace TDSrv.Methods
{
    public static class Chats
    {

        private static string EscapeNewLines(string str)
        {
            return str.Replace("\n", "<br>");
        }

        private static string LimitPreviewTextLength(string text)
        {
            if (text.Length > 24)
                return text.Substring(0, 24) + "...";

            return text;
        }

        public static string QueryChats(Dictionary<string, string> args)
        {
            if(args.ContainsKey("count"))
            {
                bool encoded = !args.ContainsKey("notEncoded");
                int count = int.Parse(args["count"]);
                StringBuilder ret = new StringBuilder();

                List<Chat> chats = HttpServer.Instance.Client.QueryChats(count);
                ret.AppendLine(string.Format("Count={0}", chats.Count));

                foreach(Chat chat in chats)
                {
                    string text = LimitPreviewTextLength(chat.LastMessageText);

                    ret.AppendLine("Begin");
                    ret.AppendLine("ID=" + chat.ID);
                    ret.AppendLine("Date=" + chat.LastMessageDate);
                    ret.AppendLine("Name=" + chat.Name);
                    ret.AppendLine("Text=" + (encoded ? Uri.EscapeDataString(text) : EscapeNewLines(text)));
                    ret.AppendLine("MsgId=" + chat.LastMessageID);
                    ret.AppendLine("Photo=" + chat.Photo);
                    ret.AppendLine("End");
                }

                return ret.ToString();
            }

            return HttpGenericResponse.InternalException.ToString();
        }

        public static string QueryMessages(Dictionary<string, string> args)
        {
            if(args.ContainsKey("chat_id") && args.ContainsKey("count") && args.ContainsKey("last_message_id"))
            {
                bool encoded = !args.ContainsKey("notEncoded");
                long chatId = long.Parse(args["chat_id"]);
                long lastMsgId = long.Parse(args["last_message_id"]);
                int count = int.Parse(args["count"]);

                StringBuilder ret = new StringBuilder();
                List<Message> messages = HttpServer.Instance.Client.QueryMessagesInChat(chatId, lastMsgId, count);
                ret.AppendLine(string.Format("Count={0}", messages.Count));

                foreach(Message message in messages)
                {
                    string text = message.Text != null ? message.Text : "";

                    ret.AppendLine("Begin");
                    ret.AppendLine("ID=" + message.ID);
                    ret.AppendLine("Date=" + message.Date);
                    ret.AppendLine("Sender=" + message.Sender);
                    ret.AppendLine("Text=" + (encoded ? Uri.EscapeDataString(text) : EscapeNewLines(text)));
                    ret.AppendLine("End");
                }

                return ret.ToString();
            }

            return HttpGenericResponse.InternalException.ToString();
        }

        public static string SendMessage(Dictionary<string, string> args)
        {
            if (args.ContainsKey("chat_id") && args.ContainsKey("reply_to") && args.ContainsKey("text"))
            {
                long chatId = long.Parse(args["chat_id"]);
                long replyTo = long.Parse(args["reply_to"]);
                string text = Uri.UnescapeDataString(args["text"].Replace('+', ' '));

                HttpServer.Instance.Client.SendTextMessage(chatId, replyTo, text);

                return HttpGenericResponse.OK.ToString();
            }

            return HttpGenericResponse.InternalException.ToString();
        }
    }
}
