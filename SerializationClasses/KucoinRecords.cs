using System.Text.Json.Serialization;

namespace CryptoExchangesRestLibrary.SerializationClasses.Kucoin;

/// <summary>
/// KuCoin orderbook response.
/// </summary>
public record class KucoinOrderbookResponse(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("msg")] string? Message,
    [property: JsonPropertyName("data")] KucoinOrderbookData? Data
);

/// <summary>
/// KuCoin orderbook data.
/// </summary>
public record class KucoinOrderbookData(
    [property: JsonPropertyName("time")] long Time,
    [property: JsonPropertyName("sequence")] string Sequence,
    [property: JsonPropertyName("bids")] List<List<string>> Bids,
    [property: JsonPropertyName("asks")] List<List<string>> Asks
);

/// <summary>
/// KuCoin withdrawal quotas response.
/// </summary>
public record class KucoinWithdrawalResponse(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("msg")] string? Message,
    [property: JsonPropertyName("data")] KucoinWithdrawalData? Data
);

/// <summary>
/// KuCoin withdrawal data.
/// </summary>
public record class KucoinWithdrawalData(
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("chain")] string Chain,
    [property: JsonPropertyName("withdrawMinFee")] string WithdrawMinFee,
    [property: JsonPropertyName("withdrawMinSize")] string WithdrawMinSize,
    [property: JsonPropertyName("isWithdrawEnabled")] bool IsWithdrawEnabled,
    [property: JsonPropertyName("precision")] int Precision,
    [property: JsonPropertyName("reason")] string? Reason
);
