using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Persistence;
using Orleans.Providers;
using Orleans.Persistence.Aerospike;
using Orleans.Runtime;
using Orleans.Storage;
using System;
using System.Collections.Generic;
using System.Text;
using Orleans.Persistence.Aerospike.Serializer;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Hosting extensions for the Aerospike storage provider.
    /// </summary>
    public static class AeorpspikeHostingExtensions
    {
        /// <summary>
        /// Adds a Aerospike grain storage provider as the default provider
        /// </summary>
        public static ISiloHostBuilder AddAerospikeGrainStorageAsDefault(this ISiloHostBuilder builder, Action<AerospikeStorageOptions> configureOptions)
        {
            return builder.AddAerospikeGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
        }

        /// <summary>
        /// Adds a Aerospike grain storage provider.
        /// </summary>
        public static ISiloHostBuilder AddAerospikeGrainStorage(this ISiloHostBuilder builder, string name, Action<AerospikeStorageOptions> configureOptions)
        {
            return builder.ConfigureServices(services => services.AddAerospikeGrainStorage(name, configureOptions));
        }

        /// <summary>
        /// Adds a Aerospike grain storage provider as the default provider
        /// </summary>
        public static ISiloHostBuilder AddAerospikeGrainStorageAsDefault(this ISiloHostBuilder builder, Action<OptionsBuilder<AerospikeStorageOptions>> configureOptions = null)
        {
            return builder.AddAerospikeGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
        }

        /// <summary>
        /// Adds a Aerospike grain storage provider.
        /// </summary>
        public static ISiloHostBuilder AddAerospikeGrainStorage(this ISiloHostBuilder builder, string name, Action<OptionsBuilder<AerospikeStorageOptions>> configureOptions = null)
        {
            return builder.ConfigureServices(services => services.AddAerospikeGrainStorage(name, configureOptions));
        }

        /// <summary>
        /// Adds a Aerospike grain storage provider as the default provider
        /// </summary>
        public static ISiloBuilder AddAerospikeGrainStorageAsDefault(this ISiloBuilder builder, Action<AerospikeStorageOptions> configureOptions)
        {
            return builder.AddAerospikeGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
        }

        /// <summary>
        /// Adds a Aerospike grain storage provider.
        /// </summary>
        public static ISiloBuilder AddAerospikeGrainStorage(this ISiloBuilder builder, string name, Action<AerospikeStorageOptions> configureOptions)
        {
            return builder.ConfigureServices(services => services.AddAerospikeGrainStorage(name, configureOptions));
        }

        /// <summary>
        /// Adds a Aerospike grain storage provider as the default provider
        /// </summary>
        public static ISiloBuilder AddAerospikeGrainStorageAsDefault(this ISiloBuilder builder, Action<OptionsBuilder<AerospikeStorageOptions>> configureOptions = null)
        {
            return builder.AddAerospikeGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
        }

        /// <summary>
        /// Adds a Aerospike grain storage provider.
        /// </summary>
        public static ISiloBuilder AddAerospikeGrainStorage(this ISiloBuilder builder, string name, Action<OptionsBuilder<AerospikeStorageOptions>> configureOptions = null)
        {
            return builder.ConfigureServices(services => services.AddAerospikeGrainStorage(name, configureOptions));
        }

        /// <summary>
        /// Adds a Aerospike grain storage provider as the default provider
        /// </summary>
        public static IServiceCollection AddAerospikeGrainStorageAsDefault(this IServiceCollection services, Action<AerospikeStorageOptions> configureOptions)
        {
            return services.AddAerospikeGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, ob => ob.Configure(configureOptions));
        }

        /// <summary>
        /// Adds a Aerospike grain storage provider.
        /// </summary>
        public static IServiceCollection AddAerospikeGrainStorage(this IServiceCollection services, string name, Action<AerospikeStorageOptions> configureOptions)
        {
            return services.AddAerospikeGrainStorage(name, ob => ob.Configure(configureOptions));
        }

        /// <summary>
        /// Adds a Aerospike grain storage provider as the default provider
        /// </summary>
        public static IServiceCollection AddAerospikeGrainStorageAsDefault(this IServiceCollection services, Action<OptionsBuilder<AerospikeStorageOptions>> configureOptions = null)
        {
            return services.AddAerospikeGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
        }

        /// <summary>
        /// Adds a Aerospike grain storage provider.
        /// </summary>
        public static IServiceCollection AddAerospikeGrainStorage(this IServiceCollection services, string name,
            Action<OptionsBuilder<AerospikeStorageOptions>> configureOptions = null)
        {
            configureOptions?.Invoke(services.AddOptions<AerospikeStorageOptions>(name));
            services.AddTransient<IConfigurationValidator>(sp => new AerospikeStorageOptionsValidator(sp.GetService<IOptionsMonitor<AerospikeStorageOptions>>().Get(name), name));
            services.ConfigureNamedOptionForLogging<AerospikeStorageOptions>(name);
            services.TryAddSingleton(sp => sp.GetServiceByName<IGrainStorage>(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME));
            services.AddTransient<IAerospikeSerializer, AerospikeOrleansSerializer>();
            return services.AddSingletonNamedService(name, AerospikeGrainStorageFactory.Create)
                           .AddSingletonNamedService(name, (s, n) => (ILifecycleParticipant<ISiloLifecycle>)s.GetRequiredServiceByName<IGrainStorage>(n));
        }

        public static IServiceCollection UseAerospikeSerializer<T>(this IServiceCollection services) where T : class, IAerospikeSerializer
        {
            services.RemoveAll<IAerospikeSerializer>();
            return services.AddTransient<IAerospikeSerializer, T>();
        }

        public static ISiloBuilder UseAerospikeSerializer<T>(this ISiloBuilder builder) where T : class, IAerospikeSerializer
        {
            return builder.ConfigureServices(services => services.UseAerospikeSerializer<T>());
        }
    }
}
