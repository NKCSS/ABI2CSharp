using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using Abi2CSharp.Extensions;

namespace Abi2CSharp
{
    class Program
    {
        const int RecordsPerRequest = 100;
        const string EosioContractName = "eosio";
        const string SetAbiActionName = "setabi";
        const string ContractActionSeparator = ":";
        public static Dictionary<string, Dictionary<uint, byte[]>> ExternalABIs = new Dictionary<string, Dictionary<uint, byte[]>>();
        #region Simple Type Mapping
        public static Dictionary<string, string> AbiTypeMapping = new Dictionary<string, string> {
            { "name", "Model.eosio.Name"},
            { "asset", "Model.eosio.Asset" },
            { "symbol", "Model.eosio.Symbol" },
            { "checksum256", "Model.eosio.CheckSum256" },
            { "time_point", "Model.eosio.TimePoint" },
            { "time_point_sec", "Model.eosio.TimePointSec" },
            { "uint64", "ulong" },
            { "uint32", "uint" },
            { "uint16", "ushort" },
            { "uint8", "byte" },
            { "int64", "long" },
            { "int32", "int" },
            { "int16", "short" },
            { "int8", "byte" },
            { "float32", "Single" },
            { "float64", "double" },
        };
        #endregion
        static async Task Main(string[] args)
        {
            if ((args?.Length ?? 0) == 0)
            {
                Console.WriteLine("Please speicfy the contract you want to generate code for. Optionally, also specify the block, and the classname to export.");
            }
            else
            {
                string contractName, exportName;
                #region Load ABI Cache
                // Load ABI's
                var abiDir = new DirectoryInfo(AutoMappedConfig.abiFolder);
                if (!abiDir.Exists)
                {
                    Console.WriteLine($"ABI folder not found, trying to create it (if it fails, creater it manualyl or give this programm more rights): {abiDir.FullName}");
                    abiDir.Create();
                }
                var abisPerContract = abiDir.GetFiles($"*{AutoMappedConfig.abiFileSeparator}{AutoMappedConfig.abiFileSuffix}");
                Console.WriteLine($"Loading {abisPerContract.Length} contracts and their ABI history...");
                int readCount = 0;
                foreach (var fi in abisPerContract)
                {
                    contractName = fi.Name.Split(AutoMappedConfig.abiFileSeparator)[0];
                    var abis = JsonConvert.DeserializeObject<Dictionary<uint, byte[]>>(File.ReadAllText(fi.FullName));
                    ExternalABIs.Add(contractName, abis);
                    readCount += abis.Count;
                }
                Console.WriteLine($"Loaded {readCount} ABI's from our local cache!");
                Console.WriteLine();
                #endregion
                exportName = contractName = args[0];
                Model.ABI.Response abi;
                uint blockNr;
                #region Argument Parsing/Validation
                switch (args.Length)
                {
                    case 1:
                        {
                            blockNr = uint.MaxValue;
                            break;
                        }
                    case 3:
                        {
                            // Contract, block nummer, className
                            if (uint.TryParse(args[1], out blockNr))
                            {
                                exportName = args[2];
                            }
                            else if (uint.TryParse(args[2], out blockNr))
                            {
                                exportName = args[1];
                            }
                            else throw new ArgumentException($"Neither '{args[1]}' or '{args[2]}' was able to be parsed as a block number sadly...", nameof(args));
                            break;
                        }
                    case 2:
                        {
                            // Contract, then, if the 2nd is parseable as number, it's a block number, otherwise, it's the class name/export name
                            if (!uint.TryParse(args[1], out blockNr))
                            {
                                blockNr = uint.MaxValue;
                                exportName = args[1];
                            }
                            break;
                        }
                    default: throw new ArgumentException($"We don't support {args.Length} arguments. You need to specify the contract, then, optionally, the block number (will use latest if not supplied) and optionally the export name.", nameof(args));
                }
                #endregion
                abi = await GetAbi(contractName, blockNr);
                Func<string, Model.ABI.Struct> getStruct = (structName) =>
                {
                    if (abi.structIndexLookup.TryGetValue(abi.typeLookup.TryGetValue(structName, out string deferredName) ? deferredName : structName, out int structIndex))
                    {
                        var actionFields = abi.structs[structIndex];
                        return actionFields;
                    }
                    return null;
                };
                Func<Model.ABI.Struct, Dictionary<string, string>> mapTypes = (s) =>
                {
                    var result = new Dictionary<string, string>();
                    string t, mt;
                    bool nullable;
                    foreach (var field in s.fields)
                    {
                        t = field.type;
                        nullable = t.EndsWith('?') || t.EndsWith('$');
                        if (nullable) t = t.Substring(0, t.Length - 1);
                        bool isArray = t.EndsWith("[]");
                        mt = AbiTypeMapping.TryGetValue(isArray ? t.Substring(0, t.Length - 2) : t, out string mappedType) ? $"{mappedType}{(isArray ? "[]" : string.Empty)}" : t;
                        if (nullable && !isArray) mt += "?";
                        result.Add(field.name, mt);
                    }
                    return result;
                };
                Func<string, Dictionary<string, string>> getFields = (typeName) => {
                    var result = new Dictionary<string, string>();
                    var extract = getStruct(typeName);
                    if (extract != null)
                    {
                        result = mapTypes(extract);
                    }
                    else
                    {
                        throw new ApplicationException($"Could not find Struct '{typeName}'");
                    }
                    return result;
                };
                Dictionary<string, Dictionary<string, string>> customTypeMappings = new Dictionary<string, Dictionary<string, string>>();
                string CustomTypePrefix = $"Model.{exportName}";
                Dictionary<string, List<string>> VariantUsageLookup = new Dictionary<string, List<string>>();
                string interfaceName;
                foreach (var variant in abi.variants)
                {
                    interfaceName = $"I{(abi.typeInverseLookup.TryGetValue(variant.name, out string friendlyName) ? friendlyName : variant.name)}";
                    AbiTypeMapping.Add(variant.name, interfaceName);
                    foreach (var type in variant.types)
                    {
                        if (!VariantUsageLookup.TryGetValue(type, out var interfaces))
                        {
                            interfaces = new List<string>();
                            VariantUsageLookup.Add(type, interfaces);
                        }
                        interfaces.Add(interfaceName);
                    }
                }
                foreach(var type in abi.types)
                {
                    AbiTypeMapping.Add(type.new_type_name, AbiTypeMapping.TryGetValue(type.type, out string mappedName) ? mappedName : type.type);
                }
                foreach (var type in abi.structs)
                {
                    Console.WriteLine($"TYPE {type.name}: {JsonConvert.SerializeObject(type.fields)}");
                    customTypeMappings.Add(type.name, mapTypes(type));
                }
                foreach (var tbl in abi.tables)
                {
                    var fields = getFields(tbl.type);
                    Console.WriteLine($"TABLE {tbl.name} ({tbl.type}): {JsonConvert.SerializeObject(fields)}");
                }
                foreach (var action in abi.actions)
                {
                    var fields = getFields(action.type);
                    Console.WriteLine($"ACTION {action.name} ({action.type}): {JsonConvert.SerializeObject(fields)}");
                }
                Console.WriteLine();
                var cg = new Templates.AbiCodeGen(
                    contractName: contractName, 
                    exportName: exportName, 
                    actions: abi.actions,
                    tables: abi.tables,
                    types: customTypeMappings,
                    includeEosioModels: AutoMappedConfig.includeEosioModels,
                    includeEosSharpTest: AutoMappedConfig.includeEosSharpTest,
                    includeExtensions: AutoMappedConfig.includeExtensions,
                    api: AutoMappedConfig.api,
                    chainId: AutoMappedConfig.chainId,
                    variantUsageLookup: VariantUsageLookup
                );
                var codeText = cg.TransformText();
                Console.WriteLine(codeText);
                File.WriteAllText($"{exportName}.cs", codeText);
                Console.WriteLine($"Generated code written to {exportName}.cs");
                if (AutoMappedConfig.includeEosSharpTest)
                {
                    Console.WriteLine($"Note: since you enabled {nameof(AutoMappedConfig.includeEosSharpTest)}, when using the generated file, you need to add a reference to the EosSharp project.");
                }
            }
        }
        public static async Task<Model.ABI.Response> GetAbi(string contractName, uint blockNumber)
        {
            Model.ABI.Response result = null;
            if (!ExternalABIs.TryGetValue(contractName, out var contractAbis))
            {
                contractAbis = await GetABIs(contractName);
                ExternalABIs.Add(contractName, contractAbis);
            }
            foreach (uint startBlock in contractAbis.Keys.OrderByDescending(x => x))
            {
                if (startBlock <= blockNumber)
                {
                    var data = contractAbis[startBlock];
                    if ((data?.Length ?? 0) > 0)
                    {
                        result = new Model.ABI.Response(contractAbis[startBlock]);
                        break;
                    }
                    else
                    {
                        WarningLog(blockNumber, $"!! Empty ABI at block {startBlock} for contract {contractName} !!");
                    }
                }
            }
            if (result == null)
            {
                // Keep track of the last known block at time of retrieving the ABI's previously to know if we need to check again (compare against blockNumber parameter).
            }
            return result;
        }
        static async Task<Dictionary<uint, byte[]>> GetABIs(string accountName)
        {
            Console.WriteLine($"Retrieving ABI's for contract {accountName}");
            int retrieved = 0;
            HttpResponseMessage result = null;
            string json = null;
            var http = new HttpClient();
            Model.HistoryAPI.ABIRequest data = new Model.HistoryAPI.ABIRequest();
            Dictionary<uint, byte[]> abis = new Dictionary<uint, byte[]>();
            do
            {
                result = await http.GetAsync($"{AutoMappedConfig.historyApi}/v2/history/get_actions?limit={RecordsPerRequest}&skip={retrieved}&account={accountName}&filter={Uri.EscapeDataString($"{EosioContractName}{ContractActionSeparator}{SetAbiActionName}")}&sort=1{(accountName == EosioContractName ? $"&act.authorization.actor={EosioContractName}" : "")}");
                json = await result.Content.ReadAsStringAsync();
                data = JsonConvert.DeserializeObject<Model.HistoryAPI.ABIRequest>(json);
                retrieved += data.actions.Length;
                foreach (var x in data.actions)
                {
                    //NOTE: Technically, there can be two ABI updates inside the same block. We are not going to account for that now, so we use this overwrite flow to just keep the latest ABI for the block.
                    abis[x.block_num] = x.act.data.abi.ToByteArrayFastest();
                }
                Console.WriteLine($"ABI Count: {abis.Count}");
                Thread.Sleep(5000);
            }
            while (data.total.value > retrieved);
            File.WriteAllText(Path.Combine(AutoMappedConfig.abiFolder, $"{accountName}{AutoMappedConfig.abiFileSeparator}{AutoMappedConfig.abiFileSuffix}"), JsonConvert.SerializeObject(abis));
            return abis;
        }
        internal static void WarningLog(uint blockNumber, string msg, bool print = false)
        {
            var logFileName = Path.Combine(AutoMappedConfig.logFolder, AutoMappedConfig.blockWarningLogFile);
            var logFile = new FileInfo(logFileName);
            if(!logFile.Directory.Exists)
            {
                Console.WriteLine($"Logs folder not found, trying to create it (if it fails, creater it manualyl or give this programm more rights): {logFile.Directory.FullName}");
                logFile.Directory.Create();
            }
            File.AppendAllText(logFileName, $"[{blockNumber}@{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}{Environment.NewLine}");
            if (AutoMappedConfig.printAll || print) Console.WriteLine(msg);
        }
    }
}