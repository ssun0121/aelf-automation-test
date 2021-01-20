using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Contracts.LotteryContract;
using AElf.Contracts.Profit;
using AElf.Contracts.TokenHolder;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Automation.LotteryTest
{
    public class Lottery
    {
        private static ILog Logger { get; set; } = Log4NetHelper.GetLogger();

        public string NativeSymbol;
        public string Symbol;
        public int UserTestCount;
        public string SellerAccount;
        private long _price;
        private int _cashDuration;
        private int _bonus;
        private int _profitsRate;
        private const int RateDecimals = 4;
        private string _virtualAddress;
        private int _userCount;
        public Dictionary<LotteryType, long> Rewards { get; set; }
        public List<long> FiveBitId { get; set; }
        public List<long> FiveBitRewardId { get; set; }
        public List<UserInfo> UserInfos { get; set; }


        public readonly string InitAccount;
        public readonly ContractManager Manager;
        public readonly LotteryContract LotteryService;
        public readonly LotteryContractContainer.LotteryContractStub LotteryContractStub;
        public readonly TokenHolderContract TokenHolderService;
        public readonly TokenHolderContractContainer.TokenHolderContractStub TokenHolderContractStub;
        public readonly TokenContract TokenService;

        public Lottery()
        {
            var lotteryContract = ConfigInfo.ReadInformation.LotteryContract;
            var contractServices = GetContractServices(lotteryContract);
            Manager = contractServices.ContractManager;
            TokenService = Manager.Token;
            LotteryService = contractServices.LotteryService;
            TokenHolderService = Manager.TokenHolder;
            TokenHolderContractStub = Manager.TokenHolderStub;
            InitAccount = contractServices.CallAccount;
            NativeSymbol = TokenService.GetPrimaryTokenSymbol();
            LotteryContractStub =
                LotteryService.GetTestStub<LotteryContractContainer.LotteryContractStub>(InitAccount);
            GetContractConfig();
            FiveBitId = new List<long>();
            FiveBitRewardId = new List<long>();
        }

        private ContractServices GetContractServices(string lotteryContract)
        {
            var url = ConfigInfo.ReadInformation.Url;
            var initAccount = ConfigInfo.ReadInformation.InitAccount;
            var password = ConfigInfo.ReadInformation.Password;

            var contractService = new ContractServices(url, initAccount, password,
                lotteryContract);
            return contractService;
        }

        private void GetContractConfig()
        {
            Logger.Info("*** Get test config: ");
            _price = ConfigInfo.ReadInformation.Price;
            _cashDuration = ConfigInfo.ReadInformation.CashDuration;
            _bonus = ConfigInfo.ReadInformation.Bonus;
            _profitsRate = ConfigInfo.ReadInformation.ProfitsRate;
            Symbol = ConfigInfo.ReadInformation.Symbol;
            _userCount = ConfigInfo.ReadInformation.UserCount;
            SellerAccount = ConfigInfo.ReadInformation.SellerAccount;
            UserTestCount = ConfigInfo.ReadInformation.TestUserCount;
        }

        public List<string> GetTestAddress()
        {
            UserInfos = new List<UserInfo>();
            var nodeManager = Manager.NodeManager;
            var config = ConfigInfo.ReadInformation.Config;
            NodeInfoHelper.SetConfig(config);
            var nodesAccount = NodeInfoHelper.Config.Nodes.Select(l => l.Account).ToList();
            var testUsers = nodeManager.ListAccounts().FindAll(a =>
                !a.Equals(SellerAccount) && !a.Equals(InitAccount) && !nodesAccount.Contains(a));

            if (testUsers.Count >= _userCount)
            {
                var users = testUsers.Take(_userCount).ToList();
                foreach (var user in users)
                    UserInfos.Add(new UserInfo(user));
                return users;
            }
            
            var newAccounts = GenerateTestUsers(nodeManager, _userCount - testUsers.Count);
            testUsers.AddRange(newAccounts);
            foreach (var account in testUsers)
            {
                nodeManager.UnlockAccount(account);
                UserInfos.Add(new UserInfo(account));
            }
            return testUsers;
        }

        public List<string> TakeRandomUserAddress(int count, List<string> allUser)
        {
            if (allUser.Count <=count)
                return allUser;
            var numberList = CommonHelper.TakeRandomNumberList(count, 0, allUser.Count-1);
            var randomList = numberList.Select(num => allUser[num]).ToList();
            return randomList;
        }

        public void DrawJob(List<string> tester)
        {
            ExecuteStandaloneTask(new Action[]
            {
                () => AsyncHelper.RunSync(Draw),
                () => AsyncHelper.RunSync(() => TakeReward(tester))
            });
        }

        public void BuyJob(List<string> tester)
        {
            ExecuteStandaloneTask(new Action[]
            {
                () => AsyncHelper.RunSync(() => Buy(tester))
            });
        }

        public void CheckBoard()
        {
            ExecuteStandaloneTask(new Action[]
            {
                () => AsyncHelper.RunSync(GetRewardAmountsBoard),
                () => AsyncHelper.RunSync(GetPeriodCountBoard)
            });
        }

        public async Task GetLotteryContractInfo()
        {
            var lotteryContract = ConfigInfo.ReadInformation.LotteryContract;
            if (lotteryContract == "")
            {
                GetContractConfig();
                Logger.Info("*** Initialize lottery contracts:");
                var result =
                    LotteryService.ExecuteMethodWithResult(LotteryMethod.Initialize,
                        new InitializeInput
                        {
                            TokenSymbol = Symbol,
                            CashDuration = _cashDuration,
                            BonusRate = _bonus,
                            Price = _price,
                            ProfitsRate = _profitsRate
                        });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var schemeLog = result.Logs.First(l => l.Name.Contains(nameof(SchemeCreated))).NonIndexed;
                var schemeInfo = SchemeCreated.Parser.ParseFrom(ByteString.FromBase64(schemeLog));
                Logger.Info(schemeInfo);
                _virtualAddress = schemeInfo.VirtualAddress.ToBase58();

                Logger.Info("*** Transfer to lottery contract:");
                TokenService.TransferBalance(InitAccount, LotteryService.ContractAddress, 10000000000000, Symbol);
            }
            else
            {
                Logger.Info($"*** Get lottery contracts info {lotteryContract}");
//                var profit = Manager.ProfitStub;
                var token = await LotteryContractStub.GetTokenSymbol.CallAsync(new Empty());
                var cash = await LotteryContractStub.GetCashDuration.CallAsync(new Empty());
                var price = await LotteryContractStub.GetPrice.CallAsync(new Empty());
                var bonus = await LotteryContractStub.GetBonusRate.CallAsync(new Empty());
                var profitRate = await LotteryContractStub.GetProfitsRate.CallAsync(new Empty());
                var schemeInfo = await TokenHolderContractStub.GetScheme.CallAsync(LotteryService.Contract);
//                var virtualAddress = await profit.GetSchemeAddress.CallAsync(new SchemePeriod
//                    {Period = 0, SchemeId = schemeInfo.SchemeId});
                var virtualAddress = ConfigInfo.ReadInformation.VirtualAddress;

                Symbol = token.Value;
                _cashDuration = cash.Value;
                _price = price.Value;
                _bonus = bonus.Rate;
                _profitsRate = profitRate.Rate;
                _virtualAddress = virtualAddress;
            }

            Logger.Info("*** InitRewards: ");
            InitRewards();
        }

        private async Task Buy(IEnumerable<string> testers)
        {
            foreach (var sender in testers)
            {
                var betInfos = new List<BetBody>();
                var countList = new List<int>();
                var type = CommonHelper.RandomEnumValue<LotteryType>();
                switch (type)
                {
                    case LotteryType.Simple:
                        countList = CommonHelper.TakeRandomNumberList(2, 1, 4, true);
                        betInfos = new List<BetBody>
                        {
                            new BetBody {Bets = {CommonHelper.TakeRandomNumberList(countList.First(), 0, 3)}},
                            new BetBody {Bets = {CommonHelper.TakeRandomNumberList(countList.Last(), 0, 3)}}
                        };
                        break;
                    case LotteryType.OneBit:
                        countList = CommonHelper.TakeRandomNumberList(1, 1, 10, true);
                        betInfos = new List<BetBody>
                        {
                            new BetBody {Bets = {CommonHelper.TakeRandomNumberList(countList.First(), 0, 9)}}
                        };
                        break;
                    case LotteryType.TwoBit:
                        countList = CommonHelper.TakeRandomNumberList(2, 1, 10, true);
                        betInfos = new List<BetBody>
                        {
                            new BetBody {Bets = {CommonHelper.TakeRandomNumberList(countList.First(), 0, 9)}},
                            new BetBody {Bets = {CommonHelper.TakeRandomNumberList(countList.Last(), 0, 9)}}
                        };
                        break;
                    case LotteryType.ThreeBit:
                        countList = CommonHelper.TakeRandomNumberList(3, 1, 10, true);
                        betInfos = new List<BetBody>
                        {
                            new BetBody {Bets = {CommonHelper.TakeRandomNumberList(countList[0], 0, 9)}},
                            new BetBody {Bets = {CommonHelper.TakeRandomNumberList(countList[1], 0, 9)}},
                            new BetBody {Bets = {CommonHelper.TakeRandomNumberList(countList[2], 0, 9)}}
                        };
                        break;
                    case LotteryType.FiveBit:
                        countList = CommonHelper.TakeRandomNumberList(5, 1, 3, true);
                        betInfos = new List<BetBody>
                        {
                            new BetBody {Bets = {CommonHelper.TakeRandomNumberList(countList[0], 0, 9)}},
                            new BetBody {Bets = {CommonHelper.TakeRandomNumberList(countList[1], 0, 9)}},
                            new BetBody {Bets = {CommonHelper.TakeRandomNumberList(countList[2], 0, 9)}},
                            new BetBody {Bets = {CommonHelper.TakeRandomNumberList(countList[3], 0, 9)}},
                            new BetBody {Bets = {CommonHelper.TakeRandomNumberList(countList[4], 0, 9)}}
                        };
                        break;
                }

                var totalCount = countList.Aggregate<int, long>(1, (current, count) => count * current);
                var totalAmount = totalCount * _price;
                var bonus = totalAmount.Mul(_bonus).Div(GetRateDenominator());
                var profit = totalAmount.Mul(_profitsRate).Div(GetRateDenominator());
                CheckBalance(totalAmount, sender, Symbol);

                var initBalance = TokenService.GetUserBalance(sender, Symbol);
                var contractBalance = TokenService.GetUserBalance(LotteryService.ContractAddress, Symbol);
                var sellerBalance = TokenService.GetUserBalance(SellerAccount, Symbol);
                var profitBalance = TokenService.GetUserBalance(_virtualAddress, Symbol);

                TokenService.SetAccount(sender);
                var approve = TokenService.ApproveToken(sender, LotteryService.ContractAddress,
                    totalAmount, Symbol);
                approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                var stub = LotteryService.GetTestStub<LotteryContractContainer.LotteryContractStub>(sender);
                var result = await stub.Buy.SendAsync(new BuyInput
                {
                    Type = (int) type,
                    Seller = SellerAccount.ConvertAddress(),
                    BetInfos = {betInfos}
                });
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                if (type.Equals(LotteryType.FiveBit))
                {
                    var lotteries = await stub.GetLotteries.CallAsync(new GetLotteriesInput
                    {
                        Offset = 0,
                        Limit = 50
                    });
                    FiveBitId.Add(lotteries.Lotteries.Last().Id);
                }

                var initBalanceAfter = TokenService.GetUserBalance(sender, Symbol);
//                initBalanceAfter.ShouldBe(initBalance - totalAmount);
                var profitAfter = TokenService.GetUserBalance(_virtualAddress, Symbol);
                profitAfter.ShouldBe(profitBalance + profit);
                var contractBalanceAfter = TokenService.GetUserBalance(LotteryService.ContractAddress, Symbol);
//                contractBalanceAfter.ShouldBe(contractBalance + (totalAmount - bonus - profit));
                var sellerBalanceAfter = TokenService.GetUserBalance(SellerAccount, Symbol);
                sellerBalanceAfter.ShouldBe(sellerBalance + bonus);
                Logger.Info($"*** User {sender} spent {totalAmount} on lottery, bonus {bonus}, profit {profit}");
                var userInfo = UserInfos.First(u => u.User.Equals(sender));
                userInfo.Balance = initBalanceAfter;
                userInfo.SpentAmount = userInfo.SpentAmount + totalAmount;
            }
        }

        private async Task Draw()
        {
            var period = await LotteryContractStub.GetCurrentPeriod.CallAsync(new Empty());
            var latest = await LotteryContractStub.GetLatestDrawPeriod.CallAsync(new Empty());
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

            var prepareDrawResult = await LotteryContractStub.PrepareDraw.SendAsync(new Empty());
            prepareDrawResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var result = await LotteryContractStub.Draw.SendAsync(new Int64Value {Value = input});
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var info = await LotteryContractStub.GetPeriod.CallAsync(new Int64Value {Value = input});
            Logger.Info(
                $"*** Period {latest.PeriodNumber + 1} LuckNumber: {info.LuckyNumber}; DrawBlockNumber: {info.DrawBlockNumber}");

            var lastAfter = await LotteryContractStub.GetLatestDrawPeriod.CallAsync(new Empty());
            lastAfter.PeriodNumber.ShouldBe(latest.PeriodNumber + 1);
            var periodsAfter = await LotteryContractStub.GetCurrentPeriod.CallAsync(new Empty());
            periodsAfter.PeriodNumber.ShouldBe(period.PeriodNumber + 1);
        }

        private async Task TakeReward(IEnumerable<string> testers)
        {
            foreach (var sender in testers)
            {
                long rewardAmount = 0;
                var balance = TokenService.GetUserBalance(sender, Symbol);
                var contractBalance = TokenService.GetUserBalance(LotteryService.ContractAddress, Symbol);

                var stub = LotteryService.GetTestStub<LotteryContractContainer.LotteryContractStub>(sender);
                var rewardedLotteries = await stub.GetRewardedLotteries.CallAsync(new GetLotteriesInput
                {
                    Offset = 0,
                    Limit = 50
                });
                CheckBalance(Rewards[LotteryType.FiveBit], LotteryService.ContractAddress, Symbol);

                foreach (var id in rewardedLotteries.Lotteries.Select(i => i.Id).ToList())
                {
                    var result = await stub.TakeReward.SendAsync(new TakeRewardInput
                    {
                        LotteryId = id
                    });
                    result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                    var info = await stub.GetLottery.CallAsync(new GetLotteryInput {LotteryId = id});

                    var reward = info.Lottery.Reward;
                    if (info.Lottery.Type.Equals((int) LotteryType.Simple))
                    {
                        (reward % Rewards[(LotteryType) info.Lottery.Type]).ShouldBe(0);
                    }
                    else
                        reward.ShouldBe(Rewards[(LotteryType) info.Lottery.Type]);

                    rewardAmount = rewardAmount + reward;
                    info.Lottery.Cashed.ShouldBeTrue();
                    if (info.Lottery.Type.Equals((int) LotteryType.FiveBit))
                    {
                        FiveBitRewardId.Add(id);
                        Logger.Info($" **** {sender} win a top prize! Id: {info.Lottery.Id}");
                    }
                }

                var afterBalance = TokenService.GetUserBalance(sender, Symbol);
                var afterContractBalance = TokenService.GetUserBalance(LotteryService.ContractAddress, Symbol);
//                afterBalance.ShouldBe(balance + rewardAmount);
//                afterContractBalance.ShouldBe(contractBalance - rewardAmount);

                Logger.Info(
                    $"*** {sender} before reward balance: {balance}, after: {afterBalance}, reward amount: {rewardAmount}\n");

                foreach (var lottery in rewardedLotteries.Lotteries)
                    Logger.Info($"{lottery.Id}: bet infos=>{lottery.BetInfos} reward=>{lottery.Reward}\n");
                var userInfo = UserInfos.First(u => u.User.Equals(sender));
                userInfo.Balance = afterBalance;
                userInfo.RewardAmount = userInfo.RewardAmount + rewardAmount;
            }
        }

        private async Task GetRewardAmountsBoard()
        {
            var res = await LotteryContractStub.GetRewardAmountsBoard.CallAsync(new Empty());
            foreach (var rewardAmount in res.RewardAmountList)
                Logger.Info($"*** The RewardAmount Board: " +
                            $"\naddress: {rewardAmount.Address} ==> reward: {rewardAmount.Amount}");
        }

        private async Task GetPeriodCountBoard()
        {
            var res = await LotteryContractStub.GetPeriodCountBoard.CallAsync(new Empty());
            foreach (var periodCount in res.PeriodCountList)
                Logger.Info($"*** The PeriodCount Board: " +
                            $"\naddress: {periodCount.Address} ==> period: {periodCount.Count}");
        }

        public void CheckNativeSymbolBalance(List<string> testers)
        {
            foreach (var tester in testers)
            {
                var balance = TokenService.GetUserBalance(tester);
                if (balance < 10_00000000)
                    TokenService.TransferBalance(InitAccount, tester, 100_00000000);
            }
        }

        public void CheckBalance(long needBalance, string sender, string symbol)
        {
            var balance = TokenService.GetUserBalance(sender, symbol);
            if (balance <= needBalance)
                TokenService.TransferBalance(InitAccount, sender, needBalance, symbol);
        }

        public void CalculateRate()
        {
            if (FiveBitId.Count <= 0) return;
            var rate = FiveBitRewardId.Count / FiveBitId.Count;
            Logger.Info(
                $"*** Top prize rate: {rate}, FiveBitCount: {FiveBitId.Count}, RewardIdCount: {FiveBitRewardId.Count}");
        }

        public void CheckUserRewardRate()
        {
            foreach (var userInfo in UserInfos)
            {
                var rate = userInfo.SpentAmount != 0 ? (decimal)userInfo.RewardAmount / userInfo.SpentAmount: 0;
                Logger.Info($"*** {userInfo.User} total spend {userInfo.SpentAmount}; total reward {userInfo.RewardAmount}; rate: {rate}");
            }
        }

        private int GetRateDenominator()
        {
            var result = BancorHelper.Pow(10, RateDecimals);
            return result;
        }

        private void InitRewards()
        {
            var tokenInfo = TokenService.GetTokenInfo(Symbol);
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

        private List<string> GenerateTestUsers(INodeManager manager, int count)
        {
            var accounts = new List<string>();
            Parallel.For(0, count, i =>
            {
                var account = manager.NewAccount();
                accounts.Add(account);
            });

            return accounts;
        }

        private void ExecuteStandaloneTask(IEnumerable<Action> actions, int sleepSeconds = 0,
            bool interrupted = false)
        {
            foreach (var action in actions)
                try
                {
                    action.Invoke();
                }
                catch (Exception e)
                {
                    Logger.Error($"Execute action {action.Method.Name} got exception: {e.Message}", e);
                    if (interrupted)
                        break;
                }

            if (sleepSeconds != 0)
                Thread.Sleep(1000 * sleepSeconds);
        }
    }
}