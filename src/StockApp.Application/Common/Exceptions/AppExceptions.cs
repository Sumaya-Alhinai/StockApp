namespace StockApp.Application.Common.Exceptions;

public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation failures occurred.")
    {
        Errors = errors;
    }
}

public class NotFoundException : Exception
{
    public NotFoundException(string name, object key)
        : base($"{name} with key '{key}' was not found.") { }
}

public class ConflictException : Exception
{
    public string Code { get; }

    public ConflictException(string code, string message) : base(message)
    {
        Code = code;
    }
}

public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException() : base("Invalid email or password.") { }
}