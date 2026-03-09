namespace CryptoExchangesRestLibrary.Exceptions;

/// <summary>
/// Base exception for exchange API errors.
/// </summary>
public class ExchangeApiException : Exception
{
    public string ExchangeName { get; }
    public int? StatusCode { get; }
    public string? ErrorCode { get; }

    public ExchangeApiException(string exchangeName, string message)
        : base(message)
    {
        ExchangeName = exchangeName;
    }

    public ExchangeApiException(string exchangeName, string message, int statusCode)
        : base(message)
    {
        ExchangeName = exchangeName;
        StatusCode = statusCode;
    }

    public ExchangeApiException(string exchangeName, string message, string errorCode)
        : base(message)
    {
        ExchangeName = exchangeName;
        ErrorCode = errorCode;
    }

    public ExchangeApiException(string exchangeName, string message, Exception innerException)
        : base(message, innerException)
    {
        ExchangeName = exchangeName;
    }
}

/// <summary>
/// Exception for HTTP 503 Service Unavailable errors.
/// </summary>
public class Http503Exception : ExchangeApiException
{
    public Http503Exception(string exchangeName)
        : base(exchangeName, "Service temporarily unavailable (HTTP 503)")
    {
    }

    public Http503Exception(string exchangeName, string message)
        : base(exchangeName, message)
    {
    }

    public Http503Exception(string exchangeName, string message, Exception innerException)
        : base(exchangeName, message, innerException)
    {
    }
}

/// <summary>
/// Exception for HTTP 429 Rate Limit errors.
/// </summary>
public class RateLimitException : ExchangeApiException
{
    public TimeSpan? RetryAfter { get; }

    public RateLimitException(string exchangeName, TimeSpan? retryAfter = null)
        : base(exchangeName, "Rate limit exceeded")
    {
        RetryAfter = retryAfter;
    }

    public RateLimitException(string exchangeName, string message, TimeSpan? retryAfter = null)
        : base(exchangeName, message)
    {
        RetryAfter = retryAfter;
    }
}

/// <summary>
/// Exception for authentication/authorization errors (invalid API key, signature, etc.).
/// </summary>
public class AuthenticationException : ExchangeApiException
{
    public AuthenticationException(string exchangeName, string message)
        : base(exchangeName, message)
    {
    }

    public AuthenticationException(string exchangeName, string message, Exception innerException)
        : base(exchangeName, message, innerException)
    {
    }
}

/// <summary>
/// Exception for data parsing/deserialization errors.
/// </summary>
public class DataParsingException : ExchangeApiException
{
    public Type? TargetType { get; }

    public DataParsingException(string exchangeName, string message, Type? targetType = null)
        : base(exchangeName, message)
    {
        TargetType = targetType;
    }

    public DataParsingException(string exchangeName, string message, Exception innerException, Type? targetType = null)
        : base(exchangeName, message, innerException)
    {
        TargetType = targetType;
    }
}

/// <summary>
/// Exception for invalid symbol/ticker format.
/// </summary>
public class InvalidSymbolException : ExchangeApiException
{
    public string Symbol { get; }

    public InvalidSymbolException(string exchangeName, string symbol)
        : base(exchangeName, $"Invalid symbol: {symbol}")
    {
        Symbol = symbol;
    }

    public InvalidSymbolException(string exchangeName, string symbol, string message)
        : base(exchangeName, message)
    {
        Symbol = symbol;
    }
}
