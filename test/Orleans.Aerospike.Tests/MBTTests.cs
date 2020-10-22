using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Clustering.Aerospike;
using Orleans.Configuration;
using Orleans.Messaging;
using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Orleans.Aerospike.Tests
{
    /// <summary>
    /// Tests for operation of Orleans Membership Table using Azure Aerospike DB
    /// </summary>
    public class MBTTests : MembershipTableTestsBase/*, IClassFixture<AzureStorageBasicTests>*/
    {
        public MBTTests() : base(CreateFilters())
        {
        }

        private static LoggerFilterOptions CreateFilters()
        {
            var filters = new LoggerFilterOptions();
            //filters.AddFilter(typeof(OrleansSiloInstanceManager).FullName, LogLevel.Trace);
            //filters.AddFilter("Orleans.Storage", LogLevel.Trace);
            return filters;
        }

        protected override IMembershipTable CreateMembershipTable(ILogger logger)
        {
            var options = new AerospikeClusteringOptions()
            {
                CleanupOnInit = true,
                SetName = "OrleansMBRTest"
            };
            return new AerospikeMembershipTable(this.loggerFactory, Options.Create(new ClusterOptions { ClusterId = this.clusterId }), Options.Create(options));
        }

        protected override IGatewayListProvider CreateGatewayListProvider(ILogger logger)
        {
            var options = new AerospikeGatewayOptions()
            {
                SetName = "OrleansMBRTest"
            };

            return new AerospikeGatewayListProvider(this.loggerFactory,
                Options.Create(options),
                Options.Create(new ClusterOptions { ClusterId = this.clusterId }),
                Options.Create(new GatewayOptions()));
        }

        protected override Task<string> GetConnectionString()
        {
            //TestUtils.CheckForAzureStorage();
            return Task.FromResult("");
        }

        [Fact]
        public async Task GetGateways()
        {
            await MembershipTable_GetGateways();
        }

        [Fact]
        public async Task ReadAll_EmptyTable()
        {
            await MembershipTable_ReadAll_EmptyTable();
        }

        [Fact]
        public async Task InsertRow()
        {
            await MembershipTable_InsertRow();
        }

        [Fact]
        public async Task ReadRow_Insert_Read()
        {
            await MembershipTable_ReadRow_Insert_Read();
        }

        [Fact]
        public async Task ReadAll_Insert_ReadAll()
        {
            await MembershipTable_ReadAll_Insert_ReadAll();
        }

        [Fact]
        public async Task UpdateRow()
        {
            await MembershipTable_UpdateRow();
        }

        [Fact]
        public async Task UpdateIAmAlive()
        {
            await MembershipTable_UpdateIAmAlive();
        }
    }
}
