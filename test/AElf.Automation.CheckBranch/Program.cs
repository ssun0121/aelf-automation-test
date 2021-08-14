using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;

namespace AElf.Automation.CheckBranch
{
    class Program
    {
        public static int Main(string[] args)
        {
            return CommandLineApplication.Execute<Program>(args);
        }

        private async Task OnExecute(CommandLineApplication app)
        {
            //Init Logger
            Log4NetHelper.LogInit("CheckBranch");
            Logger = Log4NetHelper.GetLogger();
            Logger.Info($"node: {NodeUrl}");
            _nodeManager = new NodeManager(NodeUrl);
            _nodeServices = new NodeServices(_nodeManager);
            var initialStatus = await _nodeManager.ApiClient.GetChainStatusAsync();
            Logger.Info($"Start check from LIB height {initialStatus.LastIrreversibleBlockHeight}");
            var branchesInfo = new Dictionary<long, List<Branch>>();
            var allFork = new List<ForkBranch>();
            var lib = initialStatus.LastIrreversibleBlockHeight;
            while (lib < initialStatus.LastIrreversibleBlockHeight + long.Parse(Count))
            {
                var status = await _nodeManager.ApiClient.GetChainStatusAsync();
                var branches = _nodeServices.GetChainStatus(status);
                var statusLib = status.LastIrreversibleBlockHeight;
                long updateStatusLib;
                branchesInfo[statusLib] = branches;
                Logger.Info($"LIB: {statusLib}");
                do
                {
                    status = await _nodeManager.ApiClient.GetChainStatusAsync();
                    branches = _nodeServices.GetChainStatus(status);
                    updateStatusLib = status.LastIrreversibleBlockHeight;
                    branchesInfo[statusLib] = branches;
                } while (updateStatusLib == statusLib);
                lib = statusLib;
            }

            foreach (var branchInfo in branchesInfo)
            {
                var fork = await _nodeServices.CheckBranch(branchInfo.Key, branchInfo.Value);
                var forkInfo = new ForkBranch(fork.First().Key, fork.First().Value);
                allFork.Add(forkInfo);
            }

            var json = CommonHelper.GetJson(allFork);
            var jsonFormatting = CommonHelper.ConvertJsonString(json);
            var node = NodeUrl.Split(":");
            var path = CommonHelper.MapPath($"forkInfo_{node.First().Split(".").Last()}_{Limit}_{Times}.json");
            await using StreamWriter file = File.CreateText($"{path}");
            //serialize object directly into file stream
            await File.WriteAllTextAsync(path, jsonFormatting);

            var sum = 0;
            for (var i = initialStatus.LastIrreversibleBlockHeight; i <= lib; i++)
            {
                var blockInfo = await _nodeServices.CheckBlockInfo(i);
                var count = blockInfo.Body.TransactionsCount;
                sum += count;
                Logger.Info($"BlockNumber: {i} has {count} transactions");
            }

            var blockCount = lib - initialStatus.LastIrreversibleBlockHeight;

            var forkBranchList = new List<string>();
            foreach (var branch in from forkBranch in allFork
                from branch in forkBranch.Branches
                where !forkBranchList.Contains(branch.BlockHash)
                select branch)
            {
                forkBranchList.Add(branch.BlockHash);
                Logger.Info($"fork branch: {branch.BlockHash}");
            }
            Logger.Info($"total transaction: {sum}, block count {blockCount}, average: {sum / (blockCount)}, fork count: {forkBranchList.Count}");
        }

        private static ILog Logger { get; set; }
        private INodeManager _nodeManager;
        private NodeServices _nodeServices;

        #region Parameter Option

        [Option("-u|--url", Description = "check node url ")]
        private static string NodeUrl { get; set; }

        [Option("-t|--times", Description = "check times ")]
        private static string Times{ get; set; }
        
        [Option("-bc|--count", Description = "check block count ")]
        private static string Count { get; set; }

        [Option("-l|--limit", Description = "tx limit ")]
        private static string Limit { get; set; }

        #endregion
    }
}