using Orleans.Runtime;
using System.Threading.Tasks;

namespace Orleans.Aerospike.Tests.Grains
{
    public interface ITestGrain : IGrainWithGuidKey
    {
        Task WriteState(TestState state);
        Task<TestState> GetState();

        Task<IGrainReminder> RegisterReminder(string name);
        Task<bool> ReminderExist(string name);
        Task<bool> ReminderTicked();
        Task DismissReminder(string name);
    }
}
