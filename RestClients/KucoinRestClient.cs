using System.Globalization;
using System.Net.Http.Json;
using CryptoExchangesRestLibrary.Models;
using CryptoExchangesRestLibrary.RestClients.Abstraction;
using CryptoExchangesRestLibrary.SerializationClasses;

namespace CryptoExchangesRestLibrary.RestClients;

public class KucoinRestClient : ExchangeRestClient
{
    private const string _host = "https://api.kucoin.com";

    public KucoinRestClient()
        : base()
    { }

    public void SetApiCredentials(string apiKey, string apiSecret)
        => _apiCredentials = new ApiCredendetails(apiKey, apiSecret);

    public override async Task<OrderbookResponce> GetOrderbookAsync(string symbol, int limit = 20)
    {
        symbol = symbol.Contains("USDT") 
            ? symbol.Replace("USDT", "-USDT") 
            : symbol + "-USDT";
    
        string path = $"/api/v1/market/orderbook/level2_{limit}?symbol={symbol}";
        Uri uri = new Uri($"{_host}{path}");
        
        var response = await _client.GetAsync(uri);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"[KucoinExchange]:  ошибка при получении ордербука по паре {symbol}");
        var result = await response.Content.ReadFromJsonAsync<KucoinOrderbookResponse>();
        
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
        return new OrderbookResponce(
            Symbol: symbol,
            Asks: asks,
            Bids: bids
        );
    }
    public override Task<List<string>> GetSymbolsAsync()
    {
        throw new NotImplementedException();
    }

    public override async Task<WithdrawalDataResponce> GetWithdrawalDataAsync(string symbol)
    {
        symbol = symbol.Contains("USDT") ? symbol.Replace("USDT", string.Empty) : symbol;
        string path = $"/api/v1/withdrawals/quotas?currency={symbol}&chain";
        Uri uri = new Uri($"{_host}{path}");
        var response = await _client.GetAsync(uri);
        // if (!response.IsSuccessStatusCode)
        //     throw new HttpRequestException("[KucoinExchange]: ошибка при получении информации для перевода");
        var tempresponce = await response.Content.ReadAsStringAsync();
        Console.WriteLine(tempresponce);
        return null;
    }

    public override string GetUrl(string symbol)
    {
        throw new NotImplementedException();
    }
}