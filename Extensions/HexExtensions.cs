using System;
namespace Abi2CSharp.Extensions
{
    public static class HexExtensions
    {
        const int Zero = (int)'0';
        const int HexAFOffset = 10;
        const int UpperCaseA = (int)'A';
        const int UpperCaseAOffset = UpperCaseA - HexAFOffset;
        const int LowerCaseA = (int)'a';
        const int LowerCaseAOffset = LowerCaseA - HexAFOffset;
        public static int GetHexVal(this char hex)
        {
            int val = (int)hex;
            if (val < UpperCaseA)
                return val - Zero;
            else if (val < LowerCaseA)
                return val - UpperCaseAOffset;
            return
                val - LowerCaseAOffset;
        }
        public static byte[] ToByteArrayFastest(this string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");
            int byteCount = hex.Length >> 1;

            byte[] arr = new byte[byteCount];

            for (int i = 0; i < byteCount; ++i)
            {
                arr[i] = (byte)((hex[i << 1].GetHexVal() << 4) + hex[(i << 1) + 1].GetHexVal());
            }

            return arr;
        }
        public static string ToHexUpper(this byte[] value)
        {
            char[] result = new char[value.Length * 2];
            int index = 0;
            byte b;
            for (int ix = 0; ix < result.Length; ix += 2)
            {
                b = value[index++];
                result[ix] = GetHexUpper(b / 16);
                result[ix + 1] = GetHexUpper(b % 16);
            }
            return new string(result);
        }
        public static string ToHexLower(this byte[] value)
        {
            char[] result = new char[value.Length * 2];
            int index = 0;
            byte b;
            for (int ix = 0; ix < result.Length; ix += 2)
            {
                b = value[index++];
                result[ix] = GetHexLower(b / 16);
                result[ix + 1] = GetHexLower(b % 16);
            }
            return new string(result);
        }
        static char GetHexUpper(int i)
        {
            if (i < 0 || i > 15) throw new ArgumentException("Value must be between 0 and 15");
            else if (i < 10) return (char)(i + '0');
            return (char)(i - 10 + 'A');
        }
        static char GetHexLower(int i)
        {
            if (i < 0 || i > 15) throw new ArgumentException("Value must be between 0 and 15");
            else if (i < 10) return (char)(i + '0');
            return (char)(i - 10 + 'a');
        }
    }
}
