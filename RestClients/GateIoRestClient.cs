using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CryptoExchangesRestLibrary.RestClients.Abstraction;
using CryptoExchangesRestLibrary.SerializationClasses.GateIo;

namespace CryptoExchangesRestLibrary.RestClients;

public class GateIoRestClient : ExchangeRestClient
{
    private const string _host = "https://api.gateio.ws";
    private const string _payloadSha512 = "cf83e1357eefb8bdf1542850d66d8007d620e4050b5715dc83f4a921d36ce9ce47d0d13c5d85f2b0ff8318d2877eec2f63b931bd47417a81a538327af927da3e";

    public GateIoRestClient() : base() { }
    public GateIoRestClient(HttpClient client) : base(client) { }
    public GateIoRestClient(int timeoutSeconds) : base(timeoutSeconds) { }

    public override string GetUrl(string symbol) =>
        $"https://www.gate.com/ru/trade/{symbol.Replace("USDT", "_USDT")}";

    public override async Task<OrderbookResponse> GetOrderbookAsync(string symbol, int limit = 10)
    {
        string normalized = NormalizeSymbol(symbol);

        string url = $"{_host}/api/v4/spot/order_book?currency_pair={normalized}&limit={limit}";
        var response = await _client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"[GateIoRestClient] Failed to fetch orderbook ({response.StatusCode}) for symbol {normalized}");

        var apiResponse = await response.Content.ReadFromJsonAsync<InnerGateIoOrderbookResponse>();

        if (apiResponse == null)
            throw new JsonException(
                $"[GateIoRestClient] Failed to deserialize orderbook for {normalized}");

        return new OrderbookResponse(
            Symbol: DenormalizeSymbol(normalized),
            Asks: ConvertToDictionary(apiResponse.Asks),
            Bids: ConvertToDictionary(apiResponse.Bids)
        );
    }

    public override async Task<List<string>> GetSymbolsAsync()
    {
        string url = $"{_host}/api/v4/spot/currency_pairs";
        var response = await _client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"[GateIoRestClient] Failed to fetch symbols ({response.StatusCode})");

        var apiResponse = await response.Content.ReadFromJsonAsync<List<GateIoSymbolResponse>>();

        if (apiResponse == null)
            throw new JsonException("[GateIoRestClient] Failed to deserialize symbols list");

        return apiResponse
            .Where(x => x.TradeStatus == "tradable")
            .Select(x => DenormalizeSymbol(x.Id))
            .ToList();
    }

    public override async Task<WithdrawalDataResponse> GetWithdrawalDataAsync(string symbol)
    {
        if (_apiCredentials == null)
            throw new InvalidOperationException("[GateIoRestClient] API credentials required for withdrawal data");

        string normalizedSymbol = NormalizeSymbolForWithdrawal(symbol);
        string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        string feeEndpoint = "/api/v4/wallet/fee";
        string query = $"currency={normalizedSymbol}";
        string signature = GenerateSignature("GET", feeEndpoint, query, timestamp);

        string feeUrl = $"{_host}{feeEndpoint}?{query}";
        var feeRequest = new HttpRequestMessage(HttpMethod.Get, feeUrl);
        feeRequest.Headers.Add("Accept", "application/json");
        feeRequest.Headers.Add("Timestamp", timestamp);
        feeRequest.Headers.Add("KEY", _apiCredentials.ApiKey);
        feeRequest.Headers.Add("SIGN", signature);

        string chainsEndpoint = $"/api/v4/wallet/currency_chains";
        string chainsUrl = $"{_host}{chainsEndpoint}?currency={normalizedSymbol}";

        var feeResponse = await _client.SendAsync(feeRequest);
        var chainsResponse = await _client.GetAsync(chainsUrl);

        if (!feeResponse.IsSuccessStatusCode || !chainsResponse.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"[GateIoRestClient] Failed to fetch withdrawal data for symbol {symbol}");

        var feeResult = await feeResponse.Content.ReadFromJsonAsync<GateIoFeeInfo>();
        var chainsResult = await chainsResponse.Content.ReadFromJsonAsync<List<GateIoChainsResult>>();

        if (feeResult == null || chainsResult == null)
            throw new JsonException(
                $"[GateIoRestClient] Failed to deserialize withdrawal data for symbol {symbol}");

        decimal fee = Math.Abs(decimal.Parse(feeResult.DeliveryMakerFee, CultureInfo.InvariantCulture));

        var chains = chainsResult.Select(chain => new BlockchainDataResponse(
            Name: chain.Chain,
            FullName: chain.NameEn,
            CanDeposit: chain.IsDepositDisabled != 1,
            CanWithdraw: chain.IsWihdrawDisabled != 1,
            Fee: fee
        )).ToList();

        return new WithdrawalDataResponse(
            Symbol: normalizedSymbol,
            Chains: chains
        );
    }

    private static string NormalizeSymbol(string symbol)
    {
        symbol = symbol.ToUpperInvariant();
        return symbol.EndsWith("USDT") ? symbol.Replace("USDT", "_USDT") : $"{symbol}_USDT";
    }

    private static string DenormalizeSymbol(string symbol)
    {
        return symbol.Replace("_", "");
    }

    private static string NormalizeSymbolForWithdrawal(string symbol)
    {
        symbol = symbol.ToUpperInvariant();
        return symbol.EndsWith("USDT") ? symbol.Replace("USDT", "") : symbol;
    }

    private string GenerateSignature(string method, string url, string queryString, string timestamp)
    {
        string payload = $"{method}\n{url}\n{queryString}\n{_payloadSha512}\n{timestamp}";
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_apiCredentials.ApiSecret));
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