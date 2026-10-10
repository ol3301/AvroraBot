using Core.Abstractions;
using Core.Entities;
using Core.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Core.Ai;

public class AiOperationsCoreService(
    IModelProvider modelProvider,
    AiRequestBuilder  aiRequestBuilder,
    ILogger<AiOperationsCoreService> logger)
{
    public async IAsyncEnumerable<GenerateTextStreamingResponseModel> GenerateTextStreaming(UserEntity user,
        string text,
        string? imageId,
        string baseInstructions)
    {
        var requestModel = await aiRequestBuilder.BuildTextRequest(user, text, imageId);

        var (client, reasoning) = modelProvider
            .ProvideTextModel(user.TextModelId);

        var stream = client
            .GetStreamingResponseAsync(requestModel.GetMessages(), new ChatOptions
            {
                Tools =
                [
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

            if (string.IsNullOrEmpty(chunk.Text))
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

    public async Task<GenerateImageResponseModel> GenerateImage(UserEntity user, string prompt)
    {
        if (user.Config.AvailableImageRequestCount < 1)
        {
            throw new BotValidationException(
                "У вас закончились доступные генерации изображений. Приобретите дополнительные генерации, чтобы продолжить.");
        }

        var response = await modelProvider
            .ProvideImageModel(user.ImageModelId)
            .GenerateImagesAsync(prompt);

        DomainLogging.LogImageResponse(logger, response);

        var dataContent = response.Contents.OfType<DataContent>().FirstOrDefault();
        if (dataContent == null)
        {
            throw new BotSystemException("dataContent is null");
        }

        user.Config.AvailableImageRequestCount -= 1;

        return new GenerateImageResponseModel(
            InputTokenCount: response.Usage.InputTokenCount.Value,
            OutputTokenCount: response.Usage.OutputTokenCount.Value,
            DataLength: dataContent.Data.Length,
            MediaType: dataContent.MediaType,
            Data: dataContent.Data);
    }

    public async Task<EditImageResponseModel> EditImage(UserEntity user, ReadOnlyMemory<byte> data, string mediaType,
        string prompt)
    {
        if (user.Config.AvailableImageRequestCount < 1)
        {
            throw new BotValidationException(
                "У вас закончились доступные генерации изображений. Приобретите дополнительные генерации, чтобы продолжить.");
        }

        var response = await modelProvider
            .ProvideImageModel(user.ImageModelId)
            .EditImageAsync(new DataContent(data, mediaType), prompt);

        DomainLogging.LogImageResponse(logger, response);

        var dataContent = response.Contents.OfType<DataContent>().FirstOrDefault();
        if (dataContent == null)
        {
            throw new BotSystemException("dataContent is null");
        }

        user.Config.AvailableImageRequestCount -= 1;

        return new EditImageResponseModel(
            response.Usage.InputTokenCount.Value,
            response.Usage.OutputTokenCount.Value,
            dataContent.Data.Length,
            dataContent.MediaType,
            dataContent.Data);
    }
}