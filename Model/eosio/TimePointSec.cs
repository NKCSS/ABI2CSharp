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
        public TimePointSec(uint value)
        {
            Value = value;
        }
        public TimePointSec(DateTime value)
        {
            Moment = value;
        }
        public static implicit operator uint(TimePointSec value) => value.Value;
        public static implicit operator DateTime(TimePointSec value) => value.Moment;
        public static implicit operator TimePointSec(uint value) => new TimePointSec(value);
        public static implicit operator TimePointSec(DateTime value) => new TimePointSec(value);
        public override string ToString() => Moment.ToString("yyyy-MM-dd HH:mm:ss.fff");

        public string Serialize() => Value.ToString();

        public TimePointSec Deserialize(JsonReader reader) => uint.Parse(reader.ReadAsString());
    }
}
