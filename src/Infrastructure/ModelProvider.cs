using Core.Abstractions;
using Core.Ai;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public class ModelProvider(IServiceProvider provider) : IModelProvider
{
    public ChatClientConfiguration ProvideTextModel(TextModelEnum model) =>
        provider.GetRequiredKeyedService<ChatClientConfiguration>(model);
    
    public IImageGenerator ProvideImageModel(ImageModelEnum model) =>
        provider.GetRequiredKeyedService<IImageGenerator>(model);
}