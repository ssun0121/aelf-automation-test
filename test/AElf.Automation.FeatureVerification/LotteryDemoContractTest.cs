using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Contracts.LotteryDemoContract;
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

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class LotteryDemoContractTest
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }

        private TokenContract _tokenContract;
        private GenesisContract _genesisContract;
        private LotteryDemoContract _lotteryDemoContract;
        private LotteryDemoContractContainer.LotteryDemoContractStub _lotteryDemoStub;
        private LotteryDemoContractContainer.LotteryDemoContractStub _adminLotteryDemoStub;

        private string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        private string TestAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        private List<string> Tester = new List<string>();
        private static string RpcUrl { get; } = "192.168.197.44:8001";
        private string Symbol { get; } = "LOT";
        private const long Price = 100_00000000;

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("ContractTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env2-main");
            Tester = NodeOption.AllNodes.Select(l => l.Account).ToList();
            NodeManager = new NodeManager(RpcUrl);
            Logger.Info(RpcUrl);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
            _lotteryDemoContract = new LotteryDemoContract(NodeManager, InitAccount,"RXcxgSXuagn8RrvhQAV81Z652EEYSwR6JLnqHYJ5UVpEptW8Y");
            Logger.Info($"Lottery contract : {_lotteryDemoContract.ContractAddress}");
