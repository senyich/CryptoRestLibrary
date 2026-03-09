using System.Text.Json.Serialization;

namespace CryptoExchangesRestLibrary.SerializationClasses;

/// <summary>
/// Generic orderbook entry with price and quantity as strings.
/// Exchanges return decimals as strings to preserve precision.
/// </summary>
public record class PriceLevel(
    [property: JsonPropertyName("price")] string Price,
    [property: JsonPropertyName("quantity")] string Quantity
);

/// <summary>
/// Generic orderbook response structure.
/// </summary>
public record class OrderbookData(
    [property: JsonPropertyName("bids")] List<List<string>> Bids,
    [property: JsonPropertyName("asks")] List<List<string>> Asks
);

/// <summary>
/// Generic API response wrapper with data field.
/// </summary>
public record class ApiResponse<T>(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("msg")] string? Message,
    [property: JsonPropertyName("data")] T? Data
);

/// <summary>
/// Generic API response with result field (used by Bybit, etc.).
/// </summary>
public record class ApiResultResponse<T>(
    [property: JsonPropertyName("retCode")] int RetCode,
    [property: JsonPropertyName("retMsg")] string? RetMsg,
    [property: JsonPropertyName("result")] T? Result
);

/// <summary>
/// Network/chain information for withdrawal data.
/// </summary>
public record class NetworkInfo(
    [property: JsonPropertyName("network")] string Network,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("withdrawEnable")] bool WithdrawEnable,
    [property: JsonPropertyName("depositEnable")] bool DepositEnable,
    [property: JsonPropertyName("withdrawFee")] string WithdrawFee
);

/// <summary>
/// Coin/currency information for withdrawal data.
/// </summary>
public record class CoinInfo(
    [property: JsonPropertyName("coin")] string Coin,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("networkList")] List<NetworkInfo>? NetworkList
);
