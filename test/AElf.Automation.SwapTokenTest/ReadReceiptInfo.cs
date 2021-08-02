using System.Collections.Generic;
using System.IO;
using AElfChain.Common.Helpers;
using Newtonsoft.Json;

namespace AElf.Automation.SwapTokenTest
{
    public class ReceiptInfo
    {
        [JsonProperty("id")] public long ReceiptId { get; set; }
        [JsonProperty("address")] public string Receiver { get; set; }
        [JsonProperty("amount")] public string Amount { get; set; }
    }

    public class ReceiptList
    {
        [JsonProperty("ReceiptList")] public List<ReceiptInfo> ReceiptInfos { get; set; }
    }

    public class ReadReceiptInfo
    {
        private static ReceiptList _instance;
        private static string _jsonContent;
        private static readonly object LockObj = new object();

        public static ReceiptList Config => GetConfigInfo();

        private static ReceiptList GetConfigInfo()
        {
            lock (LockObj)
            {
                var localPath = CommonHelper.GetDefaultDataDir();
                var configFile = Path.Combine(localPath, $@"tokenSwapTest/ReceiptInfo_1.json");
                _jsonContent = File.ReadAllText(configFile);
                _instance = JsonConvert.DeserializeObject<ReceiptList>(_jsonContent);
            }

            return _instance;
        }
        
    }
}