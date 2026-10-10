using Core;

namespace Application.Commands;

public record CreateStartInvoiceCommand(
    long UserId,
    int Stars,
    ToolsetDefinition Toolset
);