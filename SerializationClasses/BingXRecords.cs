using System.Text.Json.Serialization;

namespace CryptoExchangesRestLibrary.SerializationClasses.BingX;

public record BingXContractsResponse(
    int Code,
    string Message,
    List<BingXContract> Data
);
public record BingXContract(
    string ContractId,
    string Symbol,
    string Size,
    int QuantityPrecision,
    int PricePrecision,
    decimal FeeRate,
    decimal MakerFeeRate,
    decimal TakerFeeRate,
    decimal TradeMinLimit,
    decimal TradeMinQuantity,
    decimal TradeMinUSDT,
    string Currency,
    string Asset,
    int Status,
    string ApiStateOpen,
    string ApiStateClose
);
public record BingXOrderbookResponse(
    int Code,
    string Msg,
    BingXOrderbookData Data
);

public record BingXOrderbookData(
    long T,
    List<List<string>> Bids,
    List<List<string>> Asks,
    List<List<string>> BidsCoin,
    List<List<string>> AsksCoin
);
public record ApiResponse<T>(
    int Code,  
    long Timestamp,
    List<T> Data
);
public record CoinData(
    string Coin,
    string Name,
    List<NetworkData> NetworkList
);

public record NetworkData(
    string Network, 
    string Name,     
    bool WithdrawEnable,
    bool DepositEnable,
    string WithdrawFee
);