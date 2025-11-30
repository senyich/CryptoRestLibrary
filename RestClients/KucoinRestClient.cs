using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using CryptoExchangesRestLibrary.Models;
using CryptoExchangesRestLibrary.RestClients.Abstraction;
using CryptoExchangesRestLibrary.SerializationClasses.Kucoin;
using JsonException = System.Text.Json.JsonException;

namespace CryptoExchangesRestLibrary.RestClients;

public class KucoinRestClient : ExchangeRestClient
{
    private const string _host = "https://api.kucoin.com";
    private string _passPhrase = null;
    public KucoinRestClient()
        : base()
    { }

    public void SetApiCredentials(string ApiKey, string ApiSecret)
        => _apiCredentials = new ApiCredendetails(ApiKey, ApiSecret);
    public void SetPassPhrase(string passPhrase)
        => _passPhrase = passPhrase;
    public override async Task<OrderbookResponse> GetOrderbookAsync(string symbol, int limit = 10)
    {
        limit = 20;
        symbol = symbol.Contains("USDT") 
            ? symbol.Replace("USDT", "-USDT") 
            : symbol + "-USDT";
    
        string path = $"/api/v1/market/orderbook/level2_{limit}?symbol={symbol}";
        Uri uri = new Uri($"{_host}{path}");
        
        var response = await _client.GetAsync(uri);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"[KucoinExchange]:  ошибка при получении ордербука по паре {symbol}");
        var result = await response.Content.ReadFromJsonAsync<KucoinOrderbookResponse>();
        if (result == null || result.Data == null )
            throw new JsonException("[KucoinExchange]: ошибка при десериализации ответа");
        var asks = result.Data.Asks
            .ToDictionary(
                x => decimal.Parse(x[0], CultureInfo.InvariantCulture), 
                x => decimal.Parse(x[1], CultureInfo.InvariantCulture)
            );
        var bids = result.Data.Bids
            .ToDictionary(
                x => decimal.Parse(x[0], CultureInfo.InvariantCulture), 
                x => decimal.Parse(x[1], CultureInfo.InvariantCulture)
            );
        return new OrderbookResponse(
            Symbol: symbol,
            Asks: asks,
            Bids: bids
        );
    }
    public override Task<List<string>> GetSymbolsAsync()
    {
        throw new NotImplementedException();
    }
    public override async Task<WithdrawalDataResponse> GetWithdrawalDataAsync(string symbol)
    {
        symbol = symbol.Replace("USDT", "", StringComparison.OrdinalIgnoreCase).ToUpper();
        string path = $"/api/v1/withdrawals/quotas?currency={symbol}";
    
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    
        string method = "GET";
        string endpoint = path;
        string strToSign = $"{timestamp}{method}{endpoint}";

        string signature = GenerateSignature(strToSign, _apiCredentials.ApiSecret);
    
        string signedPassphrase = GenerateSignature(_passPhrase, _apiCredentials.ApiSecret);

        var request = new HttpRequestMessage(HttpMethod.Get, $"{_host}{path}");
        request.Headers.Add("KC-API-KEY", _apiCredentials.ApiKey);
        request.Headers.Add("KC-API-SIGN", signature);
        request.Headers.Add("KC-API-TIMESTAMP", timestamp.ToString());
        request.Headers.Add("KC-API-PASSPHRASE", signedPassphrase); 
        request.Headers.Add("KC-API-KEY-VERSION", "2"); 

        using var response = await _client.SendAsync(request);
    
        if (!response.IsSuccessStatusCode)
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"[KucoinClient]: ошибка получения информации о переводе: {response.StatusCode}, {errorContent}");
        }
        var tempResponse = await response.Content.ReadFromJsonAsync<KucoinWithdrawalLimitResponse>();
        if (tempResponse == null)
            throw new JsonException("[KucoinClient]: ошибка десериализации ответа");
        var withdrawalData = new WithdrawalDataResponse(
            Symbol: tempResponse.Data.Currency,
            Chains: new List<BlockchainDataResponse>
            {
                new BlockchainDataResponse(
                    Name: tempResponse.Data.Chain,
                    FullName: tempResponse.Data.Chain, 
                    CanWithdraw: tempResponse.Data.IsWithdrawEnabled,
                    CanDeposit: true,
                    Fee: decimal.Parse(tempResponse.Data.WithdrawMinFee, CultureInfo.InvariantCulture))
            }
        );
        return withdrawalData;
    }
    private string GenerateSignature(string data, string ApiSecret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ApiSecret));
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }
    public override string GetUrl(string symbol)
    {
        throw new NotImplementedException();
    }
}