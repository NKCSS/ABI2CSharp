using Newtonsoft.Json;
using System;

namespace Abi2CSharp.Model.HistoryAPI
{
    public class Action
    {
        [JsonProperty("@timestamp")]
        public DateTime attimestamp { get; set; }
        public DateTime timestamp { get; set; }
        public uint block_num { get; set; }
        public string trx_id { get; set; }
        public Act act { get; set; }
        public string[] notified { get; set; }
        public Account_Ram_Deltas[] account_ram_deltas { get; set; }
        public long global_sequence { get; set; }
        public string producer { get; set; }
        public int action_ordinal { get; set; }
        public int creator_action_ordinal { get; set; }
    }
}
