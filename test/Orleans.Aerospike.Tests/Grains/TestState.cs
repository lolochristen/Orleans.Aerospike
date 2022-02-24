using System;
using System.Collections.Generic;

namespace Orleans.Aerospike.Tests.Grains
{
    [Serializable]
    [GenerateSerializer]
    public class TestState
    {
        [Id(0)] public int NumberInt { get; set; }
        [Id(1)] public long NumberLong { get; set; }
        [Id(2)] public string Text { get; set; }
        [Id(3)] public float NumberFloat { get; set; }
        [Id(4)] public double NumberDouble { get; set; }
        [Id(5)] public DateTime DateTime { get; set; }
        [Id(6)] public Guid Guid { get; set; }
        [Id(7)] public List<string> StringList { get; set; } = new List<string>();
        [Id(8)] public short NumberShort { get; set; }
        [Id(9)] public char Char { get; set; }
        [Id(10)] public byte[] ByteArray { get; set; }
        [Id(11)] public byte Byte { get; set; }
        [Id(12)] public uint NumberUInt { get; set; }
        [Id(13)] public List<int> NumberList { get; set; } = new List<int>();
        [Id(14)] public List<float> FloatList { get; set; } = new List<float>();
        [Id(15)] public Dictionary<string, int> MapStringInt { get; set; }
        [Id(16)] public Dictionary<string, TestState> StateInState { get; set; }

        public static TestState CreateTestState()
        {
            return new TestState()
            {
                Text = "TEST-" + Guid.NewGuid() + "öäüè スリッパ",
                Guid = Guid.NewGuid(),
                DateTime = DateTime.Now,
                NumberDouble = 1,
                NumberFloat = 2,
                NumberInt = int.MaxValue,
                NumberLong = long.MaxValue,
                NumberShort = short.MaxValue,
                Byte = byte.MaxValue,
                ByteArray = new byte[] { byte.MaxValue, byte.MaxValue },
                Char = 'Z',
                StringList = new List<string>() { "1", "2", "3" },
                NumberUInt = uint.MaxValue,
                NumberList = new List<int>() { 1, 2, 3 },
                FloatList = new List<float>() { 1.1f, 2.2f, 3.3f},
                MapStringInt = new Dictionary<string, int>() { { "KEY1", 123 }, { "KEY2", 567 } },
                StateInState = new Dictionary<string, TestState>() { { "TEST1", new TestState() { Text = "TestInTest" } } }
            };
        }
    }
}
