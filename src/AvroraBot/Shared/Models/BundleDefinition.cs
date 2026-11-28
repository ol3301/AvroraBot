
namespace AvroraBot.Shared.Models;

public record BundleDefinition(
    int Stars,
    string Name,
    string Description,
    BundleKind Kind,
    ToolsetDefinition ToolSet);
