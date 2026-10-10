using Microsoft.Extensions.AI;

namespace Core.Ai;

public record ChatClientConfiguration(IChatClient Client, ReasoningOptions Reasoning);