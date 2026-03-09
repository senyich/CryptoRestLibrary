using System.Text.Json.Serialization;

namespace CryptoExchangesRestLibrary.SerializationClasses.GateIo;

/// <summary>
/// Gate.io orderbook response.
/// </summary>
public record class GateIoOrderbookResponse(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("current")] long Current,
    [property: JsonPropertyName("update")] long Update,
    [property: JsonPropertyName("bids")] List<List<string>> Bids,
    [property: JsonPropertyName("asks")] List<List<string>> Asks
);

/// <summary>
/// Gate.io currency pair response.
/// </summary>
public record class GateIoCurrencyPair(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("base")] string Base,
    [property: JsonPropertyName("base_name")] string? BaseName,
    [property: JsonPropertyName("quote")] string Quote,
    [property: JsonPropertyName("quote_name")] string? QuoteName,
    [property: JsonPropertyName("fee")] string Fee,
    [property: JsonPropertyName("min_base_amount")] string? MinBaseAmount,
    [property: JsonPropertyName("min_quote_amount")] string? MinQuoteAmount,
    [property: JsonPropertyName("amount_precision")] int AmountPrecision,
    [property: JsonPropertyName("precision")] int Precision,
    [property: JsonPropertyName("trade_status")] string TradeStatus,
    [property: JsonPropertyName("sell_start")] long? SellStart,
    [property: JsonPropertyName("buy_start")] long? BuyStart
);

/// <summary>
/// Gate.io fee information.
/// </summary>
public record class GateIoFeeInfo(
    [property: JsonPropertyName("user_id")] long UserId,
    [property: JsonPropertyName("taker_fee")] string TakerFee,
    [property: JsonPropertyName("maker_fee")] string MakerFee,
    [property: JsonPropertyName("gt_discount")] bool GtDiscount,
    [property: JsonPropertyName("gt_taker_fee")] string? GtTakerFee,
    [property: JsonPropertyName("gt_maker_fee")] string? GtMakerFee,
    [property: JsonPropertyName("delivery_taker_fee")] string DeliveryTakerFee,
    [property: JsonPropertyName("delivery_maker_fee")] string DeliveryMakerFee
);

/// <summary>
/// Gate.io chain result for withdrawals.
/// </summary>
public record class GateIoChainResult(
    [property: JsonPropertyName("chain")] string Chain,
    [property: JsonPropertyName("name_cn")] string NameCn,
    [property: JsonPropertyName("name_en")] string NameEn,
    [property: JsonPropertyName("contract_address")] string? ContractAddress,
    [property: JsonPropertyName("is_disabled")] int IsDisabled,
    [property: JsonPropertyName("is_deposit_disabled")] int IsDepositDisabled,
    [property: JsonPropertyName("is_withdraw_disabled")] int IsWithdrawDisabled
);
