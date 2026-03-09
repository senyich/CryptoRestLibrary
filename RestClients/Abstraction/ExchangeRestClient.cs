using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CryptoExchangesRestLibrary.Exceptions;

namespace CryptoExchangesRestLibrary.RestClients.Abstraction;

/// <summary>
/// Base class for all exchange REST clients.
/// Provides common functionality for HTTP requests, error handling, and retry policies.
/// </summary>
public abstract class ExchangeRestClient : IAuthenticatedExchangeClient
{
    protected readonly HttpClient _client;
    protected ApiCredentials? _apiCredentials;
    protected static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    protected static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
    protected static readonly int MaxRetries = 3;
    protected static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(100);

    /// <summary>Base constructor — creates a new HttpClient with default timeout.</summary>
    protected ExchangeRestClient()
        : this(CreateClientWithTimeout(DefaultTimeout))
    { }

    /// <summary>Constructor for injecting HttpClient (recommended with DI).</summary>
    protected ExchangeRestClient(HttpClient client)
    {
        _client = client;
        if (_client.Timeout == default)
            _client.Timeout = DefaultTimeout;
    }

    /// <summary>Constructor with custom timeout.</summary>
    protected ExchangeRestClient(TimeSpan timeout)
        : this(CreateClientWithTimeout(timeout))
    { }

    /// <summary>Constructor with custom timeout in seconds.</summary>
    protected ExchangeRestClient(int timeoutSeconds)
        : this(CreateClientWithTimeout(TimeSpan.FromSeconds(timeoutSeconds)))
    { }

    private static HttpClient CreateClientWithTimeout(TimeSpan timeout) =>
        new HttpClient { Timeout = timeout };

    /// <summary>Sets API credentials for authenticated requests.</summary>
    public void SetApiCredentials(string apiKey, string apiSecret) =>
        _apiCredentials = new ApiCredentials(apiKey, apiSecret);

    /// <summary>Sets API credentials including passphrase (for exchanges that require it, e.g., KuCoin).</summary>
    public void SetApiCredentials(string apiKey, string apiSecret, string passPhrase) =>
        _apiCredentials = new ApiCredentials(apiKey, apiSecret, passPhrase);

    /// <summary>
    /// Sends HTTP request with automatic retry on transient failures.
    /// </summary>
    protected async Task<HttpResponseMessage> SendWithRetryAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        var lastException = default(Exception);

        while (attempt < MaxRetries)
        {
            try
            {
                var response = await _client.SendAsync(request, cancellationToken);

                if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    throw new Http503Exception(GetExchangeName(), $"Service unavailable. Attempt {attempt + 1}/{MaxRetries}");
                }

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta
                        ?? response.Headers.RetryAfter?.Date?.Subtract(DateTimeOffset.UtcNow)
                        ?? RetryDelay;
                    throw new RateLimitException(GetExchangeName(), $"Rate limit exceeded. Attempt {attempt + 1}/{MaxRetries}", retryAfter);
                }

                return response;
            }
            catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout)
            {
                lastException = ex;
                attempt++;
                if (attempt >= MaxRetries) break;
                await Task.Delay(RetryDelay * attempt, cancellationToken);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                lastException = ex;
                attempt++;
                if (attempt >= MaxRetries) break;
                await Task.Delay(RetryDelay * attempt, cancellationToken);
            }
        }

        throw lastException ?? new HttpRequestException($"Failed after {MaxRetries} attempts");
    }

    /// <summary>
    /// Reads and deserializes JSON response with error handling.
    /// </summary>
    protected async Task<T?> ReadFromJsonAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<T>(DefaultJsonOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new DataParsingException(GetExchangeName(), $"Failed to deserialize response to {typeof(T).Name}", ex, typeof(T));
        }
    }

    /// <summary>
    /// Ensures response is successful, throws appropriate exception otherwise.
    /// </summary>
    protected void EnsureSuccessStatusCode(HttpResponseMessage response, string operation, string? symbol = null)
    {
        if (!response.IsSuccessStatusCode)
        {
            var symbolInfo = symbol != null ? $" for symbol {symbol}" : string.Empty;
            var message = $"Failed to {operation}{symbolInfo} (HTTP {(int)response.StatusCode} {response.StatusCode})";

            throw response.StatusCode switch
            {
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                    new AuthenticationException(GetExchangeName(), message),
                HttpStatusCode.ServiceUnavailable =>
                    new Http503Exception(GetExchangeName(), message),
                HttpStatusCode.TooManyRequests =>
                    new RateLimitException(GetExchangeName(), message),
                _ => new ExchangeApiException(GetExchangeName(), message, (int)response.StatusCode)
            };
        }
    }

    /// <summary>Returns exchange name for error messages.</summary>
    protected virtual string GetExchangeName() => GetType().Name.Replace("RestClient", "");

    /// <summary>Normalizes a string to invariant culture (useful for decimal parsing).</summary>
    protected decimal ParseDecimalInvariant(string value) =>
        decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result)
            ? result
            : throw new DataParsingException(GetExchangeName(), $"Failed to parse decimal value: {value}");

    /// <summary>Converts a list of string pairs to a dictionary of decimals.</summary>
    protected Dictionary<decimal, decimal> ConvertToOrderbookDictionary(List<List<string>> orders)
    {
        var dict = new Dictionary<decimal, decimal>(orders.Count);

        foreach (var entry in orders)
        {
            if (entry.Count < 2) continue;

            var price = ParseDecimalInvariant(entry[0]);
            var quantity = ParseDecimalInvariant(entry[1]);

            dict[price] = quantity;
        }

        return dict;
    }
    /// <summary>Fetches orderbook for the specified symbol.</summary>
    /// <param name="symbol">Trading pair symbol (e.g., "BTCUSDT")</param>
    /// <param name="limit">Depth limit (exchange-specific)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public abstract Task<OrderbookResponse> GetOrderbookAsync(string symbol, int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>Fetches list of available trading symbols.</summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public abstract Task<List<string>> GetSymbolsAsync(CancellationToken cancellationToken = default);

    /// <summary>Fetches withdrawal data for the specified symbol (requires API credentials).</summary>
    /// <param name="symbol">Asset symbol (e.g., "BTC")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public abstract Task<WithdrawalDataResponse> GetWithdrawalDataAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>Returns an exchange-specific trading URL.</summary>
    /// <param name="symbol">Trading pair symbol</param>
    public abstract string GetUrl(string symbol);
}

/// <summary>Represents an orderbook response from an exchange.</summary>
public record class OrderbookResponse(
    string Symbol,
    Dictionary<decimal, decimal> Asks,
    Dictionary<decimal, decimal> Bids
);

/// <summary>Represents withdrawal data response with blockchain information.</summary>
public record class WithdrawalDataResponse(
    string Symbol,
    List<BlockchainDataResponse> Chains
);

/// <summary>Represents blockchain/chain data for withdrawals.</summary>
public record class BlockchainDataResponse(
    string Name,
    string FullName,
    bool CanWithdraw,
    bool CanDeposit,
    decimal Fee
);

/// <summary>Holds exchange API credentials (key + secret, optional passphrase).</summary>
public record class ApiCredentials(
    string ApiKey,
    string ApiSecret,
    string? PassPhrase = null
);
