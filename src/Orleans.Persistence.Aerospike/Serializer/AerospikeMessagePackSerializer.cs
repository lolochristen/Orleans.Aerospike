using Aerospike.Client;

namespace Orleans.Persistence.Aerospike.Serializer
{
    public class AerospikeMessagePackSerializer : IAerospikeSerializer
    {
        public Bin[] Serialize(IGrainState grainState)
        {
            var data = MessagePack.MessagePackSerializer.Typeless.Serialize(grainState.State);

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
            grainState.State = MessagePack.MessagePackSerializer.Typeless.Deserialize(data);
        }
    }



}

