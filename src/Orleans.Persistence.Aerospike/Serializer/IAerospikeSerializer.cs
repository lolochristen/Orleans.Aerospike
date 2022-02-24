using Aerospike.Client;

namespace Orleans.Persistence.Aerospike.Serializer
{
    public interface IAerospikeSerializer
    {
        Bin[] Serialize<T>(IGrainState<T> grainState);
        void Deserialize<T>(Record record, IGrainState<T> grainState);
    }
}
