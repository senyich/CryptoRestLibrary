using System.Text.Json.Serialization;

namespace CryptoExchangesRestLibrary.SerializationClasses.Bybit;

/// <summary>
/// Bybit API response wrapper.
/// </summary>
public record class BybitApiResponse<T>(
    [property: JsonPropertyName("retCode")] int RetCode,
    [property: JsonPropertyName("retMsg")] string? RetMsg,
    [property: JsonPropertyName("result")] T? Result
);

/// <summary>
/// Bybit tickers response.
/// </summary>
public record class BybitTickersResult(
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("list")] List<BybitTickerItem>? List
);

/// <summary>
/// Bybit ticker item.
/// </summary>
public record class BybitTickerItem(
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("bid1Price")] string Bid1Price,
    [property: JsonPropertyName("bid1Size")] string Bid1Size,
    [property: JsonPropertyName("ask1Price")] string Ask1Price,
    [property: JsonPropertyName("ask1Size")] string Ask1Size,
    [property: JsonPropertyName("lastPrice")] string LastPrice,
    [property: JsonPropertyName("prevPrice24h")] string PrevPrice24h,
    [property: JsonPropertyName("price24hPcnt")] string Price24hPcnt,
    [property: JsonPropertyName("highPrice24h")] string HighPrice24h,
    [property: JsonPropertyName("lowPrice24h")] string LowPrice24h,
    [property: JsonPropertyName("turnover24h")] string Turnover24h,
    [property: JsonPropertyName("volume24h")] string Volume24h
);

/// <summary>
/// Bybit orderbook result.
/// </summary>
public record class BybitOrderbookResult(
    [property: JsonPropertyName("s")] string Symbol,
    [property: JsonPropertyName("a")] List<List<string>> Asks,
    [property: JsonPropertyName("b")] List<List<string>> Bids,
    [property: JsonPropertyName("ts")] long Timestamp
);

/// <summary>
/// Bybit coin info result for withdrawals.
/// </summary>
public record class BybitCoinInfoResult(
    [property: JsonPropertyName("rows")] List<BybitCoinInfoRow>? Rows
);

/// <summary>
/// Bybit coin info row.
/// </summary>
public record class BybitCoinInfoRow(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("coin")] string Coin,
    [property: JsonPropertyName("remainAmount")] string RemainAmount,
    [property: JsonPropertyName("chains")] List<BybitChainInfo>? Chains
);

/// <summary>
/// Bybit chain information.
/// </summary>
public record class BybitChainInfo(
    [property: JsonPropertyName("chainType")] string ChainType,
    [property: JsonPropertyName("confirmation")] string Confirmation,
    [property: JsonPropertyName("withdrawFee")] string WithdrawFee,
    [property: JsonPropertyName("depositMin")] string DepositMin,
    [property: JsonPropertyName("withdrawMin")] string WithdrawMin,
    [property: JsonPropertyName("chain")] string Chain,
    [property: JsonPropertyName("chainDeposit")] string ChainDeposit,
    [property: JsonPropertyName("chainWithdraw")] string ChainWithdraw,
    [property: JsonPropertyName("minAccuracy")] string MinAccuracy,
    [property: JsonPropertyName("withdrawPercentageFee")] string WithdrawPercentageFee,
    [property: JsonPropertyName("contractAddress")] string? ContractAddress = null
);
