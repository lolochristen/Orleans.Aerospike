using Aerospike.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Messaging;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Clustering.Aerospike
{
    public class AerospikeGatewayListProvider : IGatewayListProvider
    {
        private TimeSpan _maxStalness;
        private bool _isUpdated;
        private string _clusterId;
        private TimeSpan _maxStaleness;
        private ILoggerFactory _loggerFactory;
        private AerospikeGatewayOptions _options;
        private AsyncClient _client;

        public TimeSpan MaxStaleness => _maxStalness;

        public bool IsUpdatable => _isUpdated;

        public AerospikeGatewayListProvider(ILoggerFactory loggerFactory, IOptions<AerospikeGatewayOptions> options,
            IOptions<ClusterOptions> clusterOptions, IOptions<GatewayOptions> gatewayOptions)
        {
            _clusterId = clusterOptions.Value.ClusterId;
            _maxStaleness = gatewayOptions.Value.GatewayListRefreshPeriod;
            _loggerFactory = loggerFactory;
            _options = options.Value;
        }

        public Task<IList<Uri>> GetGateways()
        {
            return Task.Run<IList<Uri>>(() =>
            {
                var recordSet = _client.Query(new QueryPolicy { sendKey = true }, new Statement()
                {
                    Filter = Filter.Equal("clusterid", _clusterId),
                    Namespace = _options.Namespace,
                    SetName = _options.SetName,
                    IndexName = "clusterIdx"
                });

                var uris = new List<Uri>();
                while (recordSet.Next())
                {
                    var record = recordSet.Record;

                    var status = (SiloStatus)record.GetInt("status");

                    if (status == SiloStatus.Active)
                    {
                        var address = SiloAddress.New(new System.Net.IPEndPoint(System.Net.IPAddress.Parse(record.GetString("address")), record.GetInt("proxyport")), record.GetInt("generation"));
                        uris.Add(address.ToGatewayUri());
                    }
                }

                return uris;
            });
        }

        public async Task InitializeGatewayListProvider()
        {
            _client = new AsyncClient(_options.Host, _options.Port);
        }
    }
}
