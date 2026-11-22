using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace AvroraBot.Services.Shared;

public class ModelProvider(IServiceProvider provider)
{
    public ChatClientConfiguration ProvideTextModel(TextModelEnum model) =>
        provider.GetRequiredKeyedService<ChatClientConfiguration>(model);
    
    public IImageGenerator ProvideImageModel(ImageModelEnum model) =>
        provider.GetRequiredKeyedService<IImageGenerator>(model);
}