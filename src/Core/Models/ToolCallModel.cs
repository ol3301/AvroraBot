namespace Core.Models;

public record ToolCallModel(string Name, IDictionary<string, string>? Arguments);