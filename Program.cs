using System;
using System.Windows.Forms;

namespace PlaylandBoxer;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Global exception handlers to surface runtime errors to console and a log file
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            try
            {
                var ex = e.ExceptionObject as Exception;
                var msg = $"UnhandledException: {ex?.Message}\n{ex?.StackTrace}";
                Console.Error.WriteLine(msg);
                File.AppendAllText("app-error.log", DateTime.Now + " - " + msg + Environment.NewLine);
            }
            catch { }
        };

        System.Windows.Forms.Application.ThreadException += (s, e) =>
        {
            try
            {
                var ex = e.Exception;
                var msg = $"ThreadException: {ex.Message}\n{ex.StackTrace}";
                Console.Error.WriteLine(msg);
                File.AppendAllText("app-error.log", DateTime.Now + " - " + msg + Environment.NewLine);
            }
            catch { }
        };

        try
        {
            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            var msg = $"Startup exception: {ex.Message}\n{ex.StackTrace}";
            Console.Error.WriteLine(msg);
            try { File.AppendAllText("app-error.log", DateTime.Now + " - " + msg + Environment.NewLine); } catch { }
            throw;
        }
    }
}
