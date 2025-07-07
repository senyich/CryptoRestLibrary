using System.Text.Json.Serialization;

namespace CryptoExchangesRestLibrary.SerializationClasses.GateIo;

public record GateIoChainsResult(
    string Chain,
    string Name_cn,
    string Name_en,
    string Contract_adress,
    int Is_disabled,
    int Is_deposit_disabled,
    int Is_wihdraw_disabled
);
public record GateIoSymbolResponse(
    string Id,
    string Base,
    string BaseName,
    string Quote,
    string QuoteName,
    string Fee,
    string MinBaseAmount,
    string MinQuoteAmount,
    string MaxQuoteAmount,
    int AmountPrecision,
    int Precision,
    string Trade_Status,
    long SellStart,
    long BuyStart,
    string Type,
    string TradeUrl
);
public record InnerGateIoOrderbookResponse(
    long Id,
    long Current,
    long Update,
    List<List<string>> Bids,
    List<List<string>> Asks
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