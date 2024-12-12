using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Telegram.Bot.Types;

namespace FSTB2.Models
{
    [JsonObject]
    public class FsReply
    {
        public Kit Kit { get; set; }
    }

    [JsonObject]
    public class Kit
    {
        public List<EventBucket> eventBuckets { get; set; }
    }

    [JsonObject]
    public class EventBucket
    {
        public List<Event> events { get; set; }
    }

    [JsonObject]
    public class Event
    {
        public int Id { get; set; }
        public DateTimeOffset BeginsAt { get; set; }
        public DateTime Date
        {
            get
            {
                var beginsAt = BeginsAt;
                var dateTime = beginsAt.DateTime;
                beginsAt = BeginsAt;
                var offset = beginsAt.Offset;
                return dateTime - offset;
            }
        }
    }
    
    [JsonObject]
    public class ChatWithUser
    {
        [JsonIgnore] public int    LastMessageId { get; set; }
        private             ChatId _chat;
        [JsonIgnore]
        public ChatId Chat
        {
            get => _chat ??= new ChatId(UserId);
            set => _chat = value;
        }
        public long           UserId               { get; set; }
        public string         Name                 { get; set; }
        public string?        LName                { get; set; }
        public string?        UName                { get; set; }
        public List<DateTime> AlarmDays            { get; set; } = new();
        public DateTime       LastNotificationDate { get; set; }
        public bool           EnableWeekendAlarm   { get; set; }
        
        public ChatWithUser()
        {

        }

        public ChatWithUser(User user)
        {
            UserId = user.Id;
            Name   = user.FirstName;
            LName  = user.LastName;
            UName  = user.Username;
            Chat   = new ChatId(UserId);
        }
    }
}
