namespace Core.Ai;

public record ToolCallModel(string Name, IDictionary<string, string>? Arguments);