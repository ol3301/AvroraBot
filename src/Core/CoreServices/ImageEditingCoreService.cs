using Core.Abstractions;
using Core.Entities;
using Core.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Core.CoreServices;

public class ImageEditingCoreService(IModelProvider provider, ILogger<ImageEditingCoreService> logger)
{
    public async Task<EditImageResponseModel> EditImage(UserEntity user, ReadOnlyMemory<byte> data, string mediaType, string prompt)
    {
        if (user.Config.AvailableImageRequestCount < 1)
        {
            throw new BotValidationException("У вас закончились доступные генерации изображений. Приобретите дополнительные генерации, чтобы продолжить.");
        }

        var response = await provider
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