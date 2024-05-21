using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

public static class CommandHandlers
{
    public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message && update.Message?.Type == Telegram.Bot.Types.Enums.MessageType.Text)
        {
            if (update.Message.Text != null)
            {
                await UserSessionManager.HandleCommand(botClient, update.Message);
            }
        }
    }
}
