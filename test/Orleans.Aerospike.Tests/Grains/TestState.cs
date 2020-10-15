using System;
using System.Collections.Generic;

namespace Orleans.Aerospike.Tests.Grains
{
    public class TestState
    {
        public int NumberInt { get; set; }
        public long NumberLong { get; set; }
        public string Text { get; set; }
        public float NumberFloat { get; set; }
        public double NumberDouble { get; set; }
        public DateTime DateTime { get; set; }
        public Guid Guid { get; set; }
        public List<string> Data { get; set; } = new List<string>();
        public short NumberShort { get; set; }
        public char Char { get; set; }
        public byte[] ByteArray { get; set; }
        public byte Byte { get; set; }
        public uint NumberUInt { get; set; }

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
                Data = new List<string>() { "1", "2", "3" },
                NumberUInt = uint.MaxValue
            };
        }
    }
}
