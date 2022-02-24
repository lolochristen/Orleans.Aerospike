using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Clustering.Aerospike;
using Orleans.Hosting;
using Orleans.Messaging;
using System;

namespace Microsoft.Extensions.Hosting
{
    public static class AerospikeHostingExtensions
    {
        public static ISiloBuilder UseAerospikeMembership(this ISiloBuilder builder,
           Action<AerospikeClusteringOptions> configureOptions)
        {
            return builder.ConfigureServices(services => services.UseAerospikeMembership(configureOptions));
        }

        public static ISiloBuilder UseAerospikeMembership(this ISiloBuilder builder,
            Action<OptionsBuilder<AerospikeClusteringOptions>> configureOptions)
        {
            return builder.ConfigureServices(services => services.UseAerospikeMembership(configureOptions));
        }

        public static ISiloBuilder UseAerospikeMembership(this ISiloBuilder builder)
        {
            return builder.ConfigureServices(services =>
            {
                services.AddOptions<AerospikeClusteringOptions>();
                services.AddSingleton<IMembershipTable, AerospikeMembershipTable>();
            });
        }

        public static IClientBuilder UseAerospikeGatewayListProvider(this IClientBuilder builder, Action<AerospikeGatewayOptions> configureOptions)
        {
            return builder.ConfigureServices(services => services.UseAerospikeGatewayListProvider(configureOptions));
        }

        public static IClientBuilder UseAerospikeGatewayListProvider(this IClientBuilder builder)
        {
            return builder.ConfigureServices(services =>
            {
                services.AddOptions<AerospikeGatewayOptions>();
                services.AddSingleton<IGatewayListProvider, AerospikeGatewayListProvider>();
            });
        }

        public static IClientBuilder UseAerospikeGatewayListProvider(this IClientBuilder builder, Action<OptionsBuilder<AerospikeGatewayOptions>> configureOptions)
        {
            return builder.ConfigureServices(services => services.UseAerospikeGatewayListProvider(configureOptions));
        }

        public static IServiceCollection UseAerospikeMembership(this IServiceCollection services,
            Action<AerospikeClusteringOptions> configureOptions)
        {
            return services.UseAerospikeMembership(ob => ob.Configure(configureOptions));
        }

        public static IServiceCollection UseAerospikeMembership(this IServiceCollection services,
            Action<OptionsBuilder<AerospikeClusteringOptions>> configureOptions)
        {
            configureOptions?.Invoke(services.AddOptions<AerospikeClusteringOptions>());
            return services.AddSingleton<IMembershipTable, AerospikeMembershipTable>();
        }

        public static IServiceCollection UseAerospikeGatewayListProvider(this IServiceCollection services,
            Action<AerospikeGatewayOptions> configureOptions)
        {
            return services.UseAerospikeGatewayListProvider(ob => ob.Configure(configureOptions));
        }

        public static IServiceCollection UseAerospikeGatewayListProvider(this IServiceCollection services,
            Action<OptionsBuilder<AerospikeGatewayOptions>> configureOptions)
        {
            configureOptions?.Invoke(services.AddOptions<AerospikeGatewayOptions>());
            return services.AddSingleton<IGatewayListProvider, AerospikeGatewayListProvider>();
        }
    }
}
