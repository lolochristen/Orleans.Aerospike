using Aerospike.Client;

namespace Orleans.Persistence.Aerospike.Serializer
{
    public interface IAerospikeSerializer
    {
        Bin[] Serialize(IGrainState grainState);
        void Deserialize(Record record, IGrainState grainState);
    }
}

