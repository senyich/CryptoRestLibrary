using System.Globalization;
using System.Net.Http.Json;
using CryptoExchangesRestLibrary.RestClients.Abstraction;

namespace CryptoExchangesRestLibrary.RestClients;

public class BingXRestClient : ExchangeRestClient
{
    private const string _host = "https://open-api.bingx.com";

    public BingXRestClient()
        : base()
    { }

    public async override Task<OrderbookResponce> GetOrderbookAsync(string symbol, int limit = 10)
    {
        symbol = symbol.Contains("USDT") ? symbol.Replace("USDT", "-USDT") : symbol+"-USDT";
        string path = $"/openApi/swap/v2/quote/depth" +
                      $"?symbol={symbol}" +
                      $"&limit={limit}";
        Uri uri = new Uri($"{_host}{path}");
        var response = await _client.GetAsync(uri);
        if (!response.IsSuccessStatusCode)
            throw new Exception($"[BingXExchange]: http ошибка при получении ордербука по паре {symbol}");
        var apiResponse = await response.Content.ReadFromJsonAsync<BingXOrderbookResponse>();
    
        if (apiResponse?.Data == null || apiResponse.Code != 0)
            throw new Exception($"[BingXExchange]: Ошибка в данных ордербука для пары {symbol}");
        
        var bids = ConvertToDictionary(apiResponse.Data.Bids);
        var asks = ConvertToDictionary(apiResponse.Data.Asks);
        
        return new OrderbookResponce(
            Symbol: symbol.Replace("-", ""),
            Asks: asks,
            Bids: bids
        );
    }
    public async override Task<List<string>> GetSymbolsAsync()
    {
        string path = "/openApi/swap/v2/quote/contracts";
        Uri uri = new Uri($"{_host}{path}");
        var response = await _client.GetAsync(uri);
        
        if (!response.IsSuccessStatusCode)
            throw new Exception($"[BingXRestClient]: HTTP ошибка при получении списка торговых пар");
        
        var apiResponse = await response.Content.ReadFromJsonAsync<BingXContractsResponse>();
        
        return apiResponse?.Data?
            .Where(x => x.Status == 1)
            .Select(x => x.Symbol.Replace("-", ""))
            .ToList() ?? throw new Exception($"[BingXRestClient]: не удалось распарсить ответ");
    }
    private Dictionary<decimal, decimal> ConvertToDictionary(List<List<string>> orders)
    {
        return orders.ToDictionary(
            x => decimal.Parse(x[0], CultureInfo.InvariantCulture),
            x => decimal.Parse(x[1], CultureInfo.InvariantCulture)  
        );
    }
    private record BingXContractsResponse(
        int Code,
        string Message,
        List<BingXContract> Data
    );
    private record BingXContract(
        string ContractId,
        string Symbol,
        string Size,
        int QuantityPrecision,
        int PricePrecision,
        decimal FeeRate,
        decimal MakerFeeRate,
        decimal TakerFeeRate,
        decimal TradeMinLimit,
        decimal TradeMinQuantity,
        decimal TradeMinUSDT,
        string Currency,
        string Asset,
        int Status,
        string ApiStateOpen,
        string ApiStateClose
    );
    private record BingXOrderbookResponse(
        int Code,
        string Msg,
        BingXOrderbookData Data
    );

    private record BingXOrderbookData(
        long T,
        List<List<string>> Bids,
        List<List<string>> Asks,
        List<List<string>> BidsCoin,
        List<List<string>> AsksCoin
    );
}
