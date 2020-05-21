using System.Collections.Generic;
using System.IO;
using AElfChain.Common.Helpers;
using Newtonsoft.Json;

namespace AElf.Automation.LotteryTest
{
    public class EnvironmentInfo
    {
        [JsonProperty("Environment")] public string Environment { get; set; }
        [JsonProperty("ConfigFile")] public string ConfigFile { get; set; }
        [JsonProperty("Owner")] public string Owner { get; set; }
        [JsonProperty("Password")] public string Password { get; set; }
    }

    public class TokenInfo
    {
        [JsonProperty("Symbol")] public string Symbol { get; set; }
        [JsonProperty("Price")] public long Price { get; set; }
    }

    public class LotteryConfig
    {
        [JsonProperty("TestEnvironment")] public string TestEnvironment { get; set; }
        [JsonProperty("EnvironmentInfo")] public List<EnvironmentInfo> EnvironmentInfos { get; set; }
        [JsonProperty("LotteryContract")] public string LotteryContract { get; set; }
        [JsonProperty("TokenInfo")] public TokenInfo TokenInfo { get; set; }
    }
    
    public static class ConfigHelper
    {
        private static LotteryConfig _instance;
        private static string _jsonContent;
        private static readonly object LockObj = new object();

        public static LotteryConfig Config => GetConfigInfo();

        private static LotteryConfig GetConfigInfo()
        {
            lock (LockObj)
            {
                if (_instance != null) return _instance;

                var configFile = Path.Combine(Directory.GetCurrentDirectory(), "lottery.json");
                _jsonContent = File.ReadAllText(configFile);
                _instance = JsonConvert.DeserializeObject<LotteryConfig>(_jsonContent);
            }

            return _instance;
        }
    }
}