namespace ManagementSystem.Application.Exceptions;

public sealed class EmailAlreadyRegisteredException : Exception
{
    public EmailAlreadyRegisteredException(string email)
        : base($"An account with email '{email}' is already registered.")
    {
    }
}
