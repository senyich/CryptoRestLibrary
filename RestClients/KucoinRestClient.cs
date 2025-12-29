using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CryptoExchangesRestLibrary.RestClients.Abstraction;
using CryptoExchangesRestLibrary.SerializationClasses.Kucoin;

namespace CryptoExchangesRestLibrary.RestClients;

public class KucoinRestClient : ExchangeRestClient
{
    private const string _host = "https://api.kucoin.com";
    private string _passPhrase = null;

    public KucoinRestClient() : base() { }
    public KucoinRestClient(HttpClient client) : base(client) { }
    public KucoinRestClient(int timeoutSeconds) : base(timeoutSeconds) { }

    public void SetPassPhrase(string passPhrase) => _passPhrase = passPhrase;

    public override string GetUrl(string symbol) =>
        $"https://www.kucoin.com/trade/{symbol.Replace("USDT", "-USDT")}";

    public override async Task<OrderbookResponse> GetOrderbookAsync(string symbol, int limit = 10)
    {
        limit = 20;
        string normalized = NormalizeSymbol(symbol);

        string url = $"{_host}/api/v1/market/orderbook/level2_{limit}?symbol={normalized}";
        var response = await _client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"[KucoinRestClient] Failed to fetch orderbook ({response.StatusCode}) for symbol {normalized}");

        var apiResponse = await response.Content.ReadFromJsonAsync<KucoinOrderbookResponse>();

        if (apiResponse?.Data == null)
            throw new JsonException(
                $"[KucoinRestClient] Failed to deserialize orderbook for {normalized}");

        return new OrderbookResponse(
            Symbol: normalized,
            Asks: ConvertToDictionary(apiResponse.Data.Asks),
            Bids: ConvertToDictionary(apiResponse.Data.Bids)
        );
    }

    public override async Task<List<string>> GetSymbolsAsync()
    {
        throw new NotImplementedException();
    }

    public override async Task<WithdrawalDataResponse> GetWithdrawalDataAsync(string symbol)
    {
        if (_apiCredentials == null)
            throw new InvalidOperationException("[KucoinRestClient] API credentials required for withdrawal data");

        if (string.IsNullOrEmpty(_passPhrase))
            throw new InvalidOperationException("[KucoinRestClient] Passphrase required for withdrawal data");

        string normalizedSymbol = NormalizeSymbolForWithdrawal(symbol);

        string path = $"/api/v1/withdrawals/quotas?currency={normalizedSymbol}";
        string url = $"{_host}{path}";

        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string method = "GET";

        string signature = GenerateSignature(timestamp, method, path);
        string signedPassphrase = GeneratePassphraseSignature();

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("KC-API-KEY", _apiCredentials.ApiKey);
        request.Headers.Add("KC-API-SIGN", signature);
        request.Headers.Add("KC-API-TIMESTAMP", timestamp.ToString());
        request.Headers.Add("KC-API-PASSPHRASE", signedPassphrase);
        request.Headers.Add("KC-API-KEY-VERSION", "2");

        var response = await _client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"[KucoinRestClient] Failed to fetch withdrawal data ({response.StatusCode}) for symbol {symbol}: {errorContent}");
        }

        var apiResponse = await response.Content.ReadFromJsonAsync<KucoinWithdrawalLimitResponse>();

        if (apiResponse?.Data == null)
            throw new JsonException(
                $"[KucoinRestClient] Failed to deserialize withdrawal data for symbol {symbol}");

        return new WithdrawalDataResponse(
            Symbol: apiResponse.Data.Currency,
            Chains: new List<BlockchainDataResponse>
            {
                new BlockchainDataResponse(
                    Name: apiResponse.Data.Chain,
                    FullName: apiResponse.Data.Chain,
                    CanWithdraw: apiResponse.Data.IsWithdrawEnabled,
                    CanDeposit: true, // Assuming deposit is always enabled as in original
                    Fee: decimal.Parse(apiResponse.Data.WithdrawMinFee, CultureInfo.InvariantCulture)
                )
            }
        );
    }

    private static string NormalizeSymbol(string symbol)
    {
        symbol = symbol.ToUpperInvariant();
        return symbol.Contains("USDT") ? symbol.Replace("USDT", "-USDT") : $"{symbol}-USDT";
    }

    private static string NormalizeSymbolForWithdrawal(string symbol)
    {
        symbol = symbol.ToUpperInvariant();
        return symbol.Replace("USDT", "").Replace("-", "");
    }

    private string GenerateSignature(long timestamp, string method, string path)
    {
        string strToSign = $"{timestamp}{method}{path}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiCredentials.ApiSecret));
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(strToSign));
        return Convert.ToBase64String(hash);
    }

    private string GeneratePassphraseSignature()
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiCredentials.ApiSecret));
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(_passPhrase));
        return Convert.ToBase64String(hash);
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

