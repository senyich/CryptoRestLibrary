namespace CryptoExchangesRestLibrary.RestClients.Abstraction;

/// <summary>
/// Interface for cryptocurrency exchange REST clients.
/// Defines common operations for fetching orderbooks, symbols, and withdrawal data.
/// </summary>
public interface IExchangeClient
{
    /// <summary>
    /// Fetches the orderbook for a trading pair.
    /// </summary>
    /// <param name="symbol">Trading pair symbol (e.g., "BTCUSDT")</param>
    /// <param name="limit">Depth limit (exchange-specific)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Orderbook with bids and asks</returns>
    Task<OrderbookResponse> GetOrderbookAsync(string symbol, int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches list of available trading symbols.
    /// </summary>
    /// <returns>List of symbol names</returns>
    Task<List<string>> GetSymbolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches withdrawal data for a cryptocurrency.
    /// Requires API credentials to be set.
    /// </summary>
    /// <param name="symbol">Asset symbol (e.g., "BTC")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Withdrawal data with blockchain/network information</returns>
    Task<WithdrawalDataResponse> GetWithdrawalDataAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns an exchange-specific URL for trading the specified symbol.
    /// </summary>
    /// <param name="symbol">Trading pair symbol</param>
    /// <returns>Trading page URL</returns>
    string GetUrl(string symbol);
}

/// <summary>
/// Interface for exchange clients that support API credentials.
/// </summary>
public interface IAuthenticatedExchangeClient : IExchangeClient
{
    /// <summary>
    /// Sets API credentials for authenticated requests.
    /// </summary>
    /// <param name="apiKey">API key</param>
    /// <param name="apiSecret">API secret</param>
    void SetApiCredentials(string apiKey, string apiSecret);
}
