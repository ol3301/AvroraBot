namespace Application.Commands;

public record SubmitStarInvoiceCommand(long UserId, Guid InvoiceId);