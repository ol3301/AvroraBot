
namespace AvroraBot.Features.Commands;

public record SignUpUserCommand(
    long UserId,
    string Username,
    string Fullname,
    ToolsetDefinition Plan
);