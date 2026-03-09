using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CryptoExchangesRestLibrary.Exceptions;
using CryptoExchangesRestLibrary.RestClients.Abstraction;
using CryptoExchangesRestLibrary.SerializationClasses.Kucoin;

namespace CryptoExchangesRestLibrary.RestClients;

/// <summary>
/// REST client for KuCoin exchange.
/// Note: KuCoin requires an additional passphrase parameter.
/// </summary>
public class KucoinRestClient : ExchangeRestClient
{
    private const string Host = "https://api.kucoin.com";
    private const string BaseWebsiteUrl = "https://www.kucoin.com/trade/";
    private string? _passPhrase;

    public KucoinRestClient() : base() { }
    public KucoinRestClient(HttpClient client) : base(client) { }
    public KucoinRestClient(int timeoutSeconds) : base(timeoutSeconds) { }

    protected override string GetExchangeName() => "Kucoin";

    /// <summary>
    /// Sets the passphrase (required for KuCoin API).
    /// Must be called after SetApiCredentials.
    /// </summary>
    public void SetPassPhrase(string passPhrase) => _passPhrase = passPhrase;

    public override string GetUrl(string symbol) =>
        $"{BaseWebsiteUrl}{symbol.ToUpperInvariant().Replace("USDT", "-USDT")}";

    public override async Task<OrderbookResponse> GetOrderbookAsync(string symbol, int limit = 10, CancellationToken cancellationToken = default)
    {
        // KuCoin only supports fixed depth levels
        limit = 20;
        var normalized = NormalizeSymbol(symbol);
        var url = $"{Host}/api/v1/market/orderbook/level2_{limit}?symbol={normalized}";

        var response = await SendWithRetryAsync(new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
        EnsureSuccessStatusCode(response, "fetch orderbook", normalized);

        var apiResponse = await ReadFromJsonAsync<KucoinOrderbookResponse>(response, cancellationToken);
        if (apiResponse?.Data == null)
            throw new DataParsingException(GetExchangeName(), "Received null orderbook response", typeof(KucoinOrderbookResponse));

        return new OrderbookResponse(
            Symbol: normalized,
            Asks: ConvertToOrderbookDictionary(apiResponse.Data.Asks),
            Bids: ConvertToOrderbookDictionary(apiResponse.Data.Bids)
        );
    }

    public override Task<List<string>> GetSymbolsAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("GetSymbolsAsync not yet implemented for KuCoin");
    }

    public override async Task<WithdrawalDataResponse> GetWithdrawalDataAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (_apiCredentials == null)
            throw new AuthenticationException(GetExchangeName(), "API credentials required for withdrawal data");

        if (string.IsNullOrEmpty(_passPhrase))
            throw new AuthenticationException(GetExchangeName(), "Passphrase required for KuCoin withdrawal data. Call SetPassPhrase() first.");

        var normalizedSymbol = NormalizeSymbolForWithdrawal(symbol);
        var path = $"/api/v1/withdrawals/quotas?currency={normalizedSymbol}";
        var url = $"{Host}{path}";

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        const string method = "GET";

        var signature = GenerateSignature(timestamp, method, path);
        var signedPassphrase = GeneratePassphraseSignature();

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("KC-API-KEY", _apiCredentials.ApiKey);
        request.Headers.Add("KC-API-SIGN", signature);
        request.Headers.Add("KC-API-TIMESTAMP", timestamp.ToString());
        request.Headers.Add("KC-API-PASSPHRASE", signedPassphrase);
        request.Headers.Add("KC-API-KEY-VERSION", "2");

        var response = await SendWithRetryAsync(request, cancellationToken);
        EnsureSuccessStatusCode(response, "fetch withdrawal data", symbol);

        var apiResponse = await ReadFromJsonAsync<KucoinWithdrawalResponse>(response, cancellationToken);
        if (apiResponse?.Data == null)
            throw new DataParsingException(GetExchangeName(), "Failed to deserialize withdrawal data", typeof(KucoinWithdrawalResponse));

        var data = apiResponse.Data;
        return new WithdrawalDataResponse(
            Symbol: data.Currency,
            Chains:
            [
                new BlockchainDataResponse(
                    Name: data.Chain,
                    FullName: data.Chain,
                    CanWithdraw: data.IsWithdrawEnabled,
                    CanDeposit: true, // KuCoin doesn't provide deposit status in this endpoint
                    Fee: ParseDecimalInvariant(data.WithdrawMinFee)
                )
            ]
        );
    }

    private static string NormalizeSymbol(string symbol)
    {
        symbol = symbol.ToUpperInvariant();
        return symbol.Contains("USDT") ? symbol.Replace("USDT", "-USDT") : $"{symbol}-USDT";
    }

    private static string NormalizeSymbolForWithdrawal(string symbol) =>
        symbol.ToUpperInvariant().Replace("USDT", "").Replace("-", "");

    private string GenerateSignature(long timestamp, string method, string path)
    {
        var strToSign = $"{timestamp}{method}{path}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiCredentials!.ApiSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(strToSign));
        return Convert.ToBase64String(hash);
    }

    private string GeneratePassphraseSignature()
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiCredentials!.ApiSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(_passPhrase!));
        return Convert.ToBase64String(hash);
    }
}
