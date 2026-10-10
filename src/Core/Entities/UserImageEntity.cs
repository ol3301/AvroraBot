using Core.Enums;

namespace Core.Entities;

public class UserImageEntity
{
    public long MessageId { get; set; }
    public long UserId { get; set; }
    public ImageModelEnum ImageModelId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Prompt { get; set; } = string.Empty;
    public int DataSize { get; set; }
    public string MediaType { get; set; }
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
}