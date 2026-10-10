using System.Text;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramBot.Utilities;

public class MessageDraftState
{
    private readonly TelegramBotClient _bot;
    private readonly CancellationTokenSource _cts = new();
    private readonly long _chatId;
    private readonly ILogger _logger;
    private readonly int _draftId;
    private readonly StringBuilder _stateSb = new();
    private readonly StringBuilder _textSb = new();

    private DateTime _lastSendTime = new();
    private int _lastLength = 0;

    public MessageDraftState(TelegramBotClient bot, long chatId, string state, ILogger logger)
    {
        _bot = bot;
        _chatId = chatId;
        _logger = logger;
        _draftId = Random.Shared.Next();
        _stateSb.Append(state);
    }

    public async Task Refresh(string? text = null)
    {
        if (!string.IsNullOrEmpty(text))
        {
            _textSb.Append(text);
        }
        
        var newLength = _stateSb.Length + _textSb.Length;
        if (_lastSendTime.AddMilliseconds(400) < DateTime.UtcNow && newLength != 0 && newLength != _lastLength )
        {
            var sb = new StringBuilder();
            if (_textSb.Length == 0)
            {
                sb.Append(_stateSb);
            }
            sb.Append(_textSb);
                        
            await _bot.SendRichMessageDraft(_chatId, _draftId, new InputRichMessage
            {
                Html = sb.ToString()
            });
                        
            _logger.LogInformation("Refresh sent");
                        
            _lastSendTime = DateTime.UtcNow;
            _lastLength = sb.Length;
        }
    }
    
    public string GetText()
    {
        return _textSb.ToString();
    }
}