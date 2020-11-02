using Aerospike.Client;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
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

                if (property.PropertyType == typeof(int))
                    property.SetValue(obj, (int)(long)recordBin.Value); // cast. is stored as long
                else if (property.PropertyType == typeof(uint))
                    property.SetValue(obj, (uint)(long)recordBin.Value); // cast. is stored as long
                else if (property.PropertyType == typeof(short))
                    property.SetValue(obj, (short)(long)recordBin.Value); // cast. is stored as long
                else if (property.PropertyType == typeof(ushort))
                    property.SetValue(obj, (ushort)(long)recordBin.Value); // cast. is stored as long
                else if (property.PropertyType == typeof(byte))
                    property.SetValue(obj, (byte)(long)recordBin.Value); // cast. is stored as long
                else if (property.PropertyType == typeof(char))
                    property.SetValue(obj, char.Parse((string)recordBin.Value));
                else if (property.PropertyType == typeof(float))
                    property.SetValue(obj, (float)(double)recordBin.Value); // is stored as double
                else if (property.PropertyType == typeof(string)
                    || property.PropertyType == typeof(short)
                    || property.PropertyType == typeof(long)
                    || property.PropertyType == typeof(ushort)
                    || property.PropertyType == typeof(ulong)
                    || property.PropertyType == typeof(byte)
                    || property.PropertyType == typeof(char)
                    || property.PropertyType == typeof(byte[])
                    || property.PropertyType == typeof(double))
                {
                    property.SetValue(obj, recordBin.Value); // direct
                }
                else if (property.PropertyType == typeof(Guid))
                {
                    property.SetValue(obj, new Guid((byte[])recordBin.Value));
                }
                else if (property.PropertyType.IsEnum)
                {
                    object enumValue;
                    Enum.TryParse(property.PropertyType, recordBin.Value.ToString(), out enumValue);
                    property.SetValue(obj, enumValue);
                }
                else if (typeof(IList<string>).IsAssignableFrom(property.PropertyType)) // Lists...
                    property.SetValue(obj, ((IList<object>)recordBin.Value).Select(p => p.ToString()).ToList());
                else if (typeof(IList<int>).IsAssignableFrom(property.PropertyType))
                    property.SetValue(obj, ((IList<object>)recordBin.Value).Select(p => (int)(long)p).ToList());
                else if (typeof(IList<long>).IsAssignableFrom(property.PropertyType))
                    property.SetValue(obj, ((IList<object>)recordBin.Value).Select(p => (long)p).ToList());
                else if (typeof(IList<short>).IsAssignableFrom(property.PropertyType))
                    property.SetValue(obj, ((IList<object>)recordBin.Value).Select(p => (short)(long)p).ToList());
                else if (typeof(IList<uint>).IsAssignableFrom(property.PropertyType))
                    property.SetValue(obj, ((IList<object>)recordBin.Value).Select(p => (uint)(long)p).ToList());
                else if (typeof(IList<ulong>).IsAssignableFrom(property.PropertyType))
                    property.SetValue(obj, ((IList<object>)recordBin.Value).Select(p => (ulong)p).ToList());
                else if (typeof(IList<ushort>).IsAssignableFrom(property.PropertyType))
                    property.SetValue(obj, ((IList<object>)recordBin.Value).Select(p => (ushort)(long)p).ToList());
                else if (typeof(IList<double>).IsAssignableFrom(property.PropertyType))
                    property.SetValue(obj, ((IList<object>)recordBin.Value).Select(p => (double)p).ToList());
                else if (typeof(IList<float>).IsAssignableFrom(property.PropertyType))
                    property.SetValue(obj, ((IList<object>)recordBin.Value).Select(p => (float)p).ToList());
                else if (typeof(IDictionary).IsAssignableFrom(property.PropertyType) // just generic dictionary of scalar types
                    && property.PropertyType.IsGenericType
                    && property.PropertyType.GenericTypeArguments.All(p =>
                        p == typeof(int) || p == typeof(string) || p == typeof(long)
                        || p == typeof(short) || p == typeof(uint) || p == typeof(ulong)
                        || p == typeof(ushort) || p == typeof(double) || p == typeof(float)))
                {
                    // create dynamically generic dictionary
                    var keyType = property.PropertyType.GenericTypeArguments[0];
                    var valueType = property.PropertyType.GenericTypeArguments[1];
                    var dictionaryType = typeof(Dictionary<,>).MakeGenericType(property.PropertyType.GenericTypeArguments);
                    var methodAdd = dictionaryType.GetMethod("Add");
                    var dicObj = Activator.CreateInstance(dictionaryType);
                    foreach(var keyValue in ((IDictionary)recordBin.Value).Keys)
                        methodAdd.Invoke(dicObj, new object[] { Convert.ChangeType(keyValue, keyType), Convert.ChangeType(((IDictionary)recordBin.Value)[keyValue], valueType) });
                    property.SetValue(obj, dicObj);
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
                    binValue = new Value.StringValue(((char)value).ToString());
                else if (propertyType == typeof(double))
                    binValue = new Value.DoubleValue((double)value);
                else if (propertyType == typeof(float))
                    binValue = new Value.DoubleValue((float)value);
                else if (propertyType == typeof(byte))
                    binValue = new Value.ByteValue((byte)value);
                else if (propertyType == typeof(byte[]))
                    binValue = new Value.BytesValue((byte[])value);
                else if (propertyType == typeof(Guid))
                    binValue = new Value.BytesValue(((Guid)value).ToByteArray());
                else if (propertyType.IsEnum)
                    binValue = new Value.StringValue(value.ToString());
                else if (typeof(IList<string>).IsAssignableFrom(propertyType)
                        || typeof(IList<int>).IsAssignableFrom(propertyType)
                        || typeof(IList<long>).IsAssignableFrom(propertyType)
                        || typeof(IList<short>).IsAssignableFrom(propertyType)
                        || typeof(IList<uint>).IsAssignableFrom(propertyType)
                        || typeof(IList<ulong>).IsAssignableFrom(propertyType)
                        || typeof(IList<ushort>).IsAssignableFrom(propertyType)
                        || typeof(IList<double>).IsAssignableFrom(propertyType)
                        || typeof(IList<float>).IsAssignableFrom(propertyType))
                    binValue = new Value.ListValue((IList)value);
                else if (typeof(IDictionary).IsAssignableFrom(propertyType) // just generic dictionary of scalar types
                    && propertyType.IsGenericType 
                    && propertyType.GenericTypeArguments.All(p => 
                        p == typeof(int) || p == typeof(string) || p == typeof(long) 
                        || p == typeof(short) || p == typeof(uint) || p == typeof(ulong) 
                        || p == typeof(ushort) || p == typeof(double) || p == typeof(float)))
                    binValue = new Value.MapValue((IDictionary)value);
                else // all othersto json
                    binValue = new Value.StringValue(JsonConvert.SerializeObject(value));

                binList.Add(new Bin(propertyInfo.Name, binValue));
            }
            return binList.ToArray();
        }
    }
}

