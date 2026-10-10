using Core.Models;

namespace Core.Abstractions;

public interface IBotApi
{
    Task<FileInfoResponseModel> DownloadFileInfo(string fileId);
    Task DownloadFile(string filePath, MemoryStream ms);
}