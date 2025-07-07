using CryptoExchangesRestLibrary.Models;

namespace CryptoExchangesRestLibrary.RestClients.Abstraction;

public abstract class ExchangeRestClient
{
    protected HttpClient _client;
    protected ApiCredendetails _apiCredentials;
    public void SetApiCredentials(string apiKey, string apiSecret) => _apiCredentials = new ApiCredendetails(apiKey, apiSecret);
    public ExchangeRestClient()
    {
        _client = new HttpClient();        
    }

    public abstract Task<OrderbookResponce> GetOrderbookAsync(string symbol, int limit = 10);
    public abstract Task<List<string>> GetSymbolsAsync();
    public abstract Task<WithdrawalDataResponce> GetWithdrawalDataAsync(string symbol);
    public abstract string GetUrl(string symbol);
}

public record class OrderbookResponce(    
    string Symbol,
    Dictionary<decimal, decimal> Asks, 
    Dictionary<decimal, decimal> Bids
);
public record class WithdrawalDataResponce(
    string Symbol,
    List<BlockchainDataResponce> Chains
);
public record class BlockchainDataResponce(
    string Name,
    string FullName,
    bool CanWithdraw,
    bool CanDeposit,
    decimal Fee
);