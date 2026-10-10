using Core.Abstractions;
using Core.Ai;
using Core.Entities;
using Core.Models;
using Core.Queries;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Core.CoreServices;

public class TextGenerationCoreService(IMongoDbContext context, 
    IModelProvider modelProvider,
    ImageAttachmentCoreService imageAttachmentService,
    ILogger<TextGenerationCoreService> logger)
{
    public async IAsyncEnumerable<GenerateTextStreamingResponseModel> GenerateTextStreaming(UserEntity user, 
        string text, 
        string? imageId, 
        string baseInstructions)
    {
        var requestModel = new GenerateTextRequestModel
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

        var (client, reasoning) = modelProvider
            .ProvideTextModel(user.TextModelId);

        var stream = client
            .GetStreamingResponseAsync(requestModel.GetMessages(), new ChatOptions
            {
                Tools = [
                    AIFunctionFactory.Create(AiToolList.CreateImage), 
                    AIFunctionFactory.Create(AiToolList.EditImage)
                ],
                Instructions = baseInstructions,
                Reasoning = reasoning
            });

        var chunks = new List<ChatResponseUpdate>();
        await foreach (var chunk in stream)
        {
            chunks.Add(chunk);

            if(string.IsNullOrEmpty(chunk.Text))
            {
                continue;
            }

            yield return new GenerateTextStreamingResponseModel(Text: chunk.Text);
        }

        var response = chunks.ToChatResponse();
        
        DomainLogging.LogTextResponse(logger, response);
        
        var tool = response.Messages.First().Contents.OfType<FunctionCallContent>().FirstOrDefault();

        var result = new GenerateTextResponseModel(
            Text: response.ToString(),
            InputTokenCount: response.Usage.InputTokenCount.Value,
            OutputTokenCount: response.Usage.OutputTokenCount.Value,
            Tool: tool == null
                ? null
                : new ToolCallModel(
                    Name: tool.Name,
                    Arguments: tool.Arguments?.ToDictionary(x => x.Key, x => x.Value.ToString())
                ),
            AttachedImage: requestModel.ImageAttachment);

        yield return new GenerateTextStreamingResponseModel
        {
            Response = result
        };
    }
}