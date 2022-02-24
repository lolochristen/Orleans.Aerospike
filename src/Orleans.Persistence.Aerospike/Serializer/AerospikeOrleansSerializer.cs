using Aerospike.Client;

namespace Orleans.Persistence.Aerospike.Serializer
{
    public class AerospikeOrleansSerializer : IAerospikeSerializer
    {
        private readonly Serialization.Serializer _serializationManager;

        public AerospikeOrleansSerializer(Serialization.Serializer serializationManager)
        {
            _serializationManager = serializationManager;
        }

        public Bin[] Serialize<T>(IGrainState<T> grainState)
        {
            var data = _serializationManager.SerializeToArray(grainState.State);

            Bin[] bins = new Bin[]
            {
                new Bin("data", data),
                new Bin("type", typeof(T).Name)
            };
            return bins;
        }

        public void Deserialize<T>(Record record, IGrainState<T> grainState)
        {
            var data = record.bins["data"] as byte[];
            grainState.State = _serializationManager.Deserialize<T>(data);
        }
    }
}
