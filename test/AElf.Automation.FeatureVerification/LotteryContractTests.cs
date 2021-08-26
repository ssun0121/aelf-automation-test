using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Lottery;
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
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class LotteryContractTests
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }

        private TokenContract _tokenContract;
        private GenesisContract _genesisContract;
        private LotteryContract _lotteryContract;
        private string InitAccount { get; } = "2qjoi1ZxwmH2XqQFyMhykjW364Ce2cBxxRp47zrvZFT4jqW8Ru";
        private string TestAccount { get; } = "ZrAFaqdr79MWYkxA49Hp2LUdSVHdP6fJh3kDw4jmgC7HTgrni";
        //2qjoi1ZxwmH2XqQFyMhykjW364Ce2cBxxRp47zrvZFT4jqW8Ru
        //zptx91dhHVJjJRxf5Wg5KAoMrDrWX6i1H2FAyKAiv2q8VZfbg
        private readonly List<string> _tester = new List<string>
        {
            "ZrAFaqdr79MWYkxA49Hp2LUdSVHdP6fJh3kDw4jmgC7HTgrni",
            "bBEDoBnPK28bYFf1M28hYLFVuGnkPkNR6r59XxGNmYfr7aRff",
            "FHdcx45K5kovWsAKSb3rrdyNPFus8eoJ1XTQE7aXFHTgfpgzN",
            "Gazx8ei5CnzPRnaFGojZemak8VJsC5ETqzC1CGqNb76ZM3BMY",
            "Muca5ZVorWCV51BNATadyC6f72871aZm2WnHfsrkioUHwyP8j",
            "bP7RkGBN5vK1wDFjuUbWh49QVLMWAWMuccYK1RSh9hRrVcP7v"
        };

        private readonly List<string> _tester2 = new List<string>();

        private long testerCount = 100;
        //2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS
        //DHo2K7oUXXq3kJRs1JpuwqBJP56gqoaeSKFfuvr9x8svf3vEJ
        private string _lotteryContractAddress = "DHo2K7oUXXq3kJRs1JpuwqBJP56gqoaeSKFfuvr9x8svf3vEJ";

        private static string RpcUrl { get; } = "127.0.0.1:8000";
        private string Symbol { get; } = "ELF";
        private bool isNeedInit = false;
        private long AllAward = 7300_00000000;

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("ContractTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-online-stage-main");
            NodeManager = new NodeManager(RpcUrl);
            Logger.Info(RpcUrl);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount,"12345678");
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
            _lotteryContract = _lotteryContractAddress == ""
                ? new LotteryContract(NodeManager, InitAccount)
                : new LotteryContract(NodeManager, InitAccount, _lotteryContractAddress);
            Logger.Info($"Lottery contract : {_lotteryContract.ContractAddress}");
            if (isNeedInit)
                InitializeLotteryContract();
            Logger.Info(_tokenContract.GetUserBalance(_lotteryContract.ContractAddress));
            Logger.Info(_tokenContract.GetUserBalance(InitAccount));
            // _tokenContract.TransferBalance(InitAccount, _lotteryContract.ContractAddress, 15000_00000000);
            // for (int i = 0; i < testerCount; i++)
            // {
            //     var account = NodeManager.AccountManager.NewAccount();
            //     _tester2.Add(account);
            // }
           //2nyC8hqq3pGnRu8gJzCsTaxXB6snfGxmL2viimKXgEfYWGtjEh
           //DHo2K7oUXXq3kJRs1JpuwqBJP56gqoaeSKFfuvr9x8svf3vEJ
        }

        [TestMethod]
        public void InitializeLotteryContract()
        {
            var result = _lotteryContract.ExecuteMethodWithResult(LotteryMethod.Initialize, new InitializeInput
            {
                Admin = InitAccount.ConvertAddress(),
                StartTimestamp = Timestamp.FromDateTime(new DateTime(2021,8,23,22,00,00,00).ToUniversalTime()),
                ShutdownTimestamp = Timestamp.FromDateTime(new DateTime(2021,8,26,12,00,00,00).ToUniversalTime()),
                RedeemTimestamp = Timestamp.FromDateTime(new DateTime(2021,8,26,12,00,00,00).ToUniversalTime()),
                StopRedeemTimestamp = Timestamp.FromDateTime(new DateTime(2021,8,27,15,00,00,00).ToUniversalTime())
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
        
        [TestMethod]
        public void InitializeLotteryContract_Default()
        {
            var result = _lotteryContract.ExecuteMethodWithResult(LotteryMethod.Initialize, new InitializeInput
            {
                Admin = InitAccount.ConvertAddress(),
                StartTimestamp = DateTime.UtcNow.Add(TimeSpan.FromSeconds(10)).ToTimestamp(),
                ShutdownTimestamp = DateTime.UtcNow.Add(TimeSpan.FromHours(5)).ToTimestamp(),
                RedeemTimestamp = DateTime.UtcNow.Add(TimeSpan.FromHours(5)).ToTimestamp(),
                StopRedeemTimestamp = DateTime.UtcNow.Add(TimeSpan.FromHours(7)).ToTimestamp()
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void Stake(string buyer, long amount)
        {
            _tokenContract.TransferBalance(InitAccount, buyer, 100000_00000000, Symbol);
            _tokenContract.ApproveToken(buyer, _lotteryContractAddress, amount, Symbol);
            var balance = _tokenContract.GetUserBalance(buyer);
            var originStakingAmount =
                _lotteryContract.CallViewMethod<Int64Value>(LotteryMethod.GetStakingAmount,
                    buyer.ConvertAddress());
            _lotteryContract.SetAccount(buyer);
            var result = _lotteryContract.ExecuteMethodWithResult(LotteryMethod.Stake, new Int64Value {Value = amount});
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            if (result.Logs.Length == 2)
            {
                var logs = result.Logs.First(l => l.Name.Contains(nameof(Staked))).NonIndexed;
                var stakeInfo = Staked.Parser.ParseFrom(ByteString.FromBase64(logs));
                stakeInfo.User.ShouldBe(buyer.ConvertAddress());
                stakeInfo.Amount.ShouldBe(amount);
                Logger.Info($"User: {stakeInfo.User},\n" +
                            $"stake amount: {stakeInfo.Amount},\n" +
                            $"{stakeInfo.LotteryCodeList}");
            }

            var stakingAmount =
                _lotteryContract.CallViewMethod<Int64Value>(LotteryMethod.GetStakingAmount,
                    buyer.ConvertAddress());
            stakingAmount.Value.ShouldBe(originStakingAmount.Value + amount);
            Logger.Info(stakingAmount);
            var lotteryCodeListByUserAddress = _lotteryContract.CallViewMethod<Int64List>(
                LotteryMethod.GetLotteryCodeListByUserAddress, buyer.ConvertAddress());
            Logger.Info(lotteryCodeListByUserAddress);
            var afterBalance = _tokenContract.GetUserBalance(buyer);
            balance.ShouldBe(afterBalance + amount);
            var ownLottery =
                _lotteryContract.CallViewMethod<OwnLottery>(LotteryMethod.GetOwnLottery, buyer.ConvertAddress());
            Logger.Info(buyer);
            Logger.Info(ownLottery);
            ownLottery.LotteryCodeList.ShouldBe(lotteryCodeListByUserAddress.Value);
            ownLottery.TotalStakingAmount.ShouldBe(stakingAmount.Value);
        }

        [TestMethod]
        public void MultiStake()
        {
            var originBalance = _tokenContract.GetUserBalance(_lotteryContract.ContractAddress);
            long totalAmount = 0;
            for (int i = 0; i < 1; i++)
            {
                long a = 0;
                foreach (var tester in _tester)
                {
                    var random = CommonHelper.GenerateRandomNumber(100, 20100);
                    var amount = (long) random * 100000000;
                    // var amount = 20100_00000000;
                }

                totalAmount += a;
            }

            var balance = _tokenContract.GetUserBalance(_lotteryContractAddress);
            balance.ShouldBe(originBalance + totalAmount);
            Logger.Info(balance);
        }

        [TestMethod]
        public void GetTotalLotteryCount()
        {
            var count = _lotteryContract.CallViewMethod<Int64Value>(LotteryMethod.GetTotalLotteryCount, new Empty());
            Logger.Info(count);
        }

        [TestMethod]
        public void GetCurrentPeriodId()
        {
            var periodId = _lotteryContract.CallViewMethod<Int32Value>(LotteryMethod.GetCurrentPeriodId, new Empty());
            Logger.Info(periodId);
        }

        [TestMethod]
        public void GetOwnLottery()
        {
            long totalAwardAmount = 0;
            var periodId = _lotteryContract.CallViewMethod<Int32Value>(LotteryMethod.GetCurrentPeriodId, new Empty());
            _tester.Add(InitAccount);
            foreach (var test in _tester)
            {
                var ownLottery =
                    _lotteryContract.CallViewMethod<OwnLottery>(LotteryMethod.GetOwnLottery, test.ConvertAddress());
                Logger.Info(test);
                Logger.Info(ownLottery);
                totalAwardAmount += ownLottery.TotalAwardAmount;
            }

            var exceptAwardAmount = (periodId.Value - 1) * AllAward;
            Logger.Info(exceptAwardAmount);

            totalAwardAmount.ShouldBe(exceptAwardAmount);
        }

        [TestMethod]
        public void Claim()
        {
            var periodId = _lotteryContract.CallViewMethod<Int32Value>(LotteryMethod.GetCurrentPeriodId, new Empty());
            Logger.Info(periodId);

            var getAwardList =
                _lotteryContract.CallViewMethod<AwardList>(LotteryMethod.GetAwardList, new GetAwardListInput
                {
                    PeriodId = periodId.Value -1
                });
            Logger.Info(getAwardList);
            var claimer = getAwardList.Value.First().Owner.ToBase58();
            var list = getAwardList.Value.Where(a => a.Owner.Equals(claimer.ConvertAddress())).ToList();
            var balance = _tokenContract.GetUserBalance(claimer);
            var contractBalance = _tokenContract.GetUserBalance(_lotteryContract.ContractAddress);
            var ownLottery =
                _lotteryContract.CallViewMethod<OwnLottery>(LotteryMethod.GetOwnLottery, claimer.ConvertAddress());
            Logger.Info(ownLottery);
            var exceptAwardAmount = ownLottery.TotalAwardAmount - ownLottery.ClaimedAwardAmount;
            _lotteryContract.SetAccount(claimer);
            var claim = _lotteryContract.ExecuteMethodWithResult(LotteryMethod.Claim, new Empty());
            claim.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logs = claim.Logs.First(l => l.Name.Equals(nameof(Claimed))).NonIndexed;
            var claimed = Claimed.Parser.ParseFrom(ByteString.FromBase64(logs));
            claimed.Amount.ShouldBe(exceptAwardAmount);
            claimed.User.ShouldBe(claimer.ConvertAddress());
            claimed.PeriodId.ShouldBe(periodId.Value);
            foreach (var id in list)
                claimed.ClaimedLotteryCodeList.Value.ShouldContain(id.LotteryCode);

            var afterBalance = _tokenContract.GetUserBalance(claimer);
            afterBalance.ShouldBe(balance + exceptAwardAmount);
            var afterContractBalance = _tokenContract.GetUserBalance(_lotteryContract.ContractAddress);
            afterContractBalance.ShouldBe(contractBalance - exceptAwardAmount);

            var afterOwnLottery =
                _lotteryContract.CallViewMethod<OwnLottery>(LotteryMethod.GetOwnLottery, claimer.ConvertAddress());
            Logger.Info(afterOwnLottery);
            afterOwnLottery.ClaimedAwardAmount.ShouldBe(ownLottery.TotalAwardAmount);

            var afterGetAwardList =
                _lotteryContract.CallViewMethod<AwardList>(LotteryMethod.GetAwardList, new GetAwardListInput
                {
                    PeriodId = periodId.Value - 1,
                    StartIndex = 0,
                    Count = 120
                });
            var afterList = afterGetAwardList.Value.Where(a => a.Owner.Equals(claimer.ConvertAddress())).ToList();
            afterList.All(a => a.IsClaimed).ShouldBeTrue();
            foreach (var award in afterList)
            {
                var lottery =
                    _lotteryContract.CallViewMethod<Lottery>(LotteryMethod.GetLottery, new Int64Value {Value = award.LotteryCode});
                lottery.Owner.ShouldBe(claimer.ConvertAddress());
                lottery.AwardIdList.ShouldContain(award.AwardId);
                Logger.Info($"\nOwner: {lottery.Owner}\n" +
                            $"IssueTime: {lottery.IssueTimestamp}\n" +
                            $"Code: {lottery.LotteryCode}\n" +
                            $"AwardIdList : {lottery.AwardIdList}\n" +
                            $"LatestClaimedAwardId: {lottery.LatestClaimedAwardId}");
            }
        }

        [TestMethod]
        public void Redeem()
        {
            foreach (var tester in _tester)
            {
                var stakingAmount =
                    _lotteryContract.CallViewMethod<Int64Value>(LotteryMethod.GetStakingAmount,
                        tester.ConvertAddress());
                Logger.Info($"{tester} staking amount {stakingAmount.Value}");
                var ownLottery =
                    _lotteryContract.CallViewMethod<OwnLottery>(LotteryMethod.GetOwnLottery, tester.ConvertAddress());
                Logger.Info(ownLottery);
                var balance = _tokenContract.GetUserBalance(tester);
                _lotteryContract.SetAccount(tester);
                var result = _lotteryContract.ExecuteMethodWithResult(LotteryMethod.Redeem, new Empty());
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var afterBalance = _tokenContract.GetUserBalance(tester);
                afterBalance.ShouldBe(balance + stakingAmount.Value);
                
                ownLottery =
                    _lotteryContract.CallViewMethod<OwnLottery>(LotteryMethod.GetOwnLottery, tester.ConvertAddress());
                Logger.Info(ownLottery);
                ownLottery.IsRedeemed.ShouldBeTrue();
                ownLottery.TotalStakingAmount.ShouldBe(0);
            }
        }

        #region Draw

        [TestMethod]
        [DataRow("zptx91dhHVJjJRxf5Wg5KAoMrDrWX6i1H2FAyKAiv2q8VZfbg")]
        public void Draw_OnlyOne(string account)
        {
            var periodId = _lotteryContract.CallViewMethod<Int32Value>(LotteryMethod.GetCurrentPeriodId, new Empty());
            Logger.Info(periodId);
            var totalLottery =
                _lotteryContract.CallViewMethod<Int64Value>(LotteryMethod.GetTotalLotteryCount, new Empty());
            Logger.Info(totalLottery);
            var periodInfo = _lotteryContract.CallViewMethod<PeriodAward>(LotteryMethod.GetPeriodAward,
                new Int64Value {Value = periodId.Value});
            Logger.Info(periodInfo);

            Logger.Info("Get testers' lottery");
            var lotteryCodeListByUserAddress = _lotteryContract.CallViewMethod<Int64List>(
                LotteryMethod.GetLotteryCodeListByUserAddress, account.ConvertAddress());
            Logger.Info(lotteryCodeListByUserAddress);

            var draw = _lotteryContract.ExecuteMethodWithResult(LotteryMethod.Draw,
                new DrawInput
                {
                    PeriodId = periodId.Value
                });
            draw.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var afterPeriodId =
                _lotteryContract.CallViewMethod<Int32Value>(LotteryMethod.GetCurrentPeriodId, new Empty());
            afterPeriodId.Value.ShouldBe(periodId.Value + 1);
            Logger.Info(afterPeriodId);
            var afterTotalLottery =
                _lotteryContract.CallViewMethod<Int64Value>(LotteryMethod.GetTotalLotteryCount, new Empty());
            Logger.Info(afterTotalLottery);
            periodInfo = _lotteryContract.CallViewMethod<PeriodAward>(LotteryMethod.GetPeriodAward,
                new Int64Value {Value = periodId.Value});
            Logger.Info(periodInfo);
            var afterPeriodInfo = _lotteryContract.CallViewMethod<PeriodAward>(LotteryMethod.GetPeriodAward,
                new Int64Value {Value = afterPeriodId.Value});
            Logger.Info(afterPeriodInfo);

            var getAwardList =
                _lotteryContract.CallViewMethod<AwardList>(LotteryMethod.GetAwardList, new GetAwardListInput
                {
                    PeriodId = periodId.Value
                });
            Logger.Info(getAwardList);

            var codeList = getAwardList.Value.Select(a => a.LotteryCode).ToList();
            var b = checkList(codeList);
            b.ShouldBeFalse();
            getAwardList.Value.Count.ShouldBe(totalLottery.Value >= 26 ? 26 : (int) totalLottery.Value);
        }
        
        [TestMethod]
        public void Draw_ManyTester()
        {
            foreach (var tester in _tester)
            {
                Draw_OnlyOne(tester);
            }
        }

        
        [TestMethod]
        [DataRow(0,23)]
        public void GetAward(int startIndex, int count)
        {
            var periodId = _lotteryContract.CallViewMethod<Int32Value>(LotteryMethod.GetCurrentPeriodId, new Empty());
            Logger.Info(periodId);
            
            var allAwardList =
                _lotteryContract.CallViewMethod<AwardList>(LotteryMethod.GetAwardList, new GetAwardListInput
                {
                    PeriodId = periodId.Value -1 
                });
            var codeList = allAwardList.Value.Select(a => a.LotteryCode).ToList();
            var b = checkList(codeList);
            b.ShouldBeFalse();
            Logger.Info(codeList);
            var startAward = (periodId.Value - 2) * allAwardList.Value.Count + 1;
            while (allAwardList.Value.Count > startIndex)
            {
                var getAwardList =
                    _lotteryContract.CallViewMethod<AwardList>(LotteryMethod.GetAwardList, new GetAwardListInput
                    {
                        PeriodId = periodId.Value-1,
                        Count = count,
                        StartIndex = startIndex
                    });
                Logger.Info(getAwardList);
                var endIndex = startAward + count > allAwardList.Value.Count * (periodId.Value - 1)
                    ? allAwardList.Value.Count * (periodId.Value - 1)
                    : startAward + count;
                
                for (var j = startAward ; j < endIndex; j++)
                {
                    var award = _lotteryContract.CallViewMethod<Award>(LotteryMethod.GetAward, new Int64Value {Value = j});
                    Logger.Info(award);
                    getAwardList.Value.ShouldContain(award);
                }
                startIndex += count;
                startAward += count;
            }
        }

        [TestMethod]
        public void GetPeriodAward()
        {
            var periodId = _lotteryContract.CallViewMethod<Int32Value>(LotteryMethod.GetCurrentPeriodId, new Empty());
            Logger.Info(periodId);
            var currentPeriodAwardInfo = _lotteryContract.CallViewMethod<PeriodAward>(LotteryMethod.GetPeriodAward,
                new Int64Value {Value = periodId.Value});
            Logger.Info(currentPeriodAwardInfo);
            var periodAwardInfo = _lotteryContract.CallViewMethod<PeriodAward>(LotteryMethod.GetPeriodAward,
                new Int64Value {Value = periodId.Value - 1});
            Logger.Info(periodAwardInfo);
        }

        [TestMethod]
        public void GetLottery()
        {
            var periodId = _lotteryContract.CallViewMethod<Int32Value>(LotteryMethod.GetCurrentPeriodId, new Empty());
            Logger.Info(periodId);
            var totalLottery =
                _lotteryContract.CallViewMethod<Int64Value>(LotteryMethod.GetTotalLotteryCount, new Empty());
            for (var i = 1; i <= totalLottery.Value; i++)
            {
                var lottery =
                    _lotteryContract.CallViewMethod<Lottery>(LotteryMethod.GetLottery, new Int64Value {Value = i});
              
                Logger.Info(lottery.AwardIdList);
                Logger.Info($"\nOwner: {lottery.Owner}\n" +
                            $"IssueTime: {lottery.IssueTimestamp}\n" +
                            $"Code: {lottery.LotteryCode}\n" +
                            $"AwardIdList : {lottery.AwardIdList}\n" +
                            $"LatestClaimedAwardId: {lottery.LatestClaimedAwardId}");
            }
        }

        [TestMethod]
        public void GetLotteryCode()
        {
            var periodId = _lotteryContract.CallViewMethod<Int32Value>(LotteryMethod.GetCurrentPeriodId, new Empty());
            Logger.Info(periodId);
            var totalLottery =
                _lotteryContract.CallViewMethod<Int64Value>(LotteryMethod.GetTotalLotteryCount, new Empty());
            var getAwardList =
                _lotteryContract.CallViewMethod<AwardList>(LotteryMethod.GetAwardList, new GetAwardListInput
                {
                    PeriodId = periodId.Value - 1,
                    StartIndex = 0,
                    Count = 120
                });
            Logger.Info(getAwardList);
            var codeList = getAwardList.Value.Select(a => a.LotteryCode).ToList();
            var b = checkList(codeList);
            b.ShouldBeFalse();
            getAwardList.Value.Count.ShouldBe(totalLottery.Value >= 26 ? 26 : (int) totalLottery.Value);
        }

        [TestMethod]
        public void GetAwardListByUserAddress()
        {
            var user = "zptx91dhHVJjJRxf5Wg5KAoMrDrWX6i1H2FAyKAiv2q8VZfbg";
            var list = _lotteryContract.CallViewMethod<AwardList>(LotteryMethod.GetAwardListByUserAddress,
                user.ConvertAddress());
            Logger.Info(list);
            var lotteryCodeList =
                _lotteryContract.CallViewMethod<Int64List>(LotteryMethod.GetLotteryCodeListByUserAddress, user.ConvertAddress());
            Logger.Info(lotteryCodeList);
            var awardMap =
                _lotteryContract.CallViewMethod<AwardAmountMap>(LotteryMethod.GetAwardAmountMap, user.ConvertAddress());
            Logger.Info(awardMap);
        }

        #endregion

        #region change time

        [TestMethod]
        public void ResetTimestamp()
        {
            var reset = _lotteryContract.ExecuteMethodWithResult(LotteryMethod.ResetTimestamp, new InitializeInput
            {
                StartTimestamp = Timestamp.FromDateTime(new DateTime(2021,8,23,18,00,00,00).ToUniversalTime()),
                ShutdownTimestamp = Timestamp.FromDateTime(new DateTime(2021,8,26,12,00,00,00).ToUniversalTime()),
                RedeemTimestamp = Timestamp.FromDateTime(new DateTime(2021,8,26,12,00,00,00).ToUniversalTime()),
                StopRedeemTimestamp = Timestamp.FromDateTime(new DateTime(2021,8,27,15,00,00,00).ToUniversalTime())
            });
            reset.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void GetTimestamp()
        {
            var reset = _lotteryContract.CallViewMethod<Timestamp>(LotteryMethod.GetStartTimestamp, new Empty());
            Logger.Info(reset);
        }

        #endregion

        private bool checkList(List<long> list)
        {
            var newList = new long [list.Count];
            var idx = 0;
            foreach (var t in list)
            {
                if (!newList.Contains(t))
                {
                    newList[idx] = t;
                    idx++;
                }
                else
                {
                    return true;
                }
            }

            return false;
        }
    }
}