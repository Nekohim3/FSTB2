using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Serilog;

namespace FSTB2
{
    public static class Logger
    {
        public static Serilog.Core.Logger Log;

        static Logger()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            if (!Directory.Exists("Logs"))
                Directory.CreateDirectory("Logs");
            Log     = new LoggerConfiguration().WriteTo.File($"Logs\\App{timestamp}.log").CreateLogger();
            Info($"Logger init at {timestamp}. Version 2.0.0");
        }

        public static void Error(string msg)
        {
            Log.Error(msg);
        }

        public static void Error(Exception e, string msg = "")
        {
            Log.Error(e, msg);
        }

        public static async void ErrorQ(string msg)
        {
            Error(msg);
            await MessageBoxManager.GetMessageBoxStandard("", "Возникла ошибка. Приложение будет закрыто. Подробности находятся в лог файле.", ButtonEnum.Ok, Icon.Error).ShowWindowDialogAsync(g.MainWindow);
            //MessageBox.Show("Возникла ошибка. Приложение будет закрыто. Подробности находятся в лог файле.", "", MessageBoxButton.OK, MessageBoxImage.Error);
            //g.Driver?.Chrome?.Quit();
            Process.GetCurrentProcess().Kill();
        }

        public static async void ErrorQ(Exception e, string msg = "")
        {
            Error(e, msg);
            await MessageBoxManager.GetMessageBoxStandard("", "Возникла ошибка. Приложение будет закрыто. Подробности находятся в лог файле.", ButtonEnum.Ok, Icon.Error).ShowWindowDialogAsync(g.MainWindow);
            //g.Driver?.Chrome?.Quit();
            Process.GetCurrentProcess().Kill();
        }

        public static void Info(string msg)
        {
            Log.Information(msg);
        }
    }
}
