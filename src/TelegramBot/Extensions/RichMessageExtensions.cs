using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBot.Extensions;

public static class RichMessageExtensions
{
    public static string? ExtractImageFromPhotoBlock(this RichMessage msg)
    {
        return (msg
                .Blocks
                .FirstOrDefault(x => x.Type == RichBlockType.Photo) as RichBlockPhoto)?
            .Photo
            .Last().FileId;
    }
}