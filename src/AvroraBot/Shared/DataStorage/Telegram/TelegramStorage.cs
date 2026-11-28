using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace AvroraBot.Shared.DataStorage.Telegram;

public class TelegramStorage(TelegramBotClient bot, IOptions<AppConfig> config)
{
    public async Task<ImageAttachmentModel> DownloadImage(string fileId)
    {
        var fileInfo = await DownloadFileInfo(fileId);

        if (!config.Value.AllowedImages.Contains(fileInfo.Extension))
        {
            throw new BotValidationException(StringsService.BadImageExtensionMessage(fileInfo.Extension, config.Value.AllowedImages));
        }

        if (fileInfo.FileSize > config.Value.MaxFileSize)
        {
            throw new BotValidationException(StringsService.BadImageSizeMessage(fileInfo.FileSize, config.Value.MaxFileSize));
        }

        using var ms = new MemoryStream();
        await bot.DownloadFile(fileInfo.FilePath, ms);

        return new ImageAttachmentModel(
            FileInfo: fileInfo,
            Data: ms.ToArray()
        );
    }

    private async Task<FileInfoResponseModel> DownloadFileInfo(string fileId)
    {
        var fileInfo = await bot.GetFile(fileId);

        return new FileInfoResponseModel(
            FilePath: fileInfo.FilePath,
            FileSize: fileInfo.FileSize.Value,
            Extension: Path.GetExtension(fileInfo.FilePath),
            ExtensionStripped: Path.GetExtension(fileInfo.FilePath).Remove(0, 1));
    }
}
