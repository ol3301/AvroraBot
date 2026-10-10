using Core.Enums;

namespace Core.Entities;

public class InvoiceEntity
{
    public Guid InvoiceId { get; init; } = Guid.NewGuid();
    public required long UserId { get; set; }
    public required decimal Amount { get; set; }
    public required InvoiceCurrencyEnum Currency { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public required ToolsetDefinition Toolset { get; set; }
}