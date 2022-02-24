using Aerospike.Client;

namespace Orleans.Persistence.Aerospike.Serializer
{
    public class AerospikeMessagePackSerializer : IAerospikeSerializer
    {
        public Bin[] Serialize<T>(IGrainState<T> grainState)
        {
            var data = MessagePack.MessagePackSerializer.Typeless.Serialize(grainState.State);

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
            grainState.State = (T)MessagePack.MessagePackSerializer.Typeless.Deserialize(data);
        }
    }



}
