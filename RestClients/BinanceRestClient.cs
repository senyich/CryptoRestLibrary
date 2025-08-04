using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using CryptoExchangesRestLibrary.RestClients.Abstraction;
using CryptoExchangesRestLibrary.SerializationClasses;

namespace CryptoExchangesRestLibrary.RestClients;

public class BinanceRestClient : ExchangeRestClient
{
    private const string _host = "https://api.binance.com";
    public BinanceRestClient() 
        : base()
    { }

    public override string GetUrl(string symbol)
        => $"http://binance.com/ru/trade/{symbol.Replace("USDT", "_USDT")}";
    public override async Task<OrderbookResponce> GetOrderbookAsync(string symbol, int limit = 10)
    {
        symbol = !symbol.ToUpper().Contains("USDT") 
            ? symbol + "USDT" 
            : symbol;
        string path = $"/api/v3/depth" +
                      $"?symbol={symbol}" +
                      $"&limit={limit}";
        var response = await _client.GetAsync(new Uri($"{_host}{path}"));
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException("[BinanceRestClient]: Ошибка при полуении ордербука");
        var tempResponse = await response.Content.ReadFromJsonAsync<InnerBinanceOrderbookResponse>();
        if (tempResponse == null)
            throw new JsonException($"[BinanceRestClient]: Не удалось десериализовать ответ для пары {symbol}");
        return new OrderbookResponce(
            Symbol: symbol, 
            Asks: ConvertToDictionary(tempResponse.Bids),
            Bids: ConvertToDictionary(tempResponse.Asks)
        );
    }
    public override async Task<List<string>> GetSymbolsAsync()
    {
        string path = "/api/v3/ticker/price";
        var response = await _client.GetAsync(new Uri($"{_host}{path}"));
        if(!response.IsSuccessStatusCode)
            throw new HttpRequestException("[BinanceRestClient]: Ошибка при полуении символов");
        var tempResponse = await response.Content.ReadFromJsonAsync<List<InnerBinanceSymbolsResponce>>();
        if (tempResponse == null)
            throw new Exception($"[BinanceRestClient]: Не удалось десериализовать ответ для всех торговых пар");
        return tempResponse
            .Select(t => t.Symbol)
            .Where(t=>t.Contains("USDT"))
            .ToList();
    }

    public override async Task<WithdrawalDataResponce> GetWithdrawalDataAsync(string symbol)
    {
        throw new NotImplementedException();
    }
    private Dictionary<decimal, decimal> ConvertToDictionary(List<List<string>> orders)
    {
        return orders.ToDictionary(
            x => decimal.Parse(x[0], CultureInfo.InvariantCulture),
            x => decimal.Parse(x[1], CultureInfo.InvariantCulture)  
        );
    }
}