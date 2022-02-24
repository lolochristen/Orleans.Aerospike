using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Persistence.Aerospike;
using Orleans.Storage;
using System;

namespace Orleans.Persistence
{
    public static class AerospikeGrainStorageFactory
    {
        /// <summary>
        /// Creates a grain storage instance.
        /// </summary>
        public static IGrainStorage Create(IServiceProvider services, string name)
        {
            IOptionsMonitor<AerospikeStorageOptions> options = services.GetRequiredService<IOptionsMonitor<AerospikeStorageOptions>>();
            return ActivatorUtilities.CreateInstance<AerospikeGrainStorage>(services, options.Get(name), name);
        }
    }
}
