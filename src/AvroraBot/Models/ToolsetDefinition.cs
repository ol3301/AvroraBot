
namespace AvroraBot.Models;

public record ToolsetDefinition(
    IEnumerable<TextModelEnum> TextModels,
    IEnumerable<ImageModelEnum> ImageModels,
    int ImageRequestCount,
    ToolsetDefinition.UsageLimits Limits)
{
    public record UsageLimits(
        int? TextRequestCountPerDay,
        int? ImageRequestCountPerDay);
}