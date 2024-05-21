using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

public static class UserSessionManager
{
    private static ConcurrentDictionary<long, UserSession> sessions = new ConcurrentDictionary<long, UserSession>();

    public static async Task HandleCommand(ITelegramBotClient botClient, Message message)
    {
        if (message == null || message.Text == null) return;

        var chatId = message.Chat.Id;
        if (!sessions.TryGetValue(chatId, out var session))
        {
            session = new UserSession { ChatId = chatId, State = UserState.None };
            sessions.TryAdd(chatId, session);
        }

        var userInput = message.Text.Trim().ToLower();

        switch (userInput)
        {
            case "/start":
            case "🔄 сброс":
                session.Reset();
                await SendLanguageSelection(botClient, chatId);
                break;
            default:
                if (userInput.Contains("english") || userInput.Contains("1"))
                {
                    session.Language = Language.English;
                    session.State = UserState.AwaitingName;
                    await botClient.SendTextMessageAsync(chatId, "Great! Let's start with English. What's your name?");
                }
                else if (userInput.Contains("spanish") || userInput.Contains("2"))
                {
                    session.Language = Language.Spanish;
                    session.State = UserState.AwaitingName;
                    await botClient.SendTextMessageAsync(chatId, "¡Genial! Empecemos con español. ¿Cómo te llamas?");
                }
                else
                {
                    await HandleStateSpecificCommands(botClient, session, message);
                }
                break;
        }
    }

    private static async Task HandleStateSpecificCommands(ITelegramBotClient botClient, UserSession session, Message message)
    {
        var chatId = session.ChatId;
        string userInput = message.Text.Trim().ToLower();

        switch (session.State)
        {
            case UserState.AwaitingName:
                await HandleNameInput(botClient, session, chatId, userInput);
                break;
            case UserState.Introduction:
            case UserState.ChooseCategory:
                await HandleLanguagePractice(botClient, session, chatId, userInput);
                break;
            case UserState.AwaitingAnswer:
                await HandleAnswer(botClient, session, chatId, userInput);
                break;
            default:
                await botClient.SendTextMessageAsync(chatId, session.Language == Language.English
                    ? "Unknown command. Please try again. 🤷‍♂️"
                    : "Comando desconocido. Por favor, intenta de nuevo. 🤷‍♂️");
                break;
        }
    }

    private static async Task HandleNameInput(ITelegramBotClient botClient, UserSession session, long chatId, string userInput)
    {
        session.UserName = userInput;
        session.State = UserState.Introduction;
        await botClient.SendTextMessageAsync(chatId, session.Language == Language.English
            ? $"Welcome, {session.UserName}! I'm excited to help you learn English. Could you tell me a bit about yourself?"
            : $"Bienvenido, {session.UserName}! Estoy emocionado de ayudarte a aprender español. ¿Podrías contarme un poco sobre ti?");
    }

    private static async Task HandleLanguagePractice(ITelegramBotClient botClient, UserSession session, long chatId, string userInput)
    {
        if (userInput.Contains("introduce") || session.State == UserState.Introduction)
        {
            string response = session.Language == Language.English
                ? "Oh, very interesting! Now, how about we expand your skills with some specific exercises? Choose a category you'd like to explore:"
                : "¡Oh, qué interesante! Ahora, ¿qué te parece si ampliamos tus habilidades con algunos ejercicios específicos? Elige una categoría que te gustaría explorar:";

            await botClient.SendTextMessageAsync(chatId, response);
            session.State = UserState.ChooseCategory;
            await SendMenu(botClient, chatId, session.Language);
        }
        else
        {
            await HandleCategorySelection(botClient, session, chatId, userInput);
        }
    }

    private static async Task HandleCategorySelection(ITelegramBotClient botClient, UserSession session, long chatId, string category)
    {
        switch (category)
        {
            case "📚 grammar":
            case "📖 vocabulary":
            case "💬 idioms":
            case "🗣 phrasal verbs":
            case "🗨 conversation practice":
            case "👀 reading":
            case "✍ writing":
                await HandleSelectedCategory(botClient, session, chatId, category);
                break;
            default:
                await botClient.SendTextMessageAsync(chatId, session.Language == Language.English
                    ? "I didn't understand that. Please choose a category by clicking a button below."
                    : "No entendí eso. Por favor, elige una categoría haciendo clic en un botón de abajo.");
                await SendMenu(botClient, chatId, session.Language);
                break;
        }
    }

