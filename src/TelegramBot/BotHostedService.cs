using Application.ApplicationServices;
using Application.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.Extensions;
using TelegramBot.Routes;
using TelegramBot.ServiceBundle;

namespace TelegramBot;

public class BotHostedService(
    IServiceScopeFactory serviceFactory,
    ILogger<IServiceScopeFactory> logger,
    SignUpUserApplicationService signUpUserApplicationService,
    TelegramBotClient bot) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        bot.StartReceiving(
            updateHandler: async (_, update, cancellationToken) =>
            {
                try
                {
                    await ProcessUpdateAsync(update, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Exception");
                    await bot.AnswerSystemError(update.Message.From.Id);
                }            
            },
            errorHandler: async (_, exception, _) =>
            {
                logger.LogError(exception, "TelegramBotClient unhandled exception");
                await Task.CompletedTask;
            }, cancellationToken: stoppingToken
        );
        
        logger.LogInformation("Bot started: " + (await bot.GetMe(cancellationToken: stoppingToken)).Username);
    }

    private async Task ProcessUpdateAsync(Update update, CancellationToken _)
    {
        using var scope = serviceFactory.CreateScope();
        if (update.Message != null)
        {
            await signUpUserApplicationService.SignUp(new SignUpUserCommand(
                UserId: update.Message.From.Id,
                Username: update.Message.From.Username,
                Fullname: update.Message.From.FirstName + " " + update.Message.From.LastName,
                Plan: ServiceBundleConfig.FreeToolset)
            );
        }

        switch (update)
        {
            case { Message: { Text: "/start" } msg }:
                await scope.ServiceProvider
                    .GetRequiredService<StartRoute>()
                    .Handle(msg);
                break;
            case { Message: { Text: "/premium" } msg }:
                await scope.ServiceProvider
                    .GetRequiredService<PremiumRoute>()
                    .Handle(msg);
                break;
            case { PreCheckoutQuery: { } preCheckoutQuery }:
                await bot.AnswerPreCheckoutQuery(preCheckoutQuery.Id);
                break;
            case { Message: { SuccessfulPayment: not null } msg }:
                await scope.ServiceProvider
                    .GetRequiredService<PremiumRoute>()
                    .HandlePayment(msg);
                break;
            case { Message: { } msg }:
                await scope.ServiceProvider
                    .GetRequiredService<MessageRoute>()
                    .Handle(msg);
                break;
            case { CallbackQuery: { Data: {} data  } callback } when data.StartsWith("bundle:"):
                var selection = Enum.Parse<BundleKind>(callback.Data["bundle:".Length..]);
                        
                await scope.ServiceProvider
                    .GetRequiredService<PremiumRoute>()
                    .HandleBundleSelection(callback, selection);
                break;
            
            case { CallbackQuery: { Data: { } data } callback } when data == "-":
                await bot.AnswerCallbackQuery(callback.Id);
                break;
            
            default:
                logger.LogCritical("Unhandled update type: {UpdateType}", update.Type);
                break;
        }
    }
}