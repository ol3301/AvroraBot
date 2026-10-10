using Application.Commands;
using Core.Abstractions;
using Core.Entities;
using Core.Enums;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Application.ApplicationServices;

public class SignUpUserApplicationService(IMongoDbContext context, ILogger<SignUpUserApplicationService> logger)
{
    public async Task SignUp(SignUpUserCommand command)
    {
        if (await context.Users.AsQueryable().AnyAsync(x => x.UserId == command.UserId))
        {
            return;
        }
        
        logger.LogInformation(command.ToString());
        
        var entity = new UserEntity
        {
            UserId = command.UserId,
            Username = command.Username,
            Name = command.Fullname,
            TextModelId = TextModelEnum.Gpt56Luna,
            ImageModelId = ImageModelEnum.GptImage25Flare,
            CreatedAt = DateTime.UtcNow,
            ContextMemory = true,
            ReferralId = null,
            Config = new UserConfig
            {
                Toolset = command.Plan,
                AvailableImageRequestCount = command.Plan.ImageRequestCount,
            }
        };
        
        await context.Users.InsertOneAsync(entity);
    }
}