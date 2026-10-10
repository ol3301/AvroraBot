namespace Application.Commands;

public record GenerateImageCommand(
    long UserId,
    string ImagePrompt,
    long MessageId
);

public record GenerateImageResult(ReadOnlyMemory<byte> Image);