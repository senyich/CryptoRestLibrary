using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CryptoExchangesRestLibrary.RestClients.Abstraction;
using CryptoExchangesRestLibrary.SerializationClasses.BingX;

namespace CryptoExchangesRestLibrary.RestClients;

public class BingXRestClient : ExchangeRestClient
{
    private const string _host = "https://open-api.bingx.com";

    public BingXRestClient() : base() { }
    public BingXRestClient(HttpClient client) : base(client) { }
    public BingXRestClient(int timeoutSeconds) : base(timeoutSeconds) { }

    public override string GetUrl(string symbol) =>
        $"https://bingx.com/en/spot/{symbol}";

    public override async Task<OrderbookResponse> GetOrderbookAsync(string symbol, int limit = 10)
    {
        string normalized = NormalizeSymbol(symbol);

        string url = $"{_host}/openApi/swap/v2/quote/depth?symbol={normalized}&limit={limit}";
        var response = await _client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"[BingXRestClient] Failed to fetch orderbook ({response.StatusCode}) for symbol {normalized}");

        var apiResponse = await response.Content.ReadFromJsonAsync<BingXOrderbookResponse>();

        if (apiResponse?.Data == null || apiResponse.Code != 0)
            throw new JsonException(
                $"[BingXRestClient] Failed to deserialize orderbook for {normalized}");

        return new OrderbookResponse(
            Symbol: DenormalizeSymbol(normalized),
            Asks: ConvertToDictionary(apiResponse.Data.Asks),
            Bids: ConvertToDictionary(apiResponse.Data.Bids)
        );
    }

    public override async Task<List<string>> GetSymbolsAsync()
    {
        string url = $"{_host}/openApi/swap/v2/quote/contracts";
        var response = await _client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"[BingXRestClient] Failed to fetch symbols ({response.StatusCode})");

        var apiResponse = await response.Content.ReadFromJsonAsync<BingXContractsResponse>();

        if (apiResponse?.Data == null)
            throw new JsonException("[BingXRestClient] Failed to deserialize symbols list");

        return apiResponse.Data
            .Where(x => x.Status == 1)
            .Select(x => DenormalizeSymbol(x.Symbol))
            .ToList();
    }

    public override async Task<WithdrawalDataResponse> GetWithdrawalDataAsync(string symbol)
    {
        if (_apiCredentials == null)
            throw new InvalidOperationException("[BingXRestClient] API credentials required for withdrawal data");

        string normalizedSymbol = NormalizeSymbolForWithdrawal(symbol);
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var parameters = new Dictionary<string, string>
        {
            { "coin", normalizedSymbol },
            { "timestamp", timestamp.ToString() }
        };

        string queryString = BuildQueryString(parameters);
        string signature = GenerateSignature(queryString, _apiCredentials.ApiSecret);

        string url = $"{_host}/openApi/wallets/v1/capital/config/getall?{queryString}&signature={signature}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-BX-ApiKey", _apiCredentials.ApiKey);

        var response = await _client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"[BingXRestClient] Failed to fetch withdrawal data ({response.StatusCode}) for symbol {symbol}");

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<CoinData>>();

        if (apiResponse?.Data == null || apiResponse.Data.Count == 0)
            throw new JsonException(
                $"[BingXRestClient] No withdrawal data found for symbol {symbol}");

        var coinData = apiResponse.Data.FirstOrDefault(d =>
            d.Coin.Equals(normalizedSymbol, StringComparison.OrdinalIgnoreCase));

        if (coinData == null)
            throw new JsonException(
                $"[BingXRestClient] Coin data not found for symbol {symbol}");

        return new WithdrawalDataResponse(
            Symbol: coinData.Coin,
            Chains: coinData.NetworkList.Select(n => new BlockchainDataResponse(
                Name: n.Network,
                FullName: $"{coinData.Name} ({n.Network})",
                CanWithdraw: n.WithdrawEnable,
                CanDeposit: n.DepositEnable,
                Fee: decimal.Parse(n.WithdrawFee, CultureInfo.InvariantCulture)
            )).ToList()
        );
    }

    private static string NormalizeSymbol(string symbol)
    {
        symbol = symbol.ToUpperInvariant();
        return symbol.EndsWith("USDT") ? symbol.Replace("USDT", "-USDT") : $"{symbol}-USDT";
    }

    private static string DenormalizeSymbol(string symbol)
    {
        return symbol.Replace("-", "");
    }

    private static string NormalizeSymbolForWithdrawal(string symbol)
    {
        symbol = symbol.ToUpperInvariant();
        return symbol.EndsWith("USDT") ? symbol.Replace("USDT", "") : symbol;
    }

    private static string BuildQueryString(Dictionary<string, string> parameters)
    {
        return string.Join("&", parameters
            .OrderBy(p => p.Key)
            .Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
    }

    private static string GenerateSignature(string data, string ApiSecret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ApiSecret));
        byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
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