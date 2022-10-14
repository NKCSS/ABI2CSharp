namespace Abi2CSharp.Model.HistoryAPI
{
    public class Act
    {
        public string account { get; set; }
        public string name { get; set; }
        public Authorization[] authorization { get; set; }
        public Data data { get; set; }
    }
}
