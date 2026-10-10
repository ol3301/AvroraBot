using Core;

namespace TelegramBot.ServiceBundle;

public record ServiceBundleDefinition(
    int Stars,
    string Name,
    string Description,
    BundleKind Kind,
    ToolsetDefinition ToolSet);

public enum BundleKind
{
    StarterBundle = 1
}
