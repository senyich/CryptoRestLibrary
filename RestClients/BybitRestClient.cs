using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CryptoExchangesRestLibrary.RestClients.Abstraction;
using CryptoExchangesRestLibrary.SerializationClasses.Bybit;

namespace CryptoExchangesRestLibrary.RestClients;

public class BybitRestClient : ExchangeRestClient
{
    private const string Host = "https://api.bybit.com";

    public BybitRestClient() : base() { }
    public BybitRestClient(HttpClient client) : base(client) { }
    public BybitRestClient(int timeoutSeconds) : base(timeoutSeconds) { }

    public override string GetUrl(string symbol) =>
        $"https://www.bybit.com/en/trade/spot/{symbol.Replace("USDT", "/USDT")}";

    public override async Task<OrderbookResponse> GetOrderbookAsync(string symbol, int limit = 10)
    {
        string normalized = NormalizeSymbol(symbol);

        string url = $"{Host}/v5/market/orderbook?category=spot&symbol={normalized}&limit={limit}";
        var response = await _client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"[BybitRestClient] Failed to fetch orderbook ({response.StatusCode}) for symbol {normalized}");

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<BybitOrderbookInnerResult>>();

        if (apiResponse?.Result == null)
            throw new JsonException(
                $"[BybitRestClient] Failed to deserialize orderbook for {normalized}");

        return new OrderbookResponse(
            Symbol: normalized,
            Asks: ConvertToDictionary(apiResponse.Result.A),
            Bids: ConvertToDictionary(apiResponse.Result.B)
        );
    }

    public override async Task<List<string>> GetSymbolsAsync()
    {
        string url = $"{Host}/v5/market/tickers?category=spot";
        var response = await _client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"[BybitRestClient] Failed to fetch symbols ({response.StatusCode})");

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<BybitTickersResult>>();

        if (apiResponse?.Result?.List == null)
            throw new JsonException("[BybitRestClient] Failed to deserialize symbols list");

        return apiResponse.Result.List
            .Where(x => x.Symbol.Contains("USDT"))
            .Select(x => x.Symbol)
            .ToList();
    }

    public override async Task<WithdrawalDataResponse> GetWithdrawalDataAsync(string symbol)
    {
        if (_apiCredentials == null)
            throw new InvalidOperationException("[BybitRestClient] API credentials required for withdrawal data");

        string normalizedSymbol = NormalizeSymbolForWithdrawal(symbol);
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string recvWindow = "5000";
        string queryString = $"coin={normalizedSymbol}";

        string signature = GenerateSignature(timestamp, recvWindow, queryString);

        string url = $"{Host}/v5/asset/coin/query-info?{queryString}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-BAPI-SIGN", signature);
        request.Headers.Add("X-BAPI-API-KEY", _apiCredentials.ApiKey);
        request.Headers.Add("X-BAPI-TIMESTAMP", timestamp.ToString());
        request.Headers.Add("X-BAPI-RECV-WINDOW", recvWindow);

        var response = await _client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"[BybitRestClient] Failed to fetch withdrawal data ({response.StatusCode}) for symbol {symbol}");

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<CoinInfoResult>>();

        if (apiResponse?.Result?.Rows == null || apiResponse.RetCode != 0 || apiResponse.Result.Rows.Count == 0)
            throw new JsonException(
                $"[BybitRestClient] No withdrawal data found for symbol {symbol}");

        var coinData = apiResponse.Result.Rows[0];

        return new WithdrawalDataResponse(
            Symbol: coinData.Coin,
            Chains: coinData.Chains.Select(chain => new BlockchainDataResponse(
                Name: chain.Chain,
                FullName: chain.ChainType,
                CanWithdraw: chain.ChainWithdraw == "1",
                CanDeposit: chain.ChainDeposit == "1",
                Fee: decimal.Parse(chain.WithdrawFee, CultureInfo.InvariantCulture)
            )).ToList()
        );
    }

    private static string NormalizeSymbol(string symbol)
    {
        symbol = symbol.ToUpperInvariant();
        return symbol.EndsWith("USDT") ? symbol : $"{symbol}USDT";
    }

    private static string NormalizeSymbolForWithdrawal(string symbol)
    {
        symbol = symbol.ToUpperInvariant();
        return symbol.EndsWith("USDT") ? symbol.Replace("USDT", "") : symbol;
    }

    private string GenerateSignature(long timestamp, string recvWindow, string queryString)
    {
        string payload = $"{timestamp}{_apiCredentials.ApiKey}{recvWindow}{queryString}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiCredentials.ApiSecret));
        byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }

    private static Dictionary<decimal, decimal> ConvertToDictionary(List<List<string>> orders)
    {
        var dict = new Dictionary<decimal, decimal>(orders.Count);

        foreach (var entry in orders)
        {
            if (entry.Count < 2) continue;

            if (!decimal.TryParse(entry[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                continue;
            if (!decimal.TryParse(entry[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var quantity))
                continue;

            dict[price] = quantity;
        }

        return dict;
    }
}