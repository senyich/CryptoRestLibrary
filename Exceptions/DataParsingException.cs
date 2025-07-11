namespace CryptoExchangesRestLibrary.Exceptions;

public class DataParsingException : Exception
{
    public DataParsingException()
    {
    }
    public DataParsingException(string message)
        : base(message)
    {
    }
    public DataParsingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}