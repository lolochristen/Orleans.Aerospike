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
            return Task.Run(() =>
            {
                try
                {
                    _client.Truncate(null, _options.Namespace, _options.SetName, beforeDate.DateTime);
                }
                catch(Exception exp)
                {
                    _logger.LogError(exp, "Truncate failed. {0} {1} {2} ", _options.Namespace, _options.SetName, beforeDate);
                    throw;
                }
            });
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
            await Task.Run(async () =>
            {
                _clientPolicy = new AsyncClientPolicy()
                {
                    user = _options.Username,
                    password = _options.Password
                };

                _client = new AsyncClient(_clientPolicy, _options.Host, _options.Port);


                if (_options.CleanupOnInit)
                {
                    _client.Truncate(null, _options.Namespace, _options.SetName, null);
                }

                await PutTableVersionEntry(new TableVersion(0, ""));

                try
                {
                    var task = _client.CreateIndex(null, _options.Namespace, _options.SetName, _options.SetName + "_clusterIdx", "clusterid", IndexType.STRING);
                    task.Wait();
                }
                catch(Exception)
                {
                    // todo: evaluate if error comes from multiple index creation or other source
                }
            });
        }

        public async Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
        {
            try
            {
                await PutMembershipEntry(entry, null, false);
                await PutTableVersionEntry(tableVersion);
                return true;
            }
            catch(Exception exp)
            {
                _logger.LogError(exp, "Insert MembershipEntry failed.");
                return false;
            }
        }

        public async Task<MembershipTableData> ReadAll()
        {
            var entries = new List<Tuple<MembershipEntry, string>>();

            TableVersion version = await ReadTableVersion();

            try
            {
                var recordSet = _client.Query(null, new Statement()
                {
                    Filter = Filter.Equal("clusterid", _clusterOptions.ClusterId),
                    Namespace = _options.Namespace,
                    SetName = _options.SetName
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
        }

        private async Task<TableVersion> ReadTableVersion()
        {
            TableVersion version = null;
            var recordVersion = await _client.Get(null, Task.Factory.CancellationToken, new Key(_options.Namespace, _options.SetName, _clusterOptions.ClusterId));
            if (recordVersion != null)
                version = new TableVersion(recordVersion.GetInt("version"), recordVersion.generation.ToString());
            return version;
        }

        public async Task<MembershipTableData> ReadRow(SiloAddress key)
        {
            var siloId = GetSiloEntityId(key);

            var record = await _client.Get(null, Task.Factory.CancellationToken, new Key(_options.Namespace, _options.SetName, siloId));
            var entries = new List<Tuple<MembershipEntry, string>>();
            entries.Add(new Tuple<MembershipEntry, string>(ParseMembershipEntryRecord(record), record.generation.ToString()));

            TableVersion version = await ReadTableVersion();

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
                entry.SuspectTimes = JsonConvert.DeserializeObject<List<Tuple<SiloAddress, DateTime>>>(st, JsonSerializerHelper.SerializerSettings);
            entry.UpdateZone = record.GetInt("updatezone");
            return entry;
        }

        public async Task UpdateIAmAlive(MembershipEntry entry)
        {
            var siloid = GetSiloEntityId(entry.SiloAddress);
            await _client.Put(
                   new WritePolicy(_clientPolicy.writePolicyDefault)
                   {
                       recordExistsAction = RecordExistsAction.UPDATE
                   },
                   Task.Factory.CancellationToken,
                   new Key(_options.Namespace, _options.SetName, siloid), 
                   new Bin[] { new Bin("iamalivetime", entry.IAmAliveTime.ToBinary()) });
        }

        public async Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion)
        {
            try
            {
                await PutMembershipEntry(entry, etag, true);
                await PutTableVersionEntry(tableVersion);
                return true;
            }
            catch(Exception exp)
            {
                _logger.LogError(exp, "Update MembershipEntry failed.");
                return false;
            }
        }

        private Bin[] BuildMembershipEntryBins(MembershipEntry entry)
        {
            return new Bin[]
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
                new Bin("suspecttimes", JsonConvert.SerializeObject(entry.SuspectTimes, JsonSerializerHelper.SerializerSettings)),
                new Bin("updatezone", entry.UpdateZone),
            };
        }

        private async Task PutMembershipEntry(MembershipEntry entry, string etag, bool isUpdate)
        {
            var siloid = GetSiloEntityId(entry.SiloAddress);
            var bins = BuildMembershipEntryBins(entry);

            if (string.IsNullOrEmpty(etag) || _options.VerifyEtagGenerations == false)
            {
                await _client.Put(
                    new WritePolicy(_clientPolicy.writePolicyDefault)
                    {
                        sendKey = true, 
                        recordExistsAction = isUpdate ? RecordExistsAction.UPDATE : RecordExistsAction.CREATE_ONLY 
                    }, 
                    Task.Factory.CancellationToken, 
                    new Key(_options.Namespace, _options.SetName, siloid), bins);
            }
            else
            {
                await _client.Put(
                    new WritePolicy(_clientPolicy.writePolicyDefault)
                    {
                        sendKey = true,
                        recordExistsAction = isUpdate ? RecordExistsAction.UPDATE : RecordExistsAction.CREATE_ONLY,
                        generationPolicy = GenerationPolicy.EXPECT_GEN_EQUAL, 
                        generation = int.Parse(etag)
                    }, 
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

            if (string.IsNullOrEmpty(version.VersionEtag) || _options.VerifyEtagGenerations == false)
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