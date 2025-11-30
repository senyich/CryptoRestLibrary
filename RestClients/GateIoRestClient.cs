using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using CryptoExchangesRestLibrary.RestClients.Abstraction;
using CryptoExchangesRestLibrary.SerializationClasses.GateIo;

namespace CryptoExchangesRestLibrary.RestClients;

public class GateIoRestClient : ExchangeRestClient
{
    private const string _host = "https://api.gateio.ws";
    private const string _payloadSha512 = "cf83e1357eefb8bdf1542850d66d8007d620e4050b5715dc83f4a921d36ce9ce47d0d13c5d85f2b0ff8318d2877eec2f63b931bd47417a81a538327af927da3e";
    public GateIoRestClient() 
        : base()
    { }
    public override string GetUrl(string symbol)
        => $"https://www.gate.com/ru/trade/{symbol.Replace("USDT","_USDT")}";
    public override async Task<OrderbookResponse> GetOrderbookAsync(string symbol, int limit = 10)
    {
        symbol = !symbol.ToUpper().Contains("USDT") ? symbol + "_USDT" : symbol.Replace("USDT", "_USDT");
        string path = $"/api/v4/spot/order_book" +
                      $"?currency_pair={symbol}" +
                      $"&limit={limit}";
        symbol = symbol.ToLower().Replace("USDT", "_USDT");
        Uri uri = new Uri($"{_host}{path}");
        var response = await _client.GetAsync(uri);
        
        if (!response.IsSuccessStatusCode)
            throw new Exception($"[GateIoRestClient]: HTTP ошибка при получении ордербука по паре {symbol}");
            
        var tempResponse = await response.Content.ReadFromJsonAsync<InnerGateIoOrderbookResponse>();
        if (tempResponse == null)
            throw new Exception($"[GateIoRestClient]: Не удалось десериализовать ответ для пары {symbol}");
            
        return new OrderbookResponse(
            symbol,
            ConvertToDictionary(tempResponse.Bids),
            ConvertToDictionary(tempResponse.Asks)
        );
    }
    public override async Task<List<string>> GetSymbolsAsync()
    {
        string path = "/api/v4/spot/currency_pairs";
        Uri uri = new Uri($"{_host}{path}");
        var response = await _client.GetAsync(uri);
        if (!response.IsSuccessStatusCode)
            throw new Exception($"[GateIoRestClient]: HTTP ошибка при получении списка торговых пар");
        var tempResponse = await response.Content.ReadFromJsonAsync<List<GateIoSymbolResponse>>();
        if (tempResponse == null)
            throw new Exception($"[GateIoRestClient]: Не удалось десериализовать ответ со списком торговых пар");
        return tempResponse
            .Where(x => x.TradeStatus == "tradable")
            .Select(x => x.Id.Replace("_",string.Empty))
            .ToList();
    }
    public override async Task<WithdrawalDataResponse> GetWithdrawalDataAsync(string symbol)
    {
        if (_apiCredentials == null)
            throw new Exception("[GateIoRestClient]: Для получения информации для перевода, требуются api ключи");

        symbol = symbol.ToUpper().Contains("USDT") 
            ? symbol.Replace("USDT", string.Empty) 
            : symbol;

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        
        string feeEndpoint = "/api/v4/wallet/fee";
        string query = $"currency={symbol}";
        string chainsEndpoint = $"/api/v4/wallet/currency_chains?currency={symbol}";
        
        string signature = GenerateSignature("GET", feeEndpoint, query, timestamp);
        
        string feeUrl = $"{_host}{feeEndpoint}?{query}";
        var feeRequest = new HttpRequestMessage(HttpMethod.Get, feeUrl);
        feeRequest.Headers.Add("Accept", "application/json");
        feeRequest.Headers.Add("Timestamp", timestamp);
        feeRequest.Headers.Add("KEY", _apiCredentials.ApiKey);
        feeRequest.Headers.Add("SIGN", signature);
        
        var chainsResponse = await _client.GetAsync($"{_host}{chainsEndpoint}");
        var chainsResult = await chainsResponse.Content.ReadFromJsonAsync<List<GateIoChainsResult>>();
        
        var feeResponse = await _client.SendAsync(feeRequest);
        var feeResult = await feeResponse.Content.ReadFromJsonAsync<GateIoFeeInfo>();
        var result = chainsResult.Select(c => new BlockchainDataResponse(
            Name: c.Chain,
            FullName: c.NameEn,
            CanDeposit: c.IsDepositDisabled == 1 ? false : true,
            CanWithdraw: c.IsWihdrawDisabled == 1 ? false : true,
            Fee: Math.Abs(decimal.Parse(feeResult.DeliveryMakerFee, CultureInfo.InvariantCulture))))
            .ToList();
        return new WithdrawalDataResponse(symbol, result);
    }
    private string GenerateSignature(string method, string url, string queryParam, string timestamp)
    {
        string payload = $"{method}\n{url}\n{queryParam}\n{_payloadSha512}\n{timestamp}";
        
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_apiCredentials.ApiSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
    private Dictionary<decimal, decimal> ConvertToDictionary(List<List<string>> orders)
    {
        return orders.ToDictionary(
            x => decimal.Parse(x[0], CultureInfo.InvariantCulture),
            x => decimal.Parse(x[1], CultureInfo.InvariantCulture)  
        );
    }

}