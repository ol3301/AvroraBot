using System.Text;
using Application.ApplicationServices;
using Application.Commands;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Payments;
using TelegramBot.ServiceBundle;

namespace TelegramBot.Routes;

public class PremiumRoute(TelegramBotClient bot, 
    StarPaymentApplicationService starPaymentApplicationService)
{
    public async Task Handle(Message msg)
    {
        var sb = new StringBuilder();
        
        foreach (var bundle in ServiceBundleConfig.Bundles)
        {
            var plan = bundle.ToolSet;
            sb.Append($"""
                       <tg-section>
                         <b>{bundle.Name}</b> {bundle.Stars}⭐
                         
                       <p>ГПТ: <b>{string.Join(", ", plan.TextModels)}</b></p>
                       <p>
                        Изображения: <b>{string.Join(", ", plan.ImageModels)} </b>
                        <i>(Запросы/день: {plan.Limits.ImageRequestCountPerDay})</i>
                       </p>
                       
                       <tg-button-row align="center">
                         <tg-button type="callback_data" style="primary" data="bundle:{bundle.Kind}">{bundle.Stars}⭐Купить</tg-button>
                       </tg-button-row>
                       </tg-section>
                       """);            
        }
        
        await bot.SendRichMessage(msg.Chat.Id, new InputRichMessage
        {
            Html = sb.ToString(),
        });
    }

    public async Task HandleBundleSelection(CallbackQuery callbackQuery, BundleKind kind)
    {
        var bundle = ServiceBundleConfig.Bundles.First(b => b.Kind == kind);

        var command = new CreateStartInvoiceCommand(
            UserId: callbackQuery.From.Id,
            Stars: bundle.Stars,
            Toolset: bundle.ToolSet);
        
        var invoiceId = await starPaymentApplicationService.CreateInvoice(command);

        await bot.DeleteMessage(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
        await bot.SendInvoice(callbackQuery.From.Id,
            bundle.Name,
            bundle.Description,
            invoiceId.ToString(),
            "XTR",
            [new LabeledPrice("Label 1", bundle.Stars)]);
        await bot.AnswerCallbackQuery(callbackQuery.Id);
    }

    public async Task HandlePayment(Message msg)
    {
        var command = new SubmitStarInvoiceCommand(
            UserId: msg.From.Id,
            InvoiceId: Guid.Parse(msg.SuccessfulPayment.InvoicePayload)
        );

        await starPaymentApplicationService.SubmitInvoice(command);

        await bot.SendRichMessage(msg.Chat.Id, new InputRichMessage
        {
            Html = "Оплата премиум-доступа успешно завершена."
        });
    }
}