using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using CryptoExchangesRestLibrary.RestClients.Abstraction;
using CryptoExchangesRestLibrary.SerializationClasses.BingX;

namespace CryptoExchangesRestLibrary.RestClients;

public class BingXRestClient : ExchangeRestClient
{
    private const string _host = "https://open-api.bingx.com";
    public BingXRestClient()
        : base()
    { }
    public override string GetUrl(string symbol)
        => $"https://bingx.com/en/spot/{symbol}";
    public async override Task<OrderbookResponce> GetOrderbookAsync(string symbol, int limit = 10)
    {
        symbol = symbol.Contains("USDT") 
            ? symbol.Replace("USDT", "-USDT") 
            : symbol+"-USDT";
        string path = $"/openApi/swap/v2/quote/depth" +
                      $"?symbol={symbol}" +
                      $"&limit={limit}";
        var response = await _client.GetAsync(new Uri($"{_host}{path}"));
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
            throw new HttpRequestException($"[BingXRestClient]: ошибка при получении списка торговых пар");
        
        var apiResponse = await response.Content.ReadFromJsonAsync<BingXContractsResponse>();
        
        return apiResponse?.Data?
            .Where(x => x.Status == 1)
            .Select(x => x.Symbol.Replace("-", ""))
            .ToList() ?? throw new ArgumentNullException($"[BingXRestClient]: не удалось распарсить ответ");
    }
    public override async Task<WithdrawalDataResponce> GetWithdrawalDataAsync(string symbol)
    {
        if (_apiCredentials == null)
            throw new Exception("[BingXRestClient]: Для получения информации для перевода, требуются api ключи");
        symbol = symbol.Contains("USDT", StringComparison.OrdinalIgnoreCase)
            ? symbol.Replace("USDT", string.Empty, StringComparison.OrdinalIgnoreCase)
            : symbol;

        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        var parameters = new Dictionary<string, string>
            {
                { "coin", symbol },
                { "timestamp", timestamp.ToString() }
            }.OrderBy(p => p.Key)
            .ToDictionary(p => p.Key, p => p.Value);
        var queryString = string.Join("&", parameters.Select(
            kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"
        ));
        var signature = GenerateSignature(queryString, _apiCredentials.apiSecret);
        string path = $"/openApi/wallets/v1/capital/config/getall?{queryString}&signature={signature}";
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{_host}{path}"),
            Headers =
            {
                { "X-BX-APIKEY", _apiCredentials.apiKey }
            }
        };
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync();
        
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<CoinData>>();
        if (apiResponse?.Data == null || apiResponse.Data.Count == 0)
            return null;
        
        var coinData = apiResponse.Data.FirstOrDefault(d => 
            d.Coin.Equals(symbol, StringComparison.OrdinalIgnoreCase));
    
        if (coinData == null)
            return null;
        
        return new WithdrawalDataResponce(
            Symbol: coinData.Coin,
            Chains: coinData.NetworkList.Select(n => new BlockchainDataResponce(
                Name: n.Network,
                FullName: $"{coinData.Name} ({n.Network})",
                CanWithdraw: n.WithdrawEnable,
                CanDeposit: n.DepositEnable,
                Fee: decimal.Parse(n.WithdrawFee, CultureInfo.InvariantCulture)
            )).ToList()
        );
    }
    private string GenerateSignature(string data, string apiSecret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiSecret));
        byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }
    private Dictionary<decimal, decimal> ConvertToDictionary(List<List<string>> orders)
    {
        return orders.ToDictionary(
            x => decimal.Parse(x[0], CultureInfo.InvariantCulture),
            x => decimal.Parse(x[1], CultureInfo.InvariantCulture)  
        );
    }
}
