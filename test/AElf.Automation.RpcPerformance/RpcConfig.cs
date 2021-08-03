using System;
using System.Collections.Generic;
using AElfChain.Common.Helpers;
using Newtonsoft.Json;

namespace AElf.Automation.RpcPerformance
{
    public class RpcConfig
    {
        [JsonProperty("GroupCount")] public int GroupCount { get; set; }
        [JsonProperty("TransactionGroup")] public int TransactionGroup { get; set; }
        [JsonProperty("TransactionCount")] public int TransactionCount { get; set; }
        [JsonProperty("ServiceUrl")] public string ServiceUrl { get; set; } 
        [JsonProperty("Timeout")] public int Timeout { get; set; }
        [JsonProperty("Duration")] public int Duration { get; set; }
        
        [JsonProperty("RandomSenderTransaction")]
        public bool RandomSenderTransaction { get; set; }
        [JsonProperty("ContractAddress")] public string ContractAddress { get; set; }
        [JsonProperty("InitAccount")] public string InitAccount { get; set; }
        [JsonProperty("TokenList")] public List<string> TokenList { get; set; }
        [JsonProperty("IsNeedFee")] public bool IsNeedFee { get; set; }
        [JsonProperty("SentTxLimit")] public int SentTxLimit { get; set; }

        public static RpcConfig ReadInformation => ConfigHelper<RpcConfig>.GetConfigInfo("rpc-performance.json");
    }
}