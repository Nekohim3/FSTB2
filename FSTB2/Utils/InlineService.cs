using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using FSTB2.Models;
using Microsoft.AspNetCore.Components.Forms;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FSTB2.Utils
{
    public enum CallbackData
    {
        None,

        MainMenu,

        GetInfoAboutTickets,
        MyAlerts,

        AddAlert,
        UseWknd,
        AllWknd,
        DelAlert,
        DelAllAlerts,






        AddAlertDate,
        DelAlertDate,

    }

    public static class InlineService
    {
        public static string GetDayOfWeek(DateTime date)
        {
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Sunday:
                    return "ВС";
                case DayOfWeek.Monday:
                    return "ПН";
                case DayOfWeek.Tuesday:
                    return "ВТ";
                case DayOfWeek.Wednesday:
                    return "СР";
                case DayOfWeek.Thursday:
                    return "ЧТ";
                case DayOfWeek.Friday:
                    return "ПТ";
                case DayOfWeek.Saturday:
                    return "СБ";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        public static string GetDayWithDOW(DateTime date) => $"[{GetDayOfWeek(date)}] {date.Day}.{date.Month}.{date.Year - 2000}";

        public static InlineKeyboardMarkup GetMainMenu()
        {
            return new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
                                            {
                                                GetKeyboardButtonWithCallbackLine(("Вывести инфу о доступных билетах", CallbackData.GetInfoAboutTickets)),
                                                GetKeyboardButtonWithCallbackLine(("Мои оповещения", CallbackData.MyAlerts))
                                            });
        }

        public static InlineKeyboardMarkup GetTickets()
        {
            var ik = new List<List<InlineKeyboardButton>>();
            var tickets        = FSService.GetTickets();

            if (tickets != null)
            {
                foreach (var x in tickets.GroupBy(_ => _.Date.Date))
                {
                    ik.Add(new List<InlineKeyboardButton> { InlineKeyboardButton.WithUrl(x.Key.ToShortDateString(), $"https://widget.kassir.ru/?type=E&key=0d043285-33ff-bbbb-d1f0-4d379a98d494&domain=spb.kassir.ru&id={x.FirstOrDefault().Id}") });
                    ik.Add(x.Select(c => InlineKeyboardButton.WithUrl(c.Date.ToShortTimeString(),                   $"https://widget.kassir.ru/?type=E&key=0d043285-33ff-bbbb-d1f0-4d379a98d494&domain=spb.kassir.ru&id={c.Id}")).ToList());
                }
            }

            ik.Add(GetKeyboardButtonWithCallbackLine(("Вернуться в меню", CallbackData.MainMenu)));
            return new InlineKeyboardMarkup(ik);
        }

        public static InlineKeyboardMarkup GetAlerts(ChatWithUser user)
        {
            var ik = new List<List<InlineKeyboardButton>>();
            for (var i = 0; i < 7; i++)
            {
                if (i * g.MaxColumns >= user.AlarmDays.Count)
                    break;
                ik.Add(GetKeyboardButtonWithCallbackLine(user.AlarmDays.Skip(i * g.MaxColumns).Take(g.MaxColumns).Select(_ => (GetDayWithDOW(_), CallbackData.None)).ToArray()));
            }

            if(user.EnableWeekendAlarm)
                ik.Add(GetKeyboardButtonWithCallbackLine(("Уведомление о билетах на выходные включено", CallbackData.None)));
            if(ik.Count > 0)
                ik.Add(GetKeyboardButtonWithCallbackLine(("   ", CallbackData.None)));

            ik.Add(GetKeyboardButtonWithCallbackLine(("Добавить оповещение", CallbackData.AddAlert)));
            
            ik.Add(GetKeyboardButtonWithCallbackLine((user.EnableWeekendAlarm ? "Отменить уведомления о всех билетах на выходные" : "Уведомлять о всех билетах на выходные", CallbackData.UseWknd)));

            if (user.AlarmDays.Count > 0)
            {
                ik.Add(GetKeyboardButtonWithCallbackLine(("Удалить оповещение", CallbackData.DelAlert)));
                ik.Add(GetKeyboardButtonWithCallbackLine(("Удалить все оповещения", CallbackData.DelAllAlerts)));
            }

            ik.Add(GetKeyboardButtonWithCallbackLine(("Назад", CallbackData.MainMenu)));
            return new InlineKeyboardMarkup(ik);
        }

        public static InlineKeyboardMarkup GetDaysForNewAlert()
        {
            var ik = new List<List<InlineKeyboardButton>>();

            var days = Enumerable.Range(0, 16).Select(_ => DateTime.Now.AddDays(_));
            for (var i = 0; i < 4; i++)
                ik.Add(GetKeyboardButtonWithCallbackLine(days.Skip(i * g.MaxColumns).Take(g.MaxColumns).Select(_ => (GetDayWithDOW(_), $"AddAlertDate-{_.ToShortDateString()}")).ToArray()));

            ik.Add(GetKeyboardButtonWithCallbackLine(("Назад", CallbackData.MainMenu)));

            return new InlineKeyboardMarkup(ik);
        }

        public static InlineKeyboardMarkup GetAlertsForDelete(ChatWithUser user)
        {
            var ik = new List<List<InlineKeyboardButton>>();

            for (var i = 0; i < 7; i++)
            {
                if (i * g.MaxColumns >= user.AlarmDays.Count)
                    break;
                ik.Add(GetKeyboardButtonWithCallbackLine(user.AlarmDays.Skip(i * g.MaxColumns).Take(g.MaxColumns).Select(_ => (GetDayWithDOW(_), $"DelAlertDate-{_.ToShortDateString()}")).ToArray()));
            }
            ik.Add(GetKeyboardButtonWithCallbackLine(("Назад", CallbackData.MyAlerts)));

            return new InlineKeyboardMarkup(ik);
        }

        private static InlineKeyboardButton GetKeyboardButtonWithCallback(string text, CallbackData callbackData) => InlineKeyboardButton.WithCallbackData(text, callbackData.ToString());
        private static InlineKeyboardButton GetKeyboardButtonWithCallback(string text, string callbackData) => InlineKeyboardButton.WithCallbackData(text, callbackData);
        private static List<InlineKeyboardButton> GetKeyboardButtonWithCallbackLine(params (string text, CallbackData data)[] data) => data.Select(x => GetKeyboardButtonWithCallback(x.text, x.data)).ToList();
        private static List<InlineKeyboardButton> GetKeyboardButtonWithCallbackLine(params (string text, string data)[] data) => data.Select(x => GetKeyboardButtonWithCallback(x.text, x.data)).ToList();

        public static async Task EditLastMessageMessage(this ChatWithUser chat, string caption, InlineKeyboardMarkup km)
        {
            await g.TLC.EditMessageText(chat.Chat, chat.LastMessageId, caption);
            await g.TLC.EditMessageReplyMarkup(chat.Chat, chat.LastMessageId, km);
        }
    }
}
