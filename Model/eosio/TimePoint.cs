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
        public TimePoint(ulong value)
        {
            Value = value;
        }
        public TimePoint(DateTime value)
        {
            Moment = value;
        }
        public static implicit operator ulong(TimePoint value) => value.Value;
        public static implicit operator DateTime(TimePoint value) => value.Moment;
        public static implicit operator TimePoint(ulong value) => new TimePoint(value);
        public static implicit operator TimePoint(DateTime value) => new TimePoint(value);
        public override string ToString() => Moment.ToString("yyyy-MM-dd HH:mm:ss.fff");

        public string Serialize() => Value.ToString();

        public TimePoint Deserialize(JsonReader reader) => ulong.Parse(reader.ReadAsString());
    }
}
