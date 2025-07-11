namespace CryptoExchangesRestLibrary.Exceptions;

public class Http503Exception : Exception
{
    public Http503Exception()
    {
    }

    public Http503Exception(string message)
        : base(message)
    {
    }

    public Http503Exception(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}