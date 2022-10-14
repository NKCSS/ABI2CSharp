using System.IO;

namespace Abi2CSharp.Extensions
{
    public static class MemoryStreamExtensions
    {
        public static string ReadToEnd(this MemoryStream ms)
        {
            using (var sr = new StreamReader(ms))
            {
                return sr.ReadToEnd();
            }
        }
    }
}
