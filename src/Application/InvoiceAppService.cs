using Application.Commands;
using Core;
using Core.Abstractions;
using Core.Entities;
using Core.Enums;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Application;

public class InvoiceAppService(ILogger<InvoiceAppService> logger,
    IMongoDbContext context)
{
    public async Task<Guid> CreateStartInvoice(CreateStartInvoiceCommand command)
    {
        logger.LogInformation(command.ToString());

        var entity = new InvoiceEntity
        {
            UserId = command.UserId,
            Amount = command.Stars,
            Currency = InvoiceCurrencyEnum.TelegramStar,
            CreatedAt = DateTime.UtcNow,
            Toolset = command.Toolset
        };

        await context.Invoices.InsertOneAsync(entity);

        return entity.InvoiceId;
    }

    public async Task SubmitInvoice(SubmitStarInvoiceCommand command)
    {
        logger.LogInformation(command.ToString());
        
        var invoice = await context.Invoices
            .AsQueryable()
            .FirstOrDefaultAsync(x => x.InvoiceId == command.InvoiceId);
        var user = await  context.Users.AsQueryable().FirstAsync(x => x.UserId == command.UserId);

        if (invoice == null)
        {
            throw new BotSystemException($"Invoice with ID {command.InvoiceId} not found.");
        }

        if (invoice.PaidAt.HasValue)
        {
            throw new BotSystemException($"Invoice with ID {command.InvoiceId} has already been paid.");
        }

        invoice.PaidAt = DateTime.UtcNow;
        user.Config.Toolset = invoice.Toolset;
        user.Config.AvailableImageRequestCount = invoice.Toolset.ImageRequestCount;
        
        await context.Invoices.ReplaceOneAsync(x => x.InvoiceId == command.InvoiceId, invoice);
        await context.Users.ReplaceOneAsync(x => x.UserId == command.UserId, user);
    }
}