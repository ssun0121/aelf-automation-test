using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Contracts.LotteryDemoContract;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.Contracts.TokenHolder;
using AElf.CSharp.Core;
using AElf.Standards.ACS9;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Shouldly;
using Volo.Abp.Threading;
using ClaimProfitsInput = AElf.Contracts.TokenHolder.ClaimProfitsInput;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class LotteryDemoContractTest
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }

        private TokenContract _tokenContract;
        private TokenHolderContract _tokenHolderContract;
        private GenesisContract _genesisContract;
        private LotteryDemoContract _lotteryDemoContract;
        private LotteryDemoContractContainer.LotteryDemoContractStub _lotteryDemoStub;
        private LotteryDemoContractContainer.LotteryDemoContractStub _adminLotteryDemoStub;
        private const int RateDecimals = 4;

        private string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        private string TestAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        private string VirtualAccount { get; } = "";
        private List<string> Tester = new List<string>();
        private static string RpcUrl { get; } = "192.168.197.44:8001";
        private string Symbol { get; } = "LOT";
        private const long Price = 100_00000000;
        private const int ProfitsRate = 50;

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
            _tokenHolderContract = _genesisContract.GetTokenHolderContract(InitAccount);
            _lotteryDemoContract = new LotteryDemoContract(NodeManager, InitAccount);
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
//                if (balance < 10_00000000)
//                {
//                    _tokenContract.TransferBalance(InitAccount, tester, 20_00000000, "ELF");
//                }
//            }
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
                        ProfitsRate = ProfitsRate,
                        StartTimestamp = 
                            Timestamp.FromDateTime(new DateTime(2021, 3, 3, 18, 00, 00).ToUniversalTime()),
                        ShutdownTimestamp =
                            Timestamp.FromDateTime(new DateTime(2021, 3, 5, 18, 00, 00).ToUniversalTime())
                    });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var schemeLog = result.Logs.First(l => l.Name.Contains(nameof(SchemeCreated))).NonIndexed;
            var schemeInfo = SchemeCreated.Parser.ParseFrom(ByteString.FromBase64(schemeLog));
            Logger.Info(schemeInfo);
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
                var profitBalance = _tokenContract.GetUserBalance(VirtualAccount, Symbol);
                var totalAmount = amount * Price;
                var profit = totalAmount.Mul(ProfitsRate).Div(GetRateDenominator());

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
                Logger.Info($"start id is: {resultValue.StartId}, amount: {amount}, profit {profit}");
                var userBalance = _tokenContract.GetUserBalance(tester, Symbol);
                userBalance.ShouldBe(userBeforeBalance - amount * Price);
                var contractBalance = _tokenContract.GetUserBalance(_lotteryDemoContract.ContractAddress, Symbol);
                contractBalance.ShouldBe(balance + amount * Price - profit);
                var afterProfitBalance = _tokenContract.GetUserBalance(VirtualAccount, Symbol);
                afterProfitBalance.ShouldBe(profitBalance + profit);
            }
        }

        [TestMethod]
        public void Buy_More()
        {
            var txList = new List<string>();
            foreach (var tester in Tester)
            {
                var amount = 50;
                var userBeforeBalance = _tokenContract.GetUserBalance(tester, Symbol);
                if (userBeforeBalance < amount * Price * 10)
                    _tokenContract.TransferBalance(InitAccount, tester, amount * Price * 10, Symbol);

//                _tokenContract.SetAccount(tester);
//                var approveResult = _tokenContract.ApproveToken(tester, _lotteryDemoContract.ContractAddress,
//                    long.MaxValue, Symbol);
//                approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var allowance = _tokenContract.GetAllowance(tester, _lotteryDemoContract.ContractAddress, Symbol);
                allowance.ShouldBeGreaterThanOrEqualTo(amount * Price);
            }

            for (int i = 0; i < 10; i++)
            {
                foreach (var tester in Tester)
                {
                    var amount = CommonHelper.GenerateRandomNumber(25, 50);
                    _lotteryDemoContract.SetAccount(tester);
                    var txId = _lotteryDemoContract.ExecuteMethodWithTxId(LotteryDemoMethod.Buy, new Int64Value
                    {
                        Value = amount
                    });
                    txList.Add(txId);
                }

                Thread.Sleep(1000);
            }

//            NodeManager.CheckTransactionListResult(txList);
        }


        [TestMethod]
        public void Buy_Once()
        {
//            var amount = CommonHelper.GenerateRandomNumber(1, 10);
            var amount = 50;
            var balance = _tokenContract.GetUserBalance(_lotteryDemoContract.ContractAddress, Symbol);
            var userBeforeBalance = _tokenContract.GetUserBalance(TestAccount, Symbol);
            if (userBeforeBalance < 10000_00000000)
                _tokenContract.TransferBalance(InitAccount, TestAccount, 10000_00000000, Symbol);
            userBeforeBalance = _tokenContract.GetUserBalance(TestAccount, Symbol);

            var profitBalance = _tokenContract.GetUserBalance(VirtualAccount, Symbol);
            var totalAmount = amount * Price;
            var profit = totalAmount.Mul(ProfitsRate).Div(GetRateDenominator());

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
            Logger.Info($"start id is: {resultValue.StartId}, amount: {amount}, profit {profit}");
            var userBalance = _tokenContract.GetUserBalance(TestAccount, Symbol);
            userBalance.ShouldBe(userBeforeBalance - amount * Price);
            var contractBalance = _tokenContract.GetUserBalance(_lotteryDemoContract.ContractAddress, Symbol);
            contractBalance.ShouldBe(balance + amount * Price - profit);
            var afterProfitBalance = _tokenContract.GetUserBalance(VirtualAccount, Symbol);
            afterProfitBalance.ShouldBe(profitBalance + profit);
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
                    {"iphone12", 1},
                    {"elf500", 2},
                    {"PS5", 1}
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
                Value = period.Value - 1
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
            var result = await stub.Stake.SendAsync(new Int64Value {Value = amount});
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
            var account = Tester.Last();
            ;
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
                IsStartTimestamp = true,
                Timestamp = DateTime.UtcNow.Add(TimeSpan.FromMinutes(3)).ToTimestamp()
            });
            resetTime.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            timestamp = await _adminLotteryDemoStub.GetStakingTimestamp.CallAsync(new Empty());
            Logger.Info(timestamp);
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
                var result = await _adminLotteryDemoStub.GetBoughtLotteries.CallAsync(new GetBoughtLotteriesInput
                {
                    Period = 0,
                    StartId = 0,
                    Owner = tester.ConvertAddress()
                });
                var count = await _adminLotteryDemoStub.GetBoughtLotteriesCount.CallAsync(tester.ConvertAddress());
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
                var result = await _adminLotteryDemoStub.GetBoughtLotteriesCount.CallAsync(tester.ConvertAddress());
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

            var currentPeriodInfo =
                await _adminLotteryDemoStub.GetPeriod.CallAsync(new Int64Value {Value = period.Value});
            var currentPeriod = await _adminLotteryDemoStub.GetCurrentPeriod.CallAsync(new Empty());
            currentPeriod.BlockNumber.ShouldBe(currentPeriodInfo.BlockNumber);
            var result = await _adminLotteryDemoStub.GetSales.CallAsync(new Int64Value {Value = period.Value});
            Logger.Info($"Sales: {result.Value}");
        }

        #region acs9 

        [TestMethod]
        public async Task GetProfitConfig()
        {
            var config = await _adminLotteryDemoStub.GetProfitConfig.CallAsync(new Empty());
            Logger.Info(config);
        }

        [TestMethod]
        public async Task GetProfitsAmount()
        {
            var profitAmount = await _adminLotteryDemoStub.GetProfitsAmount.CallAsync(new Empty());
            Logger.Info(profitAmount);
        }

        [TestMethod]
        public async Task RegisterForLotteryProfits()
        {
            var account = InitAccount;
            var amount = 200;
            var approveResult =
                _tokenContract.ApproveToken(account, _tokenHolderContract.ContractAddress, amount, Symbol);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var holder = _genesisContract.GetTokenHolderStub(account);
            var registerResult =
                await holder.RegisterForProfits.SendAsync(new RegisterForProfitsInput
                {
                    SchemeManager = _lotteryDemoContract.Contract,
                    Amount = amount
                });
            registerResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined,
                registerResult.TransactionResult.Error);
        }

        [TestMethod]
        public async Task TakeContractProfits()
        {
            await GetProfitsAmount();
            var result = await _adminLotteryDemoStub.TakeContractProfits.SendAsync(new TakeContractProfitsInput
            {
                Amount = 0,
                Symbol = Symbol
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public async Task CheckProfit()
        {
            var accountList = new List<string>();
            accountList.Add(Tester[0]);
            accountList.Add(InitAccount);
            foreach (var sender in accountList)
            {
                var holder = _genesisContract.GetTokenHolderStub(sender);
                var profitMap = await holder.GetProfitsMap.CallAsync(new ClaimProfitsInput
                {
                    SchemeManager = _lotteryDemoContract.Contract,
                    Beneficiary = sender.ConvertAddress()
                });
                Logger.Info($"{sender}:{JsonConvert.SerializeObject(profitMap)}");
            }
        }

        [TestMethod]
        public async Task ClaimProfits()
        {
            var sender = InitAccount;
            var holder = _genesisContract.GetTokenHolderStub(sender);

            var profitMap = await holder.GetProfitsMap.CallAsync(new ClaimProfitsInput
            {
                SchemeManager = _lotteryDemoContract.Contract,
                Beneficiary = sender.ConvertAddress()
            });
            var amount = (profitMap.Value)[Symbol];

            var senderBalance = _tokenContract.GetUserBalance(sender, Symbol);
            var claimResult = await holder.ClaimProfits.SendAsync(new ClaimProfitsInput
            {
                SchemeManager = _lotteryDemoContract.Contract,
                Beneficiary = sender.ConvertAddress()
            });
            claimResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var afterClaim = _tokenContract.GetUserBalance(sender, Symbol);
            afterClaim.ShouldBe(senderBalance + amount);
        }

        #endregion

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

        private int GetRateDenominator()
        {
            var result = BancorHelper.Pow(10, RateDecimals);
            return result;
        }
    }
}