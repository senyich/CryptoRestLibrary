using System.Text.Json.Serialization;

namespace CryptoExchangesRestLibrary.SerializationClasses.Kucoin;

public record class KucoinOrderbookData(
    long Time,
    string Sequence,
    List<List<string>> Bids,
    List<List<string>> Asks
);

public record class KucoinOrderbookResponse(
    string Code,
    KucoinOrderbookData Data
);
public record class KucoinWithdrawalLimitData(
    string Currency,
    [property: JsonPropertyName("limitBTCAmount")] string LimitBTCAmount,
    [property: JsonPropertyName("usedBTCAmount")] string UsedBTCAmount,
    string QuotaCurrency,
    [property: JsonPropertyName("limitQuotaCurrencyAmount")] string LimitQuotaCurrencyAmount,
    [property: JsonPropertyName("usedQuotaCurrencyAmount")] string UsedQuotaCurrencyAmount,
    [property: JsonPropertyName("remainAmount")] string RemainAmount,
    [property: JsonPropertyName("availableAmount")] string AvailableAmount,
    [property: JsonPropertyName("withdrawMinFee")] string WithdrawMinFee,
    [property: JsonPropertyName("innerWithdrawMinFee")] string InnerWithdrawMinFee,
    [property: JsonPropertyName("withdrawMinSize")] string WithdrawMinSize,
    [property: JsonPropertyName("isWithdrawEnabled")] bool IsWithdrawEnabled,
    int Precision,
    string Chain,
    string? Reason,
    [property: JsonPropertyName("lockedAmount")] string LockedAmount
);

public record class KucoinWithdrawalLimitResponse(
    string Code,
    KucoinWithdrawalLimitData Data
);