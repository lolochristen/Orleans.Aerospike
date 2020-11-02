using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Persistence.Aerospike.Serializer;
using OrleansHotelBooking.Grains;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace OrleansHotelBooking.SiloHost
{
    class Program
    {
        static async Task Main(string[] args)
        {
            int siloPort = 11111;
            int gatewayPort = 30000;
            IPEndPoint primaryEndpoint = null;

            try
            {
                if (args.Length > 0)
                {
                    // cluster node
                    var instanceId = int.Parse(args[0]);
                    siloPort += instanceId;
                    gatewayPort += instanceId;

                    Console.WriteLine($"Start Cluster {instanceId} siloPort:{siloPort} gatewayPort:{gatewayPort}");

                    await new HostBuilder()
                         .UseOrleans(builder =>
                         {
                             builder
                                .Configure<ClusterOptions>(options =>
                                {
                                    options.ClusterId = "dev";
                                    options.ServiceId = "OrleansHotelBooking";
                                })
                                .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
                                .ConfigureEndpoints(siloPort, gatewayPort)
                                .UseAerospikeMembership()
                                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(HotelGrain).Assembly).WithReferences())
                                .AddAerospikeGrainStorage("bookingStorage")
                                //, options =>
                                //{
                                //    options.Host = "localhost";
                                //    options.Port = 3000;
                                //    options.Namespace = "dev";
                                //})
                                .UseAerospikeSerializer<AerospikePropertySerializer>();
                         })
                         .ConfigureServices(services =>
                         {
                             services.Configure<ConsoleLifetimeOptions>(options =>
                             {
                                 options.SuppressStatusMessages = true;
                             });
                         })
                         .ConfigureLogging(builder => { builder.AddConsole(); })
                         .RunConsoleAsync();
                }
                else
                {
                    // local dev
                    await new HostBuilder()
                         .UseOrleans(builder =>
                         {
                             builder
                                .UseLocalhostClustering(siloPort: siloPort, primarySiloEndpoint: primaryEndpoint, gatewayPort: gatewayPort) // local dev
                                .Configure<ClusterOptions>(options =>
                                {
                                    options.ClusterId = "dev";
                                    options.ServiceId = "OrleansHotelBooking";
                                })
                                .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
                                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(HotelGrain).Assembly).WithReferences())
                                .AddAerospikeGrainStorage("bookingStorage")
                                .UseAerospikeSerializer<AerospikePropertySerializer>();
                         })
                         .ConfigureServices(services =>
                         {
                             services.Configure<ConsoleLifetimeOptions>(options =>
                             {
                                 options.SuppressStatusMessages = true;
                             });
                         })
                         .ConfigureLogging(builder => { builder.AddConsole(); })
                         .RunConsoleAsync();
                }
            }
            catch(Exception exp)
            {
                Console.WriteLine("Error running Silo: " + exp.ToString());
            }
        }
    }
}
