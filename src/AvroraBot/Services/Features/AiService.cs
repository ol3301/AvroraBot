using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace AvroraBot.Services.Features;

public class AiService(
    MongoDbContext context,
    ModelProvider modelProvider,
    ImageDownloadService imageDownloadService,
    IOptions<AppConfig> config,
    ILogger<AiService> logger)
{
    public async IAsyncEnumerable<GenerateTextCommandStreamResult> GenerateTextStreaming(GenerateTextCommand command)
    {
        logger.LogInformation(command.ToString());

        var user = await context.Users.AsQueryable().FirstOrDefaultAsync(x => x.UserId == command.UserId);

        var lastMessageCount =
            await context.Messages.MessageCountForUser(command.UserId, DateTime.UtcNow.AddSeconds(-5));
        if (lastMessageCount > 3)
        {
            throw new BotValidationException(
                "Слишком много запросов. Пожалуйста, подождите несколько секунд перед повторной попыткой.");
        }

        var todayMessageCount = await context.Messages.TodayMessageCountForUser(command.UserId);
        if (todayMessageCount >= user.Config.Toolset.Limits.TextRequestCountPerDay)
        {
            throw new BotValidationException("Сегодня вы исчерпали лимит запросов. Пожалуйста, попробуйте завтра.");
        }

        var requestModel = await BuildTextRequest(user, command.Text, command.ImageId);

        var (client, reasoning) = modelProvider.ProvideTextModel(user.TextModelId);

        var stream = client
            .GetStreamingResponseAsync(requestModel.GetMessages(), new ChatOptions
            {
                Tools =
                [
                    AIFunctionFactory.Create(AiToolList.CreateImage),
                    AIFunctionFactory.Create(AiToolList.EditImage)
                ],
                Instructions = config.Value.AssistantPrompt,
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

            yield return new GenerateTextCommandStreamResult(Text: chunk.Text);
        }

        var chatResponse = chunks.ToChatResponse();

        AiResponseLogging.LogTextResponse(logger, chatResponse);

        var toolCall = chatResponse.Messages.First().Contents.OfType<FunctionCallContent>().FirstOrDefault();
        var tool = toolCall == null
            ? null
            : new ToolCallModel(
                Name: toolCall.Name,
                Arguments: toolCall.Arguments?.ToDictionary(x => x.Key, x => x.Value.ToString()));

        var response = new GenerateTextResponseModel(
            Text: chatResponse.ToString(),
            InputTokenCount: chatResponse.Usage.InputTokenCount.Value,
            OutputTokenCount: chatResponse.Usage.OutputTokenCount.Value,
            Tool: tool,
            AttachedImage: requestModel.ImageAttachment);

        await context.Messages.InsertOneAsync(new MessageEntity
        {
            MessageId = command.MessageId,
            UserId = user.UserId,
            TextModelId = user.TextModelId,
            CreatedAt = DateTime.UtcNow,
            RequestImageFilePath = response.AttachedImage?.FileInfo.FilePath,
            RequestText = command.Text,
            ResponseText = response.Text,
            InputTokens = response.InputTokenCount,
            OutputTokens = response.OutputTokenCount,
            ToolCallModel = response.Tool
        });

        yield return new GenerateTextCommandStreamResult(
            Result: new GenerateTextCommandResult(
                Text: response.Text,
                Tool: response.Tool,
                ImageAttachment: response.AttachedImage
            )
        );
    }

    public async Task<EditImageResult> EditImage(EditImageCommand command)
    {
        logger.LogInformation(command.ToString());

        if (command.ImageAttachment == null)
        {
            throw new BotValidationException(
                "Чтобы отредактировать изображение, процитируйте исходное сообщение с изображением в Telegram.");
        }

        var user = await context.Users.AsQueryable().FirstAsync(x => x.UserId == command.UserId);

        var todayImageCount = await context.TodayImageOperationCountForUser(command.UserId);
        if (todayImageCount >= user.Config.Toolset.Limits.ImageRequestCountPerDay)
        {
            throw new BotValidationException(
                "Сегодня вы исчерпали лимит на генерацию изображений. Пожалуйста, попробуйте завтра.");
        }

        EnsureImageCreditsAvailable(user);

        var imageResponse = await modelProvider
            .ProvideImageModel(user.ImageModelId)
            .EditImageAsync(
                new DataContent(command.ImageAttachment.Data,
                    $"image/{command.ImageAttachment.FileInfo.ExtensionStripped}"),
                command.ImagePrompt);

        var response = ToImageResponse(user, imageResponse);

        await (await context.Client.StartSessionAsync()).WithTransactionAsync(async (session, _) =>
        {
            await context.UserImageEdits.InsertOneAsync(session, new UserImageEditEntity
            {
                MessageId = command.MessageId,
                UserId = command.UserId,
                RequestImageFilePath = command.ImageAttachment.FileInfo.FilePath,
                ImageModelId = user.ImageModelId,
                CreatedAt = DateTime.UtcNow,
                Prompt = command.ImagePrompt,
                DataSize = response.DataLength,
                MediaType = response.MediaType,
                InputTokens = response.InputTokenCount,
                OutputTokens = response.OutputTokenCount
            });
            await context.Users.ReplaceOneAsync(session, x => x.UserId == command.UserId, user);

            return Task.CompletedTask;
        });

        return new EditImageResult(response.Data);
    }

    public async Task<GenerateImageResult> GenerateImage(GenerateImageCommand command)
    {
        logger.LogInformation(command.ToString());

        var user = await context.Users.AsQueryable().FirstOrDefaultAsync(x => x.UserId == command.UserId);

        var todayImageCount = await context.TodayImageOperationCountForUser(command.UserId);
        if (todayImageCount >= user.Config.Toolset.Limits.ImageRequestCountPerDay)
        {
            throw new BotValidationException(
                "Сегодня вы исчерпали лимит на генерацию изображений. Пожалуйста, попробуйте завтра.");
        }

        EnsureImageCreditsAvailable(user);

        var imageResponse = await modelProvider
            .ProvideImageModel(user.ImageModelId)
            .GenerateImagesAsync(command.ImagePrompt);

        var response = ToImageResponse(user, imageResponse);

        await (await context.Client.StartSessionAsync()).WithTransactionAsync(async (session, _) =>
        {
            await context.UserImages.InsertOneAsync(session, new UserImageEntity
            {
                MessageId = command.MessageId,
                UserId = command.UserId,
                ImageModelId = user.ImageModelId,
                CreatedAt = DateTime.UtcNow,
                Prompt = command.ImagePrompt,
                DataSize = response.DataLength,
                MediaType = response.MediaType,
                InputTokens = response.InputTokenCount,
                OutputTokens = response.OutputTokenCount
            });

            await context.Users.ReplaceOneAsync(session, x => x.UserId == command.UserId, user);
            return Task.CompletedTask;
        });

        return new GenerateImageResult(response.Data);
    }

    private static void EnsureImageCreditsAvailable(UserEntity user)
    {
        if (user.Config.AvailableImageRequestCount < 1)
        {
            throw new BotValidationException(
                "У вас закончились доступные генерации изображений. Приобретите дополнительные генерации, чтобы продолжить.");
        }
    }

    // Extracts the image from the model response and consumes one image credit from the user.
    private AiImageResponseModel ToImageResponse(UserEntity user, ImageGenerationResponse response)
    {
        AiResponseLogging.LogImageResponse(logger, response);

        var dataContent = response.Contents.OfType<DataContent>().FirstOrDefault();
        if (dataContent == null)
        {
            throw new BotSystemException("dataContent is null");
        }

        user.Config.AvailableImageRequestCount -= 1;

        return new AiImageResponseModel(
            InputTokenCount: response.Usage.InputTokenCount.Value,
            OutputTokenCount: response.Usage.OutputTokenCount.Value,
            DataLength: dataContent.Data.Length,
            MediaType: dataContent.MediaType,
            Data: dataContent.Data);
    }

    private async Task<AiTextRequestModel> BuildTextRequest(UserEntity user, string text, string? imageId)
    {
        var requestModel = new AiTextRequestModel
        {
            Text = text,
        };

        if (!string.IsNullOrEmpty(imageId))
        {
            requestModel.ImageAttachment = await imageDownloadService.DownloadImage(imageId);
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
