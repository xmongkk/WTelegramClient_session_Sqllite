using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TL;

namespace WTelegramClient_session_Sqllite
{
	//t.me/nader5
    static class Program
    {
        static WTelegram.Client Client;
        static User My;
        static Dictionary<long, User> Users = new();
        static Dictionary<long, ChatBase> Chats = new();

		static async Task Main(string[] _)
		{
			var exit = new SemaphoreSlim(0);
			AppDomain.CurrentDomain.ProcessExit += (s, e) => exit.Release(); 
		    var store = new SqliteStore("SQLLiteDatabase.db", "session1");
		
			Client = new WTelegram.Client(what => Config(what), store);
			
			await using (Client)
			{
				Client.OnUpdates += Client_OnUpdates;
				My = await Client.LoginUserIfNeeded();
				Console.WriteLine($"We are logged-in as {My.username ?? My.first_name + " " + My.last_name} (id {My.id})");
				var dialogs = await Client.Messages_GetAllDialogs();
				dialogs.CollectUsersChats(Users, Chats);
				await exit.WaitAsync();
			}
		}
		private static async Task Client_OnUpdates(UpdatesBase updates)
		{
			updates.CollectUsersChats(Users, Chats);
			foreach (var update in updates.UpdateList)
			{
				Console.WriteLine(update.GetType().Name);
				if (update is UpdateNewMessage { message: Message { peer_id: PeerUser { user_id: var user_id } } msg }) // private message
					if (!msg.flags.HasFlag(Message.Flags.out_)) 
						if (Users.TryGetValue(user_id, out var user))
						{
							Console.WriteLine($"New message from {user}: {msg.message}");
							if (msg.message.Equals("Ping", StringComparison.OrdinalIgnoreCase))
								await Client.SendMessageAsync(user, "Pong");
						}
			}
		}
        public static string Config(string what)
        {


            switch (what)
            {
      

                case "api_id": return "api_id"; 
				case "api_hash": return "phone_number";
				case "phone_number": return "+phone_number";
                case "first_name": return "nader";
				case "verification_code": Console.Write("Code: "); return Console.ReadLine();
				case "last_name": return "last_name";
           
                // if sign-up is required
                case "password": return "1426";     // if user has enabled 2FA
                default: return null;                  // let WTelegramClient decide the default config
            }

        }

    }
}
