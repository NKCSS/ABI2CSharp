using Abi2CSharp.Extensions;
using Abi2CSharp.Interfaces;
using Newtonsoft.Json;
using System;
using System.Globalization;

namespace Abi2CSharp.Model.eosio
{
    [JsonConverter(typeof(CustomJsonConverter<Asset>))]
	public class Asset : ICustomSerialize<Asset>
	{
		const string EnglishCultureName = "en-GB";
		static readonly CultureInfo EnglishCulture = new CultureInfo(EnglishCultureName);
		public Symbol Token { get; set; }
		public UInt64 Balance { get; set; }
		[Newtonsoft.Json.JsonIgnore]
		public decimal BalanceDecimal { get => Balance / (decimal)Token.Factor; set => Balance = (ulong)(value * (decimal)Token.Factor); }
		/// <remarks>
		/// We use the F string format so there is only a decimal, no thousand separator.
		/// </remarks>
		public override string ToString() => $"{BalanceDecimal.ToString($"F{Token.precision}", EnglishCulture)} {Token.name}";
        public Asset() { } // Empty constructor for serializing
		public Asset(Symbol token, UInt64 balance)
        {
			Token = token;
			Balance = balance;
        }
		public static implicit operator Asset(string value)
		{
			Asset result = new Asset();
			string[] parts = value.Split(' ');
			if (parts.Length != 2) throw new ArgumentException($"Cannot parse '{value}' as a valid token balance", nameof(value));
			string[] valueParts = parts[0].Split('.');
			result.Token = new Symbol(
				name: parts[1], 
				precision: (byte)(valueParts.Length == 2 ? valueParts[1].Length : 0)
			);
			if (decimal.TryParse(parts[0], NumberStyles.AllowDecimalPoint, EnglishCulture, out decimal balance)) result.BalanceDecimal = balance;
			else throw new ArgumentException($"Unable to parse '{parts[0]}' as a valid decimal");
			return result;
		}
		public static implicit operator string(Asset value) => value.ToString();
		public string Serialize() => ToString();
		public Asset Deserialize(JsonReader reader) => (string)reader.Value;
		public override int GetHashCode() => ToString().GetHashCode();
		public override bool Equals(object obj) => obj?.GetHashCode().Equals(GetHashCode()) ?? false;
		public static Asset operator +(Asset a, Asset b)
        {
			System.Diagnostics.Contracts.Contract.Requires<ArgumentException>(a.Token == b.Token, "Both assets need to be the same token!");
			return new Asset(a.Token, a.Balance + b.Balance);
		}
		public static Asset operator -(Asset a, Asset b)
		{
			System.Diagnostics.Contracts.Contract.Requires<ArgumentException>(a.Token == b.Token, "Both assets need to be the same token!");
			return new Asset(a.Token, a.Balance - b.Balance);
		}
		public static Asset operator *(Asset x, ulong multi)
			=> new Asset(x.Token, x.Balance * multi);
		public static Asset operator /(Asset x, ulong multi)
		{
			System.Diagnostics.Contracts.Contract.Requires<DivideByZeroException>(multi > 0, "Cannot divide by zero");
			return new Asset(x.Token, x.Balance / multi);
		}
	}
}