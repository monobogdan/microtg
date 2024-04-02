using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Threading;
using System.IO;

namespace TDSrv
{

    public class Message
    {
        public long Sender;
        public string Text;
        public long Date;
        public long ID;
    }

    public class Chat
    {
        public long ID;
        public string Name;
        public bool IsUser;
        public string Photo;

        public long LastMessageID;
        public string LastMessageText;
        public long LastMessageDate;
    }

    public class User
    {
        public long ID;
        public string Name;
        public string UserName;
        public string PhoneNumber;
        public bool IsBot;
    }

    /// <summary>
    /// Provides synchronous implementation for TDLib callback-based approach.
    /// </summary>
    public sealed class SyncClient
    {
        public const int SyncDefaultTimeout = 3500;
        public const int APIId = 24215120;
        public const string APIHash = "636490ea2d0b18c5665dbc87d6a3fb10";

        public string SessionID;

        public string PhoneNumber
        {
            get;
            private set;
        }

        public string AuthCode
        {
            get;
            private set;
        }

        private Client Client;
        private int InstanceID;

        // Internal auth state
        private string phoneNumber;
        private string code;
        private bool isReady;
        private EventWaitHandle waitHandle;

        private bool isOperationInProgress;

        // Chat functions state
        private List<Chat> chats;
        private int requestedChatCount;
        private List<Message> messages;
        private int requestMessageCount;
        private User user;
        private Dictionary<long, bool> chatPreloadFlags;

        private void OnAuthState(string raw, JObject obj)
        {
            JObject authState = (JObject)obj["authorization_state"];
            string type = authState["@type"].ToString();

            if (type == "authorizationStateWaitTdlibParameters")
            {
                Console.WriteLine("Preparing TDLib parameters...");
                NativeInterface.Send(InstanceID,
                    Utils.Format("{" +
                        "\"@type\": \"setTdlibParameters\", " +
                        "\"database_directory\": \"{2}\", " +
                        "\"api_id\": {0}, " +
                        "\"api_hash\": \"{1}\", " +
                        "\"use_chat_info_database\": true," +
                        "\"use_file_database\": true," +
                        "\"use_message_database\": true," +
                        "\"system_language_code\": \"en\", " +
                        "\"device_model\": \"Phone\", " +
                        "\"application_version\": \"1.0\" " +
                   "}", APIId, APIHash, Session.TDLibDirectory));

                string json = Utils.Format("{ \"@type\": \"autoDownloadSettings\", \"is_auto_download_enabled\": true, \"max_photo_file_size\": 1024000000, \"max_video_file_size\": 0, \"max_other_file_size\": 0 }");
                NativeInterface.Send(Client.InstanceId, Utils.Format("{\"@type\": \"setAutoDownloadSettings\", \"settings\": {0} }", json));
            }

            if (type == "authorizationStateWaitPhoneNumber")
            {
                Console.WriteLine("Enter phone number:");
                PhoneNumber = Console.ReadLine();
                NativeInterface.Send(InstanceID, Utils.Format("{\"@type\": \"setAuthenticationPhoneNumber\", \"phone_number\": \"{0}\" }", PhoneNumber));
            }
            
            if(type == "authorizationStateWaitCode")
            {
                Console.WriteLine("Enter code (if you see this multiple times, then you typed wrong code):");
                AuthCode = Console.ReadLine();
                NativeInterface.Send(InstanceID, Utils.Format("{\"@type\": \"checkAuthenticationCode\", \"code\": \"{0}\" }", AuthCode));
            }

            if(type == "authorizationStateReady")
            {
                // If we've just logged in, save session state
                if (PhoneNumber != null)
                    Session.Instance.WriteKey(PhoneNumber, AuthCode);
                else
                    Session.Instance.LoadState();

                Console.WriteLine("Authorized");
                Console.WriteLine("Your session key is: {0}. It will persist them same until next authorization. Use it when authorizing in client app.", Session.Instance.AccessKey);

                waitHandle.Set();
            }
        }

        /* Event process structure: getChats -> OnChatListEvent -> getChat per each chat in response -> OnChatEvent, which fills internal list and when last chat returned, it will signal main thread */    
        private void OnChatListEvent(string raw, JObject obj)
        {
            int count = (int)obj["total_count"];
            JArray chat_ids = (JArray)obj["chat_ids"];

            foreach (JValue o in chat_ids)
            {
                long id = (long)o;

                NativeInterface.Send(Client.InstanceId, Utils.Format("{\"@type\": \"getChat\", \"chat_id\": {0} }", id));
            }
        }

