# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

AvroraBot (the repo dir is `SharpBot`): a Telegram bot in .NET 10 / C# that does AI chat, image generation and image editing. It uses OpenAI through `Microsoft.Extensions.AI`, stores data in MongoDB, and takes payments in Telegram Stars. Bot-facing strings are in Russian.

## Commands

```bash
# Start MongoDB as a single-node replica set (required: the code uses transactions)
docker compose up -d mongodb

# Build everything
dotnet build AvroraBot.sln

# Run the bot (it reads appsettings.yaml and AssistantPrompt.txt from the working dir / output dir)
dotnet run --project src/AvroraBot

# Docker image (build context is the repo root)
docker build -f src/AvroraBot/Dockerfile -t avrorabot .
```

There are no test projects and no lint configuration. `MEAI001` (experimental Microsoft.Extensions.AI APIs) is suppressed solution-wide in `Directory.Build.props`.

## Configuration

`AppConfig` (`src/AvroraBot/Configuration/AppConfig.cs`) binds from the configuration root and is validated on start. Sources, later ones winning: `src/AvroraBot/appsettings.yaml`, then a `.env` file (DotNetEnv), then environment variables. The required secrets are `Telegram` (bot token), `Mongo` (connection string, e.g. `mongodb://localhost:27017/?replicaSet=rs0`) and `OpenAi`. The `AssistantPrompt` value is always read from `src/AvroraBot/AssistantPrompt.txt`, which is copied to the output directory.

The Mongo database is named `TelegramBot` (see `Data/MongoDbContext.cs`). Keep that name so existing data stays readable.

## Architecture

There is one project, `src/AvroraBot`, with no layers and no interfaces. Folders are grouped by technical role, every namespace is `AvroraBot.<Folder>`, and all of them are imported through `GlobalUsings.cs`. Concrete classes are injected directly, and `Program.cs` holds all DI wiring.

- `Hosting/BotHostedService` long-polls updates, auto-signs-up every message sender on the free toolset (`UserService`), and uses a `switch` pattern match to dispatch to `Routes/*Route` classes (`/start`, `/premium`, payments, `bundle:<BundleKind>` callbacks, and a catch-all `MessageRoute`).
- Routes build `Models/*Command` records and call feature services. The feature services enforce rate limits and daily quotas, call the AI models, and persist results through `Data/MongoDbContext`. Multi-document writes use `context.Client.StartSessionAsync().WithTransactionAsync(...)`.
- Services are split into two folders:
  - `Services/Features` holds the feature services (`AiService`, `UserService`, `InvoiceService`): one per use-case area, called by routes and hosting.
  - `Services/Shared` holds the helpers (`ImageDownloadService`, `ModelProvider`).
  - **Dependency rule:** feature services may use shared services and `Data`, but never another feature service. Shared services never use feature services. When two features need the same logic, extract it into `Services/Shared`. This rule keeps the DI graph free of cycles.
- Errors: `BotValidationException` carries a user-facing message that routes show to the user. `BotSystemException` is internal, and the user sees a generic error.
- The folder `AiTools` is not called `Tools` because that name would clash with `Telegram.Bot.Extensions.Markup`'s `Tools.EscapeMarkdown`.

### Key flows

- **Models are keyed DI services.** Text models are registered as keyed `ChatClientConfiguration` by `TextModelEnum`, and image models as keyed `IImageGenerator` by `ImageModelEnum`, all in `Program.cs`. `ModelProvider` resolves them from the user's `TextModelId` / `ImageModelId`. To add a model: add an enum value in `Enums/ModelEnums.cs`, then register it in `Program.cs`.
- **Image ops are tool calls.** Every message first goes through streaming text generation, which offers the `AiToolList.CreateImage` / `EditImage` tools. These are stub methods whose `[Description]` attributes serve as the tool prompts. The model's function call is never invoked. It is stored as a `ToolCallModel`, and `MessageRoute` then switches on `Tool.Name` to call `GenerateImage` / `EditImage` with the `imagePrompt` argument. Image editing needs the source image to be attached or quoted in a reply.
- **Streaming UX.** `MessageDraftState` throttles `SendRichMessageDraft` updates (about 400 ms) while chunks arrive. The final text goes out as a rich message.
- **Conversation memory.** When `UserEntity.ContextMemory` is set, `AiService.BuildTextRequest` replays the user's messages from the last 15 minutes out of the `Messages` collection. Earlier image tool calls are summarized as assistant text.
- **Plans and payments.** `ToolsetDefinition` (allowed models, image credit count, daily limits) is embedded in `UserEntity.Config`. Bundles are defined in `Configuration/BundleConfig.cs`. `PremiumRoute` sends a Telegram Stars invoice backed by an `InvoiceEntity`. On `SuccessfulPayment`, `InvoiceService.SubmitInvoice` copies the invoice's toolset onto the user and resets `AvailableImageRequestCount`.
- **Usage tracking.** Each operation writes a record (`MessageEntity`, `UserImageEntity`, `UserImageEditEntity`) with token counts. Daily limits are computed by counting those records (see `Data/*Queries.cs`).
