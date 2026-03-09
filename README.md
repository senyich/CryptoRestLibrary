# CryptoExchangesRestLibrary

REST client library for cryptocurrency exchanges. Provides unified access to orderbooks, trading symbols, and withdrawal data across multiple exchanges.

## Supported Exchanges

- **Binance** - Spot market data
- **Bybit** - Spot market data
- **BingX** - Spot/swap market data
- **Gate.io** - Spot market data
- **KuCoin** - Spot market data
- **MEXC** - Spot market data

## Installation

```bash
dotnet add package CryptoExchangesRestLibrary
```

Or add directly to your `.csproj`:

```xml
<ItemGroup>
    <PackageReference Include="CryptoExchangesRestLibrary" Version="1.0.0" />
</ItemGroup>
```

## Quick Start

### Basic Usage

```csharp
using CryptoExchangesRestLibrary.RestClients;

// Create client
var binance = new BinanceRestClient();

// Get orderbook
var orderbook = await binance.GetOrderbookAsync("BTCUSDT", limit: 10);
Console.WriteLine($"Best bid: {orderbook.Bids.Keys.Max()}");
Console.WriteLine($"Best ask: {orderbook.Asks.Keys.Min()}");

// Get available symbols
var symbols = await binance.GetSymbolsAsync();
Console.WriteLine($"Available pairs: {symbols.Count}");

// Get trading URL
var url = binance.GetUrl("BTCUSDT");
```

### With Dependency Injection (ASP.NET Core)

```csharp
using CryptoExchangesRestLibrary.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Register all exchange clients
builder.Services.AddAllExchangeClients();

// Or register specific clients
builder.Services.AddBinanceClient();
builder.Services.AddBybitClient();
```

Then inject in your services:

```csharp
public class MyService
{
    private readonly BinanceRestClient _binance;

    public MyService(BinanceRestClient binance)
    {
        _binance = binance;
    }

    public async Task<decimal> GetBestBidAsync(string symbol)
    {
        var orderbook = await _binance.GetOrderbookAsync(symbol);
        return orderbook.Bids.Keys.Max();
    }
}
```

### Authenticated Requests (Withdrawal Data)

```csharp
var bybit = new BybitRestClient();
bybit.SetApiCredentials("your-api-key", "your-api-secret");

var withdrawalData = await bybit.GetWithdrawalDataAsync("BTC");
foreach (var chain in withdrawalData.Chains)
{
    Console.WriteLine($"{chain.Name}: Fee = {chain.Fee}, CanWithdraw = {chain.CanWithdraw}");
}
```

### KuCoin (Requires Passphrase)

```csharp
var kucoin = new KucoinRestClient();
kucoin.SetApiCredentials("your-api-key", "your-api-secret");
kucoin.SetPassPhrase("your-passphrase"); // Required for KuCoin

var withdrawalData = await kucoin.GetWithdrawalDataAsync("BTC");
```

## API Reference

### ExchangeRestClient Base Methods

| Method | Description |
|--------|-------------|
| `GetOrderbookAsync(symbol, limit, cancellationToken)` | Fetch orderbook with bids/asks |
| `GetSymbolsAsync(cancellationToken)` | Get list of available trading pairs |
| `GetWithdrawalDataAsync(symbol, cancellationToken)` | Get withdrawal networks and fees |
| `GetUrl(symbol)` | Get exchange trading page URL |

### Response Types

```csharp
// Orderbook response
public record class OrderbookResponse(
    string Symbol,
    Dictionary<decimal, decimal> Asks,  // Price -> Quantity
    Dictionary<decimal, decimal> Bids   // Price -> Quantity
);

// Withdrawal data
public record class WithdrawalDataResponse(
    string Symbol,
    List<BlockchainDataResponse> Chains
);

// Blockchain/chain info
public record class BlockchainDataResponse(
    string Name,
    string FullName,
    bool CanWithdraw,
    bool CanDeposit,
    decimal Fee
);
```

## Exception Handling

```csharp
using CryptoExchangesRestLibrary.Exceptions;

try
{
    var orderbook = await binance.GetOrderbookAsync("INVALID");
}
catch (ExchangeApiException ex)
{
    Console.WriteLine($"Exchange: {ex.ExchangeName}");
    Console.WriteLine($"Error: {ex.Message}");
}
catch (AuthenticationException ex)
{
    // Invalid API credentials
}
catch (RateLimitException ex)
{
    // Too many requests
    Console.WriteLine($"Retry after: {ex.RetryAfter}");
}
catch (DataParsingException ex)
{
    // Failed to parse API response
}
```

## Features

- **Unified API** - Same interface for all exchanges
- **Retry Policy** - Automatic retries on transient failures
- **Rate Limit Handling** - Proper handling of HTTP 429 responses
- **Dependency Injection** - Full support for `IHttpClientFactory`
- **Cancellation Tokens** - All async methods support cancellation
- **Type-Safe Responses** - Strongly-typed response records

## Building from Source

```bash
dotnet build
dotnet test  # When tests are added
dotnet pack  # Create NuGet package
```

## License

MIT License - see LICENSE file for details.
