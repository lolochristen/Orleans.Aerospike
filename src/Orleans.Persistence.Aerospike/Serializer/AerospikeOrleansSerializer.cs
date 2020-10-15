using Aerospike.Client;
using Orleans.Serialization;

namespace Orleans.Persistence.Aerospike.Serializer
{
    public class AerospikeOrleansSerializer : IAerospikeSerializer
    {
        private readonly SerializationManager _serializationManager;

        public AerospikeOrleansSerializer(SerializationManager serializationManager)
        {
            _serializationManager = serializationManager;
        }

        public Bin[] Serialize(IGrainState grainState)
        {
            var data = _serializationManager.SerializeToByteArray(grainState.State);
            
            Bin[] bins = new Bin[]
            {
                new Bin("data", data),
                new Bin("type", grainState.Type.Name)
            };
            return bins;
        }

        public void Deserialize(Record record, IGrainState grainState)
        {
            var data = record.bins["data"] as byte[];
            grainState.State = _serializationManager.DeserializeFromByteArray<object>(data);
        }
    }
}

