using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.LotteryContract;
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
using ClaimProfitsInput = AElf.Contracts.TokenHolder.ClaimProfitsInput;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class LotteryContractTest
    {
        private const int RateDecimals = 4;

        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private INodeManager SideNodeManager { get; set; }

        private TokenContract _tokenContract;
        private TokenContract _sideTokenContract;

        private GenesisContract _genesisContract;
        private GenesisContract _sideGenesisContract;

        private TokenHolderContract _sideTokenHolderContract;

        private LotteryContract _lotteryContract;
        private LotteryContractContainer.LotteryContractStub _lotteryStub;
        private LotteryContractContainer.LotteryContractStub _adminLotteryStub;
        public Dictionary<LotteryType, long> Rewards { get; set; }

        private string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        private string SellerAccount { get; } = "eFU9Quc8BsztYpEHKzbNtUpu9hGKgwGD2tyL13MqtFkbnAoCZ";
        private string VirtualAddress { get; } = "5XPcuoT2R2C3QSY64BuHsgUCBLiWMwPo1vvMLCr3w3iKyB7Lc";

        private List<string> Tester = new List<string>
        {
            "2oSMWm1tjRqVdfmrdL8dgrRvhWu1FP8wcZidjS6wPbuoVtxhEz",
            "WRy3ADLZ4bEQTn86ENi5GXi5J1YyHp9e99pPso84v2NJkfn5k",
            "2frDVeV6VxUozNqcFbgoxruyqCRAuSyXyfCaov6bYWc7Gkxkh2",
            "2ZYyxEH6j8zAyJjef6Spa99Jx2zf5GbFktyAQEBPWLCvuSAn8D",
            "eFU9Quc8BsztYpEHKzbNtUpu9hGKgwGD2tyL13MqtFkbnAoCZ",
            "2V2UjHQGH8WT4TWnzebxnzo9uVboo67ZFbLjzJNTLrervAxnws",
            "2gfVsyYbLPehmVjZxKHZfxp9AMRUEV6KFHkZDgdU7VZf64teew",
            "2a6MGBRVLPsy6pu4SVMWdQqHS5wvmkZv8oas9srGWHJk7GSJPV",
            "h6CRCFAhyozJPwdFRd7i8A5zVAqy171AVty3uMQUQp1MB9AKa",
            "28qLVdGMokanMAp9GwfEqiWnzzNifh8LS9as6mzJFX1gQBB823",
            "2G4L1S7KPfRscRP6zmd7AdVwtptVD3vR8YoF1ZHgPotDNbZnNY"
        };

        private static string RpcUrl { get; } = "192.168.197.44:8000";
        private static string SideRpcUrl { get; } = "192.168.197.44:8001";

        private string Symbol { get; } = "LOT";
        private const long Price = 1_00000000;
        private const int Bonus = 10;
        private const int CashDuration = 30;
        private const int ProfitsRate = 1000;

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("LotteryContract");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env2-main");
            NodeManager = new NodeManager(RpcUrl);
            SideNodeManager = new NodeManager(SideRpcUrl);

            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);

            _sideGenesisContract = GenesisContract.GetGenesisContract(SideNodeManager, InitAccount);
            _sideTokenContract = _sideGenesisContract.GetTokenContract(InitAccount);
            _sideTokenHolderContract = _sideGenesisContract.GetTokenHolderContract(InitAccount);
//            _lotteryContract = new LotteryContract(SideNodeManager, InitAccount);
//            Logger.Info($"Lottery contract : {_lotteryContract}");
//            if (!_tokenContract.GetTokenInfo(Symbol).Symbol.Equals(Symbol))
//                CreateTokenAndIssue();
            _lotteryContract = new LotteryContract(SideNodeManager, InitAccount,
                "RXcxgSXuagn8RrvhQAV81Z652EEYSwR6JLnqHYJ5UVpEptW8Y");
//            InitializeLotteryContract();


            _adminLotteryStub =
                _lotteryContract.GetTestStub<LotteryContractContainer.LotteryContractStub>(InitAccount);
            _lotteryStub =
                _lotteryContract.GetTestStub<LotteryContractContainer.LotteryContractStub>(SellerAccount);
            InitRewards();
