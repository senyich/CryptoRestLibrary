using System.Text.Json.Serialization;

namespace CryptoExchangesRestLibrary.SerializationClasses;

/// <summary>
/// Binance orderbook response.
/// </summary>
public record class BinanceOrderbookResponse(
    [property: JsonPropertyName("lastUpdateId")] long LastUpdateId,
    [property: JsonPropertyName("bids")] List<List<string>> Bids,
    [property: JsonPropertyName("asks")] List<List<string>> Asks
);

/// <summary>
/// Binance ticker/price response.
/// </summary>
public record class BinanceTickerResponse(
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("price")] string Price
);

/// <summary>
/// Binance withdrawal data (network configuration).
/// </summary>
public record class BinanceCoinConfig(
    [property: JsonPropertyName("coin")] string Coin,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("networkList")] List<BinanceNetworkConfig> NetworkList
);

/// <summary>
/// Binance network configuration for withdrawals.
/// </summary>
public record class BinanceNetworkConfig(
    [property: JsonPropertyName("network")] string Network,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("withdrawEnable")] bool WithdrawEnable,
    [property: JsonPropertyName("depositEnable")] bool DepositEnable,
    [property: JsonPropertyName("withdrawFee")] string WithdrawFee,
    [property: JsonPropertyName("withdrawMin")] string? WithdrawMin = null,
    [property: JsonPropertyName("depositMin")] string? DepositMin = null
);
