using System;

namespace Orleans.Persistence.Aerospike
{
    [Serializable]
    public class AerospikeOrleansException : Exception
    {
        public AerospikeOrleansException() : base()
        {
        }

        public AerospikeOrleansException(string message) : base(message)
        {
        }

        public AerospikeOrleansException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
