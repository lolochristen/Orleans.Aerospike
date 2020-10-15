namespace Orleans.Reminders.Aerospike
{
    public class AerospikeReminderStorageOptions
    {
        public string Host { get; internal set; } = "localhost";
        public int Port { get; internal set; } = 3000;
        public string Namespace { get; internal set; } = "dev";
        public string SetName { get; internal set; } = "reminderTable";
    }
}