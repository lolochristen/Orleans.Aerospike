using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.TestingHost;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using System.Collections.Generic;
using Microsoft.VisualBasic.CompilerServices;
using Orleans.Runtime;
using Orleans.Persistence.Aerospike.Serializer;
using Orleans.Aerospike.Tests.Grains;

namespace Orleans.Aerospike.Tests
{
    public class PersistenceOrleansSerializerTests : IClassFixture<PersistenceOrleansSerializerTests.StorageFixture>
    {
        public class StorageFixture : OrleansFixture
        {
            protected override ISiloBuilder PreBuild(ISiloBuilder builder)
            {
                return builder
                    .AddAerospikeGrainStorage(OrleansFixture.TEST_STORAGE)
                    .UseAerospikeSerializer<AerospikeOrleansSerializer>();
            }
        }

        private StorageFixture _fixture;

        public PersistenceOrleansSerializerTests(StorageFixture fixture) => this._fixture = fixture;

        [Fact]
        public async Task WriteReadStateTest()
        {
            Guid grainId = Guid.NewGuid();

            var testGrain = _fixture.Client.GetGrain<ITestGrain>(grainId);
            var state = TestState.CreateTestState();
            await testGrain.WriteState(state);
            
            await _fixture.DeactivateGrains();

            var testGrain2 = _fixture.Client.GetGrain<ITestGrain>(grainId);
            var state2 = await testGrain2.GetState();

            Assert.Equal(state.Text, state2.Text);
            Assert.Equal(state.Guid, state2.Guid);
            Assert.Equal(state.NumberDouble, state2.NumberDouble);
            Assert.Equal(state.NumberFloat, state2.NumberFloat);
            Assert.Equal(state.NumberInt, state2.NumberInt);
            Assert.Equal(state.DateTime, state2.DateTime);
            Assert.Equal(state.StringList.Count, state2.StringList.Count);
            Assert.Equal(state.NumberShort, state2.NumberShort);
            Assert.Equal(state.Byte, state2.Byte);
            Assert.Equal(state.ByteArray.Length, state2.ByteArray.Length);
            Assert.Equal(state.NumberUInt, state2.NumberUInt);
        }
    }
}