        private void OnChatEvent(string raw, JObject obj)
        {
            Chat chat = new Chat();
            chat.ID = (long)obj["id"];
            chat.IsUser = (string)obj["type"]["@type"] == "chatTypePrivate";
            chat.Name = (string)obj["title"];

            if(obj.ContainsKey("photo"))
            {
                long fileId = (long)obj["photo"]["small"]["id"];

                NativeInterface.Send(InstanceID, Utils.Format("{\"@type\": \"downloadFile\", \"file_id\": {0}, \"priority\": 1, \"offset\": 0, \"synchronous\": true }", fileId));

                string localPath = (string)obj["photo"]["small"]["local"]["path"];
                chat.Photo = Path.GetFileNameWithoutExtension(localPath);
            }

            if (obj.ContainsKey("last_message") && (string)obj["last_message"]["@type"] == "message")
            {
                JObject lastMessage = (JObject)obj["last_message"];
                
                if((string)lastMessage["content"]["@type"] == "messageText")
                {
                    chat.LastMessageText = (string)lastMessage["content"]["text"]["text"];
                }
                else
                {
                    chat.LastMessageText = "NotSupportedYet";
                }

                chat.LastMessageID = (long)lastMessage["id"];
                chat.LastMessageDate = (long)lastMessage["date"];
            }
            else
            {
                chat.LastMessageText = "Unknown";
                chat.LastMessageDate = 0;
            }

            chats.Add(chat);

            requestedChatCount--;
            if (requestedChatCount == 0)
                waitHandle.Set();
        }

        private void OnUserEvent(string raw, JObject obj)
        {
            user = new User();
            user.ID = (long)obj["id"];
            user.Name = (string)obj["first_name"] + ' ' + (string)obj["last_name"];
            user.PhoneNumber = (string)obj["phone_number"];
            user.IsBot = obj["type"]["@type"].ToString() == "userTypeBot";

            waitHandle.Set();
        }

        private void OnMessagesEvent(string raw, JObject obj)
        {
            if(obj.ContainsKey("messages"))
            {
                JArray arr = (JArray)obj["messages"];

                foreach(JObject tok in arr)
                {
                    Message msg = new Message();

                    if ((string)tok["@type"] == "message")
                    {
                        msg.ID = (long)tok["id"];
                        msg.Date = (long)tok["date"];

                        if(tok.ContainsKey("content"))
                        {
                            JObject content = (JObject)tok["content"];

                            if((string)content["@type"] == "messageText")
                            {
                                msg.Text = (string)content["text"]["text"];
                            }
                        }
                    }

                    messages.Add(msg);
                }


                waitHandle.Set();
            }
        }

        private void OnError(string raw, JObject obj)
        {
            Console.WriteLine(raw);

            waitHandle.Set();
        }

        public SyncClient(string sessId)
        {
            SessionID = sessId;

            waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

            chats = new List<Chat>();
            messages = new List<Message>();

            Client = new Client();
            InstanceID = Client.InstanceId;

            chatPreloadFlags = new Dictionary<long, bool>();

            Client.AttachEventHandler("updateAuthorizationState", OnAuthState);
            Client.AttachEventHandler("chats", OnChatListEvent);
            Client.AttachEventHandler("chat", OnChatEvent);
            Client.AttachEventHandler("error", OnError);
            Client.AttachEventHandler("messages", OnMessagesEvent);
            Client.AttachEventHandler("user", OnUserEvent);
        }

        public void BeginAuthorization(string phoneNumber)
        {
            this.phoneNumber = phoneNumber;
        }

        public void EndAuthorization(string code)
        {
            this.code = code;
        }

        public void Start()
        {
            Client.Start();
        }

        public List<Chat> QueryChats(int count)
        {
            chats.Clear();
            
            requestedChatCount = count;
            string json = Utils.Format("{\"@type\": \"getChats\", \"limit\": {0} }", count);
            NativeInterface.Send(InstanceID, json);

            waitHandle.WaitOne();
            return chats;
        }

        public List<Message> QueryMessagesInChat(long chatId, long lastMessage, int count)
        {
            messages.Clear();

            requestMessageCount = count;
            string json = Utils.Format("{\"@type\": \"getChatHistory\", \"chat_id\": \"{0}\", \"from_message_id\": {1}, \"limit\": {2} }", chatId, lastMessage, count);
            NativeInterface.Send(InstanceID, json);

            waitHandle.WaitOne();

            if(!chatPreloadFlags.ContainsKey(chatId))
            {
                chatPreloadFlags.Add(chatId, true);

                return QueryMessagesInChat(chatId, lastMessage, count);
            }
            
            return messages;
        }

        public User QueryUser(long userId)
        {
            string json = Utils.Format("{\"@type\": \"getUser\", \"user_id\": \"{0}\" }", userId);
            NativeInterface.Send(InstanceID, json);

            waitHandle.WaitOne();
            return user;
        }

        public void SendTextMessage(long chatId, long replyTo, string message)
        {
            string formattedMessage = "";

            for(int i = 0; i < message.Length; i++)
            {
                int unicodeMark = (int)message[i];

                formattedMessage += "\\u" +  string.Format("{0:X4}", unicodeMark);
            }
            
            string contentJson = Utils.Format("{\"@type\": \"inputMessageText\", \"text\": { \"@type\": \"formattedText\", \"text\": \"{0}\"} }", formattedMessage);
            string json = Utils.Format("{\"@type\": \"sendMessage\", \"chat_id\": \"{0}\", \"reply_to_message_id\": \"{1}\", \"input_message_content\": {2} }", chatId, replyTo, contentJson);
            Console.WriteLine(json);
            NativeInterface.Send(InstanceID, json);
        }

        public void WaitUntilReady()
        {
            waitHandle.WaitOne();

            isReady = true;
        }

        public void Close()
        {
            Client.Close();
        }
    }
}