//            if (!_tokenContract.GetTokenInfo(Symbol).Symbol.Equals(Symbol))
//                CreateTokenAndIssue();
//            _lotteryDemoContract = new LotteryDemoContract(NodeManager, InitAccount,
//                "2WHXRoLRjbUTDQsuqR5CntygVfnDb125qdJkudev4kVNbLhTdG");
//            InitializeLotteryDemoContract();

            _adminLotteryDemoStub =
                _lotteryDemoContract.GetTestStub<LotteryDemoContractContainer.LotteryDemoContractStub>(InitAccount);
            _lotteryDemoStub =
                _lotteryDemoContract.GetTestStub<LotteryDemoContractContainer.LotteryDemoContractStub>(TestAccount);
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
        public async Task AddRewardList()
        {
            var result =
                await _adminLotteryDemoStub.AddRewardList.SendAsync(
                    new RewardList
                    {
                        RewardMap =
                        {
                            {"level1", "iphone11"},
                            {"level2", "Mac"},
                            {"level3", "switch"},
                            {"level4", "小米平板"},
                        }
                    });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var getInfo = await _adminLotteryDemoStub.GetRewardName.CallAsync(new StringValue
            {
                Value = "level3"
            });
            getInfo.Value.ShouldBe("switch");
        }

        [TestMethod]
        public async Task GetRewardName()
        {
            var result = await _adminLotteryDemoStub.GetRewardName.CallAsync(new StringValue
            {
                Value = "xxx"
            });
            result.Value.ShouldBe("xxx");
        }

        [TestMethod]
        public void Buy()
        {
            foreach (var tester in Tester)
            {
                var amount = CommonHelper.GenerateRandomNumber(1, 10);
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
                var result = _lotteryDemoContract.ExecuteMethodWithResult(LotteryDemoMethod.Buy, new Int64Value
                {
                    Value = amount
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var resultValue =
                    BoughtLotteriesInformation.Parser.ParseFrom(
                        ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
                resultValue.Amount.ShouldBe(amount);
                Logger.Info($"start id is: {resultValue.StartId}, amount: {amount}");
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
            if (userBeforeBalance < 10000_00000000)
                _tokenContract.TransferBalance(InitAccount, TestAccount, 10000_00000000, Symbol);
            userBeforeBalance = _tokenContract.GetUserBalance(TestAccount, Symbol);

            _tokenContract.SetAccount(TestAccount);
            var approveResult = _tokenContract.ApproveToken(TestAccount, _lotteryDemoContract.ContractAddress,
                amount * Price,
                Symbol);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var allowance = _tokenContract.GetAllowance(TestAccount, _lotteryDemoContract.ContractAddress, Symbol);
            allowance.ShouldBeGreaterThanOrEqualTo(amount * Price);

            _lotteryDemoContract.SetAccount(TestAccount);
            var result = _lotteryDemoContract.ExecuteMethodWithResult(LotteryDemoMethod.Buy, new Int64Value
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
        public async Task SetRewardListForOnePeriod()
        {
            var currentPeriod = await _adminLotteryDemoStub.GetCurrentPeriodNumber.CallAsync(new Empty());
            var result = await _adminLotteryDemoStub.SetRewardListForOnePeriod.SendAsync(new RewardsInfo
            {
                Period = currentPeriod.Value,
                Rewards =
                {
                    {"iphone12", 1}
                },
//                SupposedDrawDate = DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)).ToTimestamp()
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void PrepareDraw()
        {
            _lotteryDemoContract.SetAccount(InitAccount);
            var result = _lotteryDemoContract.ExecuteMethodWithResult(LotteryDemoMethod.PrepareDraw, new Empty());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
        
        [TestMethod]
        public async Task OnlyDraw()
        {
            var period = await _adminLotteryDemoStub.GetCurrentPeriodNumber.CallAsync(new Empty());
            _lotteryDemoContract.SetAccount(InitAccount);
            var drawResult = _lotteryDemoContract.ExecuteMethodWithResult(LotteryDemoMethod.Draw, new Int64Value
            {
                Value = period.Value -1
            });
            drawResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public async Task Draw()
        {
            var period = await _adminLotteryDemoStub.GetCurrentPeriodNumber.CallAsync(new Empty());
            await SetRewardListForOnePeriod();
            _lotteryDemoContract.SetAccount(InitAccount);
            var result = _lotteryDemoContract.ExecuteMethodWithResult(LotteryDemoMethod.PrepareDraw, new Empty());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var drawLag = await _adminLotteryDemoStub.GetDrawingLag.CallAsync(new Empty());
            var block = result.BlockNumber;
            var currentBlock = AsyncHelper.RunSync(() => NodeManager.ApiClient.GetBlockHeightAsync());
            while (currentBlock < block + drawLag.Value)
            {
                Thread.Sleep(5000);
                Logger.Info("Waiting block");
                currentBlock = AsyncHelper.RunSync(() => NodeManager.ApiClient.GetBlockHeightAsync());
            }

            _lotteryDemoContract.SetAccount(InitAccount);
            var drawResult = _lotteryDemoContract.ExecuteMethodWithResult(LotteryDemoMethod.Draw, new Int64Value
            {
                Value = period.Value
            });
            drawResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public async Task TakeReward()
        {
            var rewardResult = await _adminLotteryDemoStub.GetRewardResult.CallAsync(new Int64Value {Value = 1});
            var lotteries = rewardResult.RewardLotteries;
            foreach (var lottery in lotteries)
            {
//                lottery.RegistrationInformation.ShouldBe("");
                _lotteryDemoContract.SetAccount(lottery.Owner.ToBase58());
                var result = _lotteryDemoContract.ExecuteMethodWithResult(LotteryDemoMethod.TakeReward,
                    new TakeRewardInput
                    {
                        RegistrationInformation = "中奖啦",
                        LotteryId = lottery.Id
                    });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }

            rewardResult = await _adminLotteryDemoStub.GetRewardResult.CallAsync(new Int64Value {Value = 11});
            lotteries = rewardResult.RewardLotteries;
            foreach (var lottery in lotteries)
            {
                lottery.RegistrationInformation.ShouldBe("中奖啦");
            }
        }

        [TestMethod]
        public async Task TakeReward_Failed()
        {
            var rewardResult = await _lotteryDemoStub.GetRewardResult.CallAsync(new Int64Value {Value = 1});
            var lotteries = rewardResult.RewardLotteries;
            foreach (var lottery in lotteries)
            {
                lottery.RegistrationInformation.ShouldBe("");
                var errorAddress = Tester.Where(a => !a.Equals(lottery.Owner.ToBase58())).ToList().First();
                _lotteryDemoContract.SetAccount(errorAddress);
                var result = _lotteryDemoContract.ExecuteMethodWithResult(LotteryDemoMethod.TakeReward,
                    new TakeRewardInput
                    {
                        RegistrationInformation = "中奖啦",
                        LotteryId = lottery.Id
                    });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
            }
        }

        [TestMethod]
        public async Task Stake()
        {
            var totalAmount = await _lotteryDemoStub.GetStakingTotal.CallAsync(new Empty());
            Logger.Info(totalAmount.Value);
            
            var account = Tester.Last();
            var balance = _tokenContract.GetUserBalance(account, Symbol);
            var amount = balance / 10;
            var stub = _lotteryDemoContract.GetTestStub<LotteryDemoContractContainer.LotteryDemoContractStub>(account);
            var getInfo = await stub.GetStakingAmount.CallAsync(account.ConvertAddress());
            var approve =
                _tokenContract.ApproveToken(account, _lotteryDemoContract.ContractAddress, amount, Symbol);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var result = await stub.Stake.SendAsync(new Int64Value{Value = amount});
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var afterGetInfo = await stub.GetStakingAmount.CallAsync(account.ConvertAddress());
            afterGetInfo.Value.ShouldBe(getInfo.Value + amount);
            
            var afterTotalAmount = await _lotteryDemoStub.GetStakingTotal.CallAsync(new Empty());
            afterTotalAmount.Value.ShouldBe(totalAmount.Value + amount);
            Logger.Info($"{balance} {amount} {afterGetInfo.Value} {afterTotalAmount.Value}");
        }

        [TestMethod]
        public async Task GetStakingTotal()
        {
            var totalAmount = await _lotteryDemoStub.GetStakingTotal.CallAsync(new Empty());
            Logger.Info(totalAmount);
        }

        [TestMethod]
        public async Task RegisterDividend()
        {
            var account = TestAccount;
            var receiver = "0x4Bb4916673D7C638D9F8A309100770e45631C240";
            var stub = _lotteryDemoContract.GetTestStub<LotteryDemoContractContainer.LotteryDemoContractStub>(account);
            var result = await stub.RegisterDividend.SendAsync
            (
                new RegisterDividendDto
                {
                    Receiver = receiver
                });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var getInfo = await stub.GetRegisteredDividend.CallAsync(account.ConvertAddress());
            getInfo.Receiver.ShouldBe(receiver);
        }

        [TestMethod]
        public async Task GetRegisteredDividend()
        {
            var account = TestAccount;
            var stub = _lotteryDemoContract.GetTestStub<LotteryDemoContractContainer.LotteryDemoContractStub>(account);
            var info = await stub.GetRegisteredDividend.CallAsync(account.ConvertAddress());
            info.ShouldBe(new RegisterDividendDto());
        }

        [TestMethod]
        public async Task Suspend()
        {
            var result = await _adminLotteryDemoStub.Suspend.SendAsync(new Empty());
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public async Task Recover()
        {
            var result = await _adminLotteryDemoStub.Recover.SendAsync(new Empty());
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
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
            var result = await _adminLotteryDemoStub.ResetPrice.SendAsync(new Int64Value {Value = 100_00000000});
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var newPrice = await _lotteryDemoStub.GetPrice.CallAsync(new Empty());
            newPrice.Value.ShouldBe(100_00000000);
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
        public async Task SetStakingTimestamp()
        {
            var timestamp = await _adminLotteryDemoStub.GetStakingTimestamp.CallAsync(new Empty());
            Logger.Info(timestamp);

            var resetTime = await _adminLotteryDemoStub.SetStakingTimestamp.SendAsync(new SetStakingTimestampInput
            {
                IsStartTimestamp = false,
                Timestamp = DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)).ToTimestamp()
            });
        }

        [TestMethod]
        public async Task TakeBackToken()
        {
            var amount = _tokenContract.GetUserBalance(_lotteryDemoContract.ContractAddress, Symbol);
            var adminBalance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            var result = await _adminLotteryDemoStub.TakeBackToken.SendAsync(new TakeBackTokenInput
            {
                Symbol = Symbol,
                Amount = amount + 1
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            afterBalance.ShouldBe(adminBalance + amount);
            var afterContractBalance = _tokenContract.GetUserBalance(_lotteryDemoContract.ContractAddress, Symbol);
            afterContractBalance.ShouldBe(amount - amount);
        }

        [TestMethod]
        public async Task GetStakingTimestamp()
        {
            var timestamp = await _adminLotteryDemoStub.GetStakingTimestamp.CallAsync(new Empty());
            Logger.Info(timestamp);
        }

        [TestMethod]
        public async Task GetRewardResult()
        {
            var period = await _adminLotteryDemoStub.GetCurrentPeriodNumber.CallAsync(new Empty());
            Logger.Info($"Current period number is :{period.Value}");
            for (var i = 1; i <= period.Value; i++)
            {
                var result = await _adminLotteryDemoStub.GetRewardResult.CallAsync(new Int64Value {Value = i});
                var lotteries = result.RewardLotteries;
                Logger.Info($"{lotteries}");
            }
        }

        [TestMethod]
        public async Task GetBoughtLotteries()
        {
            foreach (var tester in Tester)
            {
                var result = await _lotteryDemoStub.GetBoughtLotteries.CallAsync(new GetBoughtLotteriesInput
                {
                    Period = 0,
                    StartId = 0,
                    Owner = tester.ConvertAddress()
                });
                var count = await _lotteryDemoStub.GetBoughtLotteriesCount.CallAsync(tester.ConvertAddress());
                result.Lotteries.Count.ShouldBe((int) count.Value);
                Logger.Info($"{tester} lotteries: {result.Lotteries}");
            }
        }

        [TestMethod]
        public async Task GetBoughtLotteriesCount()
        {
            long sum = 0;
            foreach (var tester in Tester)
            {
                var result = await _lotteryDemoStub.GetBoughtLotteriesCount.CallAsync(tester.ConvertAddress());
                sum = sum + result.Value;
                Logger.Info($"{tester} has {result.Value} lotteries");
            }

            var count = await _adminLotteryDemoStub.GetAllLotteriesCount.CallAsync(new Empty());
            count.Value.ShouldBe(sum);
            Logger.Info($"all lotteries are : {count.Value}");

            var rewardCount = 0;
            var period = await _adminLotteryDemoStub.GetCurrentPeriodNumber.CallAsync(new Empty());
            Logger.Info($"Current period number is :{period.Value}");
            for (var i = 1; i <= period.Value; i++)
            {
                var periodInfo = await _adminLotteryDemoStub.GetPeriod.CallAsync(new Int64Value {Value = i});
                Logger.Info(
                    $"{i}: Start id is: {periodInfo.StartId}; BlockNumber :{periodInfo.BlockNumber}; Rewards: {periodInfo.Rewards}; RewardsId :{periodInfo.RewardIds}");
                rewardCount = rewardCount + periodInfo.RewardIds.Count;
            }
        }

        [TestMethod]
        public async Task GetNoRewardLotteriesCount()
        {
            var result = await _adminLotteryDemoStub.GetNoRewardLotteriesCount.CallAsync(new Empty());
            Logger.Info($"NoRewardLotteriesCount: {result.Value}");
        }

        [TestMethod]
        public async Task GetTestAccountBoughtLotteries()
        {
            var result = await _lotteryDemoStub.GetBoughtLotteries.CallAsync(new GetBoughtLotteriesInput
            {
                Period = 0,
                Owner = TestAccount.ConvertAddress()
            });
        }

        [TestMethod]
        public async Task GetAllInfo()
        {
            var period = await _adminLotteryDemoStub.GetCurrentPeriodNumber.CallAsync(new Empty());
            Logger.Info($"Current period number is :{period.Value}");
            for (var i = 1; i <= period.Value; i++)
            {
                var periodInfo = await _adminLotteryDemoStub.GetPeriod.CallAsync(new Int64Value {Value = i});
                Logger.Info(
                    $"{i}: Start id is: {periodInfo.StartId}; BlockNumber :{periodInfo.BlockNumber}; Rewards: {periodInfo.Rewards}; RewardsId :{periodInfo.RewardIds}");
            }
            var currentPeriodInfo = await _adminLotteryDemoStub.GetPeriod.CallAsync(new Int64Value {Value = period.Value});
            var currentPeriod = await _adminLotteryDemoStub.GetCurrentPeriod.CallAsync(new Empty());
            currentPeriod.BlockNumber.ShouldBe(currentPeriodInfo.BlockNumber);
            var result = await _adminLotteryDemoStub.GetSales.CallAsync(new Int64Value {Value = period.Value - 1});
            Logger.Info($"Sales: {result.Value}");
        }


        [TestMethod]
        public void InitializeLotteryDemoContract()
        {
            var result =
                _lotteryDemoContract.ExecuteMethodWithResult(LotteryDemoMethod.Initialize,
                    new InitializeInput
                    {
                        TokenSymbol = Symbol,
                        Price = Price,
                        DrawingLag = 40,
                        MaximumAmount = 50,
                        ProfitsRate = 50,
                        StartTimestamp = DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)).ToTimestamp(),
                        ShutdownTimestamp = DateTime.UtcNow.Add(TimeSpan.FromDays(7)).ToTimestamp()
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
                TokenName = "LOT Token"
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _tokenContract.IssueBalance(InitAccount, InitAccount, 100000000_00000000, Symbol);
            _tokenContract.IssueBalance(InitAccount, TestAccount, 10000_00000000, Symbol);
        }
    }
}