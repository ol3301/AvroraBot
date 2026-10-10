using Application.Commands;
using Core;
using Core.Abstractions;
using Core.CoreServices;
using Core.Entities;
using Core.Models;
using Core.Queries;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Application.ApplicationServices;

public class TextGenerationApplicationService(
    IMongoDbContext context,
    TextGenerationCoreService textGenerationDomainService,
    IOptions<AppConfig> config,
    ILogger<TextGenerationApplicationService> logger)
{
    public async IAsyncEnumerable<GenerateTextCommandStreamResult> GenerateTextStreaming(GenerateTextCommand command)
    {
        logger.LogInformation(command.ToString());

        var user = await context.Users.AsQueryable().FirstOrDefaultAsync(x => x.UserId == command.UserId);

        var lastMessageCount = await context.Messages.MessageCountForUser(command.UserId, DateTime.UtcNow.AddSeconds(-5));
        if (lastMessageCount > 3)
        {
            throw new BotValidationException("Слишком много запросов. Пожалуйста, подождите несколько секунд перед повторной попыткой.");
        }
        
        var todayMessageCount = await context.Messages.TodayMessageCountForUser(command.UserId);
        if (todayMessageCount >= user.Config.Toolset.Limits.TextRequestCountPerDay)
        {
            throw new BotValidationException("Сегодня вы исчерпали лимит запросов. Пожалуйста, попробуйте завтра.");
        }
        
        var stream = textGenerationDomainService.GenerateTextStreaming(user,
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
}