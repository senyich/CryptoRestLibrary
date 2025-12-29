using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using CryptoExchangesRestLibrary.RestClients.Abstraction;
using CryptoExchangesRestLibrary.SerializationClasses;

namespace CryptoExchangesRestLibrary.RestClients;

public class BinanceRestClient : ExchangeRestClient
{
    private const string _host = "https://api.binance.com";
    public BinanceRestClient() : base() { }
    public BinanceRestClient(HttpClient client) : base(client) { }
    public BinanceRestClient(int timeoutSeconds) : base(timeoutSeconds) { }

    public override string GetUrl(string symbol) =>
        $"https://www.binance.com/ru/trade/{symbol.Replace("USDT", "_USDT")}";

    public override async Task<OrderbookResponse> GetOrderbookAsync(string symbol, int limit = 10)
    {
        string normalized = NormalizeSymbol(symbol);

        string url = $"{_host}/api/v3/depth?symbol={normalized}&limit={limit}";
        var response = await _client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"[BinanceRestClient] Failed to fetch orderbook ({response.StatusCode}) for symbol {normalized}");

        var raw = await response.Content.ReadFromJsonAsync<InnerBinanceOrderbookResponse>();
        if (raw == null)
            throw new JsonException(
                $"[BinanceRestClient] Failed to deserialize orderbook for {normalized}");

        return new OrderbookResponse(
            Symbol: normalized,
            Asks: ConvertToDictionary(raw.Asks),
            Bids: ConvertToDictionary(raw.Bids)
        );
    }
    public override async Task<List<string>> GetSymbolsAsync()
    {
        string url = $"{_host}/api/v3/ticker/price";

        var response = await _client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"[BinanceRestClient] Failed to fetch symbols ({response.StatusCode})");

        var tickers = await response.Content.ReadFromJsonAsync<List<InnerBinanceSymbolsResponce>>();
        if (tickers == null)
            throw new JsonException("[BinanceRestClient] Failed to deserialize symbols list");

        return tickers
            .Select(t => t.Symbol)
            .Where(s => s.EndsWith("USDT", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public override Task<WithdrawalDataResponse> GetWithdrawalDataAsync(string symbol)
    {
        throw new NotImplementedException();
    }
    private static Dictionary<decimal, decimal> ConvertToDictionary(List<List<string>> orders)
    {
        var dict = new Dictionary<decimal, decimal>(orders.Count);

        foreach (var entry in orders)
        {
            if (entry.Count < 2) continue;

            if (!decimal.TryParse(entry[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                continue;
            if (!decimal.TryParse(entry[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var quantity))
                continue;

            dict[price] = quantity;
        }

        return dict;
    }
    private static string NormalizeSymbol(string symbol)
    {
        symbol = symbol.ToUpperInvariant();

        return symbol.EndsWith("USDT")
            ? symbol
            : $"{symbol}USDT";
    }
}
