using System.Collections.Generic;
using AElfChain.Common.Helpers;
using Newtonsoft.Json;

namespace AElf.Automation.SwapTokenTest
{
    public class EnvironmentInfo
    {
        [JsonProperty("ConfigFile")] public string ConfigFile { get; set; }
        [JsonProperty("Url")] public string Url { get; set; }
        [JsonProperty("Owner")] public string Owner { get; set; }
        [JsonProperty("Password")] public string Password { get; set; }
    }
    public class SwapConfig
    {
        [JsonProperty("EnvironmentInfo")] public EnvironmentInfo EnvironmentInfo { get; set; }
        [JsonProperty("Bridge")] public string Bridge { get; set; }
        [JsonProperty("MerkleTreeRecorder")] public string MerkleTreeRecorder { get; set; }
        [JsonProperty("MerkleTreeGenerator")] public string MerkleTreeGenerator { get; set; }
        [JsonProperty("Regiment")] public string Regiment { get; set; }
        [JsonProperty("PairId")] public string PairId { get; set; }
        [JsonProperty("ReceiveAccount")] public List<string> ReceiveAccounts { get; set; }
        
        public static SwapConfig ReadInformation =>
            ConfigHelper<SwapConfig>.GetConfigInfo("swap-info.json",false);
    }
}