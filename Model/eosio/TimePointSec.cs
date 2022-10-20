using Abi2CSharp.Extensions;
using Abi2CSharp.Interfaces;
using Newtonsoft.Json;
using System;

namespace Abi2CSharp.Model.eosio
{
    [JsonConverter(typeof(CustomJsonConverter<TimePointSec>))]
    public class TimePointSec : ICustomSerialize<TimePointSec>
    {
        uint _Value;
        DateTime _Moment;
        public uint Value 
        { 
            get => _Value; 
            set 
            {
                if(value != _Value)
                {
                    _Value = value;
                    _Moment = DateTime.UnixEpoch.AddSeconds(value);
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
                    _Value = (uint)value.Subtract(DateTime.UnixEpoch).TotalSeconds;
                }
            }
        }
        public TimePointSec() { } // Empty constructor for serializing
        public TimePointSec(uint value)
        {
            Value = value;
        }
        public TimePointSec(string value)
        {
            Moment = DateTime.SpecifyKind(DateTime.ParseExact(value, "yyyy-MM-dd'T'HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture), DateTimeKind.Utc);
        }
        public TimePointSec(DateTime value)
        {
            Moment = value;
        }
        public static implicit operator uint(TimePointSec value) => value.Value;
        public static implicit operator DateTime(TimePointSec value) => value.Moment;
        public static implicit operator TimePointSec(uint value) => new TimePointSec(value);
        public static implicit operator TimePointSec(DateTime value) => new TimePointSec(value);
        public static implicit operator TimePointSec(string value) => new TimePointSec(value);
        public override string ToString() => Moment.ToString("yyyy-MM-dd HH:mm:ss.fff");

        public string Serialize() => Value.ToString();
        /// <summary>
        /// NewtonSoft already deserializes the DateTime string properly, so we just need to ensure we specify it's in UTC, but we also support ulong and raw string.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public TimePointSec Deserialize(JsonReader reader)
        {
            if (reader.Value is DateTime t) return DateTime.SpecifyKind(t, DateTimeKind.Utc);
            else if (reader.Value is string s) return uint.TryParse(s, out uint v) ? new TimePointSec(v) : new TimePointSec(s);
            else throw new ArgumentException($"Cannot deserialize '{reader.Value}' as a {nameof(TimePointSec)}");
        }
        public override int GetHashCode() => Moment.GetHashCode();
        public override bool Equals(object obj) => obj?.GetHashCode().Equals(GetHashCode()) ?? false;
    }
}
