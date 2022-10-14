namespace Abi2CSharp.Model.ABI
{
    public class Type
    {
        public string new_type_name { get; set; }
        public string type { get; set; }
        public override string ToString() => $"{new_type_name} => {type}";
    }
}
