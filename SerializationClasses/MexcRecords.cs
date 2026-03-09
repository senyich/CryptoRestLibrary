using System.Text.Json.Serialization;

namespace CryptoExchangesRestLibrary.SerializationClasses.Mexc;

/// <summary>
/// MEXC symbols response.
/// </summary>
public record class MexcSymbolsResponse(
    [property: JsonPropertyName("data")] List<string>? Data,
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("msg")] string? Message,
    [property: JsonPropertyName("timestamp")] long Timestamp
);

/// <summary>
/// MEXC orderbook response.
/// </summary>
public record class MexcOrderbookResponse(
    [property: JsonPropertyName("lastUpdateId")] long LastUpdateId,
    [property: JsonPropertyName("bids")] List<List<string>> Bids,
    [property: JsonPropertyName("asks")] List<List<string>> Asks
);

/// <summary>
/// MEXC coin configuration response for withdrawals.
/// </summary>
public record class MexcCoinConfigResponse(
    [property: JsonPropertyName("coin")] string Coin,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("networkList")] List<MexcNetworkConfig>? NetworkList
);

/// <summary>
/// MEXC network configuration.
/// </summary>
public record class MexcNetworkConfig(
    [property: JsonPropertyName("network")] string Network,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("withdrawEnable")] bool WithdrawEnable,
    [property: JsonPropertyName("depositEnable")] bool DepositEnable,
    [property: JsonPropertyName("withdrawFee")] string WithdrawFee,
    [property: JsonPropertyName("withdrawMin")] string? WithdrawMin = null,
    [property: JsonPropertyName("depositMin")] string? DepositMin = null
);
