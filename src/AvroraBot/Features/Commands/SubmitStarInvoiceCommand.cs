namespace AvroraBot.Features.Commands;

public record SubmitStarInvoiceCommand(long UserId, Guid InvoiceId);