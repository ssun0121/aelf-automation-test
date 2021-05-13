using System.Collections.Generic;
using AElfChain.Common.Helpers;
using Newtonsoft.Json;

namespace AElf.Automation.OracleTest
{
    public class EnvironmentInfo
    {
        [JsonProperty("Environment")] public string Environment { get; set; }
        [JsonProperty("Url")] public string Url { get; set; }
        [JsonProperty("Owner")] public string Owner { get; set; }
        [JsonProperty("Password")] public string Password { get; set; }
    }

    public class Info
    {
        [JsonProperty("Token")] public string Token { get; set; }
        [JsonProperty("UrlToQuery")] public string UrlToQuery { get; set; }
        [JsonProperty("AttributesToFetch")] public string AttributesToFetch { get; set; }
    }

    public class OracleConfig
    {
        [JsonProperty("TestEnvironment")] public string TestEnvironment { get; set; }
        [JsonProperty("EnvironmentInfo")] public List<EnvironmentInfo> EnvironmentInfos { get; set; }
        [JsonProperty("OracleContract")] public string OracleContract { get; set; }
        [JsonProperty("IntegerAggregatorContract")] public string IntegerAggregatorContract { get; set; }
        [JsonProperty("QueryInfo")] public List<Info> QueryInfos { get; set; }
        [JsonProperty("PayAmount")] public long PayAmount { get; set; }
        
        public static OracleConfig ReadInformation =>
            ConfigHelper<OracleConfig>.GetConfigInfo("oracle-config.json",false);
    }
}