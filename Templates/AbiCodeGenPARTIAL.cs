using System.Collections.Generic;
using System.Linq;
namespace Abi2CSharp.Templates
{
    public partial class AbiCodeGen
    {
        string contractName = "Default";
        string exportName = "Default";
        List<Model.ABI.Action> actions = new List<Model.ABI.Action>();
        List<Model.ABI.Table> tables = new List<Model.ABI.Table>();
        Dictionary<string, Dictionary<string, string>> types = new Dictionary<string, Dictionary<string, string>>();
        Dictionary<string, List<string>> variantUsageLookup = new Dictionary<string, List<string>>();
        List<string> VariantInterfaces = new List<string>();
        bool includeEosioModels;
        bool includeEosSharpTest;
        bool includeExtensions;
        string api;
        string chainId;
        public AbiCodeGen(string contractName, string exportName, List<Model.ABI.Action> actions, List<Model.ABI.Table> tables, Dictionary<string, Dictionary<string, string>> types, bool includeEosioModels, bool includeEosSharpTest, bool includeExtensions, string api, string chainId, Dictionary<string, List<string>> variantUsageLookup)
        {
            this.contractName = contractName;
            this.exportName = exportName;
            this.actions = actions;
            this.tables = tables;
            this.types = types;
            this.includeEosioModels = includeEosioModels;
            this.includeEosSharpTest = includeEosSharpTest;
            this.includeExtensions = includeExtensions;
            this.api = api;
            this.chainId = chainId;
            this.variantUsageLookup = variantUsageLookup;
            // build a unique list of the used variants using the interface names we generated
            VariantInterfaces = variantUsageLookup.Values.SelectMany(x => x).Distinct().ToList();
        }
    }
}
