using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using CryptoExchangesRestLibrary.Exceptions;
using CryptoExchangesRestLibrary.RestClients.Abstraction;
using CryptoExchangesRestLibrary.SerializationClasses.BingX;

namespace CryptoExchangesRestLibrary.RestClients;

/// <summary>
/// REST client for BingX exchange.
/// </summary>
public class BingXRestClient : ExchangeRestClient
{
    private const string Host = "https://open-api.bingx.com";
    private const string BaseWebsiteUrl = "https://bingx.com/en/spot/";

    public BingXRestClient() : base() { }
    public BingXRestClient(HttpClient client) : base(client) { }
    public BingXRestClient(int timeoutSeconds) : base(timeoutSeconds) { }

    protected override string GetExchangeName() => "BingX";

    public override string GetUrl(string symbol) => $"{BaseWebsiteUrl}{symbol.ToUpperInvariant()}";

    public override async Task<OrderbookResponse> GetOrderbookAsync(string symbol, int limit = 10, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeSymbol(symbol);
        var url = $"{Host}/openApi/swap/v2/quote/depth?symbol={normalized}&limit={limit}";

        var response = await SendWithRetryAsync(new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
        EnsureSuccessStatusCode(response, "fetch orderbook", normalized);

        var apiResponse = await ReadFromJsonAsync<BingXOrderbookResponse>(response, cancellationToken);
        if (apiResponse?.Data == null || apiResponse.Code != 0)
            throw new DataParsingException(GetExchangeName(), $"Failed to deserialize orderbook (code: {apiResponse?.Code})", typeof(BingXOrderbookResponse));

        return new OrderbookResponse(
            Symbol: DenormalizeSymbol(normalized),
            Asks: ConvertToOrderbookDictionary(apiResponse.Data.Asks),
            Bids: ConvertToOrderbookDictionary(apiResponse.Data.Bids)
        );
    }

    public override async Task<List<string>> GetSymbolsAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{Host}/openApi/swap/v2/quote/contracts";

        var response = await SendWithRetryAsync(new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
        EnsureSuccessStatusCode(response, "fetch symbols");

        var apiResponse = await ReadFromJsonAsync<BingXContractsResponse>(response, cancellationToken);
        if (apiResponse?.Data == null)
            throw new DataParsingException(GetExchangeName(), "Received null contracts list", typeof(BingXContractsResponse));

        return apiResponse.Data
            .Where(x => x.Status == 1)
            .Select(x => DenormalizeSymbol(x.Symbol))
            .ToList();
    }

    public override async Task<WithdrawalDataResponse> GetWithdrawalDataAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (_apiCredentials == null)
            throw new AuthenticationException(GetExchangeName(), "API credentials required for withdrawal data");

        var normalizedSymbol = NormalizeSymbolForWithdrawal(symbol);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var parameters = new Dictionary<string, string>
        {
            { "coin", normalizedSymbol },
            { "timestamp", timestamp.ToString() }
        };

        var queryString = BuildQueryString(parameters);
        var signature = GenerateSignature(queryString, _apiCredentials.ApiSecret);

        var url = $"{Host}/openApi/wallets/v1/capital/config/getall?{queryString}&signature={signature}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-BX-ApiKey", _apiCredentials.ApiKey);

        var response = await SendWithRetryAsync(request, cancellationToken);
        EnsureSuccessStatusCode(response, "fetch withdrawal data", symbol);

        var apiResponse = await ReadFromJsonAsync<BingXWithdrawalResponse>(response, cancellationToken);
        if (apiResponse?.Data == null || apiResponse.Data.Count == 0)
            throw new DataParsingException(GetExchangeName(), "No withdrawal data found", typeof(BingXWithdrawalResponse));

        var coinData = apiResponse.Data.FirstOrDefault(d =>
            d.Coin.Equals(normalizedSymbol, StringComparison.OrdinalIgnoreCase));

        if (coinData == null)
            throw new DataParsingException(GetExchangeName(), $"Coin data not found for symbol {symbol}", typeof(BingXCoinData));

        return new WithdrawalDataResponse(
            Symbol: coinData.Coin,
            Chains: coinData.NetworkList.Select(n => new BlockchainDataResponse(
                Name: n.Network,
                FullName: $"{coinData.Name} ({n.Network})",
                CanWithdraw: n.WithdrawEnable,
                CanDeposit: n.DepositEnable,
                Fee: ParseDecimalInvariant(n.WithdrawFee)
            )).ToList()
        );
    }

    private static string NormalizeSymbol(string symbol)
    {
        symbol = symbol.ToUpperInvariant();
        return symbol.EndsWith("USDT") ? symbol.Replace("USDT", "-USDT") : $"{symbol}-USDT";
    }

    private static string DenormalizeSymbol(string symbol) => symbol.Replace("-", "");

    private static string NormalizeSymbolForWithdrawal(string symbol)
    {
        symbol = symbol.ToUpperInvariant();
        return symbol.EndsWith("USDT") ? symbol.Replace("USDT", "") : symbol;
    }

    private static string BuildQueryString(Dictionary<string, string> parameters) =>
        string.Join("&", parameters
            .OrderBy(p => p.Key)
            .Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));

    private static string GenerateSignature(string data, string apiSecret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiSecret));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }
}
