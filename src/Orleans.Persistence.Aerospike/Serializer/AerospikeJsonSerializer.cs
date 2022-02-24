using Aerospike.Client;

namespace Orleans.Persistence.Aerospike.Serializer
{
    public class AerospikeJsonSerializer : IAerospikeSerializer
    {
        public Bin[] Serialize<T>(IGrainState<T> grainState)
        {
            var data = Newtonsoft.Json.JsonConvert.SerializeObject(grainState.State);

            Bin[] bins = new Bin[]
            {
                new Bin("data", data),
                new Bin("type", typeof(T).Name)
            };
            return bins;
        }

        public void Deserialize<T>(Record record, IGrainState<T> grainState)
        {
            var data = record.bins["data"].ToString();
            grainState.State = (T)Newtonsoft.Json.JsonConvert.DeserializeObject(data!, typeof(T));
        }
    }



}
