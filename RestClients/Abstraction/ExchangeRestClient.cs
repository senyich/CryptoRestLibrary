namespace CryptoExchangesRestLibrary.RestClients.Abstraction;

public abstract class ExchangeRestClient
{
    protected readonly HttpClient _client;
    protected ApiCredentials? _apiCredentials;

    /// <summary>Base constructor — creates a new HttpClient with default timeout.</summary>
    protected ExchangeRestClient()
        : this(CreateClientWithTimeout(10))
    { }

    /// <summary>Constructor for injecting HttpClient (recommended with DI).</summary>
    protected ExchangeRestClient(HttpClient client)
    {
        _client = client;
        if (_client.Timeout == default)
            _client.Timeout = TimeSpan.FromSeconds(10);
    }

    /// <summary>Constructor with custom timeout.</summary>
    protected ExchangeRestClient(int timeoutSeconds)
        : this(CreateClientWithTimeout(timeoutSeconds))
    { }

    private static HttpClient CreateClientWithTimeout(int seconds) =>
        new HttpClient { Timeout = TimeSpan.FromSeconds(seconds) };

    /// <summary>Sets API credentials for authenticated requests.</summary>
    public void SetApiCredentials(string ApiKey, string ApiSecret) =>
        _apiCredentials = new ApiCredentials(ApiKey, ApiSecret);

    public abstract Task<OrderbookResponse> GetOrderbookAsync(string symbol, int limit = 10);
    public abstract Task<List<string>> GetSymbolsAsync();
    public abstract Task<WithdrawalDataResponse> GetWithdrawalDataAsync(string symbol);

    /// <summary>Returns an exchange-specific trading URL.</summary>
    public abstract string GetUrl(string symbol);
}

// ========= RECORD TYPES ==========

public record class OrderbookResponse(
    string Symbol,
    Dictionary<decimal, decimal> Asks,
    Dictionary<decimal, decimal> Bids
);

public record class WithdrawalDataResponse(
    string Symbol,
    List<BlockchainDataResponse> Chains
);

public record class BlockchainDataResponse(
    string Name,
    string FullName,
    bool CanWithdraw,
    bool CanDeposit,
    decimal Fee
);

/// <summary>Holds exchange API credentials (key + secret).</summary>
public record class ApiCredentials(string ApiKey, string ApiSecret);
