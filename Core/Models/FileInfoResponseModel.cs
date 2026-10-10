namespace Core.Models;

public record FileInfoResponseModel(
    string FilePath,
    long FileSize,
    string Extension,
    string ExtensionStripped);