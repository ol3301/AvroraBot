using DotNetEnv.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Serilog;
using Telegram.Bot;

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

builder.Services.AddTransient<MessageRoute>();
builder.Services.AddTransient<StartRoute>();
builder.Services.AddTransient<PremiumRoute>();

builder.Services.AddSingleton<TelegramBotClient>(x =>
    new TelegramBotClient(x.GetRequiredService<IOptions<AppConfig>>().Value.Telegram));
builder.Services.AddSingleton<MongoDbContext>(x =>
    new MongoDbContext(new MongoClient(x.GetRequiredService<IOptions<AppConfig>>().Value.Mongo)));
builder.Services.AddSingleton<ModelProvider>();

builder.Services.AddTransient<AiService>();
builder.Services.AddTransient<UserService>();
builder.Services.AddTransient<InvoiceService>();
builder.Services.AddTransient<TelegramStorage>();

builder.Services.AddHostedService<BotHostedService>();
builder.Build().Run();
