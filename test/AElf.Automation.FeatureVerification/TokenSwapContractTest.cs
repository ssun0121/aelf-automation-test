using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Shouldly;
using Tokenswap;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class TokenSwapContractTest
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }

        private TokenContract _tokenContract;
        private GenesisContract _genesisContract;
        private TokenSwapContract _tokenSwapContract;

        private TokenSwapContractContainer.TokenSwapContractStub _tokenSwapContractStub;

//        private string InitAccount { get; } = "sCdEBrmnc1uCxbyeHWK9n7Y6CxfWxwDK1Bs43PUY3BYUFJQ5M";
        private string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";

        private string TestAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";

//        private static string RpcUrl { get; } = "13.211.28.67:8000";
        private static string RpcUrl { get; } = "192.168.197.44:8001";
        private string Symbol { get; } = "LOT";

        private string[] targetAddress;

//        private string PairId { get; } = "2b3974ae54e76015fb5f33304eeb8224dde028e7f115f2037e5d0dda5a4b4070";
        private string PairId { get; } = "2f9b59be3eec1e4494f7321099f0892b718ca4f5257a6d3af9dd124afcb867c4";

        //1471937c1b0db1ea952b4f37392003b8edacf6c57b4f6bd3b9403c6ac3514dfa -195
        //ac3ed0dac2986b4ffadb0e96648329c8aed7fadb33b4a55e4068a17fea2c51d7


        public class TreeInfos
        {
            [JsonProperty("treeInfos")] public List<TreeInfo> Trees { get; set; }
        }

        public class TreeInfo
        {
            [JsonProperty("index")] public int index { get; set; }
            [JsonProperty("root")] public string root { get; set; }
        }

        public class SwapInfo
        {
            private TreeInfos _instance;
            private string _jsonContent;
            private readonly object _lockObj = new object();

            public TreeInfos TreeInfo => GetInfo();

            private TreeInfos GetInfo()
            {
                lock (_lockObj)
                {
                    try
                    {
                        var localPath = CommonHelper.GetDefaultDataDir();
                        var config = Path.Combine(localPath, $@"tokenSwapTest/TreeInfos.json");
                        _jsonContent = File.ReadAllText(config);
                    }
                    catch (FileNotFoundException e)
                    {
                        Console.WriteLine($"Could not find file");
                        return null;
                    }

                    _instance = JsonConvert.DeserializeObject<TreeInfos>(_jsonContent);
                }

                return _instance;
            }
        }


        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("TokenSwap_");
            Logger = Log4NetHelper.GetLogger();
//            NodeInfoHelper.SetConfig("nodes-online-stage-main");
            NodeInfoHelper.SetConfig("nodes-env2-side1");
            NodeManager = new NodeManager(RpcUrl);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
//            _tokenSwapContract = new TokenSwapContract(NodeManager, InitAccount);
//            Logger.Info($"TokenSwap contract : {_tokenSwapContract}");

//            _tokenSwapContract = new TokenSwapContract(NodeManager, InitAccount,
//                "2onFLTnPEiZrXGomzJ8g74cBre2cJuHrn1yBJF3P6Xu9K5Gbth");

            _tokenSwapContract = new TokenSwapContract(NodeManager, InitAccount,
                "RXcxgSXuagn8RrvhQAV81Z652EEYSwR6JLnqHYJ5UVpEptW8Y");
            _tokenSwapContractStub =
                _tokenSwapContract.GetTestStub<TokenSwapContractContainer.TokenSwapContractStub>(InitAccount);
