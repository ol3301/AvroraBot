
namespace AvroraBot.Models;

public record SignUpUserCommand(
    long UserId,
    string Username,
    string Fullname,
    ToolsetDefinition Plan
);