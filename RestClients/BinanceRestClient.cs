using System.Globalization;
using System.Net.Http.Json;
using CryptoExchangesRestLibrary.RestClients.Abstraction;

namespace CryptoExchangesRestLibrary.RestClients;

public class BinanceRestClient : ExchangeRestClient
{
    private record InnerBinanceOrderbookResponse(
        long LastUpdateId,
        List<List<string>> Bids,
        List<List<string>> Asks
    );
    private record InnerBinanceSymbolsResponce(
        string Symbol,
        string Price
    );
    private const string _host = "https://api.binance.com";
    public BinanceRestClient() 
        : base()
    { }
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
            throw new Exception($"[BinanceRestClient]: HTTP ошибка при получении ордербука по паре {symbol}");
        var tempResponse = await response.Content.ReadFromJsonAsync<InnerBinanceOrderbookResponse>();
        if (tempResponse == null)
            throw new Exception($"[BinanceRestClient]: Не удалось десериализовать ответ для пары {symbol}");
        return new OrderbookResponce(
            symbol,
            ConvertToDictionary(tempResponse.Bids),
            ConvertToDictionary(tempResponse.Asks)
        );
    }
    public override async Task<List<string>> GetSymbolsAsync()
    {
        string path = "/api/v3/ticker/price";
        var response = await _client.GetAsync(new Uri($"{_host}{path}"));
        if (!response.IsSuccessStatusCode)
            throw new Exception($"[BinanceRestClient]: HTTP ошибка при получении всех торговых пар");
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