//            _tokenContract.TransferBalance(InitAccount, TestAccount, 1000000000000);
//            if (!_tokenContract.GetTokenInfo(Symbol).Symbol.Equals(Symbol))
//                CreateTokenAndIssue();
            targetAddress = new string[]
            {
                "2oSMWm1tjRqVdfmrdL8dgrRvhWu1FP8wcZidjS6wPbuoVtxhEz",
                "WRy3ADLZ4bEQTn86ENi5GXi5J1YyHp9e99pPso84v2NJkfn5k",
                "2frDVeV6VxUozNqcFbgoxruyqCRAuSyXyfCaov6bYWc7Gkxkh2",
                "2ZYyxEH6j8zAyJjef6Spa99Jx2zf5GbFktyAQEBPWLCvuSAn8D",
                "eFU9Quc8BsztYpEHKzbNtUpu9hGKgwGD2tyL13MqtFkbnAoCZ",
                "2V2UjHQGH8WT4TWnzebxnzo9uVboo67ZFbLjzJNTLrervAxnws",
                "EKRtNn3WGvFSTDewFH81S7TisUzs9wPyP4gCwTww32waYWtLB",
                "2LA8PSHTw4uub71jmS52WjydrMez4fGvDmBriWuDmNpZquwkNx",
                "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6",
                "YF8o6ytMB7n5VF9d1RDioDXqyQ9EQjkFK3AwLPCH2b9LxdTEq",
                "h6CRCFAhyozJPwdFRd7i8A5zVAqy171AVty3uMQUQp1MB9AKa",
                "28qLVdGMokanMAp9GwfEqiWnzzNifh8LS9as6mzJFX1gQBB823",
                "2Dyh4ASm6z7CaJ1J1WyvMPe2sJx5TMBW8CMTKeVoTMJ3ugQi3P",
                "2G4L1S7KPfRscRP6zmd7AdVwtptVD3vR8YoF1ZHgPotDNbZnNY",
                "W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo",
                "2REajHMeW2DMrTdQWn89RQ26KQPRg91coCEtPP42EC9Cj7sZ61",
                "2a6MGBRVLPsy6pu4SVMWdQqHS5wvmkZv8oas9srGWHJk7GSJPV",
                "2cv45MBBUHjZqHva2JMfrGWiByyScNbEBjgwKoudWQzp6vX8QX",
                "7BSmhiLtVqHSUVGuYdYbsfaZUGpkL2ingvCmVPx66UR5L5Lbs"
            };
        }

        [TestMethod]
        public void Transfer()
        {
            foreach (var receiver in targetAddress)
            {
                if (receiver.Equals(InitAccount)) continue;
                var elfBalance = _tokenContract.GetUserBalance(receiver);
                var balance = _tokenContract.GetUserBalance(receiver, Symbol);
                Logger.Info(
                    $"Check the balance of receiver account {receiver}, ELF balance is {elfBalance}, {Symbol} balance is {balance}");

                if (elfBalance <= 10000_00000000) continue;
                _tokenContract.SetAccount(receiver);
                _tokenContract.TransferBalance(receiver, InitAccount, elfBalance - 10000_00000000);
                if (balance <= 10000_00000000) continue;
                _tokenContract.SetAccount(receiver);
                _tokenContract.TransferBalance(receiver, InitAccount, balance - 10000_00000000, Symbol);
            }

            var initBalance = _tokenContract.GetUserBalance(InitAccount);
            var initSymbolBalance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            Logger.Info(
                $"Balance of init account {InitAccount}, ELF balance is {initBalance}, {Symbol} balance is {initSymbolBalance}");
        }

        [TestMethod]
        public async Task CreateSwap()
        {
            var originTokenSizeInByte = 32;
            var elfSwapRatio = new SwapRatio
            {
                OriginShare = 400_00_00000000,
                TargetShare = 1,
            };
            var swapRatio = new SwapRatio
            {
                OriginShare = 100_00000000,
                TargetShare = 1
            };
            var depositAmount = 100000_00000000;
            _tokenContract.ApproveToken(InitAccount, _tokenSwapContract.ContractAddress, depositAmount, Symbol);
            _tokenContract.ApproveToken(InitAccount, _tokenSwapContract.ContractAddress, depositAmount, "ELF");

            var result = await _tokenSwapContractStub.CreateSwap.SendAsync(new CreateSwapInput
            {
                OriginTokenSizeInByte = originTokenSizeInByte,
                OriginTokenNumericBigEndian = true,
                SwapTargetTokenList =
                {
                    new SwapTargetToken
                    {
                        DepositAmount = depositAmount,
                        SwapRatio = elfSwapRatio,
                        TargetTokenSymbol = "ELF"
                    },
                    new SwapTargetToken
                    {
                        DepositAmount = depositAmount,
                        SwapRatio = swapRatio,
                        TargetTokenSymbol = Symbol
                    }
                }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var pairId = result.Output;
            var swapid = SwapPairAdded.Parser
                .ParseFrom(result.TransactionResult.Logs.First(l => l.Name.Contains(nameof(SwapPairAdded))).NonIndexed)
                .SwapId;
            Logger.Info($"{pairId}");
            pairId.ShouldBe(swapid);
            var swapPair = await _tokenSwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
            {
                SwapId = pairId,
                TargetTokenSymbol = "ELF"
            });
            swapPair.RoundCount.ShouldBe(0);
            swapPair.SwappedAmount.ShouldBe(0);
            swapPair.SwappedTimes.ShouldBe(0);
            swapPair.SwapRatio.ShouldBe(elfSwapRatio);
            swapPair.TargetTokenSymbol.ShouldBe("ELF");
            swapPair.OriginTokenSizeInByte.ShouldBe(originTokenSizeInByte);
        }

        [TestMethod]
        public async Task AddManyRound()
        {
            var swapInfo = new SwapInfo();
            var treeInfo = swapInfo.TreeInfo;
            foreach (var tree in treeInfo.Trees)
            {
                await AddSwapRound(tree.root, tree.index);
            }
        }

        [TestMethod]
        [DataRow("0x72fd2306d145cbdcc7a295ee2cf36be8da6cb6e3532123a360f994be14abd76d", 0)]
        public async Task AddSwapRound(string root, int id)
        {
            var pId = Hash.LoadFromHex(PairId);
            var result = await _tokenSwapContractStub.CreateSwapRound.SendAsync(new CreateSwapRoundInput()
            {
                SwapId = pId,
                MerkleTreeRoot =
                    Hash.LoadFromHex(root),
                RoundId = id
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public async Task Deposit()
        {
            var depositAmount = 100_00000000;
            var pairId = Hash.LoadFromHex(PairId);
            _tokenContract.ApproveToken(InitAccount, _tokenSwapContract.ContractAddress, depositAmount, "ELF");
            _tokenContract.ApproveToken(InitAccount, _tokenSwapContract.ContractAddress, depositAmount, Symbol);

            var swapPairInfo = await _tokenSwapContractStub.GetSwapInfo.CallAsync(pairId);
            swapPairInfo.Controller.ShouldBe(InitAccount.ConvertAddress());
            var swapPair = await _tokenSwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                {SwapId = pairId, TargetTokenSymbol = Symbol});

            var result = await _tokenSwapContractStub.Deposit.SendAsync(new DepositInput
            {
                SwapId = pairId,
                TargetTokenSymbol = Symbol,
                Amount = depositAmount
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var elfResult = await _tokenSwapContractStub.Deposit.SendAsync(new DepositInput
            {
                SwapId = pairId,
                TargetTokenSymbol = "ELF",
                Amount = depositAmount
            });
            elfResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var afterSwapPair = await _tokenSwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                {SwapId = pairId, TargetTokenSymbol = Symbol});
            afterSwapPair.DepositAmount.ShouldBe(swapPair.DepositAmount + depositAmount);
        }

        [TestMethod]
        public async Task Withdraw()
        {
            var pairId = Hash.LoadFromHex(PairId);

            var swapPairInfo = await _tokenSwapContractStub.GetSwapInfo.CallAsync(pairId);
            swapPairInfo.Controller.ShouldBe(InitAccount.ConvertAddress());
            var swapPair = await _tokenSwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                {SwapId = pairId, TargetTokenSymbol = Symbol});

            var controllerBalance = _tokenContract.GetUserBalance(swapPairInfo.Controller.ToBase58(),Symbol);
            var result = await _tokenSwapContractStub.Withdraw.SendAsync(new WithdrawInput
            {
                SwapId = pairId,
                Amount = swapPair.DepositAmount,
                TargetTokenSymbol = Symbol
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var afterSwapPair = await _tokenSwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                {SwapId = pairId, TargetTokenSymbol = Symbol});
            afterSwapPair.DepositAmount.ShouldBe(0);

            var afterControllerBalance = _tokenContract.GetUserBalance(swapPairInfo.Controller.ToBase58(), Symbol);
            afterControllerBalance.ShouldBe(controllerBalance + swapPair.DepositAmount);
        }


        [TestMethod]
        public async Task ChangeSwapRatio()
        {
            var pairId = Hash.LoadFromHex(PairId);
            var result = await _tokenSwapContractStub.ChangeSwapRatio.SendAsync(new ChangeSwapRatioInput
            {
                SwapId = pairId,
                TargetTokenSymbol = Symbol,
                SwapRatio = new SwapRatio
                {
                    OriginShare = 100_0000000,
                    TargetShare = 1,
                }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }


        [TestMethod]
        [DataRow("0xf22e7c3926caee87e086e23ddaeecb282b2c702c9b38e0e24c844df3fd67ea49")]
        public async Task SwapToken(string sUniqueId)
        {
            var originAmount = "491976960390152585216";
            var pairId = Hash.LoadFromHex(PairId);
            var uniqueId = Hash.LoadFromHex(sUniqueId);
            var receiveAccount = "2ZYyxEH6j8zAyJjef6Spa99Jx2zf5GbFktyAQEBPWLCvuSAn8D";

            var userBalanceMap = new Dictionary<string, long>();
            var swapBalanceMap = new Dictionary<string, long>();
            var swapInfo = await _tokenSwapContractStub.GetSwapInfo.CallAsync(pairId);
            foreach (var map in swapInfo.SwapTargetTokenMap)
            {
                var balance = _tokenContract.GetUserBalance(receiveAccount, map.Key);
                userBalanceMap.Add(map.Key, balance);
                var swapBalance = await _tokenSwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                    {SwapId = pairId, TargetTokenSymbol = map.Key});
                swapBalanceMap.Add(map.Key, swapBalance.DepositAmount);
            }

            var stringInfo = "0x73cd1c1253ddf1e0149a1d3398ef0bf57471b3ef6017169476d26f651438b245";
            var isLeftInfo = "false";

            var hashList = stringInfo.Split(",").ToList();
            var boolList = isLeftInfo.Split(",").Take(hashList.Count).ToList();
            var merklePathNodes = new List<MerklePathNode>();
            for (int i = 0; i < hashList.Count; i++)
            {
                var merkle = new MerklePathNode();
                merkle.Hash = Hash.LoadFromHex(hashList[i]);
                merkle.IsLeftChildNode = Boolean.Parse(boolList[i]);
                merklePathNodes.Add(merkle);
            }

            var merklePath = new MerklePath
            {
                MerklePathNodes = {merklePathNodes}
            };
            var stub = _tokenSwapContract.GetTestStub<TokenSwapContractContainer.TokenSwapContractStub>(receiveAccount);
            var result = await stub.SwapToken.SendAsync(new SwapTokenInput
            {
                SwapId = pairId,
                OriginAmount = originAmount,
                UniqueId = uniqueId,
                ReceiverAddress = receiveAccount.ConvertAddress(),
                MerklePath = merklePath,
                RoundId = 1
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var tokenTransferredEvent = result.TransactionResult.Logs
                .Where(l => l.Name == nameof(Transferred)).ToList();

            var expectedAmountMap = new Dictionary<string, long>();
            foreach (var eEvent in tokenTransferredEvent)
            {
                var nonIndexed = Transferred.Parser.ParseFrom(eEvent.NonIndexed);
                var expectedAmount = long.Parse(originAmount.Substring(0, originAmount.Length - 10));
                var expectedElfAmount = expectedAmount / 400;
                if (nonIndexed.Amount == expectedAmount)
                {
                    var symbol = Symbol;
                    expectedAmountMap.Add(symbol, expectedAmount);
                }
                else
                {
                    var symbol = "ELF";
                    expectedAmountMap.Add(symbol, expectedElfAmount);
                }
            }

            foreach (var map in swapInfo.SwapTargetTokenMap)
            {
                var balance = _tokenContract.GetUserBalance(receiveAccount, map.Key);
                balance.ShouldBe(userBalanceMap[map.Key] + expectedAmountMap[map.Key]);
                var swapBalance = await _tokenSwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                    {SwapId = pairId, TargetTokenSymbol = map.Key});
                swapBalance.DepositAmount.ShouldBe(swapBalanceMap[map.Key] - expectedAmountMap[map.Key]);
            }

            //check
            var checkAmount = await stub.GetSwapAmounts.CallAsync(new GetSwapAmountsInput
            {
                SwapId = pairId,
                UniqueId = uniqueId
            });
            checkAmount.Receiver.ShouldBe(receiveAccount.ConvertAddress());
            checkAmount.ReceivedAmounts[Symbol].ShouldBe(expectedAmountMap[Symbol]);
            checkAmount.ReceivedAmounts["ELF"].ShouldBe(expectedAmountMap["ELF"]);
        }

        [TestMethod]
        public async Task CheckAmount_noSwap()
        {
            var uniqueId = Hash.LoadFromHex("0xe38990d0c7fc009880a9c07c23842e886c6bbdc964ce6bdd5817ad357335ee6f");
            var pairId = Hash.LoadFromHex(PairId);
            var checkAmount = await _tokenSwapContractStub.GetSwapAmounts.CallAsync(new GetSwapAmountsInput
            {
                SwapId = pairId,
                UniqueId = uniqueId
            });
            checkAmount.ShouldBe(new SwapAmounts());
        }

        [TestMethod]
        public void CheckBalance()
        {
            var balance = _tokenContract.GetUserBalance(TestAccount, Symbol);
            var elfBalance = _tokenContract.GetUserBalance(TestAccount, "ELF");
            Logger.Info($"ELF {elfBalance}; {Symbol} {balance}");

            var swapBalance = _tokenContract.GetUserBalance(_tokenSwapContract.ContractAddress, Symbol);
            var swapElfBalance = _tokenContract.GetUserBalance(_tokenSwapContract.ContractAddress, "ELF");
            Logger.Info($"ELF {swapElfBalance}; {Symbol} {swapBalance}");
        }

        [TestMethod]
        public async Task GetSwapInfo()
        {
            var pairId = Hash.LoadFromHex(PairId);
            var swapPairInfo = await _tokenSwapContractStub.GetSwapInfo.CallAsync(pairId);
            var swapPair = await _tokenSwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                {SwapId = pairId, TargetTokenSymbol = Symbol});
            var swapRound = await _tokenSwapContractStub.GetSwapRound.CallAsync(new GetSwapRoundInput
                {SwapId = pairId, TargetTokenSymbol = Symbol, RoundId = 3});
            var elfSwapPair = await _tokenSwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                {SwapId = pairId, TargetTokenSymbol = "ELF"});
            var elfSwapRound = await _tokenSwapContractStub.GetSwapRound.CallAsync(new GetSwapRoundInput
                {SwapId = pairId, TargetTokenSymbol = "ELF", RoundId = 3});
            swapPairInfo.Controller.ShouldBe(InitAccount.ConvertAddress());
            swapPair.RoundCount.ShouldBe(elfSwapPair.RoundCount);
            swapPair.SwapId.ShouldBe(elfSwapPair.SwapId);
            swapPair.OriginTokenNumericBigEndian.ShouldBe(elfSwapPair.OriginTokenNumericBigEndian);
            swapPair.SwappedTimes.ShouldBe(elfSwapPair.SwappedTimes);
            swapRound.StartTime.ShouldBe(elfSwapRound.StartTime);
            swapRound.SwappedTimes.ShouldBe(elfSwapRound.SwappedTimes);
            swapRound.MerkleTreeRoot.ShouldBe(elfSwapRound.MerkleTreeRoot);

            Logger.Info($"{swapPair.DepositAmount}");
            Logger.Info($"{elfSwapPair.DepositAmount}");
            Logger.Info($"{elfSwapPair.RoundCount}");
            Logger.Info($"All the amount is {elfSwapPair.SwappedAmount}");
            Logger.Info($"times is {elfSwapPair.SwappedTimes}");
            Logger.Info($"Current amount is {elfSwapPair.SwappedAmount}");
            Logger.Info($"Current times is {elfSwapPair.SwappedTimes}");
            Logger.Info($"Merkle root is {elfSwapRound.MerkleTreeRoot}");
        }

        [TestMethod]
        public void CheckTree()
        {
            var merkleNodes = "";
            var nodeList = merkleNodes.Split(",").ToList();
            if (nodeList.Count % 2 == 1)
                nodeList.Add(nodeList.Last());
            var leftNods = new List<Hash>();
            foreach (var n in nodeList)
            {
                var node = Hash.LoadFromHex(n);
                leftNods.Add(node);
            }

            var nodeToAdd = leftNods.Count / 2;
            var newAdded = 0;
            var i = 0;
            while (i < leftNods.Count - 1)
            {
                var left = leftNods[i++];
                var right = leftNods[i++];
                leftNods.Add(HashHelper.ConcatAndCompute(left, right));
                if (++newAdded != nodeToAdd)
                    continue;

                // complete this row
                if (nodeToAdd % 2 == 1 && nodeToAdd != 1)
                {
                    nodeToAdd++;
                    leftNods.Add(leftNods.Last());
                }

                // start a new row
                nodeToAdd /= 2;
                newAdded = 0;
            }

            leftNods.Last()
                .ShouldBe(Hash.LoadFromHex("0x7bc1c986d4d278ac315c7f0d05939c0e11ae915497f062b754d18fa0986bcf6e"));
            foreach (var node in leftNods)
            {
                Logger.Info(node.ToHex());
            }
        }

        private void CreateTokenAndIssue()
        {
            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
            {
                Symbol = Symbol,
                TotalSupply = 10_00000000_00000000,
                Decimals = 8,
                Issuer = InitAccount.ConvertAddress(),
                IsBurnable = true,
                TokenName = "LOT"
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _tokenContract.IssueBalance(InitAccount, InitAccount, 5_00000000_00000000, Symbol);
        }
    }
}