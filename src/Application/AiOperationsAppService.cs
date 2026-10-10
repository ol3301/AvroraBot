using Application.Commands;
using Core;
using Core.Abstractions;
using Core.Ai;
using Core.Entities;
using Core.Models;
using Core.Queries;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Application;

public class AiOperationsAppService(
    IMongoDbContext context,
    AiOperationsCoreService aiCoreService,
    IOptions<AppConfig> config,
    ILogger<AiOperationsAppService> logger)
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

        var stream = aiCoreService.GenerateTextStreaming(user,
            command.Text,
            command.ImageId,
            config.Value.AssistantPrompt);

        GenerateTextResponseModel response = null!;
        await foreach (var update in stream)
        {
            yield return new GenerateTextCommandStreamResult(Text: update.Text);
            response = update.Response;
        }

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

        var response = await aiCoreService.EditImage(user,
            command.ImageAttachment.Data,
            $"image/{command.ImageAttachment.FileInfo.ExtensionStripped}",
            command.ImagePrompt);

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
        logger.LogInformation(user.Config.Toolset.Limits.ImageRequestCountPerDay.ToString());
        if (todayImageCount >= user.Config.Toolset.Limits.ImageRequestCountPerDay)
        {
            throw new BotValidationException(
                "Сегодня вы исчерпали лимит на генерацию изображений. Пожалуйста, попробуйте завтра.");
        }

        var response = await aiCoreService.GenerateImage(user, command.ImagePrompt);

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
}