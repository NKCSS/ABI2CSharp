using System.Collections.Generic;

namespace Abi2CSharp.Model.ABI
{
    public class Variant
    {
        public string name { get; set; }
        public List<string> types { get; set; }
        public override string ToString() => $"{name} => [{string.Join(", ", types)}]";
    }
}
