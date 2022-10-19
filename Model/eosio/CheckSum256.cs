using Abi2CSharp.Extensions;

namespace Abi2CSharp.Model.eosio
{
    public class CheckSum256
    {
        const int BitsPerByte = 8;
        const int ExpectedLength = 256 / BitsPerByte;
        byte[] _Raw;
        string _AsString;
        public byte[] Raw
        {
            get => _Raw;
            set
            {
                _Raw = value;
                _AsString = value.ToHexLower();
            }
        }
        public string AsString
        {
            get => _AsString;
            set
            {
                _AsString = value;
                _Raw = value.ToByteArrayFastest();
            }
        }
        public CheckSum256(string value)
        {
            AsString = value;
        }
        public static implicit operator CheckSum256(string value)
        {
            int valueLength = value?.Length ?? 0;
            if (valueLength != ExpectedLength) throw new System.ArgumentException($"A {nameof(CheckSum256)} should be {ExpectedLength} bytes in length. Supplied value byte length: {valueLength}", nameof(value));
            else return new CheckSum256(value);
        }
        public static implicit operator string(CheckSum256 value) => value.AsString;
        public override string ToString() => AsString;
    }
}