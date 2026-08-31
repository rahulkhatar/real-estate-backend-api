namespace RealEstate.Core.Exceptions;

public abstract class AppException : Exception
{
    public int StatusCode { get; }

    protected AppException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string entityName, string key)
        : base($"{entityName} with id '{key}' was not found.", StatusCodes.NotFound) { }

    public NotFoundException(string message) : base(message, StatusCodes.NotFound) { }
}

public class ConflictException : AppException
{
    public ConflictException(string message) : base(message, StatusCodes.Conflict) { }
}

public class ValidationAppException : AppException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationAppException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.", StatusCodes.BadRequest)
    {
        Errors = errors;
    }
}

public class UnauthorizedAppException : AppException
{
    public UnauthorizedAppException(string message) : base(message, StatusCodes.Unauthorized) { }
}

public class PaymentGatewayNotConfiguredException : AppException
{
    public PaymentGatewayNotConfiguredException(string provider)
        : base($"{provider} is not configured yet. Add its API keys to enable payments.", StatusCodes.ServiceUnavailable) { }
}

public class AiNotConfiguredException : AppException
{
    public AiNotConfiguredException()
        : base("The AI assistant is not configured yet. Add an OpenAI API key to enable it.", StatusCodes.ServiceUnavailable) { }
}

internal static class StatusCodes
{
    public const int BadRequest = 400;
    public const int Unauthorized = 401;
    public const int NotFound = 404;
    public const int Conflict = 409;
    public const int ServiceUnavailable = 503;
}
