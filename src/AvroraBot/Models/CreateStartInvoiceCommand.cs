
namespace AvroraBot.Models;

public record CreateStartInvoiceCommand(
    long UserId,
    int Stars,
    ToolsetDefinition Toolset
);