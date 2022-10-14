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
        const int BlocksPerDay = 2 * 60 * 60 * 24;
        const string EosioContractName = "eosio";
        const string SetAbiActionName = "setabi";
        const string ContractActionSeparator = ":";
        public static Dictionary<string, Dictionary<uint, byte[]>> ExternalABIs = new Dictionary<string, Dictionary<uint, byte[]>>();
        #region Simple Type Mapping
        public static Dictionary<string, string> AbiTypeMapping = new Dictionary<string, string> {
            { "name", "Model.eosio.Name"},//"ulong" },
            { "asset", "Model.eosio.Asset" },
            { "symbol", "Model.eosio.Symbol" },
            { "uint64", "ulong" },
            { "uint32", "uint" },
            { "uint16", "ushort" },
            { "uint8", "byte" },
            { "int64", "long" },
            { "int32", "int" },
            { "int16", "short" },
            { "int8", "byte" },
            { "float64", "double" },
        };
        #endregion
        static async Task Main(string[] args)
        {
            /*
            var cfg = JsonConvert.DeserializeObject<Contracts.atomicmarket.Responses.config>(@"
{""version"":""1.3.3"",""sale_counter"":0,""auction_counter"":0,""minimum_bid_increase"":""0.10000000000000001"",""minimum_auction_duration"":120,""maximum_auction_duration"":2592000,""auction_reset_duration"":120,""supported_tokens"":[{""token_contract"":""eosio.token"",""token_symbol"":""8,WAX""}],""supported_symbol_pairs"":[{""listing_symbol"":""2,USD"",""settlement_symbol"":""8,WAX"",""delphi_pair_name"":""waxpusd"",""invert_delphi_pair"":0}],""maker_market_fee"":""0.01000000000000000"",""taker_market_fee"":""0.01000000000000000"",""atomicassets_account"":""atomicassets"",""delphioracle_account"":""delphioracle""}
");
            return;
            await Contracts.atomicmarket.Test();
            */
            //await Contracts.atomicmarket.Test();
            //return;
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
                Func<string, Dictionary<string, string>> getFields = (typeName) => {
                    var result = new Dictionary<string, string>();
                    var extract = getStruct(typeName);
                    if (extract != null)
                    {
                        foreach (var field in extract.fields)
                        {
                            bool isArray = field.type.EndsWith("[]");
                            result.Add(field.name, (AbiTypeMapping.TryGetValue(isArray ? field.type.Substring(0, field.type.Length - 2) : field.type, out string mappedType) ? $"{mappedType}{(isArray ? "[]" : string.Empty)}" : field.type));
                        }
                    }
                    else
                    {
                        throw new ApplicationException($"Could not find Struct '{typeName}'");
                    }
                    return result;
                };
                Dictionary<string, Dictionary<string, string>> customTypeMappings = new Dictionary<string, Dictionary<string, string>>();
                string CustomTypePrefix = $"Model.{exportName}";
                foreach (var type in abi.structs)
                {
                    Console.WriteLine($"TYPE {type.name}: {JsonConvert.SerializeObject(type.fields)}");
                    var fields = new Dictionary<string, string>();
                    foreach (var field in type.fields)
                    {
                        bool isArray = field.type.EndsWith("[]");
                        fields.Add(field.name, (AbiTypeMapping.TryGetValue(isArray ? field.type.Substring(0, field.type.Length - 2) : field.type, out string mappedType) ? $"{mappedType}{(isArray ? "[]" : string.Empty)}" : field.type));
                    }
                    customTypeMappings.Add(type.name, fields);
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
                    includeExtensions: AutoMappedConfig.includeExtensions
                );
                var codeText = cg.TransformText();
                Console.WriteLine(codeText);
                File.WriteAllText($"{exportName}.cs", codeText);
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
                    abis[x.block_num] = StringToByteArrayFastest(x.act.data.abi);
                }
                Console.WriteLine($"ABI Count: {abis.Count}");
                Thread.Sleep(5000);
            }
            while (data.total.value > retrieved);
            File.WriteAllText(Path.Combine(AutoMappedConfig.abiFolder, $"{accountName}{AutoMappedConfig.abiFileSeparator}{AutoMappedConfig.abiFileSuffix}"), JsonConvert.SerializeObject(abis));
            return abis;
        }
        static byte[] StringToByteArrayFastest(string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");
            int byteCount = hex.Length >> 1;

            byte[] arr = new byte[byteCount];

            for (int i = 0; i < byteCount; ++i)
            {
                arr[i] = (byte)((hex[i << 1].GetHexVal() << 4) + hex[(i << 1) + 1].GetHexVal());
            }

            return arr;
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