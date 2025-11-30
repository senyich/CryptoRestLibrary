using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CryptoExchangesRestLibrary.RestClients.Abstraction;
using CryptoExchangesRestLibrary.SerializationClasses.Mexc;

namespace CryptoExchangesRestLibrary.RestClients;

public class MexcRestClient : ExchangeRestClient
{
    private const string Host = "https://api.mexc.com";

    public MexcRestClient() : base() { }
    public MexcRestClient(HttpClient client) : base(client) { }
    public MexcRestClient(int timeoutSeconds) : base(timeoutSeconds) { }

    public override string GetUrl(string symbol) =>
        $"https://www.mexc.com/ru-RU/exchange/{symbol.Replace("USDT", "_USDT")}";

    public override async Task<OrderbookResponse> GetOrderbookAsync(string symbol, int limit = 10)
    {
        string normalized = NormalizeSymbol(symbol);

        string url = $"{Host}/api/v3/depth?symbol={normalized}&limit={limit}";
        var response = await _client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"[MexcRestClient] Failed to fetch orderbook ({response.StatusCode}) for symbol {normalized}");

        var apiResponse = await response.Content.ReadFromJsonAsync<InnerMexcOrderbookResponse>();

        if (apiResponse == null)
            throw new JsonException(
                $"[MexcRestClient] Failed to deserialize orderbook for {normalized}");

        return new OrderbookResponse(
            Symbol: normalized,
            Asks: ConvertToDictionary(apiResponse.Asks),
            Bids: ConvertToDictionary(apiResponse.Bids)
        );
    }

    public override async Task<List<string>> GetSymbolsAsync()
    {
        string url = $"{Host}/api/v3/defaultSymbols";
        var response = await _client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"[MexcRestClient] Failed to fetch symbols ({response.StatusCode})");

        var apiResponse = await response.Content.ReadFromJsonAsync<MexcSymbolsResponse>();

        if (apiResponse?.Data == null)
            throw new JsonException("[MexcRestClient] Failed to deserialize symbols list");

        return apiResponse.Data;
    }

    public override async Task<WithdrawalDataResponse> GetWithdrawalDataAsync(string symbol)
    {
        if (_apiCredentials == null)
            throw new InvalidOperationException("[MexcRestClient] API credentials required for withdrawal data");

        string normalizedSymbol = NormalizeSymbolForWithdrawal(symbol);
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        string queryString = $"coin={normalizedSymbol}&timestamp={timestamp}";
        string signature = GenerateSignature(queryString);

        string url = $"{Host}/api/v3/capital/config/getall?{queryString}&signature={signature}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-MEXC-APIKEY", _apiCredentials.ApiKey);

        var response = await _client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"[MexcRestClient] Failed to fetch withdrawal data ({response.StatusCode}) for symbol {symbol}");

        var apiResponse = await response.Content.ReadFromJsonAsync<List<CoinConfigApiResponse>>();

        if (apiResponse == null || apiResponse.Count == 0)
            throw new JsonException(
                $"[MexcRestClient] No withdrawal data found for symbol {symbol}");

        var coinConfig = apiResponse.FirstOrDefault(c =>
            c.Coin.Equals(normalizedSymbol, StringComparison.OrdinalIgnoreCase));

        if (coinConfig?.NetworkList == null)
            throw new JsonException(
                $"[MexcRestClient] No valid coin configuration found for symbol {symbol}");

        return new WithdrawalDataResponse(
            Symbol: coinConfig.Coin,
            Chains: coinConfig.NetworkList.Select(network => new BlockchainDataResponse(
                Name: network.Network,
                FullName: network.Name,
                CanWithdraw: network.WithdrawEnable,
                CanDeposit: network.DepositEnable,
                Fee: decimal.Parse(network.WithdrawFee, CultureInfo.InvariantCulture)
            )).ToList()
        );
    }

    private static string NormalizeSymbol(string symbol)
    {
        symbol = symbol.ToUpperInvariant();
        return symbol.Contains("USDT") ? symbol : $"{symbol}USDT";
    }

    private static string NormalizeSymbolForWithdrawal(string symbol)
    {
        symbol = symbol.ToUpperInvariant();
        return symbol.Replace("USDT", "");
    }

    private string GenerateSignature(string queryString)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiCredentials.ApiSecret));
        byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(queryString));
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