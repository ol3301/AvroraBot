namespace AvroraBot.Shared.Models;

public record AiImageResponseModel(
    long InputTokenCount,
    long OutputTokenCount,
    int DataLength,
    string MediaType,
    ReadOnlyMemory<byte> Data);