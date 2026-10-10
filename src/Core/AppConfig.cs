using System.ComponentModel.DataAnnotations;

namespace Core;

public record AppConfig
{
    [Required]
    public required string Telegram { get; set; } 
    [Required]
    public required string Mongo { get; set; }
    [Required]
    public required string OpenAi { get; set; }
    [MinLength(1)]
    public required IEnumerable<string> AllowedImages { get; set; }
    [Range(1, int.MaxValue)]
    public required int MaxFileSize { get; set; } 
    [Required]
    public required string AssistantPrompt { get; set; }
}