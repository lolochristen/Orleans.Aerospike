namespace Orleans.Clustering.Aerospike
{
    public class AerospikeGatewayOptions
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 3000;
        public string Namespace { get; set; } = "dev";
        public string SetName { get; set; } = "membershipTable";
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
