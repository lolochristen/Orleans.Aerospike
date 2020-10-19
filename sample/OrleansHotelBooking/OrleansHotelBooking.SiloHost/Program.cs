using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Persistence.Aerospike.Serializer;
using OrleansHotelBooking.Grains;
using System;
using System.Net;
using System.Threading.Tasks;

namespace OrleansHotelBooking.SiloHost
{
    class Program
    {
        static Task Main(string[] args)
        {
            int siloPort = 11111;
            int gatewayPort = 30000;
            IPEndPoint primaryEndpoint = null;

            return new HostBuilder()
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
                        //.ConfigureEndpoints(siloPort, gatewayPort) // real cluster
                        //.UseAerospikeMembership() // for real cluster
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
    }
}
