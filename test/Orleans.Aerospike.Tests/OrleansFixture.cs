using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace Orleans.Aerospike.Tests
{
    public class OrleansFixture : IAsyncLifetime
    {
        public IHost Host { get; }
        public IClusterClient Client { get; }

        private const string ClusterId = "TESTCLUSTER";

        // Use distinct silo ports for each test as they may run in parallel.
        private static int portUniquifier;

        public OrleansFixture()
        {
            string serviceId = Guid.NewGuid().ToString();   // This is required by some tests; Reminders will parse it as a GUID.

            var portInc = Interlocked.Increment(ref portUniquifier);
            var siloPort = EndpointOptions.DEFAULT_SILO_PORT + portInc;
            var gatewayPort = EndpointOptions.DEFAULT_GATEWAY_PORT + portInc;
            var silo = new HostBuilder()
                .ConfigureLogging(builder =>  { builder.AddConsole(); })
                .UseOrleans(b =>
                {
                    b.UseLocalhostClustering();
                    b.ConfigureEndpoints(siloPort, gatewayPort);
                    b.Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = ClusterId;
                        options.ServiceId = serviceId;
                    });
                    //b.ConfigureApplicationParts(pm => pm.AddApplicationPart(typeof(PersistenceTests).Assembly));
                    PreBuild(b);
                })
                .Build();

            this.Host = silo;

            this.Client = this.Host.Services.GetRequiredService<IClusterClient>();
        }

        protected virtual ISiloBuilder PreBuild(ISiloBuilder builder) { return builder; }

        public const string TEST_STORAGE = "TEST_STORAGE_PROVIDER";

        public Task InitializeAsync() => this.Host.StartAsync();

        public Task DisposeAsync()
        {
            try
            {
                return this.Host.StopAsync();
            }
            catch { return Task.CompletedTask; }
        }

        public async Task DeactivateGrains()
        {
            var mg = Client.GetGrain<IManagementGrain>(0);
            await mg.ForceActivationCollection(new TimeSpan(0));
        }
    }
}
