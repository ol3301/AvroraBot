using Core.Abstractions;
using Core.Models;
using Telegram.Bot;

namespace Infrastructure;

public class TelegramBotApi(TelegramBotClient bot) : IBotApi
{
    public async Task<FileInfoResponseModel> DownloadFileInfo(string fileId)
    {
        var fileInfo = await bot.GetFile(fileId);

        return new FileInfoResponseModel(
            FilePath: fileInfo.FilePath,
            FileSize: fileInfo.FileSize.Value,
            Extension: Path.GetExtension(fileInfo.FilePath),
            ExtensionStripped: Path.GetExtension(fileInfo.FilePath).Remove(0, 1));
    }

    public async Task DownloadFile(string filePath, MemoryStream ms)
    {
        await bot.DownloadFile(filePath, ms);
    }
}