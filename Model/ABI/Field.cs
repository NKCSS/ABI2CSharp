﻿namespace Abi2CSharp.Model.ABI
{
    public class Field
    {
        public string name { get; set; }
        public string type { get; set; }
        public override string ToString() => $"{name} => [{type}]";
    }
}
