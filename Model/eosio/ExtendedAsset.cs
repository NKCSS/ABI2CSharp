using Abi2CSharp.Extensions;
using Abi2CSharp.Interfaces;
using Newtonsoft.Json;
using System;
using System.Globalization;

namespace Abi2CSharp.Model.eosio
{
    [JsonConverter(typeof(CustomJsonConverter<ExtendedAsset>))]
	public class ExtendedAsset : ICustomSerialize<ExtendedAsset>
	{	
		public Asset Quantity { get; set; }
		public Name Contract { get; set; }
		public override string ToString() => $"{Quantity.ToString()}@{Contract}";
        public ExtendedAsset() { } // Empty constructor for serializing
		public ExtendedAsset(Asset quantity, Name contract)
        {
			Quantity = quantity;
			Contract = contract;
        }
		public static implicit operator ExtendedAsset(string value)
		{
			Asset result = new Asset();
			string[] parts = value.Split('@');
			if (parts.Length != 2) throw new ArgumentException($"Cannot parse '{value}' as a valid token balance", nameof(value));
			return new ExtendedAsset(parts[0], parts[1]);
		}
		public static implicit operator string(ExtendedAsset value) => value.ToString();
		public string Serialize() => ToString();
		public ExtendedAsset Deserialize(JsonReader reader)
        {
            // Read Start object Token.
            reader.Read();
            reader.Read();
            Asset quantity = (string)reader.Value;
            reader.Read();
            reader.Read();
            Name contract = (string)reader.Value;
            reader.Read();
            return new ExtendedAsset(quantity, contract);
        }
        public override int GetHashCode() => ToString().GetHashCode();
		public override bool Equals(object obj) => obj?.GetHashCode().Equals(GetHashCode()) ?? false;
		public static ExtendedAsset operator +(ExtendedAsset a, ExtendedAsset b)
        {
			System.Diagnostics.Contracts.Contract.Requires<ArgumentException>(a.Contract == b.Contract, "Both assets need to be the same contract!");
            System.Diagnostics.Contracts.Contract.Requires<ArgumentException>(a.Quantity.Token == b.Quantity.Token, "Both assets need to be the same token!");
            return new ExtendedAsset(a.Quantity + b.Quantity, a.Contract);
		}
		public static ExtendedAsset operator -(ExtendedAsset a, ExtendedAsset b)
        {
            System.Diagnostics.Contracts.Contract.Requires<ArgumentException>(a.Contract == b.Contract, "Both assets need to be the same contract!");
            System.Diagnostics.Contracts.Contract.Requires<ArgumentException>(a.Quantity.Token == b.Quantity.Token, "Both assets need to be the same token!");
            return new ExtendedAsset(a.Quantity - b.Quantity, a.Contract);
        }
		public static ExtendedAsset operator *(ExtendedAsset x, ulong multi)
			=> new ExtendedAsset(x.Quantity * multi, x.Contract);
		public static ExtendedAsset operator /(ExtendedAsset x, ulong multi)
		{
			System.Diagnostics.Contracts.Contract.Requires<DivideByZeroException>(multi > 0, "Cannot divide by zero");
			return new ExtendedAsset(x.Quantity / multi, x.Contract);
		}
	}
}