    private static async Task HandleSelectedCategory(ITelegramBotClient botClient, UserSession session, long chatId, string category)
    {
        string question = "";
        switch (category)
        {
            case "📚 grammar":
                question = session.Language == Language.English ? "What is the past tense of 'go'?" : "¿Cuál es el tiempo pasado de 'ir'?";
                session.CurrentAnswer = session.Language == Language.English ? "went" : "fui";
                break;
            case "📖 vocabulary":
                question = session.Language == Language.English ? "What is the synonym of 'happy'?" : "¿Cuál es el sinónimo de 'feliz'?";
                session.CurrentAnswer = session.Language == Language.English ? "joyful" : "contento";
                break;
            case "💬 idioms":
                question = session.Language == Language.English ? "What does 'break the ice' mean?" : "¿Qué significa 'romper el hielo'?";
                session.CurrentAnswer = session.Language == Language.English ? "start a conversation" : "empezar una conversación";
                break;
            case "🗣 phrasal verbs":
                question = session.Language == Language.English ? "What does 'give up' mean?" : "¿Qué significa 'give up'?";
                session.CurrentAnswer = session.Language == Language.English ? "surrender" : "rendirse";
                break;
            case "🗨 conversation practice":
                question = session.Language == Language.English ? "How do you introduce yourself?" : "¿Cómo te presentas?";
                session.CurrentAnswer = session.Language == Language.English ? "My name is..." : "Me llamo...";
                break;
            case "👀 reading":
                question = session.Language == Language.English ? "Read this sentence and tell me the main idea:" : "Lee esta frase y dime la idea principal:";
                session.CurrentAnswer = session.Language == Language.English ? "The main idea is..." : "La idea principal es...";
                break;
            case "✍ writing":
                question = session.Language == Language.English ? "Write a short paragraph about your favorite hobby." : "Escribe un párrafo corto sobre tu pasatiempo favorito.";
                session.CurrentAnswer = session.Language == Language.English ? "My favorite hobby is..." : "Mi pasatiempo favorito es...";
                break;
        }

        session.CurrentQuestion = question;
        session.State = UserState.AwaitingAnswer;

        await botClient.SendTextMessageAsync(chatId, question);
    }

    private static async Task HandleAnswer(ITelegramBotClient botClient, UserSession session, long chatId, string userInput)
    {
        if (userInput.Equals(session.CurrentAnswer, StringComparison.OrdinalIgnoreCase))
        {
            session.Score++;
            await botClient.SendTextMessageAsync(chatId, session.Language == Language.English
                ? $"Correct! 🎉 Your score is now {session.Score}."
                : $"¡Correcto! 🎉 Tu puntuación es ahora de {session.Score}.");
        }
        else
        {
            await botClient.SendTextMessageAsync(chatId, session.Language == Language.English
                ? $"Incorrect. The correct answer is: {session.CurrentAnswer}. ❌"
                : $"Incorrecto. La respuesta correcta es: {session.CurrentAnswer}. ❌");
        }

        session.State = UserState.ChooseCategory;
        await SendMenu(botClient, chatId, session.Language);
    }

    private static async Task SendLanguageSelection(ITelegramBotClient botClient, long chatId)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "1. English", "2. Spanish" },
            new KeyboardButton[] { "🔄 Сброс" }
        })
        { ResizeKeyboard = true };

        await botClient.SendTextMessageAsync(chatId, "Hello! I can teach you two languages: English and Spanish. Please choose which one you'd like to learn:", replyMarkup: keyboard);
    }

    private static async Task SendMenu(ITelegramBotClient botClient, long chatId, Language language)
    {
        string message = language == Language.English
            ? "Choose an exercise to continue practicing English:"
            : "Elige un ejercicio para continuar practicando español:";

        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "📚 Grammar", "📖 Vocabulary" },
            new KeyboardButton[] { "💬 Idioms", "🗣 Phrasal Verbs" },
            new KeyboardButton[] { "🗨 Conversation Practice", "👀 Reading", "✍ Writing" },
            new KeyboardButton[] { "🔄 Сброс" }
        })
        { ResizeKeyboard = true };

        await botClient.SendTextMessageAsync(chatId, message, replyMarkup: keyboard);
    }
}

public class UserSession
{
    public long ChatId { get; set; }
    public UserState State { get; set; }
    public Language Language { get; set; } = Language.None;
    public string CurrentQuestion { get; set; } = "";
    public string CurrentAnswer { get; set; } = "";
    public int Score { get; set; }
    public string UserName { get; set; } = "";

    public void Reset()
    {
        State = UserState.None;
        CurrentQuestion = "";
        CurrentAnswer = "";
        Score = 0;
        Language = Language.None;
        UserName = "";
    }
}

public enum UserState
{
    None,
    AwaitingName,
    Introduction,
    ChooseCategory,
    AwaitingAnswer
}

public enum Language
{
    None,
    English,
    Spanish
}
