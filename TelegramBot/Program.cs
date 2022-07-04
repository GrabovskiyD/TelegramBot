using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;

namespace TelegramBot
{
    public static class Program
    {
        private static TelegramBotClient botClient;
        public static async Task Main(string[] args)
        {
            botClient = new TelegramBotClient("");

            User me = await botClient.GetMeAsync();

            using CancellationTokenSource cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }
            };

            botClient.StartReceiving(
                Handlers.HandleUpdateAsync,
                Handlers.HandleErrorAsync,
                receiverOptions,
                cancellationToken: cts.Token);

            Console.WriteLine($"��� {me.FirstName} ����� ��������.");
            Console.ReadKey();

            cts.Cancel();
        }

    }
}