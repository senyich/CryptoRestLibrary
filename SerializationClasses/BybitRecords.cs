namespace CryptoExchangesRestLibrary.SerializationClasses.Bybit ;

public record ApiResponse<T>(
    int RetCode,
    string RetMsg,
    T Result
);
public record BybitTickersResult(
    string Category,
    List<BybitTickerItem> List
);
public record BybitTickerItem(
    string Symbol,
    string Bid1Price,
    string Bid1Size,
    string Ask1Price,
    string Ask1Size,
    string LastPrice,
    string PrevPrice24h,
    string Price24hPcnt,
    string HighPrice24h,
    string LowPrice24h,
    string Turnover24h,
    string Volume24h
);
public record BybitOrderbookInnerResult(
    string S,
    List<List<string>> A, 
    List<List<string>> B
);
public record CoinInfoResult(
    List<CoinInfoRow> Rows
);

public record CoinInfoRow(
    string Name,
    string Coin,
    string RemainAmount,
    List<ChainInfo> Chains
);

public record ChainInfo(
    string ChainType,
    string Confirmation,
    string WithdrawFee,
    string DepositMin,
    string WithdrawMin,
    string Chain,
    string ChainDeposit,
    string ChainWithdraw,
    string MinAccuracy,
    string WithdrawPercentageFee,
    string ContractAddress,
    string SafeConfirmNumber
);