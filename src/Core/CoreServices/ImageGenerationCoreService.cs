using Core.Abstractions;
using Core.Entities;
using Core.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Core.CoreServices;

public class ImageGenerationCoreService(IModelProvider provider, ILogger<ImageGenerationCoreService> logger)
{
    public async Task<GenerateImageResponseModel> GenerateImage(UserEntity user, string prompt)
    {
        if (user.Config.AvailableImageRequestCount < 1)
        {
            throw new BotValidationException("У вас закончились доступные генерации изображений. Приобретите дополнительные генерации, чтобы продолжить.");
        }

        var response = await provider
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
}