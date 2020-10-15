using System;
using System.Threading.Tasks;
using Orleans.Runtime;

namespace Orleans.Aerospike.Tests.Grains
{
    public class TestGrain : Grain, ITestGrain, IRemindable
    {
        private readonly IPersistentState<TestState> _testState;
        private bool ticked = false;
        public static TimeSpan ReminderWaitTime = TimeSpan.FromMinutes(1);  // Minimum wait time allowed by Orleans

        public TestGrain([PersistentState("test", OrleansFixture.TEST_STORAGE)] IPersistentState<TestState> testState)
        {
            _testState = testState;
        }

        async Task ITestGrain.WriteState(TestState state)
        {
            _testState.State = state;
            await _testState.WriteStateAsync();
        }

        Task<TestState> ITestGrain.GetState()
        {
            return Task.FromResult(_testState.State);
        }

        public async Task DismissReminder(string name)
        {
            var r = await GetReminder(name);
            await UnregisterReminder(r);
        }

        public Task ReceiveReminder(string reminderName, TickStatus status)
        {
            this.ticked = true;
            return Task.CompletedTask;
        }

        public Task<IGrainReminder> RegisterReminder(string name)
        {
            return RegisterOrUpdateReminder(name, TimeSpan.FromSeconds(2), ReminderWaitTime);
        }

        public async Task<bool> ReminderExist(string name)
        {
            var r = await GetReminder(name);
            return r != null;
        }

        public Task<bool> ReminderTicked()
        {
            return Task.FromResult(ticked);
        }
    }
}
