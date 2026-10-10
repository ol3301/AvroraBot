namespace Core.Models;

public record AiEditedImageResponseModel(
    long InputTokenCount,
    long OutputTokenCount,
    int DataLength,
    string MediaType,
    ReadOnlyMemory<byte> Data);