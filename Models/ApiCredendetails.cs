namespace CryptoExchangesRestLibrary.Models;

public class ApiCredendetails
{
    public string apiKey;
    public string apiSecret;
    public ApiCredendetails(string apiKey, string apiSecret)
    {
        this.apiKey = apiKey;
        this.apiSecret = apiSecret;
    }
}