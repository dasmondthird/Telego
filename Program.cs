using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

class Program
{
    static async Task Main(string[] args)
    {
        var botClient = new TelegramBotClient("6968201954:AAEnrNk8Ke2RWBBqTht94rvY4LpjGS7pekA");

        using var cts = new CancellationTokenSource();

        var receiverOptions = new ReceiverOptions();

        botClient.StartReceiving(
            CommandHandlers.HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cts.Token
        );

        Console.WriteLine("Bot started. Press Ctrl+C to exit.");

        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true; 
            cts.Cancel();            
        };

        await Task.Delay(Timeout.Infinite, cts.Token);
    }

    static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine("Error: " + exception.ToString());
        return Task.CompletedTask;
    }
}
