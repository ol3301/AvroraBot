using Core.Enums;

namespace Core.Entities;

public class UserEntity
{
    public long UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public TextModelEnum TextModelId { get; set; }
    public ImageModelEnum ImageModelId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Username { get; set; }
    public bool ContextMemory { get; set; }
    public string? ReferralId { get; set; }
    public UserConfig Config { get; set; }

    public bool SetTextModel(TextModelEnum model)
    {
        var areSame = TextModelId == model;
        TextModelId = model;
        return !areSame;
    }
    
    public bool SetImageModel(ImageModelEnum model)
    {
        var areSame = ImageModelId == model;
        ImageModelId = model;
        return !areSame;
    }
}

public class UserConfig
{
    public required int AvailableImageRequestCount  { get; set; }
    public required ToolsetDefinition Toolset { get; set; }
}