using Abi2CSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Abi2CSharp.Model.eosio
{
	public class Asset
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
	}
}