namespace Abi2CSharp.Model.eosio
{
    public class Symbol
    {
		const char Separator = ',';
        public byte precision { get; set; }
        public string name { get; set; }
		/// <remarks>
		/// <see cref="System.Math.Pow">System.Math.Pow(10, 0)</see> returns 1, 
		/// otherwise, this should have been written with a <see cref="System.Math.Max"/>
		/// </remarks>
		[Newtonsoft.Json.JsonIgnore]
		public double Factor { get => System.Math.Pow(10, precision); }
		public Symbol() { }
		public Symbol(string name, byte precision) {
			this.name = name;
			this.precision = precision;
		}
		public static implicit operator Symbol(string value)
        {
			string[] parts = value.Split(Separator);
			if (parts.Length != 2) throw new System.ArgumentException($"Symbol should be precision, followed by name, separated by '{Separator}'", nameof(value));
			else if (!byte.TryParse(parts[0], out byte precision)) throw new System.ArgumentException($"Can't parse '{parts[0]}' as precision", nameof(value));
			else return new Symbol(parts[1], precision);
		}
		public static implicit operator string(Symbol value) => value.ToString();
		public override string ToString() => $"{precision}{Separator}{name}";
	}
}