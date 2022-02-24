using Aerospike.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans.ApplicationParts;
using Orleans.Persistence.Aerospike.Serializer;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Storage;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Orleans.Persistence.Aerospike
{
    public class AerospikeGrainStorage : IGrainStorage, ILifecycleParticipant<ISiloLifecycle>
    {
        private AerospikeStorageOptions _options;
        private string _name;
        private ILogger _logger;
        private AsyncClient _client;
        private IAerospikeSerializer _aerospikeSerializer;

        private AsyncClientPolicy _clientPolicy;
        private BatchPolicy _readPolicy;
        private WritePolicy _writeStatePolicy;

        public AerospikeGrainStorage(string name,
            AerospikeStorageOptions options,
            IAerospikeSerializer aerospikeSerializer,
            ILoggerFactory loggerFactory)
        {
            _options = options;
            _name = name;
            _logger = loggerFactory.CreateLogger($"{typeof(AerospikeGrainStorage).FullName}.{name}");
            _aerospikeSerializer = aerospikeSerializer;
        }

        public void Participate(ISiloLifecycle lifecycle)
        {
            var name = OptionFormattingUtilities.Name<AerospikeGrainStorage>(_name);
            lifecycle.Subscribe(name, _options.InitStage, Init, Close);
        }

        public async Task Init(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Init host={Host} port={Port} ns:{Namespace} serializer={SerializerName}",
                _options.Host, _options.Port, _options.Namespace, _aerospikeSerializer.GetType().Name);

            _clientPolicy = new AsyncClientPolicy()
            {
                user = _options.Username,
                password = _options.Password
            };

            _readPolicy = new BatchPolicy(_clientPolicy.batchPolicyDefault)
            {
                sendKey = true
            };

            _writeStatePolicy = new WritePolicy(_clientPolicy.writePolicyDefault)
            {
                recordExistsAction = RecordExistsAction.REPLACE,
                sendKey = true
            };

            Log.SetLevel(Log.Level.INFO);
            Log.SetCallback((level, message) =>
            {
                LogLevel logLevel = LogLevel.None;
                switch(level)
                {
                    case Log.Level.DEBUG:
                        logLevel = LogLevel.Debug;
                        break;
                    case Log.Level.ERROR:
                        logLevel = LogLevel.Error;
                        break;
                    case Log.Level.INFO:
                        logLevel = LogLevel.Information;
                        break;
                    case Log.Level.WARN:
                        logLevel = LogLevel.Warning;
                        break;
                }
                _logger.Log(logLevel, "Aerospike-Message: {Message}", message);
            });

            _client = new AsyncClient(_clientPolicy,
                _options.Host,
                _options.Port);
        }

        private async Task Close(CancellationToken cancellationToken)
        {
            _client.Close();
        }

        public async Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var key = GetKey(grainReference, grainState);

            try
            {
                await _client.Delete(_writeStatePolicy, Task.Factory.CancellationToken, key);
                grainState.ETag = null;
                grainState.RecordExists = false;
            }
            catch (AerospikeException aep)
            {
                _logger.LogError(aep, "Failure clearing state for Grain Type {GrainType} with key {GrainKey}", grainType, grainReference.ToKeyString());
                throw new AerospikeOrleansException(aep.Message);
            }
            catch (Exception exp)
            {
                _logger.LogError(exp, "Failure clearing state for Grain Type {GrainType} with key {GrainKey}", grainType, grainReference.ToKeyString());
            }
        }

        public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var key = GetKey(grainReference, grainState);

            try
            {
                var record = await _client.Get(_readPolicy, Task.Factory.CancellationToken, key);

                if (record == null)
                {
                    grainState.ETag = null;
                    grainState.RecordExists = false;
                    return;
                }

                _aerospikeSerializer.Deserialize(record, grainState);
                grainState.ETag = record.generation.ToString();
                grainState.RecordExists = true;
            }
            catch (AerospikeException aep)
            {
                _logger.LogError(aep, "Failure reading state for Grain Type {GrainType} with key {GrainKey}", grainType, grainReference.ToKeyString());
                throw new AerospikeOrleansException(aep.Message);
            }
            catch (Exception exp)
            {
                _logger.LogError(exp, "Failure reading state for Grain Type {GrainType} with key {GrainKey}", grainType, grainReference.ToKeyString());
                grainState.ETag = null;
                grainState.RecordExists = true;
            }
        }

        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var key = GetKey(grainReference, grainState);
            //_logger.LogInformation($"Put {grainType} {grainReference.GetPrimaryKeyString()}");
            var bins = _aerospikeSerializer.Serialize(grainState);

            try
            {
                var ops = bins.Select(p => new Operation(Operation.Type.WRITE, p.name, p.value)).ToList();
                ops.Add(new Operation(Operation.Type.READ_HEADER, "", Value.AsNull));

                if (!string.IsNullOrEmpty(grainState.ETag) && _options.VerifyEtagGenerations)
                {
                    var record = await _client.Operate(new WritePolicy(_writeStatePolicy)
                    {
                        generationPolicy = GenerationPolicy.EXPECT_GEN_EQUAL,
                        generation = int.Parse(grainState.ETag),
                    }, Task.Factory.CancellationToken, key, ops.ToArray());
                    grainState.ETag = record.generation.ToString();
                }
                else
                {
                    var record = await _client.Operate(_writeStatePolicy, Task.Factory.CancellationToken, key, ops.ToArray());
                    grainState.ETag = record.generation.ToString();
                }

                grainState.RecordExists = true;
            }
            catch(AerospikeException aep)
            {
                if (aep.Result == 3)
                {
                    throw new InconsistentStateException($"Generation conflict while writing Grain Type {grainType} with key {grainReference.ToKeyString()}. Error:{aep.Message}",  grainState.ETag, "?");
                }

                _logger.LogError(aep, "Failure writing state for Grain Type {GrainType} with key {GrainKey}", grainType, grainReference.ToKeyString());
                throw new AerospikeOrleansException(aep.Message); // simple serializable exception
            }
            catch (Exception exp)
            {
                _logger.LogError(exp, "Failure writing state for Grain Type {GrainType} with key {GrainKey}", grainType, grainReference.ToKeyString());
                throw;
            }
        }

        private Key GetKey(GrainReference grainReference, IGrainState state)
        {
            return new Key(_options.Namespace, _name + "_" + state.Type.Name, grainReference.ToShortKeyString());
        }
    }
}
