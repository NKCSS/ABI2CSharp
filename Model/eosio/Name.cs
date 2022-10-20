using Abi2CSharp.Extensions;
using Abi2CSharp.Interfaces;
using Newtonsoft.Json;

namespace Abi2CSharp.Model.eosio
{
    [JsonConverter(typeof(CustomJsonConverter<Name>))]
    public class Name : ICustomSerialize<Name>
    {
        ulong _Value;
        string _Name;
        public ulong Value 
        { 
            get => _Value; 
            set 
            {
                if(value != _Value)
                {
                    _Value = value;
                    _Name = value.ToName();
                }
            } 
        }
        public string AsString { 
            get => _Name;
            set
            {
                if(value != _Name)
                {
                    _Name = value;
                    _Value = AsString.NameToLong();
                }
            }
        }
        public Name() { } // Empty constructor for serializing
        public Name(string value)
        {
            AsString = value;
        }
        public Name(ulong value)
        {
            Value = value;
        }
        public static implicit operator ulong(Name value) => value.Value;
        public static implicit operator string (Name value) => value.AsString;
        public static implicit operator Name(ulong value) => new Name(value);
        public static implicit operator Name(string value) => new Name(value);
        public override string ToString() => AsString;
        public string Serialize() => AsString;
        public Name Deserialize(JsonReader reader) => (string)reader.Value;
        public override int GetHashCode() => AsString.GetHashCode();
        public override bool Equals(object obj) => obj?.GetHashCode().Equals(GetHashCode()) ?? false;
    }
}
