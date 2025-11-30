using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CryptoExchangesRestLibrary.SerializationClasses.Mexc;
public record MexcSymbolsResponse(
    List<string> Data,
    int Code,
    string Msg,
    long Timestamp
);
public record InnerMexcOrderbookResponse(
    long LastUpdateId,
    [property: JsonPropertyName("bids")] List<List<string>> Bids,
    [property: JsonPropertyName("asks")] List<List<string>> Asks
);
public record CoinConfigApiResponse(
    [property: JsonPropertyName("coin")] string Coin,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("networkList")] List<NetworkConfigApiResponse> NetworkList
);

public record NetworkConfigApiResponse(
    [property: JsonPropertyName("network")] string Network,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("withdrawEnable")] bool WithdrawEnable,
    [property: JsonPropertyName("depositEnable")] bool DepositEnable,
    [property: JsonPropertyName("withdrawFee")] string WithdrawFee
);