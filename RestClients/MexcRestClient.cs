using System.Globalization;
using System.Net.Http.Json;
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
        throw new NotImplementedException();
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
}
