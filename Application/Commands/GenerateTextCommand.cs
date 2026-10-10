using Core.Models;

namespace Application.Commands;

public record GenerateTextCommand(
    string Text,
    string? ImageId,
    long UserId,
    long MessageId
);

public record GenerateTextCommandResult(
    string Text,
    ToolCallModel? Tool,
    ImageAttachmentModel? ImageAttachment
);

public record GenerateTextCommandStreamResult(
    string? Text = null,
    GenerateTextCommandResult? Result = null
);