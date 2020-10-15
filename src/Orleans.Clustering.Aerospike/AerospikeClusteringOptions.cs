using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.Clustering.Aerospike
{
    public class AerospikeClusteringOptions
    {
        public string Host { get; set; } = "localhost";

        public int Port { get; set; } = 3000;

        public string Namespace { get; set; } = "dev";

        public string SetName { get; set; } = "membershipTable";
    }
}
