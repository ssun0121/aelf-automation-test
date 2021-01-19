using AElfChain.Common.Helpers;
using Newtonsoft.Json;

namespace AElf.Automation.LotteryTest
{
    public class ConfigInfo
    {
        [JsonProperty("Url")] public string Url { get; set; }
        [JsonProperty("InitAccount")] public string InitAccount { get; set; }
        [JsonProperty("Password")] public string Password { get; set; }
        
        [JsonProperty("LotteryContract")] public string LotteryContract { get; set; }
        [JsonProperty("SellerAccount")] public string SellerAccount { get; set; }
        [JsonProperty("VirtualAddress")] public string VirtualAddress { get; set; }

        [JsonProperty("Symbol")] public string Symbol { get; set; }
        [JsonProperty("Price")] public int Price { get; set; }
        [JsonProperty("CashDuration")] public int CashDuration { get; set; }
        [JsonProperty("Bonus")] public int Bonus { get; set; }
        [JsonProperty("ProfitsRate")] public int ProfitsRate { get; set; }
        
        [JsonProperty("UserCount")] public int UserCount { get; set; }
        
        public static ConfigInfo ReadInformation => ConfigHelper<ConfigInfo>.GetConfigInfo("lottery-config.json");
        
    }
}
