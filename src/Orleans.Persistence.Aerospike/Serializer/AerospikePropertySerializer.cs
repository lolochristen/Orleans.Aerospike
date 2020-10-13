using Aerospike.Client;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Orleans.Persistence.Aerospike.Serializer
{
    public class AerospikePropertySerializer : IAerospikeSerializer
    {
        public void Deserialize(Record record, IGrainState grainState)
        {
            var properties = grainState.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var obj = Activator.CreateInstance(grainState.Type);

            foreach (var recordBin in record.bins)
            {
                var property = properties.SingleOrDefault(p => p.Name == recordBin.Key);

                if (property == null)
                    continue;
                //if (property.GetCustomAttribute<IgnoreAttribute>() != null)
                //    continue;

                if (property.PropertyType == typeof(string)
                    || property.PropertyType == typeof(int)
                    || property.PropertyType == typeof(short)
                    || property.PropertyType == typeof(long)
                    || property.PropertyType == typeof(uint)
                    || property.PropertyType == typeof(ushort)
                    || property.PropertyType == typeof(ulong)
                    || property.PropertyType == typeof(byte)
                    || property.PropertyType == typeof(char)
                    || property.PropertyType == typeof(byte[])
                    || property.PropertyType == typeof(double)
                    || property.PropertyType == typeof(float)
                    )
                    property.SetValue(obj, recordBin.Value);
                else if (property.PropertyType.IsEnum)
                {
                    object enumValue;
                    Enum.TryParse(property.PropertyType, recordBin.Value.ToString(), out enumValue);
                    property.SetValue(obj, enumValue);
                }
                else 
                {
                    // deserialize json
                    var o = JsonConvert.DeserializeObject(recordBin.Value.ToString(),
                        property.PropertyType);
                    property.SetValue(obj, o);
                }
            }

            grainState.State = obj;
        }

        public Bin[] Serialize(IGrainState grainState)
        {
            var properties = grainState.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            List<Bin> binList = new List<Bin>();

            binList.Add(new Bin("type", grainState.Type.Name));

            foreach (var propertyInfo in properties)
            {
                // ignore ignore attributes
                if (propertyInfo.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                    continue;
                if (propertyInfo.GetCustomAttribute<System.Text.Json.Serialization.JsonIgnoreAttribute>() != null)
                    continue;
                if (propertyInfo.GetCustomAttribute<MessagePack.IgnoreMemberAttribute>() != null)
                    continue;

                var propertyType = propertyInfo.PropertyType;
                var value = propertyInfo.GetValue(grainState.State);

                Value binValue;
                if (propertyType == typeof(int))
                    binValue = new Value.IntegerValue((int)value);
                else if (propertyType == typeof(uint))
                    binValue = new Value.UnsignedIntegerValue((uint)value);
                else if (propertyType == typeof(short))
                    binValue = new Value.ShortValue((short)value);
                else if (propertyType == typeof(ushort))
                    binValue = new Value.UnsignedShortValue((ushort)value);
                else if (propertyType == typeof(long))
                    binValue = new Value.LongValue((long)value);
                else if (propertyType == typeof(ulong))
                    binValue = new Value.UnsignedLongValue((ulong)value);
                else if (propertyType == typeof(string))
                    binValue = new Value.StringValue((string)value);
                else if (propertyType == typeof(char))
                    binValue = new Value.StringValue((string)value);
                else if (propertyType == typeof(double))
                    binValue = new Value.DoubleValue((double)value);
                else if (propertyType == typeof(float))
                    binValue = new Value.DoubleValue((float)value);
                else if (propertyType == typeof(byte))
                    binValue = new Value.ByteValue((byte)value);
                else if (propertyType == typeof(byte[]))
                    binValue = new Value.BytesValue((byte[])value);
                else if (propertyType.IsEnum)
                    binValue = new Value.StringValue(value.ToString());
                else // to json
                    binValue = new Value.StringValue(JsonConvert.SerializeObject(value));

                binList.Add(new Bin(propertyInfo.Name, binValue));
            }
            return binList.ToArray();
        }
    }



}