//
//            foreach (var tester in Tester)
//            {
//                var balance = _sideTokenContract.GetUserBalance(tester, "LOT");
//                var elfBalance = _sideTokenContract.GetUserBalance(tester, "ELF");
//                if (balance < 100_00000000)
//                    _sideTokenContract.TransferBalance(InitAccount, tester, 1000_00000000, "LOT");
//                if (elfBalance < 100_00000000)
//                    _sideTokenContract.TransferBalance(InitAccount, tester, 1000_00000000, "ELF");
//            }
        }

        [TestMethod]
        public async Task InitializeLotteryContract()
        {
            var result =
                _lotteryContract.ExecuteMethodWithResult(LotteryMethod.Initialize,
                    new InitializeInput
                    {
                        TokenSymbol = Symbol,
                        CashDuration = CashDuration,
                        BonusRate = Bonus,
                        Price = Price,
                        ProfitsRate = ProfitsRate
                    });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var schemeLog = result.Logs.First(l => l.Name.Contains(nameof(SchemeCreated))).NonIndexed;
            var schemeInfo = SchemeCreated.Parser.ParseFrom(ByteString.FromBase64(schemeLog));
            Logger.Info(schemeInfo);

            var admin = await _adminLotteryStub.GetAdmin.CallAsync(new Empty());
            admin.ShouldBe(InitAccount.ConvertAddress());

            var token = await _adminLotteryStub.GetTokenSymbol.CallAsync(new Empty());
            token.Value.ShouldBe(Symbol);

            var cash = await _adminLotteryStub.GetCashDuration.CallAsync(new Empty());
            cash.Value.ShouldBe(CashDuration);

            var price = await _adminLotteryStub.GetPrice.CallAsync(new Empty());
            price.Value.ShouldBe(Price);

            var profitRate = await _adminLotteryStub.GetProfitsRate.CallAsync(new Empty());
            profitRate.Rate.ShouldBe(ProfitsRate);

            var bonus = await _adminLotteryStub.GetBonusRate.CallAsync(new Empty());
            bonus.Decimals.ShouldBe(4);
            bonus.Rate.ShouldBe(Bonus);

            var holder = _sideGenesisContract.GetTokenHolderStub();
            var profit = _sideGenesisContract.GetProfitStub();
            var scheme = await holder.GetScheme.CallAsync(_lotteryContract.Contract);
            var schemeAddress = await profit.GetSchemeAddress.CallAsync(new SchemePeriod
            {
                Period = 0,
                SchemeId = scheme.SchemeId
            });
            schemeAddress.ShouldBe(VirtualAddress.ConvertAddress());
        }

        [TestMethod]
        public async Task Buy_Simple()
        {
            for (int i = 0; i < 4; i++)
            {
                var sender = Tester[i];
                _sideTokenContract.SetAccount(sender);
                var approve =
                    _sideTokenContract.ApproveToken(sender, _lotteryContract.ContractAddress, 2000_00000000, Symbol);
                approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                for (int j = 0; j < 4; j++)
                {
                    if (i == j) continue;
                    var list1 = new List<int> {i, j};
                    var list2 = new List<int> {j, i};
                    var totalAmount = list1.Count * list2.Count * Price;
                    var bonus = totalAmount.Mul(Bonus).Div(GetRateDenominator());
                    var profit = totalAmount.Mul(ProfitsRate).Div(GetRateDenominator());

                    var senderBalance = _sideTokenContract.GetUserBalance(sender, Symbol);
                    var contractBalance = _sideTokenContract.GetUserBalance(_lotteryContract.ContractAddress, Symbol);
                    var sellerBalance = _sideTokenContract.GetUserBalance(SellerAccount, Symbol);
                    var profitBalance = _sideTokenContract.GetUserBalance(VirtualAddress, Symbol);

                    var stub = _lotteryContract.GetTestStub<LotteryContractContainer.LotteryContractStub>(sender);
                    var result = await stub.Buy.SendAsync(new BuyInput
                    {
                        Type = (int) LotteryType.Simple,
                        Seller = SellerAccount.ConvertAddress(),
                        BetInfos =
                        {
                            new BetBody
                            {
                                Bets = {list1}
                            },
                            new BetBody
                            {
                                Bets = {list2}
                            }
                        }
                    });
                    result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                    var senderBalanceAfter = _sideTokenContract.GetUserBalance(sender, Symbol);
                    senderBalanceAfter.ShouldBe(senderBalance - totalAmount);
                    var profitBalanceAfter = _sideTokenContract.GetUserBalance(VirtualAddress, Symbol);
                    profitBalanceAfter.ShouldBe(profitBalance + profit);
                    var contractBalanceAfter =
                        _sideTokenContract.GetUserBalance(_lotteryContract.ContractAddress, Symbol);
                    contractBalanceAfter.ShouldBe(contractBalance + (totalAmount - bonus - profit));

                    var sellerBalanceAfter = _sideTokenContract.GetUserBalance(SellerAccount, Symbol);
                    sellerBalanceAfter.ShouldBe(sellerBalance + bonus);
                    Logger.Info($"Buy amount {totalAmount}, profit {profit}, bonus {bonus}");
                }
            }
        }

        [TestMethod]
        public async Task Buy_Simple_One()
        {
            var sender = Tester[5];
//            _sideTokenContract.TransferBalance(InitAccount, sender, 2000_00000, "LOT");
            _sideTokenContract.SetAccount(sender);
            var approve =
                _sideTokenContract.ApproveToken(sender, _lotteryContract.ContractAddress, 2000_00000000, Symbol);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var list1 = new List<int> {0, 1};
            var list2 = new List<int> {0, 1};
            var totalAmount = list1.Count * list2.Count * Price;
            var bonus = totalAmount.Mul(Bonus).Div(GetRateDenominator());
            var profit = totalAmount.Mul(ProfitsRate).Div(GetRateDenominator());

            var initBalance = _sideTokenContract.GetUserBalance(sender, Symbol);
            var contractBalance = _sideTokenContract.GetUserBalance(_lotteryContract.ContractAddress, Symbol);
            var sellerBalance = _sideTokenContract.GetUserBalance(SellerAccount, Symbol);
            var profitBalance = _sideTokenContract.GetUserBalance(VirtualAddress, Symbol);

            var stub = _lotteryContract.GetTestStub<LotteryContractContainer.LotteryContractStub>(sender);
            var result = await stub.Buy.SendAsync(new BuyInput
            {
                Type = (int) LotteryType.Simple,
                Seller = SellerAccount.ConvertAddress(),
                BetInfos =
                {
                    new BetBody
                    {
                        Bets = {list1}
                    },
                    new BetBody
                    {
                        Bets = {list2}
                    }
                }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var initBalanceAfter = _sideTokenContract.GetUserBalance(sender, Symbol);
            initBalanceAfter.ShouldBe(initBalance - totalAmount);
            var profitBalanceAfter = _sideTokenContract.GetUserBalance(VirtualAddress, Symbol);
            profitBalanceAfter.ShouldBe(profitBalance + profit);
            var contractBalanceAfter =
                _sideTokenContract.GetUserBalance(_lotteryContract.ContractAddress, Symbol);
            contractBalanceAfter.ShouldBe(contractBalance + (totalAmount - bonus - profit));
            var sellerBalanceAfter = _sideTokenContract.GetUserBalance(SellerAccount, Symbol);
            sellerBalanceAfter.ShouldBe(sellerBalance + bonus);
            Logger.Info($"Buy amount {totalAmount}, profit {profit}, bonus {bonus}");
        }

        [TestMethod]
        public async Task Buy_OneBit()
        {
            var sender = Tester[1];
            var list = new List<int> {1, 3, 5, 7, 9};
            var totalAmount = list.Count * Price;
            var bonus = totalAmount.Mul(Bonus).Div(GetRateDenominator());

            var approve = _sideTokenContract.ApproveToken(sender, _lotteryContract.ContractAddress,
                totalAmount, Symbol);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var initBalance = _sideTokenContract.GetUserBalance(sender, Symbol);
            var contractBalance = _sideTokenContract.GetUserBalance(_lotteryContract.ContractAddress, Symbol);
            var sellerBalance = _sideTokenContract.GetUserBalance(SellerAccount, Symbol);

            var stub = _lotteryContract.GetTestStub<LotteryContractContainer.LotteryContractStub>(sender);
            var result = await stub.Buy.SendAsync(new BuyInput
            {
                Type = (int) LotteryType.OneBit,
                Seller = SellerAccount.ConvertAddress(),
                BetInfos =
                {
                    new BetBody
                    {
                        Bets = {list}
                    }
                }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var initBalanceAfter = _sideTokenContract.GetUserBalance(sender, Symbol);
            initBalanceAfter.ShouldBe(initBalance - totalAmount);
            var contractBalanceAfter = _sideTokenContract.GetUserBalance(_lotteryContract.ContractAddress, Symbol);
            contractBalanceAfter.ShouldBe(contractBalance + (totalAmount - bonus));
            var sellerBalanceAfter = _sideTokenContract.GetUserBalance(SellerAccount, Symbol);
            sellerBalanceAfter.ShouldBe(sellerBalance + bonus);
            Logger.Info($"Buy amount {totalAmount}, bonus {bonus}");
        }

        [TestMethod]
        public async Task Buy_TwoBit()
        {
            var sender = Tester[2];
            var list1 = new List<int> {2, 4, 8};
            var list2 = new List<int> {1, 7, 5};
            var totalAmount = list1.Count * list2.Count * Price;
            var bonus = totalAmount.Mul(Bonus).Div(GetRateDenominator());

            _sideTokenContract.SetAccount(sender);
            var approve = _sideTokenContract.ApproveToken(sender, _lotteryContract.ContractAddress,
                totalAmount, Symbol);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _sideTokenContract.TransferBalance(InitAccount, sender, totalAmount, "LOT");

            var initBalance = _sideTokenContract.GetUserBalance(sender, Symbol);
            var contractBalance = _sideTokenContract.GetUserBalance(_lotteryContract.ContractAddress, Symbol);
            var sellerBalance = _sideTokenContract.GetUserBalance(SellerAccount, Symbol);

            var stub = _lotteryContract.GetTestStub<LotteryContractContainer.LotteryContractStub>(sender);
            var result = await stub.Buy.SendAsync(new BuyInput
            {
                Type = (int) LotteryType.TwoBit,
                Seller = SellerAccount.ConvertAddress(),
                BetInfos =
                {
                    new BetBody
                    {
                        Bets = {list1}
                    },
                    new BetBody
                    {
                        Bets = {list2}
                    }
                }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var initBalanceAfter = _sideTokenContract.GetUserBalance(sender, Symbol);
            initBalanceAfter.ShouldBe(initBalance - totalAmount);
            var contractBalanceAfter = _sideTokenContract.GetUserBalance(_lotteryContract.ContractAddress, Symbol);
            contractBalanceAfter.ShouldBe(contractBalance + (totalAmount - bonus));
            var sellerBalanceAfter = _sideTokenContract.GetUserBalance(SellerAccount, Symbol);
            sellerBalanceAfter.ShouldBe(sellerBalance + bonus);
            Logger.Info($"Buy amount {totalAmount}, bonus {bonus}");
        }

        [TestMethod]
        public async Task Buy_ThreeBit()
        {
            var sender = Tester[3];
            var list1 = new List<int> {0, 6};
            var list2 = new List<int> {7, 9};
            var list3 = new List<int> {1, 5};
            var totalAmount = list1.Count * list2.Count * list3.Count * Price;
            var bonus = totalAmount.Mul(Bonus).Div(GetRateDenominator());

            _sideTokenContract.TransferBalance(InitAccount, sender, totalAmount, "LOT");

            var approve = _sideTokenContract.ApproveToken(sender, _lotteryContract.ContractAddress,
                totalAmount, Symbol);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var initBalance = _sideTokenContract.GetUserBalance(sender, Symbol);
            var contractBalance = _sideTokenContract.GetUserBalance(_lotteryContract.ContractAddress, Symbol);
            var sellerBalance = _sideTokenContract.GetUserBalance(SellerAccount, Symbol);

            var stub = _lotteryContract.GetTestStub<LotteryContractContainer.LotteryContractStub>(sender);
            var result = await stub.Buy.SendAsync(new BuyInput
            {
                Type = (int) LotteryType.ThreeBit,
                Seller = SellerAccount.ConvertAddress(),
                BetInfos =
                {
                    new BetBody
                    {
                        Bets = {list1}
                    },
                    new BetBody
                    {
                        Bets = {list2}
                    },
                    new BetBody
                    {
                        Bets = {list3}
                    }
                }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var initBalanceAfter = _sideTokenContract.GetUserBalance(sender, Symbol);
            initBalanceAfter.ShouldBe(initBalance - totalAmount);
            var contractBalanceAfter = _sideTokenContract.GetUserBalance(_lotteryContract.ContractAddress, Symbol);
            contractBalanceAfter.ShouldBe(contractBalance + (totalAmount - bonus));
            var sellerBalanceAfter = _sideTokenContract.GetUserBalance(SellerAccount, Symbol);
            sellerBalanceAfter.ShouldBe(sellerBalance + bonus);
            Logger.Info($"Buy amount {totalAmount}, bonus {bonus}");
        }

        [TestMethod]
        public async Task Buy_FiveBit()
        {
            var sender = Tester[4];
            var list1 = new List<int> {0, 1};
            var list2 = new List<int> {2, 3};
            var list3 = new List<int> {4, 5};
            var list4 = new List<int> {6, 7};
            var list5 = new List<int> {8, 9};

            var totalAmount = list1.Count * list2.Count * list3.Count * list4.Count * list5.Count * Price;
            var bonus = totalAmount.Mul(Bonus).Div(GetRateDenominator());

            var approve = _sideTokenContract.ApproveToken(sender, _lotteryContract.ContractAddress,
                totalAmount, Symbol);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var initBalance = _sideTokenContract.GetUserBalance(sender, Symbol);
            var contractBalance = _sideTokenContract.GetUserBalance(_lotteryContract.ContractAddress, Symbol);
            var sellerBalance = _sideTokenContract.GetUserBalance(SellerAccount, Symbol);

            var stub = _lotteryContract.GetTestStub<LotteryContractContainer.LotteryContractStub>(sender);
            var result = await stub.Buy.SendAsync(new BuyInput
            {
                Type = (int) LotteryType.FiveBit,
                Seller = SellerAccount.ConvertAddress(),
                BetInfos =
                {
                    new BetBody
                    {
                        Bets = {list1}
                    },
                    new BetBody
                    {
                        Bets = {list2}
                    },
                    new BetBody
                    {
                        Bets = {list3}
                    },
                    new BetBody
                    {
                        Bets = {list4}
                    },
                    new BetBody
                    {
                        Bets = {list5}
                    }
                }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var initBalanceAfter = _sideTokenContract.GetUserBalance(sender, Symbol);
            initBalanceAfter.ShouldBe(initBalance - totalAmount);
            var contractBalanceAfter = _sideTokenContract.GetUserBalance(_lotteryContract.ContractAddress, Symbol);
            contractBalanceAfter.ShouldBe(contractBalance + (totalAmount - bonus));
            var sellerBalanceAfter = _sideTokenContract.GetUserBalance(SellerAccount, Symbol);
            sellerBalanceAfter.ShouldBe(sellerBalance + bonus);
            Logger.Info($"Buy amount {totalAmount}, bonus {bonus}");
        }

        [TestMethod]
        public async Task PrepareDraw()
        {
            var period = await _adminLotteryStub.GetCurrentPeriod.CallAsync(new Empty());
            var last = await _adminLotteryStub.GetLatestDrawPeriod.CallAsync(new Empty());
            last.PeriodNumber.ShouldBe(period.PeriodNumber - 1);
            var prepareDrawResult = await _adminLotteryStub.PrepareDraw.SendAsync(new Empty());
            prepareDrawResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var periodsAfter = await _adminLotteryStub.GetCurrentPeriod.CallAsync(new Empty());
            periodsAfter.PeriodNumber.ShouldBe(period.PeriodNumber + 1);
        }

        [TestMethod]
        public async Task Draw()
        {
            var period = await _adminLotteryStub.GetCurrentPeriod.CallAsync(new Empty());
            var latest = await _adminLotteryStub.GetLatestDrawPeriod.CallAsync(new Empty());
            long input;
            if (period.PeriodNumber.Equals(1))
            {
                input = 1;
            }
            else
            {
                input = latest.PeriodNumber + 1;
                latest.PeriodNumber.ShouldBe(period.PeriodNumber - 1);
            }

            var prepareDrawResult = await _adminLotteryStub.PrepareDraw.SendAsync(new Empty());
            prepareDrawResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var result = await _adminLotteryStub.Draw.SendAsync(new Int64Value {Value = input});
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var info = await _adminLotteryStub.GetPeriod.CallAsync(new Int64Value {Value = input});
            Logger.Info($"{latest.PeriodNumber + 1}: {info.LuckyNumber},{info.DrawBlockNumber}");

            var lastAfter = await _adminLotteryStub.GetLatestDrawPeriod.CallAsync(new Empty());
            lastAfter.PeriodNumber.ShouldBe(latest.PeriodNumber + 1);
            var periodsAfter = await _adminLotteryStub.GetCurrentPeriod.CallAsync(new Empty());
            periodsAfter.PeriodNumber.ShouldBe(period.PeriodNumber + 1);
        }

        [TestMethod]
        public async Task TakeReward()
        {
            var sender = Tester[1];
            long rewardAmount = 0;
            var balance = _sideTokenContract.GetUserBalance(sender, Symbol);
            var contractBalance = _sideTokenContract.GetUserBalance(_lotteryContract.ContractAddress, Symbol);

            var stub = _lotteryContract.GetTestStub<LotteryContractContainer.LotteryContractStub>(sender);
            var rewardedLotteries = await stub.GetRewardedLotteries.CallAsync(new GetLotteriesInput
            {
                Offset = 0,
                Limit = 10
            });
            foreach (var id in rewardedLotteries.Lotteries.Select(i => i.Id).ToList())
            {
                var result = await stub.TakeReward.SendAsync(new TakeRewardInput
                {
                    LotteryId = id
                });
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                var info = await stub.GetLottery.CallAsync(new GetLotteryInput {LotteryId = id});

                var reward = info.Lottery.Reward;
                rewardAmount = rewardAmount + reward;
                info.Lottery.Cashed.ShouldBeTrue();
            }

            var afterBalance = _sideTokenContract.GetUserBalance(sender, Symbol);
            var afterContractBalance = _sideTokenContract.GetUserBalance(_lotteryContract.ContractAddress, Symbol);
            afterBalance.ShouldBe(balance + rewardAmount);
            afterContractBalance.ShouldBe(contractBalance - rewardAmount);
            Logger.Info(
                $"{sender} before reward balance: {balance}, after: {afterBalance}, reward amount: {rewardAmount}");
        }

        [TestMethod]
        public async Task TakeReward_UnRewardLotteryId()
        {
            var sender = Tester[2];
            var id = 2;
            var balance = _sideTokenContract.GetUserBalance(sender, Symbol);
            var stub = _lotteryContract.GetTestStub<LotteryContractContainer.LotteryContractStub>(sender);
            var result = await stub.TakeReward.SendAsync(new TakeRewardInput
            {
                LotteryId = id
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = _sideTokenContract.GetUserBalance(sender, Symbol);
            afterBalance.ShouldBe(balance);
            Logger.Info($"{sender} before reward balance: {balance}, after: {afterBalance}");

            var lotteryInfo = await stub.GetLottery.CallAsync(new GetLotteryInput
            {
                LotteryId = id
            });
            lotteryInfo.Lottery.Cashed.ShouldBeTrue();
        }

        [TestMethod]
        public async Task TakeReward_Expired()
        {
            var sender = Tester[1];
            var id = 15;
            var balance = _sideTokenContract.GetUserBalance(sender, Symbol);
            var stub = _lotteryContract.GetTestStub<LotteryContractContainer.LotteryContractStub>(sender);
            var result = await stub.TakeReward.SendAsync(new TakeRewardInput
            {
                LotteryId = id
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.TransactionResult.Error.ShouldContain("Cannot cash expired lottery.");
            var afterBalance = _sideTokenContract.GetUserBalance(sender, Symbol);
            afterBalance.ShouldBe(balance);
            Logger.Info($"{sender} before reward balance: {balance}, after: {afterBalance}");

            var lotteryInfo = await stub.GetLottery.CallAsync(new GetLotteryInput
            {
                LotteryId = id
            });
            lotteryInfo.Lottery.Expired.ShouldBeTrue();
        }

        [TestMethod]
        public async Task TakeReward_Tester()
        {
            foreach (var sender in Tester)
            {
                long rewardAmount = 0;
                var balance = _sideTokenContract.GetUserBalance(sender, Symbol);
                var contractBalance = _sideTokenContract.GetUserBalance(_lotteryContract.ContractAddress, Symbol);

                var stub = _lotteryContract.GetTestStub<LotteryContractContainer.LotteryContractStub>(sender);
                var rewardedLotteries = await stub.GetRewardedLotteries.CallAsync(new GetLotteriesInput
                {
                    Offset = 0,
                    Limit = 10
                });
                foreach (var id in rewardedLotteries.Lotteries.Select(i => i.Id).ToList())
                {
                    var result = await stub.TakeReward.SendAsync(new TakeRewardInput
                    {
                        LotteryId = id
                    });
                    result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                    var info = await stub.GetLottery.CallAsync(new GetLotteryInput {LotteryId = id});

                    var reward = info.Lottery.Reward;
                    rewardAmount = rewardAmount + reward;
                    info.Lottery.Cashed.ShouldBeTrue();
                }

                var afterBalance = _sideTokenContract.GetUserBalance(sender, Symbol);
                var afterContractBalance = _sideTokenContract.GetUserBalance(_lotteryContract.ContractAddress, Symbol);
                afterBalance.ShouldBe(balance + rewardAmount);
                afterContractBalance.ShouldBe(contractBalance - rewardAmount);
                Logger.Info(
                    $"{sender} before reward balance: {balance}, after: {afterBalance}, reward amount: {rewardAmount}");
            }
        }

        [TestMethod]
        public async Task GetPeriods()
        {
            var currentPeriod = await _adminLotteryStub.GetCurrentPeriod.CallAsync(new Empty());
            var info = await _adminLotteryStub.GetPeriods.CallAsync(new GetPeriodsInput
            {
                StartPeriodNumber = currentPeriod.PeriodNumber, Limit = 5
            });
            Logger.Info($"Periods {info.Periods}");
        }

        [TestMethod]
        public async Task GetCurrentPeriodInfo()
        {
            var period = await _adminLotteryStub.GetCurrentPeriod.CallAsync(new Empty());
            var last = await _adminLotteryStub.GetLatestDrawPeriod.CallAsync(new Empty());
            last.PeriodNumber.ShouldBe(period.PeriodNumber - 1);
            Logger.Info($"Current period {period}");
        }

        [TestMethod]
        public async Task CheckRewardedLotteries()
        {
            foreach (var tester in Tester)
            {
                Logger.Info(tester);
                var stub = _lotteryContract.GetTestStub<LotteryContractContainer.LotteryContractStub>(tester);
                var lotteries = await stub.GetRewardedLotteries.CallAsync(new GetLotteriesInput
                {
                    Offset = 0,
                    Limit = 50
                });

                foreach (var id in lotteries.Lotteries.Select(i => i.Id).ToList())
                {
                    var lotteryInfo = await stub.GetLottery.CallAsync(new GetLotteryInput
                    {
                        LotteryId = id
                    });
                    Logger.Info($"GetLottery: {lotteryInfo.Lottery}");
                }
            }

            var result = await _adminLotteryStub.GetRewardedLotteries.CallAsync(new GetLotteriesInput
            {
                Offset = 0,
                Limit = 10
            });
            Logger.Info($"{result.Lotteries}");
        }

        [TestMethod]
        public async Task CheckRewardedLotteries_OneUser()
        {
            var tester = Tester[4];
            var stub = _lotteryContract.GetTestStub<LotteryContractContainer.LotteryContractStub>(tester);
            var lotteries = await stub.GetRewardedLotteries.CallAsync(new GetLotteriesInput
            {
                Offset = 0,
                Limit = 10
            });

            foreach (var id in lotteries.Lotteries.Select(i => i.Id).ToList())
            {
                var lotteryInfo = await stub.GetLottery.CallAsync(new GetLotteryInput
                {
                    LotteryId = id
                });
                Logger.Info($"GetLottery: {lotteryInfo.Lottery}");
            }
        }

        [TestMethod]
        public async Task CheckLotteries()
        {
            foreach (var tester in Tester)
            {
                Logger.Info(tester);
                var stub = _lotteryContract.GetTestStub<LotteryContractContainer.LotteryContractStub>(tester);
                var lotteries = await stub.GetLotteries.CallAsync(new GetLotteriesInput
                {
                    Offset = 0,
                    Limit = 50
                });
                Logger.Info(
                    $" no reward lotteries count: {lotteries.Lotteries.Where(l => l.Cashed.Equals(false)).ToList().Count}");

                foreach (var id in lotteries.Lotteries.Select(i => i.Id).ToList())
                {
                    var lotteryInfo = await stub.GetLottery.CallAsync(new GetLotteryInput
                    {
                        LotteryId = id
                    });
                    Logger.Info($"GetLottery: {lotteryInfo.Lottery}");
                }
            }

            var initLottery = await _adminLotteryStub.GetLotteries.CallAsync(new GetLotteriesInput
            {
                Offset = 0,
                Limit = 50
            });
//            Logger.Info($"{initLottery.Lotteries}");

            foreach (var id in initLottery.Lotteries.Select(i => i.Id).ToList())
            {
                var lotteryInfo = await _adminLotteryStub.GetLottery.CallAsync(new GetLotteryInput
                {
                    LotteryId = id
                });
                Logger.Info($"{lotteryInfo.Lottery}");
            }
        }

        [TestMethod]
        public async Task CheckLotteries_OneTester()
        {
            var tester = Tester[2];
            var stub = _lotteryContract.GetTestStub<LotteryContractContainer.LotteryContractStub>(tester);
            var lotteries = await stub.GetLotteries.CallAsync(new GetLotteriesInput
            {
                Offset = 0,
                Limit = 20
            });
//                Logger.Info($"GetLotteries: {lottery.Lotteries}");

            foreach (var id in lotteries.Lotteries.Select(i => i.Id).ToList())
            {
                var lotteryInfo = await stub.GetLottery.CallAsync(new GetLotteryInput
                {
                    LotteryId = id
                });
                Logger.Info($"GetLottery: {lotteryInfo.Lottery}");
            }
            
            foreach (var id in lotteries.Lotteries.Select(i => i.Id).ToList())
            {
                var lotteryInfo = await _adminLotteryStub.GetLottery.CallAsync(new GetLotteryInput
                {
                    LotteryId = id
                });
                Logger.Info($"GetLottery: {lotteryInfo.Lottery}");
            }
        }

        [TestMethod]
        public async Task GetLatestCashedLottery()
        {
            var latestCashedLottery = await _adminLotteryStub.GetLatestCashedLottery.CallAsync(new Empty());
            Logger.Info($"{latestCashedLottery}");
        }

        [TestMethod]
        public async Task GetRewardAmountsBoard()
        {
            var res = await _adminLotteryStub.GetRewardAmountsBoard.CallAsync(new Empty());
            Logger.Info(res);
        }

        [TestMethod]
        public async Task GetPeriodCountBoard()
        {
            var res = await _adminLotteryStub.GetPeriodCountBoard.CallAsync(new Empty());
            Logger.Info(res);
        }

        [TestMethod]
        public async Task GetTotalPeriodCount()
        {
            foreach (var account in Tester)
            {
//                var account = Tester[1];
                var totalPeriodCount = await _adminLotteryStub.GetTotalPeriodCount.CallAsync(account.ConvertAddress());
                Logger.Info($"{account} total period amount {totalPeriodCount}");
            }
        }

        [TestMethod]
        public async Task GetTotalRewardAmount()
        {
            foreach (var account in Tester)
            {
                var total = await _adminLotteryStub.GetTotalRewardAmount.CallAsync(account.ConvertAddress());
                Logger.Info($"{account} total reward amount {total}");
            }
        }

        [TestMethod]
        public async Task SetBonusRate()
        {
            var stub = _lotteryContract.GetTestStub<LotteryContractContainer.LotteryContractStub>(SellerAccount);
            var result = await stub.SetBonusRate.SendAsync(new Int32Value {Value = 1});
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var bonus = await stub.GetBonusRate.CallAsync(new Empty());
            bonus.Rate.ShouldBe(1);
        }

        [TestMethod]
        public async Task SetCashDuration()
        {
            var result = await _adminLotteryStub.SetCashDuration.SendAsync(new Int32Value {Value = 30});
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var cashDuration = await _adminLotteryStub.GetCashDuration.CallAsync(new Empty());
            cashDuration.Value.ShouldBe(30);
        }

        [TestMethod]
        public async Task SetAdmin()
        {
            var stub = _lotteryContract.GetTestStub<LotteryContractContainer.LotteryContractStub>(SellerAccount);
            var result = await stub.SetAdmin.SendAsync(InitAccount.ConvertAddress());
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var admin = await stub.GetAdmin.CallAsync(new Empty());
            admin.ShouldBe(InitAccount.ConvertAddress());
        }

        [TestMethod]
        public void CheckBalance()
        {
            var account = "25hBYo1mVRtzxeWo91K5PwxGjfDFFCwRZ48aqhXQ8Rw8wq2GTj";
            var sellerBalance = _sideTokenContract.GetUserBalance(SellerAccount, Symbol);
            _sideTokenContract.TransferBalance(InitAccount, _lotteryContract.ContractAddress, 1000000000000, Symbol);
            _sideTokenContract.TransferBalance(InitAccount, account, 100_00000000, Symbol);
            _sideTokenContract.TransferBalance(InitAccount, account, 100_00000000);

            var contractBalance = _sideTokenContract.GetUserBalance(_lotteryContract.ContractAddress, Symbol);
            var initBalance = _sideTokenContract.GetUserBalance(InitAccount, Symbol);

            Logger.Info(
                $"user balance: {initBalance}, contract balance: {contractBalance}, seller balance: {sellerBalance}");
        }

        [TestMethod]
        public async Task GetRewards()
        {
            var rewardInfo = await _adminLotteryStub.GetRewards.CallAsync(new Empty());
            Logger.Info($"Rewards: {rewardInfo.Rewards}");
        }

        [TestMethod]
        [DataRow(10, 1000)]
        public void CheckRate(int bonusRate, int totalAmount)
        {
            var rate = bonusRate.Div(GetRateDenominator());
            var bonus = totalAmount.Mul(bonusRate).Div(GetRateDenominator());
            Logger.Info($"{rate},{bonus}");
        }

        #region acs9 

        [TestMethod]
        public async Task GetProfitConfig()
        {
            var config = await _adminLotteryStub.GetProfitConfig.CallAsync(new Empty());
            Logger.Info(config);
        }

        [TestMethod]
        public async Task GetProfitsAmount()
        {
            var profitAmount = await _adminLotteryStub.GetProfitsAmount.CallAsync(new Empty());
            Logger.Info(profitAmount);
        }

        [TestMethod]
        public async Task RegisterForLotteryProfits()
        {
            var account = InitAccount;
            var amount = 200;
            var approveResult =
                _sideTokenContract.ApproveToken(account, _sideTokenHolderContract.ContractAddress, amount, Symbol);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var holder = _sideGenesisContract.GetTokenHolderStub(account);
            var registerResult =
                await holder.RegisterForProfits.SendAsync(new RegisterForProfitsInput
                {
                    SchemeManager = _lotteryContract.Contract,
                    Amount = amount
                });
            registerResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined,
                registerResult.TransactionResult.Error);
        }

        [TestMethod]
        public async Task TakeContractProfits()
        {
            await GetProfitsAmount();
            var result = await _adminLotteryStub.TakeContractProfits.SendAsync(new TakeContractProfitsInput
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
                var holder = _sideGenesisContract.GetTokenHolderStub(sender);
                var profitMap = await holder.GetProfitsMap.CallAsync(new ClaimProfitsInput
                {
                    SchemeManager = _lotteryContract.Contract,
                    Beneficiary = sender.ConvertAddress()
                });
                Logger.Info($"{sender}:{JsonConvert.SerializeObject(profitMap)}");
            }
        }

        [TestMethod]
        public async Task ClaimProfits()
        {
            var sender = InitAccount;
            var holder = _sideGenesisContract.GetTokenHolderStub(sender);

            var profitMap = await holder.GetProfitsMap.CallAsync(new ClaimProfitsInput
            {
                SchemeManager = _lotteryContract.Contract,
                Beneficiary = sender.ConvertAddress()
            });
            var amount = (profitMap.Value)[Symbol];

            var senderBalance = _sideTokenContract.GetUserBalance(sender, Symbol);
            var claimResult = await holder.ClaimProfits.SendAsync(new ClaimProfitsInput
            {
                SchemeManager = _lotteryContract.Contract,
                Beneficiary = sender.ConvertAddress()
            });
            claimResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var afterClaim = _sideTokenContract.GetUserBalance(sender, Symbol);
            afterClaim.ShouldBe(senderBalance + amount);
        }

        #endregion

        private void InitRewards()
        {
            var tokenInfo = _sideTokenContract.GetTokenInfo(Symbol);
            long pow = BancorHelper.Pow(10, (uint) tokenInfo.Decimals);
            Rewards = new Dictionary<LotteryType, long>
            {
                {LotteryType.Simple, pow.Mul(4)},
                {LotteryType.OneBit, pow.Mul(10)},
                {LotteryType.TwoBit, pow.Mul(100)},
                {LotteryType.ThreeBit, pow.Mul(1000)},
                {LotteryType.FiveBit, pow.Mul(100000)}
            };
        }

        private int GetRateDenominator()
        {
            var result = BancorHelper.Pow(10, RateDecimals);
            return result;
        }

        private void CreateTokenAndIssue()
        {
            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
            {
                Symbol = Symbol,
                TotalSupply = long.MaxValue,
                Decimals = 5,
                Issuer = InitAccount.ConvertAddress(),
                IsBurnable = true,
                TokenName = $"{Symbol} Token"
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _tokenContract.IssueBalance(InitAccount, InitAccount, 100000000_00000, Symbol);
            _tokenContract.IssueBalance(InitAccount, SellerAccount, 10000_00000, Symbol);
        }
    }
}