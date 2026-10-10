using Core.Ai;
using Core.Enums;
using Microsoft.Extensions.AI;

namespace Core.Abstractions;

public interface IModelProvider
{
    public ChatClientConfiguration ProvideTextModel(TextModelEnum model);

    public IImageGenerator ProvideImageModel(ImageModelEnum model);
}