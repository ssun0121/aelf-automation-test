using AElfChain.Common.Helpers;
using Newtonsoft.Json;

namespace AElf.Automation.LotteryTest
{
    public class ConfigInfo
    {
        [JsonProperty("Environment")] public Environment Environment { get; set; }
        [JsonProperty("ContractInfo")] public ContractInfo ContractInfo { get; set; }
        [JsonProperty("UserCount")] public int UserCount { get; set; }
        [JsonProperty("TestUserCount")] public int TestUserCount { get; set; }
        [JsonProperty("OnlyDraw")] public bool OnlyDraw { get; set; }
        [JsonProperty("OnlyBuy")] public bool OnlyBuy { get; set; }

        public static ConfigInfo ReadInformation => ConfigHelper<ConfigInfo>.GetConfigInfo("lottery-config.json");
    }

    public class Environment
    {
        [JsonProperty("Url")] public string Url { get; set; }
        [JsonProperty("InitAccount")] public string InitAccount { get; set; }
        [JsonProperty("Password")] public string Password { get; set; }
        [JsonProperty("Config")] public string Config { get; set; }
    }

    public class ContractInfo
    {
        [JsonProperty("LotteryContract")] public string LotteryContract { get; set; }
        [JsonProperty("SellerAccount")] public string SellerAccount { get; set; }
        [JsonProperty("VirtualAddress")] public string VirtualAddress { get; set; }

        [JsonProperty("Symbol")] public string Symbol { get; set; }
        [JsonProperty("Price")] public int Price { get; set; }
        [JsonProperty("CashDuration")] public int CashDuration { get; set; }
        [JsonProperty("Bonus")] public int Bonus { get; set; }
        [JsonProperty("ProfitsRate")] public int ProfitsRate { get; set; }
    }
}
