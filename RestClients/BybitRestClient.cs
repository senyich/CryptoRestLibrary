using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CryptoExchangesRestLibrary.Exceptions;
using CryptoExchangesRestLibrary.RestClients.Abstraction;
using CryptoExchangesRestLibrary.SerializationClasses.Bybit;

namespace CryptoExchangesRestLibrary.RestClients;

/// <summary>
/// REST client for Bybit exchange.
/// </summary>
public class BybitRestClient : ExchangeRestClient
{
    private const string Host = "https://api.bybit.com";
    private const string BaseWebsiteUrl = "https://www.bybit.com/en/trade/spot/";

    public BybitRestClient() : base() { }
    public BybitRestClient(HttpClient client) : base(client) { }
    public BybitRestClient(int timeoutSeconds) : base(timeoutSeconds) { }

    protected override string GetExchangeName() => "Bybit";

    public override string GetUrl(string symbol) =>
        $"{BaseWebsiteUrl}{symbol.ToUpperInvariant().Replace("USDT", "/USDT")}";

    public override async Task<OrderbookResponse> GetOrderbookAsync(string symbol, int limit = 10, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeSymbol(symbol);
        var url = $"{Host}/v5/market/orderbook?category=spot&symbol={normalized}&limit={limit}";

        var response = await SendWithRetryAsync(new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
        EnsureSuccessStatusCode(response, "fetch orderbook", normalized);

        var apiResponse = await ReadFromJsonAsync<BybitApiResponse<BybitOrderbookResult>>(response, cancellationToken);
        if (apiResponse?.Result == null || apiResponse.RetCode != 0)
            throw new DataParsingException(
                GetExchangeName(),
                $"Failed to deserialize orderbook (retCode: {apiResponse?.RetCode}, retMsg: {apiResponse?.RetMsg})",
                typeof(BybitOrderbookResult));

        return new OrderbookResponse(
            Symbol: normalized,
            Asks: ConvertToOrderbookDictionary(apiResponse.Result.Asks),
            Bids: ConvertToOrderbookDictionary(apiResponse.Result.Bids)
        );
    }

    public override async Task<List<string>> GetSymbolsAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{Host}/v5/market/tickers?category=spot";

        var response = await SendWithRetryAsync(new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
        EnsureSuccessStatusCode(response, "fetch symbols");

        var apiResponse = await ReadFromJsonAsync<BybitApiResponse<BybitTickersResult>>(response, cancellationToken);
        if (apiResponse?.Result?.List == null || apiResponse.RetCode != 0)
            throw new DataParsingException(
                GetExchangeName(),
                $"Failed to deserialize symbols (retCode: {apiResponse?.RetCode}, retMsg: {apiResponse?.RetMsg})",
                typeof(BybitTickersResult));

        return apiResponse.Result.List
            .Where(x => x.Symbol.Contains("USDT"))
            .Select(x => x.Symbol)
            .ToList();
    }

    public override async Task<WithdrawalDataResponse> GetWithdrawalDataAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (_apiCredentials == null)
            throw new AuthenticationException(GetExchangeName(), "API credentials required for withdrawal data");

        var normalizedSymbol = NormalizeSymbolForWithdrawal(symbol);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        const string recvWindow = "5000";
        var queryString = $"coin={normalizedSymbol}";

        var signature = GenerateSignature(timestamp, recvWindow, queryString);

        var url = $"{Host}/v5/asset/coin/query-info?{queryString}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-BAPI-SIGN", signature);
        request.Headers.Add("X-BAPI-API-KEY", _apiCredentials.ApiKey);
        request.Headers.Add("X-BAPI-TIMESTAMP", timestamp.ToString());
        request.Headers.Add("X-BAPI-RECV-WINDOW", recvWindow);

        var response = await SendWithRetryAsync(request, cancellationToken);
        EnsureSuccessStatusCode(response, "fetch withdrawal data", symbol);

        var apiResponse = await ReadFromJsonAsync<BybitApiResponse<BybitCoinInfoResult>>(response, cancellationToken);
        if (apiResponse?.Result?.Rows == null || apiResponse.RetCode != 0 || apiResponse.Result.Rows.Count == 0)
            throw new DataParsingException(GetExchangeName(), "No withdrawal data found", typeof(BybitCoinInfoResult));

        var coinData = apiResponse.Result.Rows[0];

        return new WithdrawalDataResponse(
            Symbol: coinData.Coin,
            Chains: coinData.Chains?.Select(chain => new BlockchainDataResponse(
                Name: chain.Chain,
                FullName: chain.ChainType,
                CanWithdraw: chain.ChainWithdraw == "1",
                CanDeposit: chain.ChainDeposit == "1",
                Fee: ParseDecimalInvariant(chain.WithdrawFee)
            )).ToList() ?? []
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
        var payload = $"{timestamp}{_apiCredentials!.ApiKey}{recvWindow}{queryString}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiCredentials.ApiSecret));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }
}
