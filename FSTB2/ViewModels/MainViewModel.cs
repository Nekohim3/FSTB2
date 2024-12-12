using System.Collections.Generic;
using ReactiveUI;
using System.Reactive;
using FSTB2.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia.Controls;
using FSTB2.Utils;
using Telegram.Bot;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;

namespace FSTB2.ViewModels;

public class MainViewModel : ViewModelBase
{
    #region Properties

    private DateTime _startTime;
    private int      _lastMsgId;
    private Thread   _botThread;
    private Thread   _botAlertThread;

    public ReactiveCommand<Unit, Unit> StartBotCmd { get; }
    public ReactiveCommand<Unit, Unit> StopBotCmd  { get; }
    public ReactiveCommand<Unit, Unit> Test1Cmd    { get; }

    private bool _statusOnVisible;
    public bool StatusOnVisible
    {
        get => _statusOnVisible;
        set => this.RaiseAndSetIfChanged(ref _statusOnVisible, value, nameof(StatusOnVisible));
    }

    private bool _statusOffVisible;
    public bool StatusOffVisible
    {
        get => _statusOffVisible;
        set => this.RaiseAndSetIfChanged(ref _statusOffVisible, value, nameof(StatusOffVisible));
    }

    private bool _botWork;
    public bool BotWork
    {
        get => _botWork;
        set
        {
            this.RaiseAndSetIfChanged(ref _botWork, value, nameof(BotWork));
            StatusOnVisible  = value;
            StatusOffVisible = !value;
        }
    }

    public List<ChatWithUser> ChatList { get; set; } = new();

    #endregion

    #region Ctor

    public MainViewModel()
    {
        StatusOffVisible = true;
        StatusOnVisible  = false;
        StartBotCmd      = ReactiveCommand.Create(OnStartBot);
        StopBotCmd       = ReactiveCommand.Create(OnStopBot);
        OnStartBot();
    }

    #endregion

    #region Commands

    private void OnStartBot()
    {
        if (BotWork)
            return;
        g.Load();
        BotWork    = true;
        _botThread = new Thread(BotUpdatesThreadFunc);
        _botThread.Start();
        _botAlertThread = new Thread(BotAlertThreadFunc);
        _botAlertThread.Start();
    }

    private void OnStopBot() => BotWork = false;

    private void OnTest1()
    {
    }

    #endregion

    #region Funcs

    

    #endregion
    
