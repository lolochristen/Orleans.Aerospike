using Aerospike.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans.Configuration;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Clustering.Aerospike
{
    public class AerospikeMembershipTable : IMembershipTable
    {
        private ClusterOptions _clusterOptions;
        private ILoggerFactory _loggerFactory;
        private ILogger<AerospikeMembershipTable> _logger;
        private AerospikeClusteringOptions _options;
        private AsyncClientPolicy _clientPolicy;
        private AsyncClient _client;

        public AerospikeMembershipTable(ILoggerFactory loggerFactory, IOptions<ClusterOptions> clusterOptions, IOptions<AerospikeClusteringOptions> clusteringOptions)
        {
            _clusterOptions = clusterOptions.Value;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory?.CreateLogger<AerospikeMembershipTable>();
            _options = clusteringOptions.Value;
        }

        public Task CleanupDefunctSiloEntries(DateTimeOffset beforeDate)
        {
            return Task.Run(() => _client.Truncate(new InfoPolicy { }, _options.Namespace, _options.Namespace, beforeDate.DateTime));
        }

        public async Task DeleteMembershipTableEntries(string clusterId)
        {
            var allEntries = await ReadAll();
            foreach(var entry in allEntries.Members)
            {
                _client.Delete(new WritePolicy(), new Key(_options.Namespace, _options.SetName, GetSiloEntityId(entry.Item1.SiloAddress)));
            }
        }

        public async Task InitializeMembershipTable(bool tryInitTableVersion)
        {
            _clientPolicy = new AsyncClientPolicy()
            {
                user = _options.Username,
                password = _options.Password
            };

            _client = new AsyncClient(_clientPolicy, _options.Host, _options.Port);

            await Task.Run(async () =>
            {
                await PutTableVersionEntry(new TableVersion(0, ""));

                try
                {
                    var task = _client.CreateIndex(null, _options.Namespace, _options.SetName, "clusterIdx", "clusterid", IndexType.STRING);
                    task.Wait();
                }
                catch(Exception)
                { }

            });
        }

        public async Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
        {
            await PutTableVersionEntry(tableVersion);
            await PutMembershipEntry(entry, null);
            return true;
        }

        public Task<MembershipTableData> ReadAll()
        {
            return Task.Run(() =>
            {
                var entries = new List<Tuple<MembershipEntry, string>>();

                var recordSetVersion = _client.Get(null, new Key(_options.Namespace, _options.SetName, _clusterOptions.ClusterId));

                TableVersion version = null;

                if (recordSetVersion != null)
                {
                    version = new TableVersion(recordSetVersion.GetInt("version"), recordSetVersion.generation.ToString());
                }

                try
                {
                    var recordSet = _client.Query(null, new Statement()
                    {
                        Filter = Filter.Equal("clusterid", _clusterOptions.ClusterId),
                        Namespace = _options.Namespace,
                        SetName = _options.SetName, 
                        IndexName = "clusterIdx"
                    });

                    while (recordSet.Next())
                    {
                        entries.Add(
                            new Tuple<MembershipEntry, string>(
                                ParseMembershipEntryRecord(recordSet.Record),
                                recordSet.Record.generation.ToString()));
                    }
                }
                catch (Exception exp)
                {

                }

                var data = new MembershipTableData(entries, version);
                return data;
            });
        }

        public async Task<MembershipTableData> ReadRow(SiloAddress key)
        {
            var siloId = GetSiloEntityId(key);

            var record = await _client.Get(null, Task.Factory.CancellationToken, new Key(_options.Namespace, _options.SetName, siloId));
            var entries = new List<Tuple<MembershipEntry, string>>();
            entries.Add(new Tuple<MembershipEntry, string>(ParseMembershipEntryRecord(record), record.generation.ToString()));

            TableVersion version = null;
            var recordSetVersion = _client.Get(null, new Key(_options.Namespace, _options.SetName, _clusterOptions.ClusterId));
            if (recordSetVersion != null)
                version = new TableVersion(recordSetVersion.GetInt("version"), recordSetVersion.generation.ToString());

            var data = new MembershipTableData(entries, version);
            return data;
        }

        private static MembershipEntry ParseMembershipEntryRecord(Record record)
        {
            var entry = new MembershipEntry();
            entry.FaultZone = record.GetInt("faultzone");
            entry.HostName = record.GetString("hostname");
            entry.IAmAliveTime = DateTime.FromBinary(record.GetLong("iamalivetime"));
            entry.ProxyPort = record.GetInt("proxyport");
            entry.RoleName = record.GetString("rolename");
            entry.SiloAddress = SiloAddress.New(new System.Net.IPEndPoint(System.Net.IPAddress.Parse(record.GetString("address")), record.GetInt("port")), record.GetInt("generation"));
            entry.SiloName = record.GetString("siloname");
            entry.StartTime = DateTime.FromBinary(record.GetLong("starttime"));
            entry.Status = (SiloStatus)record.GetInt("status");
            var st = record.GetString("suspecttimes");
            if (!string.IsNullOrEmpty(st))
                entry.SuspectTimes = JsonConvert.DeserializeObject<List<Tuple<SiloAddress, DateTime>>>(st);
            entry.UpdateZone = record.GetInt("updatezone");
            return entry;
        }

        public async Task UpdateIAmAlive(MembershipEntry entry)
        {
            await PutMembershipEntry(entry, null);
        }

        public async Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion)
        {
            await PutTableVersionEntry(tableVersion);
            await PutMembershipEntry(entry, etag);
            return true;
        }

        private async Task PutMembershipEntry(MembershipEntry entry, string etag)
        {
            var siloid = GetSiloEntityId(entry.SiloAddress);
            var bins = new Bin[]
            {
                new Bin("clusterid", _clusterOptions.ClusterId),
                new Bin("address", entry.SiloAddress.Endpoint.Address.ToString()),
                new Bin("port", entry.SiloAddress.Endpoint.Port),
                new Bin("generation", entry.SiloAddress.Generation),
                new Bin("faultzone", entry.FaultZone),
                new Bin("hostname", entry.HostName),
                new Bin("iamalivetime", entry.IAmAliveTime.ToBinary()),
                new Bin("proxyport", entry.ProxyPort),
                new Bin("rolename", entry.RoleName),
                new Bin("siloname", entry.SiloName),
                new Bin("starttime", entry.StartTime.ToBinary()),
                new Bin("status", (int) entry.Status),
                new Bin("suspecttimes", JsonConvert.SerializeObject(entry.SuspectTimes)),
                new Bin("updatezone", entry.UpdateZone),
            };

            if (string.IsNullOrEmpty(etag))
            {
                await _client.Put(
                    new WritePolicy(_clientPolicy.writePolicyDefault) { sendKey = true }, 
                    Task.Factory.CancellationToken, 
                    new Key(_options.Namespace, _options.SetName, siloid), bins);
            }
            else
            {
                await _client.Put(
                    new WritePolicy(_clientPolicy.writePolicyDefault) { sendKey = true, generationPolicy = GenerationPolicy.EXPECT_GEN_EQUAL, generation = int.Parse(etag)}, 
                    Task.Factory.CancellationToken, 
                    new Key(_options.Namespace, _options.SetName, siloid), bins);
            }
        }

        private async Task PutTableVersionEntry(TableVersion version)
        {
            var id = _clusterOptions.ClusterId;
            var bins = new Bin[]
            {
                new Bin("version", version.Version),
            };

            if (string.IsNullOrEmpty(version.VersionEtag))
            {
                await _client.Put(new WritePolicy(_clientPolicy.writePolicyDefault) { sendKey = true }, Task.Factory.CancellationToken, new Key(_options.Namespace, _options.SetName, id), bins);
            }
            else
            {
                await _client.Put(
                    new WritePolicy() { sendKey = true, generationPolicy = GenerationPolicy.EXPECT_GEN_EQUAL, generation = int.Parse(version.VersionEtag) }, 
                    Task.Factory.CancellationToken, 
                    new Key(_options.Namespace, _options.SetName, id), bins);
            }

        }

        private static string GetSiloEntityId(SiloAddress silo)
        {
            return $"{silo.Endpoint.Address}-{silo.Endpoint.Port}-{silo.Generation}";
        }
    }
}