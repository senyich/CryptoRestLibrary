using CryptoExchangesRestLibrary.Exceptions;
using CryptoExchangesRestLibrary.RestClients.Abstraction;
using CryptoExchangesRestLibrary.SerializationClasses;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

namespace CryptoExchangesRestLibrary.RestClients;

/// <summary>
/// REST client for Binance exchange.
/// </summary>
public class BinanceRestClient : ExchangeRestClient
{
    private const string Host = "https://api.binance.com";
    private const string BaseWebsiteUrl = "https://www.binance.com/ru/trade/";

    public BinanceRestClient() : base() { }
    public BinanceRestClient(HttpClient client) : base(client) { }
    public BinanceRestClient(int timeoutSeconds) : base(timeoutSeconds) { }

    protected override string GetExchangeName() => "Binance";

    public override string GetUrl(string symbol)
    {
        var formattedSymbol = symbol.ToUpperInvariant().Replace("USDT", "_USDT");
        return $"{BaseWebsiteUrl}{formattedSymbol}";
    }

    public override async Task<OrderbookResponse> GetOrderbookAsync(string symbol, int limit = 10, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeSymbol(symbol);
        var url = $"{Host}/api/v3/depth?symbol={normalized}&limit={limit}";

        var response = await SendWithRetryAsync(new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
        EnsureSuccessStatusCode(response, "fetch orderbook", normalized);

        var raw = await ReadFromJsonAsync<BinanceOrderbookResponse>(response, cancellationToken);
        if (raw == null)
            throw new DataParsingException(GetExchangeName(), "Received null orderbook response", typeof(BinanceOrderbookResponse));

        return new OrderbookResponse(
            Symbol: normalized,
            Asks: ConvertToOrderbookDictionary(raw.Asks),
            Bids: ConvertToOrderbookDictionary(raw.Bids)
        );
    }

    public override async Task<List<string>> GetSymbolsAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{Host}/api/v3/ticker/price";

        var response = await SendWithRetryAsync(new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
        EnsureSuccessStatusCode(response, "fetch symbols");

        var tickers = await ReadFromJsonAsync<List<BinanceTickerResponse>>(response, cancellationToken);
        if (tickers == null)
            throw new DataParsingException(GetExchangeName(), "Received null symbols list", typeof(List<BinanceTickerResponse>));

        return tickers
            .Select(t => t.Symbol)
            .Where(s => s.EndsWith("USDT", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public override async Task<WithdrawalDataResponse> GetWithdrawalDataAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeSymbol(symbol);
        var url = $"{Host}/sapi/v1/capital/config/getall";
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string queryString = $"timestamp={timestamp}";
        if (_apiCredentials == null)
            throw new AuthenticationException(GetExchangeName(), "API credentials required for withdrawal data");
        string signature = GetSignature(queryString, _apiCredentials.ApiSecret);
        string fullQuery = $"{queryString}&signature={signature}";
        var request = new HttpRequestMessage(HttpMethod.Get, $"{url}?{fullQuery}");
        request.Headers.Add("X-MBX-APIKEY", _apiCredentials.ApiKey);
        var response = await SendWithRetryAsync(request, cancellationToken);
        EnsureSuccessStatusCode(response, "fetch orderbook", normalized);
        Console.WriteLine(response.Content.ReadAsStringAsync());
        throw new NotImplementedException("binance not implemetned");
    }
    private string GetSignature(string queryString, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(queryString));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    private static string NormalizeSymbol(string symbol)
    {
        symbol = symbol.ToUpperInvariant();
        return symbol.EndsWith("USDT") ? symbol : $"{symbol}USDT";
    }
}
