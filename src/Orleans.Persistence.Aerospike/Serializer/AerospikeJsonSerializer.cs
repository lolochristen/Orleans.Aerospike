using Aerospike.Client;

namespace Orleans.Persistence.Aerospike.Serializer
{
    public class AerospikeJsonSerializer : IAerospikeSerializer
    {
        public Bin[] Serialize(IGrainState grainState)
        {
            var data = Newtonsoft.Json.JsonConvert.SerializeObject(grainState.State);

            Bin[] bins = new Bin[]
            {
                new Bin("data", data),
                new Bin("type", grainState.Type.Name)
            };
            return bins;
        }

        public void Deserialize(Record record, IGrainState grainState)
        {
            var data = record.bins["data"].ToString();
            grainState.State = Newtonsoft.Json.JsonConvert.DeserializeObject(data, grainState.Type);
        }
    }



}

