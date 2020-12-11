using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.BingoContract;
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

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class BingoGameContractTests
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }

        private TokenContract _tokenContract;
        private GenesisContract _genesisContract;
        private BingoGameContract _bingoContract;
        private BingoContractContainer.BingoContractStub _bingoGameContractStub;
        private string Symbol = "TEST";

        private string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        private string TestAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";

        private List<string> Tester = new List<string>();
        private static string RpcUrl { get; } = "192.168.197.44:8000";

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("ContractTest_");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env2-main");
            Tester = NodeOption.AllNodes.Select(l => l.Account).ToList();
            NodeManager = new NodeManager(RpcUrl);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
//            _bingoContract = new BingoGameContract(NodeManager, InitAccount);
//            Logger.Info($"Bingo contract : {_bingoContract}");
            _bingoContract = new BingoGameContract(NodeManager, InitAccount,
                "sr4zX6E7yVVL7HevExVcWv2ru3HSZakhsJMXfzxzfpnXofnZw");
            _bingoGameContractStub =
                _bingoContract.GetTestStub<BingoContractContainer.BingoContractStub>(InitAccount);
            CreateAndTransferToBingo();
        }

        [TestMethod]
        public async Task PlayManyTimes()
        {
            foreach (var tester in Tester)
            {
                var balance = _tokenContract.GetUserBalance(tester);
                if (balance < 100_00000000)
                    _tokenContract.TransferBalance(InitAccount, tester, 200_00000000);
                var amount = CommonHelper.GenerateRandomNumber(1_000, 10000_000);
                await Play(amount, tester);
                var getInfo = await _bingoGameContractStub.GetPlayerInformation.CallAsync(tester.ConvertAddress());
                var playIds = getInfo.Bouts.Select(p => p.PlayId).ToList();
                foreach (var playId in playIds)
                    Logger.Info($"{playId}");
            }
        }

        [TestMethod]
        public async Task GetInfo()
        {
            foreach (var tester in Tester)
            {
                var getInfo = await _bingoGameContractStub.GetPlayerInformation.CallAsync(tester.ConvertAddress());
                var playIds = getInfo.Bouts.Select(p => p.PlayId).ToList();
                Logger.Info($"{tester}: ");
                foreach (var playId in playIds)
                    Logger.Info($"{playId}");
            }
        }

        [TestMethod]
        public async Task OnePlayManyTimes()
        {
            var issue = _tokenContract.IssueBalance(InitAccount, TestAccount, 100000_000, Symbol);
            for (int i = 0; i < 5; i++)
            {
                var amount = CommonHelper.GenerateRandomNumber(1, 100_000);
                await Play(amount, TestAccount);
            }

            var getInfo =
                await _bingoGameContractStub.GetPlayerInformation.CallAsync(TestAccount.ConvertAddress());
            var playIds = getInfo.Bouts.Select(p => p.PlayId).ToList();
        }

        [TestMethod]
        public async Task ManyBingo()
        {
            foreach (var tester in Tester)
            {
                var getInfo = await _bingoGameContractStub.GetPlayerInformation.CallAsync(tester.ConvertAddress());
                var playIds = getInfo.Bouts.Select(p => p.PlayId).ToList();
                if (playIds.Count.Equals(0))
                    continue;
                var stub = _bingoContract.GetTestStub<BingoContractContainer.BingoContractStub>(tester);
                foreach (var playId in playIds)
                {
                    var result = await stub.Bingo.SendAsync(playId);
                    result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                    Logger.Info($"{tester}:  is {result.Output.IsWin}");
                }
            }
        }

        [TestMethod]
        public async Task Bingo()
        {
            var getInfo =
                await _bingoGameContractStub.GetPlayerInformation.CallAsync(InitAccount.ConvertAddress());
            var playIds = getInfo.Bouts.Select(p => p.PlayId).ToList();
            foreach (var playId in playIds)
            {
                var result = await _bingoGameContractStub.Bingo.SendAsync(playId);
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                Logger.Info($"{InitAccount}:  {playId} is {result.Output.IsWin}");
            }
        }

        [TestMethod]
        public async Task Play(long amount, string player)
        {
            _tokenContract.SetAccount(player);
            var balance = _tokenContract.GetUserBalance(player, Symbol);
            if (balance < amount)
                _tokenContract.IssueBalance(InitAccount, player, amount * 2);
            _tokenContract.ApproveToken(player, _bingoContract.ContractAddress, amount, Symbol);
            var allowance = _tokenContract.GetAllowance(player, _bingoContract.ContractAddress, Symbol);
            await Task.Delay(1000);
            var stub = _bingoContract.GetTestStub<BingoContractContainer.BingoContractStub>(player);
            var play = await stub.Play.SendAsync(new PlayInput
            {
                BuyAmount = amount,
                BuyType = 1,
                TokenSymbol = Symbol
            });
            play.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"{player}: Amount:{amount}; Result:{play.Output.Value}");
        }

        private void CreateAndTransferToBingo()
        {
            if (!_tokenContract.GetTokenInfo(Symbol).Equals(new TokenInfo()))
                return;
            var createInput = new CreateInput
            {
                Symbol = Symbol,
                Decimals = 3,
                IsBurnable = true,
                Issuer = InitAccount.ConvertAddress(),
                TokenName = "TEST",
                TotalSupply = long.MaxValue,
                LockWhiteList =
                {
                    _genesisContract.GetVoteContract().Contract,
                    _genesisContract.GetTreasuryContract().Contract
                }
            };
            var create = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create, createInput);
            create.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            foreach (var tester in Tester)
            {
                var issue = _tokenContract.IssueBalance(InitAccount, tester, 100000_000, Symbol);
                issue.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }

            var issueToContract =
                _tokenContract.IssueBalance(InitAccount, _bingoContract.ContractAddress, 1000000_000, Symbol);
            issueToContract.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
    }
}