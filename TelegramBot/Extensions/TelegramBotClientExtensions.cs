using Telegram.Bot;
using Telegram.Bot.Extensions.Markup;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBot.Extensions;

public static class TelegramBotClientExtensions
{
    public static async Task AnswerError(this TelegramBotClient bot, long chatId, string text)
    {
        var escapedText = Tools.EscapeMarkdown($"⚠️ {text}", ParseMode.MarkdownV2);
        await bot.SendRichMessage(chatId, new InputRichMessage
        {
            Html = escapedText
        }, disableNotification: true);
    }
    public static async Task AnswerSystemError(this TelegramBotClient bot, long chatId)
    {
        var escapedText = Tools.EscapeMarkdown("❗️ Не удалось обработать запрос, попробуйте позже.", ParseMode.MarkdownV2);
        await bot.SendRichMessage(chatId, new InputRichMessage
        {
            Html = escapedText
        }, disableNotification: true);
    }
}