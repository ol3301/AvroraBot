using Core.Ai;

namespace Core.Entities;

public class UserImageEditEntity
{
    public long MessageId { get; set; }
    public long UserId { get; set; }
    public string? RequestImageFilePath { get; set; }
    public ImageModelEnum ImageModelId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public int DataSize { get; set; }
    public string MediaType { get; set; }
}