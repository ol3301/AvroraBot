using Core;

namespace TelegramBot.Bundle;

public record BundleDefinition(
    int Stars,
    string Name,
    string Description,
    BundleKind Kind,
    ToolsetDefinition ToolSet);

public enum BundleKind
{
    StarterBundle = 1
}
