using Microsoft.Extensions.DependencyInjection;

namespace CryptoExchangesRestLibrary.DependencyInjection;

/// <summary>
/// Extension methods for registering exchange clients with dependency injection.
/// </summary>
public static class ExchangeClientServiceCollectionExtensions
{
    /// <summary>
    /// Adds Binance REST client to the service collection.
    /// </summary>
    public static IServiceCollection AddBinanceClient(this IServiceCollection services)
    {
        services.AddHttpClient<RestClients.BinanceRestClient>();
        return services;
    }

    /// <summary>
    /// Adds BingX REST client to the service collection.
    /// </summary>
    public static IServiceCollection AddBingXClient(this IServiceCollection services)
    {
        services.AddHttpClient<RestClients.BingXRestClient>();
        return services;
    }

    /// <summary>
    /// Adds Bybit REST client to the service collection.
    /// </summary>
    public static IServiceCollection AddBybitClient(this IServiceCollection services)
    {
        services.AddHttpClient<RestClients.BybitRestClient>();
        return services;
    }

    /// <summary>
    /// Adds Gate.io REST client to the service collection.
    /// </summary>
    public static IServiceCollection AddGateIoClient(this IServiceCollection services)
    {
        services.AddHttpClient<RestClients.GateIoRestClient>();
        return services;
    }

    /// <summary>
    /// Adds KuCoin REST client to the service collection.
    /// </summary>
    public static IServiceCollection AddKucoinClient(this IServiceCollection services)
    {
        services.AddHttpClient<RestClients.KucoinRestClient>();
        return services;
    }

    /// <summary>
    /// Adds MEXC REST client to the service collection.
    /// </summary>
    public static IServiceCollection AddMexcClient(this IServiceCollection services)
    {
        services.AddHttpClient<RestClients.MexcRestClient>();
        return services;
    }

    /// <summary>
    /// Adds all exchange clients to the service collection.
    /// </summary>
    public static IServiceCollection AddAllExchangeClients(this IServiceCollection services)
    {
        services.AddBinanceClient();
        services.AddBingXClient();
        services.AddBybitClient();
        services.AddGateIoClient();
        services.AddKucoinClient();
        services.AddMexcClient();
        return services;
    }
}
