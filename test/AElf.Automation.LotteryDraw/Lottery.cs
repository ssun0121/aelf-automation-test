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
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Automation.LotteryTest
{
    public class Lottery
    {
        private static ILog Logger { get; set; } = Log4NetHelper.GetLogger();
        public string Symbol;
        public long Price;
        public readonly string Owner;
        public readonly string Password;
        public string LotteryContract;
        public Dictionary<string, int> RewardList;
        public bool OnlyDraw;
        public bool OnlyBuy;
        public int TestUserCount;
        private int _userCount;

        public EnvironmentInfo EnvironmentInfo { get; set; }
        public readonly LotteryDemoContract LotteryService;
        public readonly LotteryDemoContractContainer.LotteryDemoContractStub LotteryStub;

        public readonly TokenContract TokenService;
        public readonly ContractServices ContractServices;

        public Lottery(string rewards,string counts)
        {
            GetConfig();
            GetRewardList(rewards, counts);
            Owner = EnvironmentInfo.Owner;
            Password = EnvironmentInfo.Password;
            ContractServices = GetContractServices(LotteryContract);
            TokenService = ContractServices.TokenService;
            LotteryService = ContractServices.LotteryService;
            LotteryStub = LotteryService.GetTestStub<LotteryDemoContractContainer.LotteryDemoContractStub>(Owner);
            if (LotteryContract == "")
                InitializeLotteryDemoContract();
            if (!TokenService.GetTokenInfo(Symbol).Symbol.Equals(Symbol))
                CreateTokenAndIssue();
        }

        private ContractServices GetContractServices(string lotteryContract)
        {
            var config = NodeInfoHelper.Config;
            var firstNode = config.Nodes.First();
            var contractService = new ContractServices(firstNode.Endpoint, firstNode.Account, firstNode.Password,
                lotteryContract);
            return contractService;
        }

        private void GetConfig()
        {
            var config = ConfigHelper.Config;
            LotteryContract = config.LotteryContract;
            Symbol = config.TokenInfo.Symbol;
            Price = config.TokenInfo.Price;
            OnlyDraw = config.OnlyDraw;
            OnlyBuy = config.OnlyBuy;
            TestUserCount = config.TestUserCount;
            _userCount = config.UserCount;
            
            var testEnvironment = config.TestEnvironment;
            EnvironmentInfo =
                config.EnvironmentInfos.Find(o => o.Environment.Contains(testEnvironment));
            NodeInfoHelper.SetConfig(EnvironmentInfo.ConfigFile);
        }
        
        private void GetRewardList(string rewards,string counts)
        {
            if (OnlyBuy)
                return;
            var rewardList = rewards.Split(",").ToList();
            var countList = counts.Split(",").ToList();
            try
            {
                rewardList.Count.ShouldBe(countList.Count);
            }
            catch (Exception e)
            {
                Console.WriteLine("Please input correct reward list");
                throw;
            }
            
            RewardList = new Dictionary<string, int>();
            for (int i = 0; i < rewardList.Count; i++)
            {
                var c = int.Parse(countList[i]);
                RewardList.Add(rewardList[i],c);
            }
        }
        
        public void OnlyBuyJob(List<string> tester)
        {
            ExecuteStandaloneTask(new Action[]
            {
                () => CheckElfBalance(tester),
                () => ApproveFirst(tester),
                () => Buy_More(tester)
            });
        }

        public List<string> GetTestAddress()
        {
            var nodeManager = ContractServices.NodeManager;
            var nodesAccount = NodeInfoHelper.Config.Nodes.Select(l => l.Account).ToList();
            var testUsers = nodeManager.ListAccounts().FindAll(a => !nodesAccount.Contains(a));

            if (testUsers.Count >= _userCount)
            {
                var users = testUsers.Take(_userCount).ToList();
                return users;
            }
            
            var newAccounts = GenerateTestUsers(nodeManager, _userCount - testUsers.Count);
            testUsers.AddRange(newAccounts);
            foreach (var account in testUsers)
                nodeManager.UnlockAccount(account);
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

        public void Buy()
        {
            var accounts = NodeInfoHelper.Config.Nodes;
            foreach (var account in accounts)
            {
                var buyer = account.Account;
                var amount = CommonHelper.GenerateRandomNumber(1, 10);
                var balance = TokenService.GetUserBalance(LotteryService.ContractAddress, Symbol);
                var userBeforeBalance = TokenService.GetUserBalance(buyer, Symbol);
                var userElfBalance = TokenService.GetUserBalance(buyer);
                if (userBeforeBalance < 10000_00000000)
                {
                    TokenService.SetAccount(Owner, Password);
                    TokenService.TransferBalance(Owner, buyer, 20000_00000000, Symbol, buyer);
                    userBeforeBalance = TokenService.GetUserBalance(buyer, Symbol);
                }
                if (userElfBalance < 1000_00000000)
                {
                    TokenService.SetAccount(accounts.First().Account);
                    TokenService.TransferBalance(accounts.First().Account, buyer, 10000_00000000);
                }
                
                TokenService.SetAccount(buyer);
                var approveResult = TokenService.ApproveToken(buyer, LotteryService.ContractAddress,
                    amount * Price, Symbol);
                approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var allowance = TokenService.GetAllowance(buyer, LotteryService.ContractAddress, Symbol);
                allowance.ShouldBeGreaterThanOrEqualTo(amount * Price);

                LotteryService.SetAccount(buyer);
                var result = LotteryService.ExecuteMethodWithResult(LotteryDemoMethod.Buy, new Int64Value
                {
                    Value = amount
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var resultValue =
                    BoughtLotteriesInformation.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
                resultValue.Amount.ShouldBe(amount);
                Logger.Info($"start id is: {resultValue.StartId}");
                var userBalance = TokenService.GetUserBalance(buyer, Symbol);
                userBalance.ShouldBe(userBeforeBalance - amount * Price);
                var contractBalance = TokenService.GetUserBalance(LotteryService.ContractAddress, Symbol);
                contractBalance.ShouldBe(balance + amount * Price);
            }
        }

        private void ApproveFirst(IEnumerable<string> testers)
        {
            foreach (var sender in testers)
            {
                TokenService.SetAccount(sender);
                var allowance = TokenService.GetAllowance(sender, LotteryService.ContractAddress, Symbol);
                if (allowance < 10000_0000000)
                {
                    var approve = TokenService.ApproveToken(sender, LotteryService.ContractAddress,
                        long.MaxValue - 10000_0000000, Symbol);
                    approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                }
                
                var amount = 50;
                var userBeforeBalance = TokenService.GetUserBalance(sender, Symbol);
                if (userBeforeBalance < amount * Price * 15)
                    TokenService.TransferBalance(Owner, sender, amount * Price * 15, Symbol);
            }
        }

        private void CheckElfBalance(IEnumerable<string> testers)
        {
            foreach (var sender in testers)
            {
                TokenService.SetAccount(sender);
                var userBeforeBalance = TokenService.GetUserBalance(sender);
                if (userBeforeBalance < 10_00000000)
                    TokenService.TransferBalance(Owner, sender, 100_00000000);
            }
        }

        private void Buy_More(IEnumerable<string> testers)
        {
            for (int i = 0; i < 10; i++)
            {
                var txList = new List<string>();
                foreach (var tester in testers)
                {
                    var amount = CommonHelper.GenerateRandomNumber(25, 50);
                    LotteryService.SetAccount(tester);
                    var txId = LotteryService.ExecuteMethodWithTxId(LotteryDemoMethod.Buy, new Int64Value
                    {
                        Value = amount
                    });
                    txList.Add(txId);
                }
                LotteryService.NodeManager.CheckTransactionListResult(txList);
            }
        }



        public void Draw()
        {
            var amount = AsyncHelper.RunSync(()=> LotteryStub.GetMaximumBuyAmount.CallAsync(new Empty()));
            var period =  AsyncHelper.RunSync(()=> LotteryStub.GetCurrentPeriodNumber.CallAsync(new Empty()));
            Logger.Info($"Before draw period number is :{period.Value}");            
            
            LotteryService.SetAccount(Owner, Password);
            var result = LotteryService.ExecuteMethodWithResult(LotteryDemoMethod.PrepareDraw, new Empty());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            SetRewardListForOnePeriod(period.Value);
            var block = result.BlockNumber;
            var currentBlock = AsyncHelper.RunSync(() => ContractServices.NodeManager.ApiClient.GetBlockHeightAsync());
            while (currentBlock < block + amount.Value)
            {
                Thread.Sleep(5000);
                Logger.Info("Waiting block");
                currentBlock = AsyncHelper.RunSync(() => ContractServices.NodeManager.ApiClient.GetBlockHeightAsync());
            }

            LotteryService.SetAccount(Owner, Password);
            var drawResult = LotteryService.ExecuteMethodWithResult(LotteryDemoMethod.Draw, new Int64Value
            {
                Value = period.Value
            });
            drawResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var afterPeriod =  AsyncHelper.RunSync(()=> LotteryStub.GetCurrentPeriodNumber.CallAsync(new Empty()));
            afterPeriod.Value.ShouldBe(period.Value + 1);
            Logger.Info($"After draw period number is :{afterPeriod.Value}");            
        }
        
        private void SetRewardListForOnePeriod(long period)
        {
            var result = LotteryService.ExecuteMethodWithResult(LotteryDemoMethod.SetRewardListForOnePeriod, new RewardsInfo
            {
                Period = period,
                Rewards =
                {
                    RewardList
                }
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        private void InitializeLotteryDemoContract()
        {
            LotteryService.SetAccount(Owner, Password);
            var result =
                LotteryService.ExecuteMethodWithResult(LotteryDemoMethod.Initialize,
                    new InitializeInput
                    {
                        TokenSymbol = Symbol,
                        Price = Price,
                        StartTimestamp = DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)).ToTimestamp(),
                        ShutdownTimestamp = DateTime.UtcNow.Add(TimeSpan.FromDays(7)).ToTimestamp()
                    });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        private void CreateTokenAndIssue()
        {
            var result = TokenService.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
            {
                Symbol = Symbol,
                TotalSupply = 10_00000000_00000000,
                Decimals = 8,
                Issuer = Owner.ConvertAddress(),
                IsBurnable = true,
                TokenName = "LOT"
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            TokenService.IssueBalance(Owner, Owner, 10_00000000_00000000, Symbol);
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