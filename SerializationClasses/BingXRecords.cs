using System.Text.Json.Serialization;

namespace CryptoExchangesRestLibrary.SerializationClasses.BingX;

/// <summary>
/// BingX contracts response.
/// </summary>
public record class BingXContractsResponse(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("data")] List<BingXContract>? Data
);

/// <summary>
/// BingX contract information.
/// </summary>
public record class BingXContract(
    [property: JsonPropertyName("contractId")] string ContractId,
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("size")] string Size,
    [property: JsonPropertyName("quantityPrecision")] int QuantityPrecision,
    [property: JsonPropertyName("pricePrecision")] int PricePrecision,
    [property: JsonPropertyName("feeRate")] decimal FeeRate,
    [property: JsonPropertyName("makerFeeRate")] decimal MakerFeeRate,
    [property: JsonPropertyName("takerFeeRate")] decimal TakerFeeRate,
    [property: JsonPropertyName("tradeMinLimit")] decimal TradeMinLimit,
    [property: JsonPropertyName("tradeMinQuantity")] decimal TradeMinQuantity,
    [property: JsonPropertyName("tradeMinUSDT")] decimal TradeMinUSDT,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("asset")] string Asset,
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("apiStateOpen")] string ApiStateOpen,
    [property: JsonPropertyName("apiStateClose")] string ApiStateClose
);

/// <summary>
/// BingX orderbook response.
/// </summary>
public record class BingXOrderbookResponse(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("msg")] string? Message,
    [property: JsonPropertyName("data")] BingXOrderbookData? Data
);

/// <summary>
/// BingX orderbook data.
/// </summary>
public record class BingXOrderbookData(
    [property: JsonPropertyName("t")] long Timestamp,
    [property: JsonPropertyName("bids")] List<List<string>> Bids,
    [property: JsonPropertyName("asks")] List<List<string>> Asks,
    [property: JsonPropertyName("bidsCoin")] List<List<string>>? BidsCoin,
    [property: JsonPropertyName("asksCoin")] List<List<string>>? AsksCoin
);

/// <summary>
/// BingX withdrawal data response.
/// </summary>
public record class BingXWithdrawalResponse(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("msg")] string? Message,
    [property: JsonPropertyName("data")] List<BingXCoinData>? Data
);

/// <summary>
/// BingX coin data for withdrawals.
/// </summary>
public record class BingXCoinData(
    [property: JsonPropertyName("coin")] string Coin,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("networkList")] List<BingXNetworkData> NetworkList
);

/// <summary>
/// BingX network data.
/// </summary>
public record class BingXNetworkData(
    [property: JsonPropertyName("network")] string Network,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("withdrawEnable")] bool WithdrawEnable,
    [property: JsonPropertyName("depositEnable")] bool DepositEnable,
    [property: JsonPropertyName("withdrawFee")] string WithdrawFee
);
