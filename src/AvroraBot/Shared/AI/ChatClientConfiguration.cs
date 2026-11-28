using Microsoft.Extensions.AI;

namespace AvroraBot.Shared.AI;

public record ChatClientConfiguration(IChatClient Client, ReasoningOptions Reasoning);