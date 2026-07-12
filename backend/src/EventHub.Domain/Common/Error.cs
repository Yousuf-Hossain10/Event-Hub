namespace EventHub.Domain.Common;

public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static Error Unprocessable(string code, string message) => new(code, message, ErrorType.Unprocessable);
    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);
}