    private async void BotUpdatesThreadFunc()
    {
        _startTime = DateTime.Now;
        var updates = await g.TLC.GetUpdates();
        if (updates.Any())
            _lastMsgId = updates.Last().Id;
        while (BotWork)
        {
            try
            {
                foreach (var x in await g.TLC.GetUpdates(_lastMsgId + 1))
                {
                    _lastMsgId = x.Id;
                    if (x.Message?.From != null)
                    {
                        var chat = g.UserData.FirstOrDefault(_ => _.UserId == x.Message.From.Id);
                        if (chat == null)
                        {
                            chat = new ChatWithUser(x.Message.From);
                            g.AddUser(chat);
                        }

                        if (x.Message.Text.ToLower() == "/start")
                        {
                            try
                            {
                                var msg = await g.TLC.SendMessage(chat.Chat, "Меню", replyMarkup: InlineService.GetMainMenu());
                                chat.LastMessageId = msg.MessageId;
                            }
                            catch (Exception e)
                            {
                                Logger.Error(e);
                            }
                        }
                    }

                    if (x.CallbackQuery?.From != null)
                    {
                        var chat = g.UserData.FirstOrDefault(_ => _.UserId == x.CallbackQuery.From.Id);
                        if (chat == null)
                        {
                            chat = new ChatWithUser(x.CallbackQuery.From);
                            g.AddUser(chat);
                        }

                        if (chat.LastMessageId == 0 || chat.LastMessageId != x.CallbackQuery.Message.MessageId)
                        {
                            var msg = await g.TLC.SendMessage(chat.Chat, "Меню", replyMarkup: InlineService.GetMainMenu());
                            chat.LastMessageId = msg.MessageId;
                        }

                        var dataArr = x.CallbackQuery.Data.Split('-');
                        switch (Enum.Parse<CallbackData>(dataArr[0]))
                        {
                            case CallbackData.GetInfoAboutTickets:
                                await chat.EditLastMessageMessage("Вот расписание", InlineService.GetTickets());
                                break;

                            case CallbackData.MainMenu:
                                await chat.EditLastMessageMessage("Меню", InlineService.GetMainMenu());
                                break;

                            case CallbackData.MyAlerts:
                                await chat.EditLastMessageMessage("Оповещения", InlineService.GetAlerts(chat));
                                break;

                            case CallbackData.AddAlert:
                                await chat.EditLastMessageMessage("Выберите день для оповещения", InlineService.GetDaysForNewAlert());
                                break;

                            case CallbackData.UseWknd:
                            {
                                chat.EnableWeekendAlarm = !chat.EnableWeekendAlarm;
                                g.Save();
                                await chat.EditLastMessageMessage("Оповещения", InlineService.GetAlerts(chat));
                            }
                                break;

                            case CallbackData.AddAlertDate:
                            {
                                var date = DateTime.Parse(dataArr[1]);
                                if (chat.AlarmDays.All(_ => _ != date))
                                {
                                    chat.AlarmDays.Add(date);
                                    chat.AlarmDays = chat.AlarmDays.OrderBy(_ => _.Date).ToList();
                                    g.Save();
                                }

                                await chat.EditLastMessageMessage("Оповещения", InlineService.GetAlerts(chat));
                            }
                                break;

                            case CallbackData.DelAlert:
                                await chat.EditLastMessageMessage("Выберите день для удаления", InlineService.GetAlertsForDelete(chat));
                                break;

                            case CallbackData.DelAllAlerts:
                            {
                                chat.AlarmDays.Clear();
                                g.Save();
                                await chat.EditLastMessageMessage("Оповещения", InlineService.GetAlerts(chat));
                            }
                                break;

                            case CallbackData.DelAlertDate:
                            {
                                var date = DateTime.Parse(dataArr[1]);
                                chat.AlarmDays.Remove(date);
                                g.Save();
                                await chat.EditLastMessageMessage("Оповещения", InlineService.GetAlerts(chat));
                            }
                                break;

                            default:
                                break;
                        }
                    }
                }

            }
            catch (Exception e)
            {
                Logger.Error(e, "BotUpdatesThreadFunc Error");
            }
        }
    }

    private async void BotAlertThreadFunc()
    {
        while (BotWork)
        {
            var lst = FSService.GetTickets();
            if (lst != null)
            {
                var alertedList = new List<long>();
                var lastDate    = lst.Select(_ => _.Date).Distinct().Max().Date;
                foreach (var x in g.UserData.Where(_ => _.LastNotificationDate < lastDate).Where(_ => _.AlarmDays.Select(__ => __.Date).Contains(lastDate)))
                {
                    x.LastNotificationDate = lastDate;
                    alertedList.Add(x.UserId);
                    x.AlarmDays.Remove(lastDate);
                    await g.TLC.SendMessage(x.Chat, "=======================================\nВ продаже появились билеты на " + lastDate.ToShortDateString() + "\n=======================================");
                    x.LastMessageId = 0;
                }
                if (lastDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                {
                    foreach (var x in g.UserData.Where(_ => _.LastNotificationDate < lastDate).Where(_ => _.EnableWeekendAlarm && !alertedList.Contains(_.UserId)))
                    {
                        x.LastNotificationDate = lastDate;
                        alertedList.Add(x.UserId);
                        await g.TLC.SendMessage(x.Chat, "=======================================\nВ продаже появились билеты на " + lastDate.ToShortDateString() + "\n=======================================");
                        x.LastMessageId = 0;
                    }
                }

                if (alertedList.Count > 0)
                {
                    foreach (var x in g.UserData)
                    {
                        x.AlarmDays = x.AlarmDays.OrderBy(_ => _.Date).ToList();
                        while(x.AlarmDays.Count > 0 && x.AlarmDays[0] <= lastDate)
                            x.AlarmDays.Remove(lastDate);
                    }

                    g.Save();
                }
            }
            
            Thread.Sleep(60000);
        }
    }

}
