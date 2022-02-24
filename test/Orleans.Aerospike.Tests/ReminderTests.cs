using Orleans.Aerospike.Tests.Grains;
using Orleans.Hosting;
using Orleans.Reminders.Aerospike;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Orleans.Aerospike.Tests
{
    public class ReminderTests : IClassFixture<ReminderTests.ReminderFixture>
    {
        public class ReminderFixture : OrleansFixture
        {
            protected override ISiloBuilder PreBuild(ISiloBuilder builder)
            {
                return builder
                    .AddMemoryGrainStorage(OrleansFixture.TEST_STORAGE)
                    .UseAerospikeReminder();
            }
        }

        private readonly ReminderFixture _fixture;

        public ReminderTests(ReminderFixture fixture)
        {
            this._fixture = fixture;
        }

        [Fact]
        public async Task CreateReminderTest()
        {
            var grain = _fixture.Client.GetGrain<ITestGrain>(Guid.NewGuid());
            var test = "grain1";
            var reminder = await grain.RegisterReminder(test);
            Assert.NotNull(reminder);
            Assert.True(await grain.ReminderExist(test));
            await Task.Delay((int)TestGrain.ReminderWaitTime.TotalMilliseconds);
            Assert.True(await grain.ReminderTicked());
            await grain.DismissReminder(test);
            Assert.False(await grain.ReminderExist(test));
        }
    }
}
