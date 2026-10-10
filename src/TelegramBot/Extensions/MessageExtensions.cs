using Telegram.Bot.Types;

namespace TelegramBot.Extensions;

public static class MessageExtensions
{
    public static string ExtractTextFromMessage(this Message msg)
    {
        var buffer = new List<string>();
        if (msg.ReplyToMessage != null && !string.IsNullOrEmpty(msg.ReplyToMessage.Text))
        {
            buffer.Add(msg.ReplyToMessage.Text);
        }
        
        if(msg.Quote != null && !string.IsNullOrEmpty(msg.Quote.Text))
        {
            buffer.Add(msg.Quote.Text);
        }

        if (!string.IsNullOrEmpty(msg.Text))
        {
            buffer.Add(msg.Text);
        }
        
        if(!string.IsNullOrEmpty(msg.Caption))
        {
            buffer.Add(msg.Caption);
        }

        return string.Join("\n", buffer);
    }

    public static string? ExtractImageFromMessage(this Message msg)
    {
        var image = msg.ExtractImageFromPhoto();

        if (image != null)
        {
            return image;
        }
        
        var replyToMessagePhoto = msg.ReplyToMessage?.ExtractImageFromPhoto();
        if(replyToMessagePhoto != null)
        {
            return replyToMessagePhoto;
        }

        var documentImage = msg.ExtractImageFromDocument();
        if (documentImage != null)
        {
            return documentImage;
        }

        var replyToMessageDocument = msg.ReplyToMessage?.ExtractImageFromDocument();
        if (replyToMessageDocument != null)
        {
            return replyToMessageDocument;
        }

        var richMessagePhoto = msg.RichMessage?.ExtractImageFromPhotoBlock();
        if(richMessagePhoto != null)
        {
            return richMessagePhoto;
        }
        
        var replyToMessageRichMessagePhoto = msg.ReplyToMessage?.RichMessage?.ExtractImageFromPhotoBlock();
        if (replyToMessageRichMessagePhoto != null)
        {
            return replyToMessageRichMessagePhoto;
        }

        return null;
    }

    private static string? ExtractImageFromPhoto(this Message msg)
    {
        return msg.Photo?.Last().FileId;
    }
    
    private static string? ExtractImageFromDocument(this Message msg)
    {
        return msg.Document?.MimeType?.StartsWith("image") == true ? msg.Document.FileId : null;
    }
}