using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Contracts.LotteryContract;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Volo.Abp.Threading;
using InitializeInput = AElf.Contracts.LotteryContract.InitializeInput;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class LotteryDemoContractTest
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }

        private TokenContract _tokenContract;
        private GenesisContract _genesisContract;
        private LotteryContract _lotteryDemoContract;
        private LotteryContractContainer.LotteryContractStub _lotteryDemoStub;
        private LotteryContractContainer.LotteryContractStub _adminLotteryDemoStub;

        private string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        private string TestAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        private List<string> Tester = new List<string>();
        private static string RpcUrl { get; } = "192.168.199.205:8000";
        private string Symbol { get; } = "LOT";
        private const long Price = 1000_00000000;

        [TestInitialize]
        public void Initialize()
        {
            //2WHXRoLRjbUTDQsuqR5CntygVfnDb125qdJkudev4kVNbLhTdG
            Log4NetHelper.LogInit("ContractTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env205-main");
            Tester = NodeOption.AllNodes.Select(l => l.Account).ToList();
            NodeManager = new NodeManager(RpcUrl);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
            _lotteryDemoContract = new LotteryContract(NodeManager, InitAccount);
            Logger.Info($"Lottery contract : {_lotteryDemoContract}");
//            if (!_tokenContract.GetTokenInfo(Symbol).Symbol.Equals(Symbol))
//                CreateTokenAndIssue();
//            _lotteryDemoContract = new LotteryContract(NodeManager, InitAccount,
//                "2NxwCPAGJr4knVdmwhb1cK7CkZw5sMJkRDLnT7E2GoDP2dy5iZ");
            InitializeLotteryDemoContract();

            _adminLotteryDemoStub =
                _lotteryDemoContract.GetTestStub<LotteryContractContainer.LotteryContractStub>(InitAccount);
            _lotteryDemoStub =
                _lotteryDemoContract.GetTestStub<LotteryContractContainer.LotteryContractStub>(TestAccount);
//            _tokenContract.TransferBalance(InitAccount, TestAccount, 1000_00000000, "ELF");
//            foreach (var tester in Tester)
//            {
//                var balance = _tokenContract.GetUserBalance(tester, "ELF");
//                if (balance < 100_00000000)
//                {
//                    _tokenContract.TransferBalance(InitAccount, tester, 1000_00000000, "ELF");
//                }
//            }
        }

        [TestMethod]
        public void Buy()
        {
            foreach (var tester in Tester)
            {
                var amount = CommonHelper.GenerateRandomNumber(1, 5);
                var balance = _tokenContract.GetUserBalance(_lotteryDemoContract.ContractAddress, Symbol);
                var userBeforeBalance = _tokenContract.GetUserBalance(tester, Symbol);
                if (userBeforeBalance < 10000_00000000)
                    _tokenContract.TransferBalance(InitAccount, tester, 20000_00000000, Symbol);
                userBeforeBalance = _tokenContract.GetUserBalance(tester, Symbol);

                _tokenContract.SetAccount(tester);
                var approveResult = _tokenContract.ApproveToken(tester, _lotteryDemoContract.ContractAddress,
                    amount * Price,
                    Symbol);
                approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var allowance = _tokenContract.GetAllowance(tester, _lotteryDemoContract.ContractAddress, Symbol);
                allowance.ShouldBeGreaterThanOrEqualTo(amount * Price);
                _lotteryDemoContract.SetAccount(tester);
                var result = _lotteryDemoContract.ExecuteMethodWithResult(LotteryMethod.Buy, new Int64Value
                {
                    Value = amount
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var resultValue =
                    BoughtLotteriesInformation.Parser.ParseFrom(
                        ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
                resultValue.Amount.ShouldBe(amount);
                Logger.Info($"start id is: {resultValue.StartId}");
                var userBalance = _tokenContract.GetUserBalance(tester, Symbol);
                userBalance.ShouldBe(userBeforeBalance - amount * Price);
                var contractBalance = _tokenContract.GetUserBalance(_lotteryDemoContract.ContractAddress, Symbol);
                contractBalance.ShouldBe(balance + amount * Price);
            }
        }

        [TestMethod]
        public void Buy_Once()
        {
//            var amount = CommonHelper.GenerateRandomNumber(1, 10);
            var amount = 30;
            var balance = _tokenContract.GetUserBalance(_lotteryDemoContract.ContractAddress, Symbol);
            var userBeforeBalance = _tokenContract.GetUserBalance(TestAccount, Symbol);
//                if(userBeforeBalance < 10000_00000000)
//                    _tokenContract.TransferBalance(InitAccount, TestAccount, 10000_00000000, Symbol);
//                userBeforeBalance = _tokenContract.GetUserBalance(TestAccount, Symbol);

            _tokenContract.SetAccount(TestAccount);
            var approveResult = _tokenContract.ApproveToken(TestAccount, _lotteryDemoContract.ContractAddress,
                amount * Price,
                Symbol);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var allowance = _tokenContract.GetAllowance(TestAccount, _lotteryDemoContract.ContractAddress, Symbol);
            allowance.ShouldBeGreaterThanOrEqualTo(amount * Price);

            _lotteryDemoContract.SetAccount(TestAccount);
            var result = _lotteryDemoContract.ExecuteMethodWithResult(LotteryMethod.Buy, new Int64Value
            {
                Value = amount
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var resultValue =
                BoughtLotteriesInformation.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            resultValue.Amount.ShouldBe(amount);
            Logger.Info($"start id is: {resultValue.StartId}");
            var userBalance = _tokenContract.GetUserBalance(TestAccount, Symbol);
            userBalance.ShouldBe(userBeforeBalance - amount * Price);
            var contractBalance = _tokenContract.GetUserBalance(_lotteryDemoContract.ContractAddress, Symbol);
            contractBalance.ShouldBe(balance + amount * Price);
        }

        [TestMethod]
        public void PrepareDraw()
        {
            _lotteryDemoContract.SetAccount(InitAccount);
            var result = _lotteryDemoContract.ExecuteMethodWithResult(LotteryMethod.PrepareDraw, new Empty());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void Draw()
        {
            _lotteryDemoContract.SetAccount(InitAccount);
            var result = _lotteryDemoContract.ExecuteMethodWithResult(LotteryMethod.PrepareDraw, new Empty());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var block = result.BlockNumber;
            var currentBlock = AsyncHelper.RunSync(() => NodeManager.ApiClient.GetBlockHeightAsync());
            while (currentBlock < block + 80)
            {
                Thread.Sleep(5000);
                Logger.Info("Waiting block");
                currentBlock = AsyncHelper.RunSync(() => NodeManager.ApiClient.GetBlockHeightAsync());
            }

            _lotteryDemoContract.SetAccount(InitAccount);
            var drawResult = _lotteryDemoContract.ExecuteMethodWithResult(LotteryMethod.Draw, new DrawInput
            {
                LevelsCount = {1}
            });
            drawResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public async Task TakeReward()
        {
            var rewardResult = await _adminLotteryDemoStub.GetRewardResult.CallAsync(new Int64Value {Value = 3});
            var lotteries = rewardResult.RewardLotteries.Where(l => l.Level.Equals(1)).ToList();
            foreach (var lottery in lotteries)
            {
//                lottery.RegistrationInformation.ShouldBe("");
                _lotteryDemoContract.SetAccount(lottery.Owner.ToBase58());
                var result = _lotteryDemoContract.ExecuteMethodWithResult(LotteryMethod.TakeReward, new TakeRewardInput
                {
                    RegistrationInformation = "中奖啦",
                    LotteryId = lottery.Id
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }

            rewardResult = await _adminLotteryDemoStub.GetRewardResult.CallAsync(new Int64Value {Value = 3});
            lotteries = rewardResult.RewardLotteries.Where(l => l.Level.Equals(1)).ToList();
            foreach (var lottery in lotteries)
            {
                lottery.RegistrationInformation.ShouldBe("中奖啦");
            }
        }

        [TestMethod]
        public async Task TakeReward_Failed()
        {
            var rewardResult = await _lotteryDemoStub.GetRewardResult.CallAsync(new Int64Value {Value = 1});
            var lotteries = rewardResult.RewardLotteries.Where(l => l.Level.Equals(1)).ToList();
            foreach (var lottery in lotteries)
            {
                lottery.RegistrationInformation.ShouldBe("");
                var errorAddress = Tester.Where(a => !a.Equals(lottery.Owner.ToBase58())).ToList().First();
                _lotteryDemoContract.SetAccount(errorAddress);
                var result = _lotteryDemoContract.ExecuteMethodWithResult(LotteryMethod.TakeReward, new TakeRewardInput
                {
                    RegistrationInformation = "中奖啦",
                    LotteryId = lottery.Id
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
            }
        }

        [TestMethod]
        public async Task ResetMaximumBuyAmount()
        {
            var result = await _adminLotteryDemoStub.ResetMaximumBuyAmount.SendAsync(new Int64Value {Value = 100});
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var amount = await _adminLotteryDemoStub.GetMaximumBuyAmount.CallAsync(new Empty());
            amount.Value.ShouldBe(100);
        }

        [TestMethod]
        public async Task ResetPrice()
        {
            var price = await _lotteryDemoStub.GetPrice.CallAsync(new Empty());
            price.Value.ShouldBe(Price);
            var result = await _adminLotteryDemoStub.ResetPrice.SendAsync(new Int64Value {Value = 1000_00000000});
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var newPrice = await _lotteryDemoStub.GetPrice.CallAsync(new Empty());
            newPrice.Value.ShouldBe(1000_00000000);
        }

        [TestMethod]
        public async Task ResetDrawingLag()
        {
            var result = await _adminLotteryDemoStub.ResetDrawingLag.SendAsync(new Int64Value {Value = 100});
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var drawLag = await _adminLotteryDemoStub.GetDrawingLag.CallAsync(new Empty());
            drawLag.Value.ShouldBe(100);
        }

        [TestMethod]
        public async Task GetRewardResult()
        {
            var result = await _adminLotteryDemoStub.GetRewardResult.CallAsync(new Int64Value {Value = 5});
            var lotteries = result.RewardLotteries;
            var level_1 = lotteries.Where(l => l.Level.Equals(1)).ToList();
            var level_2 = lotteries.Where(l => l.Level.Equals(2)).ToList();
            var level_3 = lotteries.Where(l => l.Level.Equals(3)).ToList();
//            var level_4 = lotteries.Where(l => l.Level.Equals(4)).ToList();
        }

        [TestMethod]
        public async Task GetBoughtLotteries()
        {
            foreach (var tester in Tester)
            {
                var result = await _lotteryDemoStub.GetBoughtLotteries.CallAsync(new GetBoughtLotteriesInput
                {
                    Period = 0,
                    StartIndex = 92,
                    Owner = tester.ConvertAddress()
                });
            }
        }

        [TestMethod]
        public async Task GetTestAccountBoughtLotteries()
        {
            var result = await _lotteryDemoStub.GetBoughtLotteries.CallAsync(new GetBoughtLotteriesInput
            {
                Period = 0,
                StartIndex = 44,
                Owner = TestAccount.ConvertAddress()
            });
        }

        [TestMethod]
        public async Task GetAllInfo()
        {
            var period = await _adminLotteryDemoStub.GetCurrentPeriodNumber.CallAsync(new Empty());
            Logger.Info($"Current period number is :{period.Value}");
            var periodInfo = await _adminLotteryDemoStub.GetPeriod.CallAsync(new Int64Value {Value = period.Value});
            Logger.Info($"Start id is: {periodInfo.StartId}; BlockNumber :{periodInfo.BlockNumber}");
            var currentPeriod = await _adminLotteryDemoStub.GetCurrentPeriod.CallAsync(new Empty());
            currentPeriod.BlockNumber.ShouldBe(periodInfo.BlockNumber);
            var result = await _adminLotteryDemoStub.GetSales.CallAsync(new Int64Value {Value = period.Value});
            Logger.Info($"{result.Value}");
//            var result1 = await _adminLotteryDemoStub.GetSales.CallAsync(new Int64Value {Value = period.Value-1});
//            Logger.Info($"{result1.Value}");
        }


        private void InitializeLotteryDemoContract()
        {
            var result =
                _lotteryDemoContract.ExecuteMethodWithResult(LotteryMethod.Initialize,
                    new InitializeInput
                    {
                        TokenSymbol = Symbol,
                        Price = Price
                    });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
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
                TokenName = "LOT Token"
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _tokenContract.IssueBalance(InitAccount, InitAccount, 100000000_00000000, Symbol);
            _tokenContract.IssueBalance(InitAccount, TestAccount, 10000_00000000, Symbol);
        }
    }
}