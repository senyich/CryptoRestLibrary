namespace CryptoExchangesRestLibrary.SerializationClasses;

public record InnerBinanceOrderbookResponse(
    long LastUpdateId,
    List<List<string>> Bids,
    List<List<string>> Asks
);
public record InnerBinanceSymbolsResponce(
    string Symbol,
    string Price
);