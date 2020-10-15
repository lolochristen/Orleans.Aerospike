using Aerospike.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans.ApplicationParts;
using Orleans.Persistence.Aerospike.Serializer;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Storage;
using System;
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
            _logger.LogInformation($"Init host={_options.Host} port={_options.Port} ns:{_options.Namespace} serializer={_aerospikeSerializer.GetType().Name}");
            _client = new AsyncClient(_options.Host, _options.Port);
        }

        private async Task Close(CancellationToken cancellationToken)
        {
            _client.Close();
        }

        public Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            return Task.CompletedTask;
        }

        public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var key = GetKey(grainReference, grainState);

            try
            {
                var record = await _client.Get(new BatchPolicy(new Policy() { sendKey = true }), Task.Factory.CancellationToken, key);

                if (record == null)
                {
                    grainState.ETag = string.Empty;
                    return;
                }

                _aerospikeSerializer.Deserialize(record, grainState);
                grainState.ETag = record.generation.ToString();
            }
            catch (AerospikeException aep)
            {
                _logger.LogError(aep, $"Failure reading state for Grain Type {grainType} with key {grainReference.ToKeyString()}.");
                throw new AerospikeOrleansException(aep.Message);
            }
            catch (Exception exp)
            {
                _logger.LogError(exp, $"Failure reading state for Grain Type {grainType} with key {grainReference.ToKeyString()}.");
                grainState.ETag = string.Empty;
            }
        }

        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var key = GetKey(grainReference, grainState);
            //_logger.LogInformation($"Put {grainType} {grainReference.GetPrimaryKeyString()}");
            var bins = _aerospikeSerializer.Serialize(grainState);

            try
            {
                if (!string.IsNullOrEmpty(grainState.ETag) && _options.VerifyEtagGenerations)
                {
                    await _client.Put(
                        new WritePolicy()
                        {
                            sendKey = true,
                            generationPolicy = GenerationPolicy.EXPECT_GEN_EQUAL,
                            generation = int.Parse(grainState.ETag)
                        }, Task.Factory.CancellationToken, key, bins);
                    grainState.ETag = (int.Parse(grainState.ETag) + 1).ToString(); // +1
                }
                else
                {
                    await _client.Put(new WritePolicy() { sendKey = true } , Task.Factory.CancellationToken, key, bins);
                    grainState.ETag = "1"; // initial 1
                }
            }
            catch(AerospikeException aep)
            {
                if (aep.Result == 3)
                {
                    throw new InconsistentStateException($"Generation conflict while writing Grain Type {grainType} with key {grainReference.ToKeyString()}. Error:{aep.Message}",  grainState.ETag, "?");
                }

                _logger.LogError(aep, $"Failure writing state for Grain Type {grainType} with key {grainReference.ToKeyString()}.");
                throw new AerospikeOrleansException(aep.Message); // simple serializable excepction
            }
            catch (Exception exp)
            {
                _logger.LogError(exp, $"Failure writing state for Grain Type {grainType} with key {grainReference.ToKeyString()}.");
                throw;
            }
        }

        private Key GetKey(GrainReference grainReference, IGrainState state)
        {
            return new Key(_options.Namespace, _name + "_" + state.Type.Name, grainReference.ToShortKeyString());
        }
    }

    [Serializable]
    public class AerospikeOrleansException : Exception
    {
        public AerospikeOrleansException() : base()
        {
        }

        public AerospikeOrleansException(string message) : base(message)
        {
        }

        public AerospikeOrleansException(string message, Exception innerException) : base(message, innerException)
        {
        }

    }
}
