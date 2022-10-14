namespace Abi2CSharp.Model.ABI
{
    public class Action
    {
        public string name { get; set; }
        public string type { get; set; }
        public string ricardian { get; set; }
        public override string ToString() =>$"{name} => [{type}]";
    }
}