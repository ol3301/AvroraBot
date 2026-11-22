namespace AvroraBot.Models;

public record SubmitStarInvoiceCommand(long UserId, Guid InvoiceId);