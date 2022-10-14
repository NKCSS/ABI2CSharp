namespace Abi2CSharp.Model.HistoryAPI
{
    public class ABIRequest
    {
        public float query_time_ms { get; set; }
        public bool cached { get; set; }
        public int lib { get; set; }
        public Total total { get; set; }
        public Action[] actions { get; set; }
    }
}
