
namespace AvroraBot.Features.Commands;

public record CreateStartInvoiceCommand(
    long UserId,
    int Stars,
    ToolsetDefinition Toolset
);