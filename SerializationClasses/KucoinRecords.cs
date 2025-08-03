using System.Text.Json.Serialization;

namespace CryptoExchangesRestLibrary.SerializationClasses;

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
