using System.Collections.Generic;

namespace Abi2CSharp.Model.ABI
{
    public class Struct
    {
        public string name { get; set; }
        public List<Field> fields { get; set; }
        public string @base { get; set; }
        public override string ToString() => $"{name}{(string.IsNullOrWhiteSpace(@base) ? "" : $" : {@base}")} => [{string.Join(", ", fields)}]";
    }
}
