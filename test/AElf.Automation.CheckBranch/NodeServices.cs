using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using JetBrains.Annotations;
using log4net;
using Volo.Abp.Threading;

namespace AElf.Automation.CheckBranch
{
    public class NodeServices
    {
        private readonly INodeManager _nodeManager;
        private readonly ILog Logger = Log4NetHelper.GetLogger();

        public NodeServices(INodeManager nodeManager)
        {
            _nodeManager = nodeManager;
        }

        public List<Branch> GetChainStatus(ChainStatusDto status)
        {
            var height = status.LastIrreversibleBlockHeight;
            var branches = status.Branches;
            var notLinkBlock = status.NotLinkedBlocks;
            var branchList = branches.Count > 1
                ? branches.Select(branch => new Branch(branch.Value, branch.Key)).ToList()
                : new List<Branch>();
            return branchList;
        }

        public async Task<Dictionary<long, List<Branch>>> CheckBranch(long height, List<Branch> branches)
        {
            var forkBranch = new Dictionary<long, List<Branch>>();
            var branchList = new List<Branch>();
            var currentHeight = await _nodeManager.ApiClient.GetBlockHeightAsync();
            foreach (var b in branches)
            {
                if (currentHeight < b.Height)
                {
                    Logger.Info($"branch height: {b.Height} higher than current height {currentHeight}");
                    continue;
                }

                var blockInfo = await _nodeManager.ApiClient.GetBlockByHeightAsync(b.Height);
                if (blockInfo.BlockHash.Equals(b.BlockHash)) continue;
                var branch = new Branch(b.Height, b.BlockHash);
                branchList.Add(branch);
            }

            forkBranch[height] = branchList;
            return forkBranch;
        }

        public async Task<long> CalculateBranchHeight(string blockHash)
        {
            var count = 0;
            while (true)
            {
                var blockInfo = await _nodeManager.ApiClient.GetBlockByHashAsync(blockHash);
                var blockHeight = blockInfo.Header.Height;
                var currentBlockHash = (await _nodeManager.ApiClient.GetBlockByHeightAsync(blockHeight)).BlockHash;

                if (blockHash == currentBlockHash)
                {
                    return count;
                }

                blockHash = blockInfo.Header.PreviousBlockHash;
                count += 1;
            }
        }


        public async Task<BlockDto> CheckBlockInfo(long height)
        {
            var blockInfo = await _nodeManager.ApiClient.GetBlockByHeightAsync(height);
            return blockInfo;
        }
    }
}