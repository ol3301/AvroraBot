
namespace AvroraBot.Features.Commands;

public record EditImageCommand(
    long UserId,
    string ImagePrompt,
    long MessageId,
    ImageAttachmentModel? ImageAttachment
);

public record EditImageResult(ReadOnlyMemory<byte> Image);