using Core.Ai;
using Core.Models;

namespace Core.Entities;

public class MessageEntity
{
    public long MessageId { get; set; }
    public long UserId { get; set; }
    public TextModelEnum TextModelId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public string? RequestImageFilePath { get; set; }
    public string RequestText { get; set; } = string.Empty;
    public string? ResponseText { get; set; }
    public ToolCallModel? ToolCallModel { get; set; }
}