using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using CryptoExchangesRestLibrary.RestClients.Abstraction;
using CryptoExchangesRestLibrary.SerializationClasses.Bybit;

namespace CryptoExchangesRestLibrary.RestClients;
public class BybitRestClient : ExchangeRestClient
{   
    private const string _host = "https://api.bybit.com";
    public BybitRestClient() : base()
    { }
    public override string GetUrl(string symbol)
        => $"https://www.bybit.com/en/trade/spot/{symbol.Replace("USDT","/USDT")}";
    public override async Task<OrderbookResponce> GetOrderbookAsync(string symbol, int limit = 10)
    {
        symbol = !symbol.ToUpper().Contains("USDT") 
            ? symbol + "USDT" 
            : symbol;
        string path = $"/v5/market/orderbook" +
                      $"?category={"spot"}" +
                      $"&symbol={symbol}" +
                      $"&limit={limit}";
        Uri uri = new Uri($"{_host}{path}");
        var response = await _client.GetAsync(uri);
        if (!response.IsSuccessStatusCode)
            throw new Exception($"[BybitRestClient]: http ошибка при получении ордербука по паре {symbol}");
        var tempResponse = await response.Content.ReadFromJsonAsync<ApiResponse<BybitOrderbookInnerResult>>();
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
        var tempResponse = await response.Content.ReadFromJsonAsync<ApiResponse<BybitTickersResult>>();
        if (tempResponse == null || tempResponse.Result?.List == null)
            throw new Exception($"[BybitRestClient]: Не удалось десериализовать ответ со списком торговых пар");
        return tempResponse
            .Result
            .List
            .Select(x => x.Symbol)
            .Where(x => x.Contains("USDT"))
            .ToList();
    }
    public override async Task<WithdrawalDataResponce> GetWithdrawalDataAsync(string symbol)
    {
        if (_apiCredentials == null)
            throw new Exception("[BybitRestClient]: Для получения информации для перевода, требуются api ключи");
        symbol = symbol.ToUpper().Contains("USDT") 
            ? symbol.Replace("USDT", string.Empty) 
            : symbol;
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var recvWindow = "5000";
        var queryString = $"coin={symbol}";

        var sign = GenerateSignature(timestamp, recvWindow, queryString);

        string path = $"/v5/asset/coin/query-info?{queryString}";
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{_host}{path}"),
            Headers =
            {
                { "X-BAPI-SIGN", sign },
                { "X-BAPI-API-KEY", _apiCredentials.apiKey },
                { "X-BAPI-TIMESTAMP", timestamp.ToString() },
                { "X-BAPI-RECV-WINDOW", recvWindow }
            }
        };

        var response = await _client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"[BybitRestClient]: HTTP ошибка при получении информации для перевода: {response.StatusCode}");

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<CoinInfoResult>>();
    
        if (apiResponse == null || apiResponse.RetCode != 0 || apiResponse.Result.Rows.Count == 0)
            throw new Exception("[BybitRestClient]: не удалось десериализовать ответе");

        var row = apiResponse.Result.Rows[0];
    
        var chains = row.Chains.Select(chain => new BlockchainDataResponce(
            Name: chain.Chain,
            FullName: chain.ChainType,
            CanWithdraw: chain.ChainWithdraw == "1",
            CanDeposit: chain.ChainDeposit == "1",
            Fee: decimal.Parse(chain.WithdrawFee, CultureInfo.InvariantCulture)
        )).ToList();
        
        return new WithdrawalDataResponce(
            Symbol: row.Coin,
            Chains: chains
        );
    }
    private string GenerateSignature(long timestamp, string recvWindow, string queryString)
    {
        var payload = $"{timestamp}{_apiCredentials.apiKey}{recvWindow}{queryString}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiCredentials.apiSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
    private Dictionary<decimal, decimal> ConvertToDictionary(List<List<string>> arr) 
        => arr.ToDictionary(
            x => decimal.Parse(x[0], CultureInfo.InvariantCulture), 
            x => decimal.Parse(x[1], CultureInfo.InvariantCulture) 
        );
}
