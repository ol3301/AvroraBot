using Telegram.Bot;
using Telegram.Bot.Extensions;
using Telegram.Bot.Types;

namespace TelegramBot.Routes;

public class StartRoute(TelegramBotClient bot)
{
    public async Task Handle(Message msg)
    {
        var welcome = """
                      👋 Ваш ИИ-помощник
                      💬 Ответы на вопросы, тексты, код и генерация изображений.
                      
                      ⚡ /premium — расширенный доступ
                      """;
        await bot.SendHtml(msg.Chat.Id, welcome);
    }
}