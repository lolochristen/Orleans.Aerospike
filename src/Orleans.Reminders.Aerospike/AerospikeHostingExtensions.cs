using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Hosting;
using System;

namespace Orleans.Reminders.Aerospike
{
    public static class AerospikeHostingExtensions
    {
        public static ISiloBuilder UseAerospikeReminder(this ISiloBuilder builder,
           Action<AerospikeReminderStorageOptions> configureOptions)
        {
            return builder.ConfigureServices(services => services.UseAerospikeReminder(configureOptions));
        }

        public static ISiloBuilder UseAerospikeReminder(this ISiloBuilder builder,
            Action<OptionsBuilder<AerospikeReminderStorageOptions>> configureOptions)
        {
            return builder.ConfigureServices(services => services.UseAerospikeReminder(configureOptions));
        }

        public static ISiloBuilder UseAerospikeReminder(this ISiloBuilder builder)
        {
            return builder.ConfigureServices(services =>
            {
                services.AddOptions<AerospikeReminderStorageOptions>();
                services.AddSingleton<IReminderTable, AerospikeReminderTable>();
            });
        }

        public static IServiceCollection UseAerospikeReminder(this IServiceCollection services,
            Action<AerospikeReminderStorageOptions> configureOptions)
        {
            return services.UseAerospikeReminder(ob => ob.Configure(configureOptions));
        }

        public static IServiceCollection UseAerospikeReminder(this IServiceCollection services,
            Action<OptionsBuilder<AerospikeReminderStorageOptions>> configureOptions)
        {
            configureOptions?.Invoke(services.AddOptions<AerospikeReminderStorageOptions>());
            return services.AddSingleton<IReminderTable, AerospikeReminderTable>();
        }

    }
}
