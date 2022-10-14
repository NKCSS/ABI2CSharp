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
    }
}
