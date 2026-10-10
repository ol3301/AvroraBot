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

public class ImageGenerationApplicationService(IMongoDbContext context, 
    ImageGenerationCoreService imageGenerationDomainService,
    ILogger<ImageGenerationApplicationService> logger)
{
    public async Task<GenerateImageResult> GenerateImage(GenerateImageCommand command)
    {
        logger.LogInformation(command.ToString());
            
        var user = await context.Users.AsQueryable().FirstOrDefaultAsync(x => x.UserId == command.UserId);

        var todayImageCount = await context.TodayImageOperationCountForUser(command.UserId);
        logger.LogInformation(user.Config.Toolset.Limits.ImageRequestCountPerDay.ToString());
        if (todayImageCount >= user.Config.Toolset.Limits.ImageRequestCountPerDay)
        {
            throw new BotValidationException("Сегодня вы исчерпали лимит на генерацию изображений. Пожалуйста, попробуйте завтра.");
        }
        
        var response = await imageGenerationDomainService.GenerateImage(user, command.ImagePrompt);

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