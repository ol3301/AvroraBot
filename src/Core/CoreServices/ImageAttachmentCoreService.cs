using Core.Abstractions;
using Core.Models;
using Microsoft.Extensions.Options;

namespace Core.CoreServices;

public class ImageAttachmentCoreService(IBotApi storage, IOptions<AppConfig> config)
{
    public async Task<ImageAttachmentModel> DownloadImage(string fileId)
    {
        var fileInfo = await storage.DownloadFileInfo(fileId);

        if (!config.Value.AllowedImages.Contains(fileInfo.Extension))
        {
            throw new BotValidationException(StringsService.BadImageExtensionMessage(fileInfo.Extension, config.Value.AllowedImages));
        }

        if (fileInfo.FileSize > config.Value.MaxFileSize)
        {
            throw new BotValidationException(StringsService.BadImageSizeMessage(fileInfo.FileSize, config.Value.MaxFileSize));
        }
        
        using var ms = new MemoryStream();
        await storage.DownloadFile(fileInfo.FilePath, ms);

        return new ImageAttachmentModel(
            FileInfo: fileInfo,
            Data: ms.ToArray()
        );
    }
}