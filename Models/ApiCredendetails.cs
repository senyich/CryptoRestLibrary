namespace CryptoExchangesRestLibrary.Models;

public class ApiCredendetails
{
    private string _apiKey;
    private string _apiSecret;
    public ApiCredendetails(string apiKey, string apiSecret)
    {
        _apiKey = apiKey;
        _apiSecret = apiSecret;
    }
}