using Core;
using Core.Ai;

namespace TelegramBot.Bundle;

public static class BundleConfig
{
    public static ToolsetDefinition FreeToolset { get; } = new(
        TextModels: [TextModelEnum.Gpt56Luna],
        ImageModels: [],
        ImageRequestCount: 0,
        Limits: new ToolsetDefinition.UsageLimits(
            TextRequestCountPerDay: 20,
            ImageRequestCountPerDay: null)
    );
    
    public static IEnumerable<BundleDefinition> Bundles { get; } =
    [
        new(
            Stars: 1,
            Name: "Starter bundle",
            Description: "Повышенные лимиты — до 30 генераций изображений",
            Kind: BundleKind.StarterBundle,
            ToolSet: new ToolsetDefinition(
                TextModels: [TextModelEnum.Gpt56Luna],
                ImageModels: [ImageModelEnum.GptImage25Flare],
                ImageRequestCount: 30,
                Limits: new ToolsetDefinition.UsageLimits(
                    TextRequestCountPerDay: null,
                    ImageRequestCountPerDay: 30)
                )
            )
    ];
}