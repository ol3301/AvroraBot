
namespace AvroraBot.Models;

public record BundleDefinition(
    int Stars,
    string Name,
    string Description,
    BundleKind Kind,
    ToolsetDefinition ToolSet);
