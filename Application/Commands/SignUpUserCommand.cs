using Core;

namespace Application.Commands;

public record SignUpUserCommand(
    long UserId,
    string Username,
    string Fullname,
    ToolsetDefinition Plan
);