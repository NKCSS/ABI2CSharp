using Abi2CSharp.Extensions;
using Abi2CSharp.Interfaces;
using Newtonsoft.Json;
using System;

namespace Abi2CSharp.Model.eosio
{
    [JsonConverter(typeof(CustomJsonConverter<TimePoint>))]
    public class TimePoint : ICustomSerialize<TimePoint>
    {
        ulong _Value;
        DateTime _Moment;
        public ulong Value 
        { 
            get => _Value; 
            set 
            {
                if(value != _Value)
                {
                    _Value = value;
                    _Moment = DateTime.UnixEpoch.AddMilliseconds(value);
                }
            } 
        }
        public DateTime Moment
        { 
            get => _Moment;
            set
            {
                if(value != _Moment)
                {
                    _Moment = value;
                    _Value = (ulong)value.Subtract(DateTime.UnixEpoch).TotalMilliseconds;
                }
            }
        }
        public TimePoint() { } // Empty constructor for serializing
        public TimePoint(ulong value)
        {
            Value = value;
        }
        public TimePoint(string value)
        {
            Moment = DateTime.SpecifyKind(DateTime.ParseExact(value, "yyyy-MM-dd'T'HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture), DateTimeKind.Utc);
        }
        public TimePoint(DateTime value)
        {
            Moment = value;
        }
        public static implicit operator ulong(TimePoint value) => value.Value;
        public static implicit operator DateTime(TimePoint value) => value.Moment;
        public static implicit operator TimePoint(ulong value) => new TimePoint(value);
        public static implicit operator TimePoint(DateTime value) => new TimePoint(value);
        public static implicit operator TimePoint(string value) => new TimePoint(value);
        public override string ToString() => Moment.ToString("yyyy-MM-dd HH:mm:ss.fff");
        public string Serialize() => Value.ToString();
        /// <summary>
        /// NewtonSoft already deserializes the DateTime string properly, so we just need to ensure we specify it's in UTC, but we also support ulong and raw string.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public TimePoint Deserialize(JsonReader reader)
        {
            if (reader.Value is DateTime t) return DateTime.SpecifyKind(t, DateTimeKind.Utc);
            else if (reader.Value is string s) return ulong.TryParse(s, out ulong v) ? new TimePoint(v) : new TimePoint(s);
            else throw new ArgumentException($"Cannot deserialize '{reader.Value}' as a {nameof(TimePoint)}");
        }
        public override int GetHashCode() => Moment.GetHashCode();
        public override bool Equals(object obj) => obj?.GetHashCode().Equals(GetHashCode()) ?? false;
    }
}
