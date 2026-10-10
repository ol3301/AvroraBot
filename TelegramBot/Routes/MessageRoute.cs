using System.ClientModel;
using Application.ApplicationServices;
using Application.Commands;
using Core;
using Core.Ai;
using Core.Models;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Extensions.Markup;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.Extensions;
using TelegramBot.Utilities;

namespace TelegramBot.Routes;

public class MessageRoute(TelegramBotClient bot, 
    TextGenerationApplicationService textGenerationService, 
    ImageGenerationApplicationService imageGenerationService, 
    ImageEditingApplicationService imageEditingService,
    ILogger<MessageRoute> logger)
{
    public async Task Handle(Message message)
    {
        try
        {
            var state = new MessageDraftState(bot, message.Chat.Id, "<tg-thinking>Думаю</tg-thinking>", logger);
            await state.Refresh();
            
            var command = new GenerateTextCommand(Text: message.ExtractTextFromMessage(),
                ImageId: message.ExtractImageFromMessage(),
                UserId: message.From.Id,
                MessageId: message.MessageId
            );

            GenerateTextCommandResult result = null!;
            var stream = textGenerationService.GenerateTextStreaming(command);
            await foreach (var update in stream)
            {
                result = update.Result;
                await state.Refresh(update.Text);
            }

            switch (result.Tool)
            {
                case {Name: nameof(AiToolList.CreateImage)}:
                    await HandleImageGeneration(message, result.Tool);
                    return;                
                case {Name: nameof(AiToolList.EditImage)}:
                    await HandleImageEditing(message, result.Tool, result.ImageAttachment);
                    return;
                default:
                    await bot.SendRichMessage(message.Chat.Id, 
                        new InputRichMessage
                        {
                            Html = Tools.EscapeMarkdown(state.GetText(), parseMode: ParseMode.MarkdownV2)
                        },
                        disableNotification: true);
                    return;
            }
        }
        catch (BotValidationException ex)
        {
            await bot.AnswerError(message.Chat.Id, ex.Message);
        }
    }
    
    public async Task HandleImageGeneration(Message message, ToolCallModel model) 
    {
        try
        {
            var state = new MessageDraftState(bot, message.Chat.Id, "<tg-thinking>Генерация изображения</tg-thinking>", logger);
            await state.Refresh();
            var command = new GenerateImageCommand(UserId: message.From.Id,
                ImagePrompt: model.Arguments["imagePrompt"],
                MessageId: message.MessageId
            );
            
            var response = await imageGenerationService.GenerateImage(command);
            using var ms = new MemoryStream(response.Image.ToArray());
            await bot.SendRichMessage(message.Chat.Id, new InputRichMessage
            {
                Blocks =
                [
                    new InputRichBlockPhoto
                    {
                        Photo = new InputMediaPhoto(InputFile.FromStream(ms)),
                    }
                ]
            }, disableNotification: true);
        }
        catch (ClientResultException ex) when (ex.Status == 400)
        {
            await bot.AnswerError(message.Chat.Id, "Не удалось сгенерировать изображение: запрос может нарушать авторские права. Попробуйте изменить промпт.");
        }
        catch (BotValidationException ex)
        {
            await bot.AnswerError(message.Chat.Id, ex.Message);
        }
    }

    public async Task HandleImageEditing(Message message, ToolCallModel model, ImageAttachmentModel? imageAttachment)
    {
        try
        {
            var state = new MessageDraftState(bot, message.Chat.Id, "<tg-thinking>Модифицирую изображение</tg-thinking>", logger);
            await state.Refresh();
            var command = new EditImageCommand(UserId: message.From.Id,
                ImagePrompt: model.Arguments["imagePrompt"],
                MessageId: message.MessageId,
                ImageAttachment: imageAttachment
            );
            
            var response = await imageEditingService.EditImage(command);
            
            using var ms = new MemoryStream(response.Image.ToArray());
            await bot.SendRichMessage(message.Chat.Id, new InputRichMessage
            {
                Blocks =
                [
                    new InputRichBlockPhoto
                    {
                        Photo = new InputMediaPhoto(InputFile.FromStream(ms)),
                    }
                ]
            }, disableNotification: true);
        }
        catch (BotValidationException ex)
        {
            await bot.AnswerError(message.Chat.Id, ex.Message);
        }
    }
}