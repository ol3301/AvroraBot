namespace Core.Models;

public record AiNewImageResponseModel(
    long InputTokenCount,
    long OutputTokenCount,
    int DataLength,
    string MediaType,
    ReadOnlyMemory<byte> Data);