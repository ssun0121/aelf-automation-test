using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElfChain.Common;
using AElfChain.Common.Helpers;
using log4net;
using McMaster.Extensions.CommandLineUtils;
using Shouldly;

namespace AElf.Automation.TokenSwapTest
{
    class Program
    {
        static int Main(string[] args)
        {
            return CommandLineApplication.Execute<Program>(args);
        }

        private async Task OnExecute(CommandLineApplication app)
        {
            if (ConfigFile != null) NodeInfoHelper.SetConfig(ConfigFile);
            Log4NetHelper.LogInit("TokenSwapTest_");
            Logger = Log4NetHelper.GetLogger();

            var tokenSwap = new TokenSwap(TokenSwapContract, PairId);
            var currentRound = await tokenSwap.CheckTree();
            var index = TokenSwapRound == 0 || TokenSwapRound > currentRound ? currentRound : TokenSwapRound;
            TreeInfos = EnvPrepare.GetDefaultEnv().GetCurrentTreeInfo(index);

            while (index < 128)
            {
                if (TreeInfos.Count != 0)
                {
                    var root = TreeInfos[index].MerkleRoot;
                    if (index > currentRound - 1 || TokenSwapRound == 0)
                    {
                        Logger.Info($"Add {index} round: {root}");
                        await tokenSwap.AddSwapRound(root, index);
                    }
                    TreeInfos[index].ReceiptCounts.ShouldBe(TreeInfos[index].Receipts.Count);
                    Logger.Info($"\nRound: {index}\nTree: {TreeInfos[index].TreeIndex}\nRoot: {root}" +
                                $"\nReceipt count:{TreeInfos[index].ReceiptCounts}");

                    var receiptInfos = TreeInfos[index].Receipts;
                    Logger.Info("Start swap token: ");
                    foreach (var receipt in receiptInfos)
                    {
                        Logger.Info($"\nReceipt id: {receipt.ReceiptId}");
                        await tokenSwap.SwapToken(receipt, index);
                    }

                    index = await tokenSwap.CheckTree();
                    if (index <= TreeInfos.Last().Key) continue;
                }

                TreeCount = TreeInfos.Count;
                while (TreeCount.Equals(TreeInfos.Count))
                {
                    Logger.Info("\nWaiting for new file...");
                    Thread.Sleep(60000);
                    TreeInfos = EnvPrepare.GetDefaultEnv().GetCurrentTreeInfo(index);
                }
            }

            Console.ReadLine();
        }

        public static Dictionary<long, TreeInfo> TreeInfos;
        public static int TreeCount;

        private static ILog Logger { get; set; }

        [Option("-c|--config", Description = "Config file about all nodes settings")]
        private static string ConfigFile { get; set; }

        [Option("-s|--swap", Description = "TokenSwap contract address")]
        private static string TokenSwapContract { get; set; }

        [Option("-p|--pairId", Description = "Swap Id")]
        private static string PairId { get; set; }

        [Option("-r|--swapRound", Description = "Swap Round")]
        private static int TokenSwapRound { get; set; }
    }
}