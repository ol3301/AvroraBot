using Core.Abstractions;
using Core.CoreServices;
using Core.Entities;
using Core.Models;
using Core.Queries;
using Microsoft.Extensions.AI;

namespace Core.Ai;

public class AiRequestBuilder(
    ImageAttachmentCoreService imageAttachmentService,
    IMongoDbContext context)
{
    public async Task<AiTextRequestModel> BuildTextRequest(UserEntity user,
        string text,
        string? imageId)
    {
        var requestModel = new AiTextRequestModel
        {
            Text = text,
        };

        if (!string.IsNullOrEmpty(imageId))
        {
            requestModel.ImageAttachment = await imageAttachmentService.DownloadImage(imageId);
        }

        if (user.ContextMemory)
        {
            var messages = await context.Messages.MessageHistoryForUser(user.UserId, DateTime.UtcNow.AddMinutes(-15));

            messages
                .ToList()
                .ForEach(x =>
                {
                    requestModel.Messages.Add(new ChatMessage(ChatRole.User, x.RequestText));

                    if (x.ToolCallModel?.Name == nameof(AiToolList.CreateImage))
                    {
                        requestModel.Messages.Add(new ChatMessage(ChatRole.Assistant,
                            $"I generated photo with prompt: {x.ToolCallModel.Arguments.First().Value}"));
                        return;
                    }

                    if (x.ToolCallModel?.Name == nameof(AiToolList.EditImage))
                    {
                        requestModel.Messages.Add(new ChatMessage(ChatRole.Assistant,
                            $"I edited photo with prompt: {x.ToolCallModel.Arguments.First().Value}"));
                        return;
                    }

                    requestModel.Messages.Add(new ChatMessage(ChatRole.Assistant, x.ResponseText));
                });
        }

        return requestModel;
    }
}