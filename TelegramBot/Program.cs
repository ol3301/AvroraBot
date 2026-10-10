using Application.ApplicationServices;
using Core;
using Core.Abstractions;
using Core.Ai;
using Core.CoreServices;
using Core.Enums;
using DotNetEnv.Configuration;
using Infrastructure;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Serilog;
using Telegram.Bot;
using TelegramBot;
using TelegramBot.Routes;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddYamlFile("appsettings.yaml", optional: false);
builder.Configuration.AddDotNetEnv();
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddSerilog((_, lc) =>
{
    lc.WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
});
builder.Services.AddOptions<AppConfig>()
    .BindConfiguration("")
    .Configure(config => { config.AssistantPrompt = File.ReadAllText("AssistantPrompt.txt"); })
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<TelegramBotClient>(x =>
    new TelegramBotClient(x.GetRequiredService<IOptions<AppConfig>>().Value.Telegram));
builder.Services.AddSingleton<IMongoDbContext, MongoDbContext>(x =>
    new MongoDbContext(new MongoClient(x.GetRequiredService<IOptions<AppConfig>>().Value.Mongo)));

builder.Services.AddKeyedSingleton<ChatClientConfiguration>(TextModelEnum.Gpt56Luna, (x, _) =>
    new ChatClientConfiguration
    (
        Client: new OpenAI.Chat.ChatClient(
                "gpt-5.6-luna",
                x.GetRequiredService<IOptions<AppConfig>>().Value.OpenAi)
            .AsIChatClient(),
        Reasoning: new ReasoningOptions { Effort = ReasoningEffort.None }
    ));

builder.Services.AddKeyedImageGenerator(ImageModelEnum.GptImage25Flare, x => new OpenAI.Images.ImageClient(
        "gpt-image-2.5-flare-2026-09-08",
        x.GetRequiredService<IOptions<AppConfig>>().Value.OpenAi)
    .AsIImageGenerator());

builder.Services.AddSingleton<IModelProvider, ModelProvider>();

builder.Services.AddTransient<MessageRoute>();
builder.Services.AddTransient<StartRoute>();
builder.Services.AddTransient<PremiumRoute>();
builder.Services.AddTransient<IBotApi, TelegramBotApi>();
builder.Services.AddTransient<TextGenerationApplicationService>();
builder.Services.AddTransient<ImageGenerationApplicationService>();
builder.Services.AddTransient<ImageEditingApplicationService>();
builder.Services.AddTransient<SignUpUserApplicationService>();
builder.Services.AddTransient<StarPaymentApplicationService>();
builder.Services.AddTransient<TextGenerationCoreService>();
builder.Services.AddTransient<ImageGenerationCoreService>();
builder.Services.AddTransient<ImageEditingCoreService>();
builder.Services.AddTransient<ImageAttachmentCoreService>();

builder.Services.AddHostedService<BotHostedService>();
builder.Build().Run();
