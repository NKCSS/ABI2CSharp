using Abi2CSharp.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace Abi2CSharp.Model.ABI
{
    public class Response
    {
        const string V1dot1 = "eosio::abi/1.1";
        const string V1dot2 = "eosio::abi/1.2";
        public string version { get; set; }
        public List<Struct> structs { get; set; }
        public Dictionary<string, int> structIndexLookup { get; set; }
        public List<Action> actions { get; set; }
        public Dictionary<string, int> actionIndexLookup { get; set; }
        public List<Type> types { get; set; }
        public Dictionary<string, string> typeLookup { get; set; }
        public Dictionary<string, string> typeInverseLookup { get; set; }
        public List<Variant> variants { get; set; }
        public Dictionary<string, Variant> variantLookup { get; set; }
        public List<Table> tables { get; set; }
        public List<(string id, string body)> ricardian_clauses { get; set; }
        public Response() { } // Empty constructor for serializing
        public void RebuildLookups()
        {
            structIndexLookup = new Dictionary<string, int>();
            actionIndexLookup = new Dictionary<string, int>();
            typeLookup = new Dictionary<string, string>();
            Type t;
            for (int i = 0; i < types.Count; ++i)
            {
                t = types[i];
                typeLookup.Add(t.new_type_name, t.type);
                typeInverseLookup.Add(t.type, t.new_type_name);
            }
            Struct s;
            for (int i = 0; i < structs.Count; ++i)
            {
                s = structs[i];
                structIndexLookup.Add(s.name, i);
            }
            Action a;
            for (int i = 0; i < (actions?.Count?? 0); ++i)
            {
                a = actions[i];
                actionIndexLookup.Add(a.name, i);
            }
            variantLookup = variants.ToDictionary(x => x.name,x => x);
        }
        public Response(byte[] raw)
        {
            structIndexLookup = new Dictionary<string, int>();
            actionIndexLookup = new Dictionary<string, int>();
            typeLookup = new Dictionary<string, string>();
            typeInverseLookup = new Dictionary<string, string>();
            using (var ms = new MemoryStream(raw))
            {
                using (var br = new BinaryReader(ms))
                {
                    version = br.ReadEosioString();
                    types = new List<Type>(br.DecodeInt32());
                    for (int i = 0; i < types.Capacity; ++i)
                    {
                        var t = new Type
                        {
                            new_type_name = br.ReadEosioString(),
                            type = br.ReadEosioString(),
                        };
                        types.Add(t);
                        typeLookup.Add(t.new_type_name, t.type);
                        typeInverseLookup.Add(t.type, t.new_type_name);
                    }
                    structs = new List<Struct>(br.DecodeInt32());
                    for (int i = 0; i < structs.Capacity; ++i)
                    {
                        var s = new Struct {
                            name = br.ReadEosioString(),
                            @base = br.ReadEosioString(),
                            fields = new List<Field>(br.DecodeInt32()),
                        };
                        for (int j = 0; j < s.fields.Capacity; ++j)
                        {
                            s.fields.Add(new Field { 
                                name = br.ReadEosioString(),
                                type = br.ReadEosioString(),
                            });
                        }
                        structs.Add(s);
                        structIndexLookup.Add(s.name, i);
                    }
                    actions = new List<Action>(br.DecodeInt32());
                    for (int i = 0; i < actions.Capacity; ++i)
                    {
                        actions.Add(new Action { 
                            name = br.ReadUInt64().ToName(),
                            type = br.ReadEosioString(),
                            ricardian = br.ReadEosioString(),
                        });
                        actionIndexLookup.Add(actions[i].name, i);
                    }
                    tables = new List<Table>(br.DecodeInt32());
                    for (int i = 0; i < tables.Capacity; ++i)
                    {
                        var t = new Table
                        {
                            name = br.ReadUInt64().ToName(),
                            index_type = br.ReadEosioString(),
                        };
                        t.key_names = new List<string>(br.DecodeInt32());
                        for(int j = 0; j < t.key_names.Capacity; ++j)
                        {
                            t.key_names.Add(br.ReadEosioString());
                        }
                        t.key_types = new List<string>(br.DecodeInt32());
                        for (int j = 0; j < t.key_types.Capacity; ++j)
                        {
                            t.key_types.Add(br.ReadEosioString());
                        }
                        t.type = br.ReadEosioString();
                        tables.Add(t);
                    }
                    int errorMessages, abiExtensions, actionResults, kv_tables;
                    if (br.BaseStream.Position < br.BaseStream.Length)
                    {
                        ricardian_clauses = new List<(string id, string body)>(br.DecodeInt32());
                        for(int i = 0; i < ricardian_clauses.Capacity; ++i)
                        {
                            ricardian_clauses.Add((br.ReadEosioString(), br.ReadEosioString()));
                        }
                    }
                    if (br.BaseStream.Position < br.BaseStream.Length) errorMessages = br.DecodeInt32();
                    if (br.BaseStream.Position < br.BaseStream.Length) abiExtensions = br.DecodeInt32();
                    if (br.BaseStream.Position < br.BaseStream.Length)
                    {
                        variants = new List<Variant>(br.DecodeInt32());
                        for(int i = 0; i < variants.Capacity; ++i)
                        {
                            var v = new Variant
                            {
                                name = br.ReadEosioString(),
                                types = new List<string>(br.DecodeInt32()),
                            };
                            for (int j = 0; j < v.types.Capacity; ++j)
                            {
                                v.types.Add(br.ReadEosioString());
                            }
                            variants.Add(v);
                        }
                        variantLookup = variants.ToDictionary(x => x.name, x => x);
                    }
                    if (br.BaseStream.Position < br.BaseStream.Length) actionResults = br.DecodeInt32();
                    if (br.BaseStream.Position < br.BaseStream.Length) kv_tables = br.DecodeInt32();
                    if (br.BaseStream.Position != br.BaseStream.Length)
                    {
                        Console.WriteLine($"Stream Position: {br.BaseStream.Position}/{br.BaseStream.Length}");
                    }
                }
            }
        }
    }
}