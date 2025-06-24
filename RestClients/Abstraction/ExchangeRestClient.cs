using CryptoExchangesRestLibrary.Models;

namespace CryptoExchangesRestLibrary.RestClients.Abstraction;

public abstract class ExchangeRestClient
{
    protected HttpClient _client;
    protected ApiCredendetails _apiCredendetails;
    public void SetApiCredentials(string apiKey, string apiSecret) => _apiCredendetails = new ApiCredendetails(apiKey, apiSecret);
    public ExchangeRestClient()
    {
        _client = new HttpClient();        
    }

    public abstract Task<OrderbookResponce> GetOrderbookAsync(string symbol, int limit = 10);
    public abstract Task<List<string>> GetSymbolsAsync();
}

public record class OrderbookResponce(    
    string Symbol,
    Dictionary<decimal, decimal> Asks, 
    Dictionary<decimal, decimal> Bids
);