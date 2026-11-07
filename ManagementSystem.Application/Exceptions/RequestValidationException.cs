namespace ManagementSystem.Application.Exceptions;

public sealed class RequestValidationException : Exception
{
    public RequestValidationException(string message)
        : base(message)
    {
    }
}
