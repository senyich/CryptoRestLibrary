using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CryptoExchangesRestLibrary.Exceptions;
using CryptoExchangesRestLibrary.RestClients.Abstraction;
using CryptoExchangesRestLibrary.SerializationClasses.Mexc;

namespace CryptoExchangesRestLibrary.RestClients;

/// <summary>
/// REST client for MEXC exchange.
/// </summary>
public class MexcRestClient : ExchangeRestClient
{
    private const string Host = "https://api.mexc.com";
    private const string BaseWebsiteUrl = "https://www.mexc.com/ru-RU/exchange/";

    public MexcRestClient() : base() { }
    public MexcRestClient(HttpClient client) : base(client) { }
    public MexcRestClient(int timeoutSeconds) : base(timeoutSeconds) { }

    protected override string GetExchangeName() => "Mexc";

    public override string GetUrl(string symbol) =>
        $"{BaseWebsiteUrl}{symbol.ToUpperInvariant().Replace("USDT", "_USDT")}";

    public override async Task<OrderbookResponse> GetOrderbookAsync(string symbol, int limit = 10, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeSymbol(symbol);
        var url = $"{Host}/api/v3/depth?symbol={normalized}&limit={limit}";

        var response = await SendWithRetryAsync(new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
        EnsureSuccessStatusCode(response, "fetch orderbook", normalized);

        var apiResponse = await ReadFromJsonAsync<MexcOrderbookResponse>(response, cancellationToken);
        if (apiResponse == null)
            throw new DataParsingException(GetExchangeName(), "Received null orderbook response", typeof(MexcOrderbookResponse));

        return new OrderbookResponse(
            Symbol: normalized,
            Asks: ConvertToOrderbookDictionary(apiResponse.Asks),
            Bids: ConvertToOrderbookDictionary(apiResponse.Bids)
        );
    }

    public override async Task<List<string>> GetSymbolsAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{Host}/api/v3/defaultSymbols";

        var response = await SendWithRetryAsync(new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
        EnsureSuccessStatusCode(response, "fetch symbols");

        var apiResponse = await ReadFromJsonAsync<MexcSymbolsResponse>(response, cancellationToken);
        if (apiResponse?.Data == null)
            throw new DataParsingException(GetExchangeName(), "Received null symbols list", typeof(MexcSymbolsResponse));

        return apiResponse.Data;
    }

    public override async Task<WithdrawalDataResponse> GetWithdrawalDataAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (_apiCredentials == null)
            throw new AuthenticationException(GetExchangeName(), "API credentials required for withdrawal data");

        var normalizedSymbol = NormalizeSymbolForWithdrawal(symbol);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var queryString = $"coin={normalizedSymbol}&timestamp={timestamp}";
        var signature = GenerateSignature(queryString);

        var url = $"{Host}/api/v3/capital/config/getall?{queryString}&signature={signature}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-MEXC-APIKEY", _apiCredentials.ApiKey);

        var response = await SendWithRetryAsync(request, cancellationToken);
        EnsureSuccessStatusCode(response, "fetch withdrawal data", symbol);

        var apiResponse = await ReadFromJsonAsync<List<MexcCoinConfigResponse>>(response, cancellationToken);
        if (apiResponse == null || apiResponse.Count == 0)
            throw new DataParsingException(GetExchangeName(), "No withdrawal data found", typeof(List<MexcCoinConfigResponse>));

        var coinConfig = apiResponse.FirstOrDefault(c =>
            c.Coin.Equals(normalizedSymbol, StringComparison.OrdinalIgnoreCase));

        if (coinConfig?.NetworkList == null)
            throw new DataParsingException(GetExchangeName(), $"No valid coin configuration found for symbol {symbol}", typeof(MexcCoinConfigResponse));

        return new WithdrawalDataResponse(
            Symbol: coinConfig.Coin,
            Chains: coinConfig.NetworkList.Select(network => new BlockchainDataResponse(
                Name: network.Network,
                FullName: network.Name,
                CanWithdraw: network.WithdrawEnable,
                CanDeposit: network.DepositEnable,
                Fee: ParseDecimalInvariant(network.WithdrawFee)
            )).ToList()
        );
    }

    private static string NormalizeSymbol(string symbol)
    {
        symbol = symbol.ToUpperInvariant();
        return symbol.Contains("USDT") ? symbol : $"{symbol}USDT";
    }

    private static string NormalizeSymbolForWithdrawal(string symbol) =>
        symbol.ToUpperInvariant().Replace("USDT", "");

    private string GenerateSignature(string queryString)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiCredentials!.ApiSecret));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(queryString));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }
}
