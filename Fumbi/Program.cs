using Fumbi.Helpers;
using Serilog;
using System;

namespace Fumbi
{
    public static class Program
    {
        static Program()
        {
            Console.CancelKeyPress += OnCancelKeyPress;

            Log.Logger = new LoggerConfiguration().MinimumLevel.Verbose()
                .WriteTo.Async(w => w.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}] {Message:lj}{NewLine}{Exception}"), bufferSize: 1000, blockWhenFull: true)
                .CreateLogger();

            Database.Initialize();
        }

        private static void Main()
        {
            Bot.Start(Config.Instance.BotToken);
        }

        private static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Exit();
            Environment.Exit(0);
        }

        private static void Exit()
        {
            Bot.Stop();

            ImageCache.Dispose();

            Log.CloseAndFlush();
        }
    }
}
