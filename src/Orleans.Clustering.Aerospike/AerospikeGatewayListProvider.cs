using Aerospike.Client;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Messaging;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.Clustering.Aerospike
{
    public class AerospikeGatewayListProvider : IGatewayListProvider, IDisposable
    {
        private bool _isUpdated;
        private readonly string _clusterId;
        private readonly TimeSpan _maxStaleness;
        private readonly AerospikeGatewayOptions _options;
        private AsyncClientPolicy _clientPolicy;
        private AsyncClient _client;

        public TimeSpan MaxStaleness => _maxStaleness;

        public bool IsUpdatable => _isUpdated;

        public AerospikeGatewayListProvider(IOptions<AerospikeGatewayOptions> options,
            IOptions<ClusterOptions> clusterOptions, IOptions<GatewayOptions> gatewayOptions)
        {
            _clusterId = clusterOptions.Value.ClusterId;
            _maxStaleness = gatewayOptions.Value.GatewayListRefreshPeriod;
            _options = options.Value;
        }

        public Task<IList<Uri>> GetGateways()
        {
            return Task.Run<IList<Uri>>(() =>
            {
                var recordSet = _client.Query(null, new Statement()
                {
                    Filter = Filter.Equal("clusterid", _clusterId),
                    Namespace = _options.Namespace,
                    SetName = _options.SetName
                });

                var uris = new List<Uri>();
                while (recordSet.Next())
                {
                    var record = recordSet.Record;

                    var status = (SiloStatus)record.GetInt("status");
                    var proxyPort = record.GetInt("proxyport");

                    if (status == SiloStatus.Active && proxyPort > 0)
                    {
                        var address = SiloAddress.New(new System.Net.IPEndPoint(System.Net.IPAddress.Parse(record.GetString("address")), proxyPort), record.GetInt("generation"));
                        uris.Add(address.ToGatewayUri());
                    }
                }

                return uris;
            });
        }

        public Task InitializeGatewayListProvider()
        {
            return Task.Run(() =>
            {
                _clientPolicy = new AsyncClientPolicy()
                {
                    user = _options.Username,
                    password = _options.Password
                };

                _client = new AsyncClient(_clientPolicy, _options.Host, _options.Port);
            });
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
