using System.Text.Json.Serialization;

namespace CryptoExchangesRestLibrary.SerializationClasses.GateIo;

public record GateIoChainsResult(
    [property: JsonPropertyName("chain")]string Chain,
    [property: JsonPropertyName("name_cn")]string NameCn,
    [property: JsonPropertyName("name_en")]string NameEn,
    [property: JsonPropertyName("contract_adress")]string ContractAdress,
    [property: JsonPropertyName("is_disabled")]int IsDisabled,
    [property: JsonPropertyName("is_deposit_disabled")]int IsDepositDisabled,
    [property: JsonPropertyName("is_withdraw_disabled")]int IsWihdrawDisabled
);
public record GateIoSymbolResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("base")]  string Base,
    [property: JsonPropertyName("baseName")]  string BaseName,
    [property: JsonPropertyName("quote")]  string Quote,
    [property: JsonPropertyName("quoteName")]  string QuoteName,
    [property: JsonPropertyName("fee")] string Fee,
    [property: JsonPropertyName("minBaseAmount")] string MinBaseAmount,
    [property: JsonPropertyName("minQuoteAmount")] string MinQuoteAmount,
    [property: JsonPropertyName("maxQuoteAmount")] string MaxQuoteAmount,
    [property: JsonPropertyName("amountPrecision")] int AmountPrecision,
    [property: JsonPropertyName("precisiom")] int Precision,
    [property: JsonPropertyName("trade_status")] string TradeStatus,
    [property: JsonPropertyName("sellStart")] long SellStart,
    [property: JsonPropertyName("buyStart")] long BuyStart,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("tradeUrl")] string TradeUrl
);
public record InnerGateIoOrderbookResponse(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("current")] long Current,
    [property: JsonPropertyName("update")] long Update,
    [property: JsonPropertyName("bids")] List<List<string>> Bids,
    [property: JsonPropertyName("asks")] List<List<string>> Asks
);
public record GateIoFeeInfo(
    [property: JsonPropertyName("user_id")] long UserId,
    [property: JsonPropertyName("taker_fee")] string TakerFee,
    [property: JsonPropertyName("maker_fee")] string MakerFee,
    [property: JsonPropertyName("gt_discount")] bool GtDiscount,
    [property: JsonPropertyName("gt_taker_fee")] string GtTakerFee,
    [property: JsonPropertyName("gt_maker_fee")] string GtMakerFee,
    [property: JsonPropertyName("loan_fee")] string LoanFee,
    [property: JsonPropertyName("point_type")] string PointType,
    [property: JsonPropertyName("futures_taker_fee")] string FuturesTakerFee,
    [property: JsonPropertyName("futures_maker_fee")] string FuturesMakerFee,
    [property: JsonPropertyName("delivery_taker_fee")] string DeliveryTakerFee,
    [property: JsonPropertyName("delivery_maker_fee")] string DeliveryMakerFee,
    [property: JsonPropertyName("debit_fee")] int DebitFee
);