using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Abi2CSharp
{
    public static class AutoMappedConfig
    {
        public static string chainId { get; private set; }
        public static string api { get; private set; }
        public static string historyApi { get; private set; }
        public static string abiFolder { get; private set; }
        public static string abiFileSuffix { get; private set; }
        public static char abiFileSeparator { get; private set; }
        public static string logFolder { get; private set; }
        public static string blockWarningLogFile { get; private set; }
        public static bool printAll { get; private set; }
        public static bool includeEosioModels { get; private set; }
        public static bool includeEosSharpTest { get; private set; }
        public static bool includeExtensions { get; private set; }
        static AutoMappedConfig()
        {
            Type t = typeof(AutoMappedConfig), intType = typeof(Int32), uintType = typeof(UInt32), intArrayType = typeof(Int32[]), boolType = typeof(Boolean), decimalType = typeof(Decimal), charType = typeof(char), stringArrayType = typeof(List<string>);
            foreach (var property in t.GetProperties())
            {
                try
                {
                    var data = System.Configuration.ConfigurationManager.AppSettings[property.Name];

                    if (property.PropertyType == boolType)
                    {
                        var boolValue = false;
                        bool.TryParse(data, out boolValue);
                        property.SetValue(null, boolValue);
                    }
                    else if (property.PropertyType == intType)
                    {
                        var number = 0;
                        int.TryParse(data, out number);
                        property.SetValue(null, number);
                    }
                    else if (property.PropertyType == intArrayType)
                    {
                        property.SetValue(null, data.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).Select(el => int.Parse(el)).ToArray());
                    }
                    else if (property.PropertyType == stringArrayType)
                    {
                        property.SetValue(null, JsonConvert.DeserializeObject<List<string>>(data));
                    }
                    else if (property.PropertyType == decimalType)
                    {
                        var number = 0M;
                        decimal.TryParse(data, out number);
                        property.SetValue(null, number);
                    }
                    else if (property.PropertyType == charType)
                    {
                        property.SetValue(null, data[0]);
                    }
                    else if (property.PropertyType == uintType)
                    {
                        var number = 0U;
                        uint.TryParse(data, out number);
                        property.SetValue(null, number);
                    }
                    else
                    {
                        property.SetValue(null, data);
                    }
                }
                catch (Exception ex)
                {
                    var x = ex.Message + "test";
                }
            }
        }
    }
}
