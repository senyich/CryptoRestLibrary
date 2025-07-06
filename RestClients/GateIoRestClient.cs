using System.Globalization;
using System.Net.Http.Json;
using CryptoExchangesRestLibrary.RestClients.Abstraction;

namespace CryptoExchangesRestLibrary.RestClients;

public class GateIoRestClient : ExchangeRestClient
{
    private const string _host = "https://api.gateio.ws";
    public GateIoRestClient() 
        : base()
    { }
    public override async Task<OrderbookResponce> GetOrderbookAsync(string symbol, int limit = 10)
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
            
        return new OrderbookResponce(
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
            .Where(x => x.Trade_Status == "tradable")
            .Select(x => x.Id.Replace("_",string.Empty))
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
    private record GateIoSymbolResponse(
        string Id,
        string Base,
        string BaseName,
        string Quote,
        string QuoteName,
        string Fee,
        string MinBaseAmount,
        string MinQuoteAmount,
        string MaxQuoteAmount,
        int AmountPrecision,
        int Precision,
        string Trade_Status,
        long SellStart,
        long BuyStart,
        string Type,
        string TradeUrl
    );
    private record InnerGateIoOrderbookResponse(
        long Id,
        long Current,
        long Update,
        List<List<string>> Bids,
        List<List<string>> Asks
    );
}