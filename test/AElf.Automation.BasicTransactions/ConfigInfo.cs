using AElfChain.Common.Helpers;
using Newtonsoft.Json;

namespace AElf.Automation.BasicTransaction
{
    public class ConfigInfo
    {
        [JsonProperty("ServiceUrl")] public string Url { get; set; }
        [JsonProperty("NewNode")] public string NewNode { get; set; }
        [JsonProperty("InitAccount")] public string InitAccount { get; set; }
        [JsonProperty("Password")] public string Password { get; set; }
        [JsonProperty("TransferAmount")] public long TransferAmount { get; set; }
        [JsonProperty("ExecuteMode")] public int ExecuteMode { get; set; }
        [JsonProperty("Times")] public int Times { get; set; }
        [JsonProperty("ContractCount")] public int ContractCount { get; set; }
        
        [JsonProperty("TokenAddress")] public string TokenAddress { get; set; }
        [JsonProperty("WrapperAddress")] public string WrapperAddress { get; set; }
        [JsonProperty("Symbol")] public string Symbol { get; set; }
        
        public static ConfigInfo ReadInformation => ConfigHelper<ConfigInfo>.GetConfigInfo("base-config.json");
    }
}