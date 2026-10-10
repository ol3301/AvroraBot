using Core.Ai;
using Microsoft.Extensions.AI;

namespace Core.Models;

public class AiTextRequestModel
{
    public required string Text { get; set; }
    public ImageAttachmentModel? ImageAttachment { get; set; }
    public IList<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

    public IEnumerable<ChatMessage> GetMessages()
    {
        var list = new List<ChatMessage>();

        if (Messages.Any())
        {
            list.AddRange(Messages);
        }

        var userMessage = new ChatMessage(ChatRole.User, Text);
        if (ImageAttachment != null)
        {
            userMessage.Contents.Add(new DataContent(
                ImageAttachment.Data,
                $"image/{ImageAttachment.FileInfo.ExtensionStripped}"));
        }

        list.Add(userMessage);
        return list;
    }
}

public record GenerateTextResponseModel(
    string Text,
    long InputTokenCount,
    long OutputTokenCount,
    ToolCallModel? Tool,
    ImageAttachmentModel? AttachedImage);

public record GenerateTextStreamingResponseModel(
    string? Text = null,
    GenerateTextResponseModel? Response = null);