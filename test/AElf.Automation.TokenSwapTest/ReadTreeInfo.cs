using System;
using System.Collections.Generic;
using System.IO;
using AElfChain.Common.Helpers;
using Newtonsoft.Json;

namespace AElf.Automation.TokenSwapTest
{
    public class TreeInfo
    {
        [JsonProperty("tree_index")] public string TreeIndex { get; set; }
        [JsonProperty("merkle_root")] public string MerkleRoot { get; set; }
        [JsonProperty("index_receipt_id")] public long IndexReceiptId { get; set; }
        [JsonProperty("receipt_counts")] public long ReceiptCounts { get; set; }
        [JsonProperty("receipts")] public List<ReceiptInfo> Receipts { get; set; }
    }

    public class ReceiptInfo
    {
        [JsonProperty("receipt_id")] public long ReceiptId { get; set; }
        [JsonProperty("uid")] public string UniqueId { get; set; }
        [JsonProperty("targetAddress")] public string Receiver { get; set; }
        [JsonProperty("amount")] public string Amount { get; set; }
        [JsonProperty("merkle_path")] public MerklePath MerklePath { get; set; }
    }

    public class MerklePath
    {
        [JsonProperty("nodes")] public List<string> Nodes { get; set; }
        [JsonProperty("positions")] public List<bool> Positions { get; set; }
        [JsonProperty("path_length")] public int PathLength { get; set; }
    }

    public class SwapInfo
    {
        private TreeInfo _instance;
        private readonly long _treeIndex;
        private string _jsonContent;
        private readonly object _lockObj = new object();

        public SwapInfo(long treeIndex)
        {
            _treeIndex = treeIndex;
        }

        public TreeInfo TreeInfo => GetInfo(_treeIndex);

        private TreeInfo GetInfo(long i)
        {
            lock (_lockObj)
            {
                try
                {
                    var localPath = CommonHelper.GetDefaultDataDir();
                    var config = Path.Combine(localPath, $@"tokenSwapTest/{i+1}.json");
                    _jsonContent = File.ReadAllText(config);
                }
                catch (FileNotFoundException e)
                {
                    Console.WriteLine($"Could not find file {i+1}");
                    return null;
                }
                
                _instance = JsonConvert.DeserializeObject<TreeInfo>(_jsonContent);
            }

            return _instance;
        }
    }
}