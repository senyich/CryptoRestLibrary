using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using CryptoExchangesRestLibrary.RestClients.Abstraction;

namespace CryptoExchangesRestLibrary.RestClients;

public class MexcRestClient : ExchangeRestClient
{
    private const string _host = "https://api.mexc.com";
    public MexcRestClient() 
        : base()
    { }

    public override string GetUrl(string symbol)
        => $"https://www.mexc.com/ru-RU/exchange/{symbol.Replace("USDT", "_USDT")}";
    public override async Task<OrderbookResponce> GetOrderbookAsync(string symbol, int limit = 10)
    {
        symbol = !symbol.ToUpper().Contains("USDT") ? symbol + "USDT" : symbol;
        string path = $"/api/v3/depth" +
                      $"?symbol={symbol}" +
                      $"&limit={limit}";
        Uri uri = new Uri($"{_host}{path}");
        var response = await _client.GetAsync(uri);
        if (!response.IsSuccessStatusCode)
            throw new Exception($"[BinanceRestClient]: HTTP ошибка при получении ордербука по паре {symbol}");
        var tempResponse = await response.Content.ReadFromJsonAsync<InnerMexcOrderbookResponse>();
        if (tempResponse == null)
            throw new Exception($"[BinanceRestCl6ient]: Не удалось десериализовать ответ для пары {symbol}");
        return new OrderbookResponce(
            symbol,
            ConvertToDictionary(tempResponse.Bids),
            ConvertToDictionary(tempResponse.Asks)
        );
    }
    public override async Task<List<string>> GetSymbolsAsync()
    {
        string path = "/api/v3/defaultSymbols";
        Uri uri = new Uri($"{_host}{path}");
    
        var response = await _client.GetAsync(uri);
        
        if (!response.IsSuccessStatusCode)
            throw new Exception($"[MexcRestClient]: HTTP error while fetching trading pairs. Status code: {response.StatusCode}");
            
        var apiResponse = await response.Content.ReadFromJsonAsync<MexcSymbolsResponse>();
        
        if (apiResponse == null || apiResponse.Data == null)
            throw new Exception($"[MexcRestClient]: Failed to deserialize trading pairs response");
            
        return apiResponse.Data;
    }
    public override async Task<WithdrawalDataResponce> GetWithdrawalDataAsync(string symbol)
    {
        if (_apiCredentials == null)
            throw new Exception("[MexcRestClient]: Для получения информации для перевода, требуются api ключи");
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var queryString = $"coin={symbol}&timestamp={timestamp}";
        var sign = GenerateSignature(queryString);
        var path = "/api/v3/capital/config/getall";
        
        string uri = $"{_host}{path}?{queryString}&signature={sign}";
        
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Add("X-MEXC-APIKEY", _apiCredentials.apiKey);
        request.Headers.Add("Accept", "application/json");
        
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<List<CoinConfigApiResponse>>();
        if (data == null || data.Count == 0)
            return null;
        var coinConfig = data.FirstOrDefault(c => 
            c.Coin.Equals(symbol, StringComparison.OrdinalIgnoreCase));
        var chains = coinConfig.NetworkList.Select(network => 
            new BlockchainDataResponce(
                Name: network.Network,
                FullName: network.Name,
                CanWithdraw: network.WithdrawEnable,
                CanDeposit: network.DepositEnable,
                Fee: decimal.Parse(network.WithdrawFee, CultureInfo.InvariantCulture)
            )).ToList();
        return new WithdrawalDataResponce(
            Symbol: coinConfig.Coin,
            Chains: chains
        );
    }

    private string GenerateSignature(string queryString)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiCredentials.apiSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(queryString));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    private Dictionary<decimal, decimal> ConvertToDictionary(List<List<string>> orders)
    {
        return orders.ToDictionary(
            x => decimal.Parse(x[0], CultureInfo.InvariantCulture),
            x => decimal.Parse(x[1], CultureInfo.InvariantCulture)  
        );
    }
    private record MexcSymbolsResponse(
        List<string> Data,
        int Code,
        string Msg,
        long Timestamp
    );
    private record InnerMexcOrderbookResponse(
        long LastUpdateId,
        List<List<string>> Bids,
        List<List<string>> Asks
    );
    private record CoinConfigApiResponse(
        [property: JsonPropertyName("coin")] string Coin,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("networkList")] List<NetworkConfigApiResponse> NetworkList
    );

    private record NetworkConfigApiResponse(
        [property: JsonPropertyName("network")] string Network,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("withdrawEnable")] bool WithdrawEnable,
        [property: JsonPropertyName("depositEnable")] bool DepositEnable,
        [property: JsonPropertyName("withdrawFee")] string WithdrawFee
    );


}
