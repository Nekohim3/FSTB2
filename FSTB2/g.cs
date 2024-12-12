using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using FSTB2.Models;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FSTB2
{
    public static class g
    {
        public static int                MaxColumns = 4;
        public static Window             MainWindow;
        public static List<ChatWithUser> UserData = new();
        public static TelegramBotClient  TLC;
        public static ChatId             MomChatChat = new(699561154L);

        static g() => TLC = new TelegramBotClient("5240710415:AAHEpsaDb3jCRyOQX2Ju7Xkg4x8tycew7b0");

        public static void Load()
        {
            if (!System.IO.File.Exists("data"))
                return;
            try
            {
                UserData = JsonConvert.DeserializeObject<List<ChatWithUser>>(System.IO.File.ReadAllText("data")) ?? new List<ChatWithUser>();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in Load data!");
            }
        }

        public static void Save() => System.IO.File.WriteAllText("data", JsonConvert.SerializeObject(UserData));

        public static void AddUser(ChatWithUser chat)
        {
            UserData.Add(chat);
            Save();
        }
    }
}
