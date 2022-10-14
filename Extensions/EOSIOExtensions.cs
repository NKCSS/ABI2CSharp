using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;

namespace Abi2CSharp.Extensions
{
    public static class EOSIOExtensions
    {
        const char NULL = '\0';
        const int BitsPerByte = 8;
        const int BytesPer256Bits = 256 / BitsPerByte;
        //Block 1: Jun-05-2019, 02:00:00 PM +2
        //Block 2: Jun-24-2019, 08:01:26 PM +2
        //static readonly DateTime FirstWaxBlock = new DateTime(2019, 6, 24, 20, 1, 25, DateTimeKind.Utc);
        const int TicksPerMs = 10_000;
        const long MsPerBlock = 500L;
        #region Name-based constants
        const int NameMaxCharLength = 13;
        /// <summary>
        /// The amount of bits an Antelope name is encoded into.
        /// </summary>
        const int NameBitLength = 64;
        /// <summary>
        /// The amount of bits we can use per character.
        /// </summary>
        const int BitsPerNameValue = 5;
        /// <summary>
        /// The amount of charcters that can use the full bit length we need (12)
        /// </summary>
        const int NameFullBitCharCount = NameBitLength / BitsPerNameValue;
        /// <summary>
        /// The amount of bits that can have the full-length (60 in our case)
        /// </summary>
        const int NameBitsWithFullBitLength = NameFullBitCharCount * BitsPerNameValue;
        /// <summary>
        /// The amount of bits that remain for the last value (4)
        /// </summary>
        const int NameRestBits = NameBitLength - NameBitsWithFullBitLength;
        /// <summary>
        /// The last bit index that has <see cref="BitsPerNameValue"/> bits per encoded character.
        /// </summary>
        /// <remarks>
        /// Indexes are 0-based, so we take the amount of bits that are full-length values and substract 1.
        /// </remarks>
        const int LastFullLengthNameBitIndex = NameBitsWithFullBitLength - 1;
        /// <summary>
        /// The bitmask we use to extract the bits from the value. We shift by the bit length (e.g. overshoot), 
        /// then substract 1 to get a full set of binary 1 flags for our desired bit length.
        /// </summary>
        const int NameValueBitMask = (1 << BitsPerNameValue) - 1;
        /// <summary>
        /// The bitmask we use to extract the bits from the value. We shift by the bit length (e.g. overshoot), 
        /// then substract 1 to get a full set of binary 1 flags for our desired bit length.
        /// </summary>
        const int NameRestBitMask = (1 << NameRestBits) - 1;
        #endregion
        public const long BlockEpoch = 946684800000L;
        static Dictionary<char, byte> CharByteLookup;
        static Dictionary<byte, char> ByteCharLookup;
        static Dictionary<string, byte> HexLookup;
        static NumberFormatInfo nfi;
        static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        static (string currentDir, uint minBlock, uint maxBlock) BlockDirCache;
        static EOSIOExtensions()
        {
            nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            nfi.NumberGroupSeparator = " ";
            BlockDirCache = (string.Empty, uint.MaxValue, uint.MaxValue);
            CharByteLookup = new Dictionary<char, byte>();
            ByteCharLookup = new Dictionary<byte, char>();
            CharByteLookup.Add('.', 0);
            ByteCharLookup.Add(0, '.');
            for (byte i = 1; i <= 5; ++i)
            {
                CharByteLookup.Add(i.ToString()[0], i);
                ByteCharLookup.Add(i, i.ToString()[0]);
            }
            byte offset = 'a' - 6;
            for (char c = 'a'; c <= 'z'; ++c)
            {
                CharByteLookup.Add(c, (byte)((byte)c - offset));
                ByteCharLookup.Add((byte)((byte)c - offset), c);
            }
            HexLookup = new Dictionary<string, byte>(byte.MaxValue);
            string hex;
            for (int i = byte.MinValue; i <= byte.MaxValue; ++i)
            {
                hex = i.ToString("x2");
                HexLookup.Add(hex, (byte)i);
                // Add uppercase variants too, just to be sure
                hex = hex.ToUpperInvariant();
                if (!HexLookup.ContainsKey(hex)) HexLookup.Add(hex, (byte)i);
            }
        }
        public static DateTime FromBlockTime(this UInt32 value) => Epoch.AddMilliseconds(value * MsPerBlock + BlockEpoch);
        // There is about 4,5h of time drift, so can't use it
        //public static DateTime GetTimeOfBlock(this UInt32 blockNumber) => FirstWaxBlock.AddMilliseconds(blockNumber * MsPerBlock);
        public static UInt32 ToBlockTime(this DateTime value) => (UInt32)Math.Round((value.Subtract(Epoch).TotalMilliseconds - BlockEpoch) / 500D);
        public static byte[] Serialize(this string value) => value.Encode();
        public static byte[] Encode(this string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            byte[] result = new byte[bytes.Length + 1];
            result[0] = (byte)bytes.Length;
            Array.Copy(bytes, 0, result, 1, bytes.Length);
            return result;
        }
        public static byte[] EncodeCheckSum256(this string hex)
        {
            int ix = 0; // track index separately, slightly faster than i/2
            byte[] result = new byte[BytesPer256Bits];
            for (int i = 0; i < BytesPer256Bits; i += 2)
                // use substring over new string(new char[] { hex[i], hex[i + 1] }) because it's 10% faster
                result[ix++] = HexLookup[hex.Substring(i, 2)];
            return result;
        }
        public static string ReadEosioString(this BinaryReader br) => Encoding.UTF8.GetString(br.ReadBytes(br.DecodeInt32()));
        public static string ReadEosioString(this BinaryReader br, int length) => Encoding.UTF8.GetString(br.ReadBytes(length)).TrimEnd(NULL);
        public static byte[] ReadEosioByteArray(this BinaryReader br) => br.ReadBytes(br.ReadByte());
        public static string ReadEosio_checksum256(this BinaryReader br) => ByteArrayToHexViaLookup32(br.ReadBytes(32));
        #region ByteToHex; source: https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa/24343727#24343727
        static readonly uint[] _lookup32 = CreateLookup32();
        static uint[] CreateLookup32()
        {
            var result = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                string s = i.ToString("x2");
                result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
            }
            return result;
        }
        public static string ByteArrayToHexViaLookup32(this byte[] bytes)
        {
            var lookup32 = _lookup32;
            var result = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                var val = lookup32[bytes[i]];
                result[2 * i] = (char)val;
                result[2 * i + 1] = (char)(val >> 16);
            }
            return new string(result);
        }
        #endregion
        public static string ToName(this ulong value)
        {
            char[] result = new char[NameMaxCharLength];
            byte v;
            char c;
            int resultIndex = 0;
            // The first 60 bits are 5-bits per value; 
            for (int i = 0; i < NameBitsWithFullBitLength; i += BitsPerNameValue)
            {
                v = (byte)((value >> LastFullLengthNameBitIndex - i) & NameValueBitMask);
                c = ByteCharLookup[v];
                result[resultIndex++] = c;
            }
            v = (byte)(value & NameRestBitMask);
            c = ByteCharLookup[v];
            result[resultIndex] = c;
            // Strip any trailing 0-values (e.g. '.')
            return new string(result).TrimEnd(ByteCharLookup[0]);
        }
        public static ulong NameToLong(this string name)
        {
            ulong result = 0L;
            int bitIndex = 0, i;
            byte c;
            // Process the full-bit-length characters
            for (i = 0; i < NameFullBitCharCount; i++)
            {
                c = i < name.Length ? CharByteLookup[name[i]] : (byte)0;
                if ((c & 0b00001) == 0b00001) result += 1UL << (59 - bitIndex);
                if ((c & 0b00010) == 0b00010) result += 1UL << (60 - bitIndex);
                if ((c & 0b00100) == 0b00100) result += 1UL << (61 - bitIndex);
                if ((c & 0b01000) == 0b01000) result += 1UL << (62 - bitIndex);
                if ((c & 0b10000) == 0b10000) result += 1UL << (63 - bitIndex);
                bitIndex += 5;
            }
            // Process the last 4 bits
            c = i < name.Length ? CharByteLookup[name[i]] : (byte)0;
            if ((c & 0b0001) == 0b0001) result += 1UL;
            if ((c & 0b0010) == 0b0010) result += 1UL << 1;
            if ((c & 0b0100) == 0b0100) result += 1UL << 2;
            if ((c & 0b1000) == 0b1000) result += 1UL << 3;
            return result;
        }
    }
}
