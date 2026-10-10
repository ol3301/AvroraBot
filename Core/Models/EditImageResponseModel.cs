namespace Core.Models;

public record EditImageResponseModel(
    long InputTokenCount,
    long OutputTokenCount,
    int DataLength,
    string MediaType,
    ReadOnlyMemory<byte> Data);