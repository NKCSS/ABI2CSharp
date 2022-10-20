using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using Abi2CSharp.Interfaces;
using Newtonsoft.Json;

namespace Abi2CSharp.Extensions
{
    public class CustomJsonConverter<T>
        : Newtonsoft.Json.JsonConverter
        where T : Interfaces.ICustomSerialize<T>
    {
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is Interfaces.ICustomSerialize<T> t) writer.WriteValue(t.Serialize());
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (objectType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICustomSerialize<>)))
            {
                if ((existingValue ?? Activator.CreateInstance<T>()) is ICustomSerialize<T> t) return t.Deserialize(reader);
            }
            return null;
        }
    }
}
