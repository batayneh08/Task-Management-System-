namespace ManagementSystem.Application.Exceptions;

public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("The email or password is incorrect.")
    {
    }
}
