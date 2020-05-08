using System;
using System.Collections.Generic;
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
        private string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        private string TestAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        private static string RpcUrl { get; } = "192.168.197.40:8000";
        private string Symbol { get; } = "LOT";

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("TokenSwap_");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env2-main");

            NodeManager = new NodeManager(RpcUrl);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
//            _tokenSwapContract = new TokenSwapContract(NodeManager, InitAccount);
//            Logger.Info($"TokenSwap contract : {_tokenSwapContract}");
            _tokenSwapContract = new TokenSwapContract(NodeManager, InitAccount,
                "2NxwCPAGJr4knVdmwhb1cK7CkZw5sMJkRDLnT7E2GoDP2dy5iZ");
            _tokenSwapContractStub =
                _tokenSwapContract.GetTestStub<TokenSwapContractContainer.TokenSwapContractStub>(InitAccount);
            if (!_tokenContract.GetTokenInfo(Symbol).Symbol.Equals(Symbol))
                CreateTokenAndIssue();
        }

        [TestMethod]
        public async Task CreateSwap()
        {
            var originTokenSizeInByte = 32;
            var swapRatio = new SwapRatio
            {
                OriginShare = 100_00000000,
                TargetShare = 1,
            };
            var depositAmount = 1000000_00000000;
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
                        SwapRatio = swapRatio,
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
                TargetTokenSymbol = Symbol
            });
            swapPair.CurrentRound.ShouldBeNull();
            swapPair.SwappedAmount.ShouldBe(0);
            swapPair.SwappedTimes.ShouldBe(0);
            swapPair.SwapRatio.ShouldBe(swapRatio);
            swapPair.TargetTokenSymbol.ShouldBe(Symbol);
            swapPair.OriginTokenSizeInByte.ShouldBe(originTokenSizeInByte);
        }

        [TestMethod]
        [DataRow("86e7854c42bfb50c7265268e29ba37900b013966e8a6a7055e38870edf18c71c")]
        public async Task AddSwapRound(string pairId)
        {
            var pId = HashHelper.HexStringToHash(pairId);
            var result = await _tokenSwapContractStub.AddSwapRound.SendAsync(new AddSwapRoundInput
            {
                SwapId = pId,
                MerkleTreeRoot =
                    HashHelper.HexStringToHash("bfb750a77743f285356d995ead571da5552abea0fe78555a2bb8d6202820b9e0")
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        [DataRow("86e7854c42bfb50c7265268e29ba37900b013966e8a6a7055e38870edf18c71c")]
        public async Task Deposit(string sPairId)
        {
            var depositAmount = 10000000_00000000;
            var pairId = HashHelper.HexStringToHash(sPairId);
            _tokenContract.ApproveToken(InitAccount, _tokenSwapContract.ContractAddress, depositAmount, "ELF");
            _tokenContract.ApproveToken(InitAccount, _tokenSwapContract.ContractAddress, depositAmount, Symbol);

            var beforeBalance = _tokenContract.GetUserBalance(_tokenSwapContract.ContractAddress, Symbol);
            var swapPairInfo = await _tokenSwapContractStub.GetSwapInfo.CallAsync(pairId);
            swapPairInfo.Controller.ShouldBe(InitAccount.ConvertAddress());
            var swapPair = await _tokenSwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                {SwapId = pairId, TargetTokenSymbol = Symbol});
            swapPair.DepositAmount.ShouldBe(beforeBalance);

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

            var afterBalance = _tokenContract.GetUserBalance(_tokenSwapContract.ContractAddress, Symbol);
            swapPair = await _tokenSwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                {SwapId = pairId, TargetTokenSymbol = Symbol});
            swapPair.DepositAmount.ShouldBe(afterBalance);
            afterBalance.ShouldBe(beforeBalance + depositAmount);
        }

        [TestMethod]
        [DataRow("86e7854c42bfb50c7265268e29ba37900b013966e8a6a7055e38870edf18c71c")]
        public async Task ChangeSwapRatio(string sPairId)
        {
            var pairId = HashHelper.HexStringToHash(sPairId);
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
        [DataRow("86e7854c42bfb50c7265268e29ba37900b013966e8a6a7055e38870edf18c71c",
            "0xaa796dee6b6abca795103a49a9715b499482dcf870f9237b2a7b03a3c93fd310")]
        public async Task SwapToken(string sPairId, string sUniqueId)
        {
            var originAmount = "6800000000000000000000";
            var pairId = HashHelper.HexStringToHash(sPairId);
            var uniqueId = HashHelper.HexStringToHash(sUniqueId);

            var receiveAccount = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
            var beforeBalance = _tokenContract.GetUserBalance(receiveAccount, Symbol);
            var beforeElfBalance = _tokenContract.GetUserBalance(receiveAccount, "ELF");

            var beforeSwapBalance = _tokenContract.GetUserBalance(_tokenSwapContract.ContractAddress, Symbol);
            var swapPair = await _tokenSwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                {SwapId = pairId, TargetTokenSymbol = Symbol});
            swapPair.DepositAmount.ShouldBe(beforeSwapBalance);

            var beforeSwapElfBalance = _tokenContract.GetUserBalance(_tokenSwapContract.ContractAddress, "ELF");
            var swapElfPair = await _tokenSwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                {SwapId = pairId, TargetTokenSymbol = "ELF"});
            swapElfPair.DepositAmount.ShouldBe(beforeSwapElfBalance);

            var stringInfo =
                "0x68be3add86eeebad8d18c953b56101d85ca6645d1e9f7af1eeef8c303983b1b6," +
                "0x45c8e3793f65016876fac123c33e3040e6d3accd2ff043af6d1b3cdc942b3098," +
                "0x64c0d8c3f3adfc9a86d5877edad2c996adfa2d2542ff112ae7211ca3061c48c6," +
                "0x5c6bebfdf003853b8106617869d2d70d8540295d4ae83e88b4dd0f00f184a8c7," +
                "0x1ef3dc51355ce778dd5fbf79f9fdfb4cad19a2c237b9269f6e66df554d2de08b," +
                "0x1abb68676945f2f3a5dfd04a4947dac2d96cd73b9725e16dc1f508cc61abd3cb," +
                "0x19a7b93a89015adaea08153a7fcea971f540dc8ae4f42d3f8ddb653c56a496d3";
            
            var isLeftInfo = "false,true,false,false,false,false,true," +
                             "false,false,false,false,false,false,false," +
                             "false,false,false,false,false,false,false," +
                             "false,false,false,false,false,false,false," +
                             "false,false";
            
            var hashList = stringInfo.Split(",").ToList();
            var boolList = isLeftInfo.Split(",").Take(hashList.Count).ToList();
            var merklePathNodes = new List<MerklePathNode>();
            for (int i = 0; i < hashList.Count; i++ )
            {
                var merkle = new MerklePathNode();
                merkle.Hash =  HashHelper.HexStringToHash(hashList[i]);
                merkle.IsLeftChildNode = Boolean.Parse(boolList[i]);
                merklePathNodes.Add(merkle);
            }

            var merklePath = new MerklePath
            {
                MerklePathNodes = { merklePathNodes }
            };
            var result = await _tokenSwapContractStub.SwapToken.SendAsync(new SwapTokenInput
            {
                SwapId = pairId,
                OriginAmount = originAmount,
                UniqueId = uniqueId,
                ReceiverAddress = receiveAccount.ConvertAddress(),
                MerklePath = merklePath
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var tokenTransferredEvent = result.TransactionResult.Logs
                .First(l => l.Name == nameof(Transferred));
            var nonIndexed = Transferred.Parser.ParseFrom(tokenTransferredEvent.NonIndexed);
            var expectedAmount = 680000000000;
            nonIndexed.Amount.ShouldBe(expectedAmount);
            
            var balance = _tokenContract.GetUserBalance(receiveAccount, Symbol);
            var elfBalance = _tokenContract.GetUserBalance(receiveAccount, "ELF");
            elfBalance.ShouldBe(beforeElfBalance + expectedAmount);
            balance.ShouldBe(beforeBalance + expectedAmount);

            var afterSwapBalance = _tokenContract.GetUserBalance(_tokenSwapContract.ContractAddress, Symbol);
            swapPair = await _tokenSwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                {SwapId = pairId, TargetTokenSymbol = Symbol});
            swapPair.DepositAmount.ShouldBe(afterSwapBalance);
            swapPair.DepositAmount.ShouldBe(beforeSwapBalance - expectedAmount);

            var afterSwapElfBalance = _tokenContract.GetUserBalance(_tokenSwapContract.ContractAddress, "ELF");
            swapElfPair = await _tokenSwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                {SwapId = pairId, TargetTokenSymbol = "ELF"});
            swapElfPair.DepositAmount.ShouldBe(afterSwapElfBalance);
            swapElfPair.DepositAmount.ShouldBe(beforeSwapElfBalance - expectedAmount);
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
        [DataRow("86e7854c42bfb50c7265268e29ba37900b013966e8a6a7055e38870edf18c71c")]
        public async Task GetSwapInfo(string sPairId)
        {
            var pairId = HashHelper.HexStringToHash(sPairId);
            var swapPairInfo = await _tokenSwapContractStub.GetSwapInfo.CallAsync(pairId);
            var swapPair = await _tokenSwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                {SwapId = pairId, TargetTokenSymbol = Symbol});
            var swapRound = await _tokenSwapContractStub.GetCurrentSwapRound.CallAsync(new GetCurrentSwapRoundInput
                {SwapId = pairId, TargetTokenSymbol = Symbol});
            var elfSwapPair = await _tokenSwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                {SwapId = pairId, TargetTokenSymbol = "ELF"});
            var elfSwapRound = await _tokenSwapContractStub.GetCurrentSwapRound.CallAsync(new GetCurrentSwapRoundInput
                {SwapId = pairId, TargetTokenSymbol = "ELF"});
            swapPairInfo.Controller.ShouldBe(InitAccount.ConvertAddress());
            swapPair.SwapRatio.OriginShare.ShouldBe(100_00000000);
            swapPair.CurrentRound.ShouldBe(elfSwapPair.CurrentRound);
            swapPair.SwapId.ShouldBe(elfSwapPair.SwapId);
            swapPair.OriginTokenNumericBigEndian.ShouldBe(elfSwapPair.OriginTokenNumericBigEndian);
            swapPair.SwappedTimes.ShouldBe(elfSwapPair.SwappedTimes);

            swapRound.StartTime.ShouldBe(elfSwapRound.StartTime);
            swapRound.SwappedTimes.ShouldBe(elfSwapRound.SwappedTimes);
            swapRound.MerkleTreeRoot.ShouldBe(elfSwapRound.MerkleTreeRoot);

            Logger.Info($"All the amount is {swapPair.SwappedAmount}");
            Logger.Info($"times is {swapPair.SwappedTimes}");
            Logger.Info($"Current amount is {swapRound.SwappedAmount}");
            Logger.Info($"Current times is {swapRound.SwappedTimes}");
            Logger.Info($"Merkle root is {swapRound.MerkleTreeRoot}");
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
                IsProfitable = true,
                TokenName = "LOT"
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _tokenContract.IssueBalance(InitAccount, InitAccount, 5_00000000_00000000, Symbol);
        }
    }
}