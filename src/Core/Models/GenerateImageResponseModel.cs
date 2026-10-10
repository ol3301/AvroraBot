namespace Core.Models;

public record GenerateImageResponseModel(
    long InputTokenCount,
    long OutputTokenCount,
    int DataLength,
    string MediaType,
    ReadOnlyMemory<byte> Data);