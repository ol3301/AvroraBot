using Microsoft.Extensions.AI;

namespace AvroraBot.Configuration;

public record ChatClientConfiguration(IChatClient Client, ReasoningOptions Reasoning);