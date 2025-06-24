using System.Globalization;
using System.Net.Http.Json;
using CryptoExchangesRestLibrary.Models;
using CryptoExchangesRestLibrary.RestClients.Abstraction;

namespace CryptoExchangesRestLibrary.RestClients;
public class BybitRestClient : ExchangeRestClient
{    
    private record BybitTickersResponse(
        int RetCode,
        string RetMsg,
        BybitTickersResult Result
    );
    private record BybitTickersResult(
        string Category,
        List<BybitTickerItem> List
    );
    private record BybitTickerItem(
        string Symbol,
        string Bid1Price,
        string Bid1Size,
        string Ask1Price,
        string Ask1Size,
        string LastPrice,
        string PrevPrice24h,
        string Price24hPcnt,
        string HighPrice24h,
        string LowPrice24h,
        string Turnover24h,
        string Volume24h
    );
    private record InnerBybitOrderbookResponse(
        int RetCode,
        string RetMsg,
        BybitOrderbookInnerResult Result
    );
    private record BybitOrderbookInnerResult(
        string S,
        List<List<string>> A, 
        List<List<string>> B
    );
    private const string _host = "https://api.bybit.com";
    public BybitRestClient() : base()
    { }
    public override async Task<OrderbookResponce> GetOrderbookAsync(string symbol, int limit = 10)
    {
        symbol = !symbol.ToUpper().Contains("USDT") ? symbol + "USDT" : symbol;
        string path = $"/v5/market/orderbook" +
                      $"?category={"spot"}" +
                      $"&symbol={symbol}" +
                      $"&limit={limit}";
        Uri uri = new Uri($"{_host}{path}");
        var response = await _client.GetAsync(uri);
        if (!response.IsSuccessStatusCode)
            throw new Exception($"[BybitRestClient]: http ошибка при получении ордербука по паре {symbol}");
        var tempResponse = await response.Content.ReadFromJsonAsync<InnerBybitOrderbookResponse>();
        if (tempResponse == null)
            throw new Exception($"[BybitRestClient]: Не удалось десериализовать ответ для пары {symbol}");
        return new OrderbookResponce(
            symbol,
            ConvertToDictionary(tempResponse.Result.A),
            ConvertToDictionary(tempResponse.Result.B)
        );
    }

    public override async Task<List<string>> GetSymbolsAsync()
    {
        string path = "/v5/market/tickers?category=spot";
        Uri uri = new Uri($"{_host}{path}");
        var response = await _client.GetAsync(uri);
        if (!response.IsSuccessStatusCode)
            throw new Exception($"[BybitRestClient]: HTTP ошибка при получении списка торговых пар");
        var tempResponse = await response.Content.ReadFromJsonAsync<BybitTickersResponse>();
        if (tempResponse == null || tempResponse.Result?.List == null)
            throw new Exception($"[BybitRestClient]: Не удалось десериализовать ответ со списком торговых пар");
        return tempResponse
            .Result
            .List
            .Select(x => x.Symbol)
            .Where(x => x.Contains("USDT"))
            .ToList();
    }

    private Dictionary<decimal, decimal> ConvertToDictionary(List<List<string>> arr) 
        => arr.ToDictionary(
            x => decimal.Parse(x[0], CultureInfo.InvariantCulture), 
            x => decimal.Parse(x[1], CultureInfo.InvariantCulture) 
        );
}
