using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CryptoExchangesRestLibrary.Exceptions;
using CryptoExchangesRestLibrary.RestClients.Abstraction;
using CryptoExchangesRestLibrary.SerializationClasses.GateIo;

namespace CryptoExchangesRestLibrary.RestClients;

/// <summary>
/// REST client for Gate.io exchange.
/// </summary>
public class GateIoRestClient : ExchangeRestClient
{
    private const string Host = "https://api.gateio.ws";
    private const string BaseWebsiteUrl = "https://www.gate.com/ru/trade/";
    private const string PayloadSha512 = "cf83e1357eefb8bdf1542850d66d8007d620e4050b5715dc83f4a921d36ce9ce47d0d13c5d85f2b0ff8318d2877eec2f63b931bd47417a81a538327af927da3e";

    public GateIoRestClient() : base() { }
    public GateIoRestClient(HttpClient client) : base(client) { }
    public GateIoRestClient(int timeoutSeconds) : base(timeoutSeconds) { }

    protected override string GetExchangeName() => "GateIo";

    public override string GetUrl(string symbol) =>
        $"{BaseWebsiteUrl}{symbol.ToUpperInvariant().Replace("USDT", "_USDT")}";

    public override async Task<OrderbookResponse> GetOrderbookAsync(string symbol, int limit = 10, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeSymbol(symbol);
        var url = $"{Host}/api/v4/spot/order_book?currency_pair={normalized}&limit={limit}";

        var response = await SendWithRetryAsync(new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
        EnsureSuccessStatusCode(response, "fetch orderbook", normalized);

        var apiResponse = await ReadFromJsonAsync<GateIoOrderbookResponse>(response, cancellationToken);
        if (apiResponse == null)
            throw new DataParsingException(GetExchangeName(), "Received null orderbook response", typeof(GateIoOrderbookResponse));

        return new OrderbookResponse(
            Symbol: DenormalizeSymbol(normalized),
            Asks: ConvertToOrderbookDictionary(apiResponse.Asks),
            Bids: ConvertToOrderbookDictionary(apiResponse.Bids)
        );
    }

    public override async Task<List<string>> GetSymbolsAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{Host}/api/v4/spot/currency_pairs";

        var response = await SendWithRetryAsync(new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
        EnsureSuccessStatusCode(response, "fetch symbols");

        var apiResponse = await ReadFromJsonAsync<List<GateIoCurrencyPair>>(response, cancellationToken);
        if (apiResponse == null)
            throw new DataParsingException(GetExchangeName(), "Received null currency pairs list", typeof(List<GateIoCurrencyPair>));

        return apiResponse
            .Where(x => x.TradeStatus == "tradable")
            .Select(x => DenormalizeSymbol(x.Id))
            .ToList();
    }

    public override async Task<WithdrawalDataResponse> GetWithdrawalDataAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (_apiCredentials == null)
            throw new AuthenticationException(GetExchangeName(), "API credentials required for withdrawal data");

        var normalizedSymbol = NormalizeSymbolForWithdrawal(symbol);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        var feeEndpoint = "/api/v4/wallet/fee";
        var query = $"currency={normalizedSymbol}";
        var signature = GenerateSignature("GET", feeEndpoint, query, timestamp);

        var feeUrl = $"{Host}{feeEndpoint}?{query}";
        var feeRequest = new HttpRequestMessage(HttpMethod.Get, feeUrl);
        feeRequest.Headers.Add("Accept", "application/json");
        feeRequest.Headers.Add("Timestamp", timestamp);
        feeRequest.Headers.Add("KEY", _apiCredentials.ApiKey);
        feeRequest.Headers.Add("SIGN", signature);

        var chainsEndpoint = $"/api/v4/wallet/currency_chains";
        var chainsUrl = $"{Host}{chainsEndpoint}?currency={normalizedSymbol}";

        var feeResponse = await SendWithRetryAsync(feeRequest, cancellationToken);
        var chainsResponse = await SendWithRetryAsync(new HttpRequestMessage(HttpMethod.Get, chainsUrl), cancellationToken);

        EnsureSuccessStatusCode(feeResponse, "fetch withdrawal fee", symbol);
        EnsureSuccessStatusCode(chainsResponse, "fetch withdrawal chains", symbol);

        var feeResult = await ReadFromJsonAsync<GateIoFeeInfo>(feeResponse, cancellationToken);
        var chainsResult = await ReadFromJsonAsync<List<GateIoChainResult>>(chainsResponse, cancellationToken);

        if (feeResult == null || chainsResult == null)
            throw new DataParsingException(GetExchangeName(), "Failed to deserialize withdrawal data", typeof(GateIoFeeInfo));

        var fee = Math.Abs(ParseDecimalInvariant(feeResult.DeliveryMakerFee));

        var chains = chainsResult.Select(chain => new BlockchainDataResponse(
            Name: chain.Chain,
            FullName: chain.NameEn,
            CanDeposit: chain.IsDepositDisabled != 1,
            CanWithdraw: chain.IsWithdrawDisabled != 1,
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

    private static string DenormalizeSymbol(string symbol) => symbol.Replace("_", "");

    private static string NormalizeSymbolForWithdrawal(string symbol)
    {
        symbol = symbol.ToUpperInvariant();
        return symbol.EndsWith("USDT") ? symbol.Replace("USDT", "") : symbol;
    }

    private string GenerateSignature(string method, string url, string queryString, string timestamp)
    {
        var payload = $"{method}\n{url}\n{queryString}\n{PayloadSha512}\n{timestamp}";
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_apiCredentials!.ApiSecret));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }
}
