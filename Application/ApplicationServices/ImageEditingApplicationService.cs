using Application.Commands;
using Core;
using Core.Abstractions;
using Core.CoreServices;
using Core.Entities;
using Core.Queries;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Application.ApplicationServices;

public class ImageEditingApplicationService(IMongoDbContext context,
    ImageEditingCoreService imageEditingDomainService,
    ILogger<ImageEditingApplicationService> logger)
{
    public async Task<EditImageResult> EditImage(EditImageCommand command)
    {
        logger.LogInformation(command.ToString());

        if (command.ImageAttachment == null)
        {
            throw new BotValidationException("Чтобы отредактировать изображение, процитируйте исходное сообщение с изображением в Telegram.");
        }
        
        var user = await context.Users.AsQueryable().FirstAsync(x => x.UserId == command.UserId);
        
        var todayImageCount = await context.TodayImageOperationCountForUser(command.UserId);
        if (todayImageCount >= user.Config.Toolset.Limits.ImageRequestCountPerDay)
        {
            throw new BotValidationException("Сегодня вы исчерпали лимит на генерацию изображений. Пожалуйста, попробуйте завтра.");
        }

        var response = await imageEditingDomainService.EditImage(user, 
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
}