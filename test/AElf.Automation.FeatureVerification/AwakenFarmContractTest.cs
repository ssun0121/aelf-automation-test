using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Awaken.Contracts.Farm;
using Awaken.Contracts.Swap;
using Google.Protobuf;
using log4net;
using Shouldly;
using Volo.Abp.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using InitializeInput = Awaken.Contracts.Farm.InitializeInput;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class AwakenFarmOneContractTest
    {
        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;
        private AwakenTokenContract _awakenTokenContract;
        private AwakenSwapContract _awakenSwapContract;
        private AwakenFarmTwoContract _awakenPoolTwoContract;
        private AwakenFarmContract _awakenFarmContract;
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private string tokenAddress = "2nyC8hqq3pGnRu8gJzCsTaxXB6snfGxmL2viimKXgEfYWGtjEh";
        private string swapAddress = "2u6Dd139bHvZJdZ835XnNKL5y6cxqzV9PEWD5fZdQXdFZLgevc";
        private string farmAddress = "DHo2K7oUXXq3kJRs1JpuwqBJP56gqoaeSKFfuvr9x8svf3vEJ";
        private string farmTwoAddress = "2hqsqJndRAZGzk96fsEvyuVBTAvoBjcuwTjkuyJffBPueJFrLa";
        private string InitAccount { get; } = "J6zgLjGwd1bxTBpULLXrGVeV74tnS2n74FFJJz7KNdjTYkDF6";
        private string ToolAddress { get; } = "J6zgLjGwd1bxTBpULLXrGVeV74tnS2n74FFJJz7KNdjTYkDF6";
        private string AdminAddress { get; } = "yy71DP4pMvGAqnohHPJ3rzmxoi1qxHk4uXg8kdRvgXjnYcE1G";
        private string TestAddress { get; } = "ZgM2YJtoLQNLqCTAFHwzu7bo77sk6NGuvVaq6ccCHxQkknhpD";
        private static string RpcUrl { get; } = "http://192.168.67.166:8000";
        private const long Digits = 1_00000000;
        private const string Multiplier = "1000000000000";
        private const long Block0 = 10000;
        private const long Block1 = Block0 * 4;
        private const long PerBlock0 = 100 * Digits;
        private const long PerBlock1 = PerBlock0 / 2;
        private const long Cycle = Block0 * 4;
        private const string DistributeToken = "AWAKENT";
        private const string NewRewardToken = "USDT";
        private bool isNeedInitialize = false;

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("AwakenFarmContractTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env2-main");

            NodeManager = new NodeManager(RpcUrl);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
            _awakenTokenContract = tokenAddress == ""
                ? new AwakenTokenContract(NodeManager, InitAccount)
                : new AwakenTokenContract(NodeManager, InitAccount, tokenAddress);
            _awakenSwapContract = swapAddress == ""
                ? new AwakenSwapContract(NodeManager, InitAccount)
                : new AwakenSwapContract(NodeManager, InitAccount, swapAddress);
            _awakenFarmContract = farmAddress == ""
                ? new AwakenFarmContract(NodeManager, InitAccount)
                : new AwakenFarmContract(NodeManager, InitAccount, farmAddress);
            _awakenPoolTwoContract = farmTwoAddress == ""
                ? new AwakenFarmTwoContract(NodeManager, InitAccount)
                : new AwakenFarmTwoContract(NodeManager, InitAccount, farmTwoAddress);

            CreateToken(NewRewardToken, 6, InitAccount.ConvertAddress());
            if (_tokenContract.GetUserBalance(AdminAddress) < 100_00000000)
                _tokenContract.TransferBalance(InitAccount, AdminAddress, 10000_00000000);
            if (_tokenContract.GetUserBalance(TestAddress) < 100_00000000)
                _tokenContract.TransferBalance(InitAccount, TestAddress, 10000_00000000);
        }

        [TestMethod]
        public void InitializeTest()
        {
            if (isNeedInitialize)
                InitializeOtherContract();
            var currentHeight = AsyncHelper.RunSync(() => NodeManager.ApiClient.GetBlockHeightAsync());
            var addBlock = 1000;
            var startBlock = currentHeight.Add(addBlock);
            var totalReward = GetTotalReward();
            Logger.Info($"TotalReward: {totalReward}, CurrentHeight: {currentHeight}");
            var admin = AdminAddress.ConvertAddress();
            var input = new InitializeInput
            {
                Admin = admin,
                LpTokenContract = _awakenTokenContract.Contract,
                StartBlock = startBlock,
                Block0 = Block0,
                Block1 = Block1,
                DistributeTokenPerBlock0 = PerBlock0,
                DistributeTokenPerBlock1 = PerBlock1,
                Cycle = Cycle,
                TotalReward = totalReward
            };
            {
                input.StartBlock = currentHeight.Div(2);
                //"Invalid Input:StartBlock"
                var result = _awakenFarmContract.ExecuteMethodWithResult(FarmMethod.Initialize, input);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Invalid Input:StartBlock");
            }
            {
                input.StartBlock = currentHeight.Add(addBlock);
                var result = _awakenFarmContract.ExecuteMethodWithResult(FarmMethod.Initialize, input);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                CheckInitializeState(totalReward, currentHeight, addBlock, admin);
            }
            {
                //"Already initialized."
                var result = _awakenFarmContract.ExecuteMethodWithResult(FarmMethod.Initialize, new InitializeInput());
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Already initialized.");
            }

            //Initialize pool two
            var poolTwoStart = startBlock;
            var poolTwoPerBlock = new BigIntValue(PerBlock1);
            var havingPeriod = Block0.Add(Block1);
            Logger.Info($"\nHavingPeriod: {havingPeriod}\n" +
                        $"PerBlock: {poolTwoPerBlock}" +
                        $"StartBlock: {poolTwoStart}");
            var totalPoolReward = GetPoolTwoTotalReward(poolTwoPerBlock, havingPeriod);
            var initializePoolTwo = _awakenPoolTwoContract.Initialize(DistributeToken, poolTwoPerBlock, havingPeriod,
                poolTwoStart, totalPoolReward, _awakenTokenContract.ContractAddress);
            initializePoolTwo.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            CreateToken(DistributeToken, 8, InitAccount.ConvertAddress());
            _tokenContract.IssueBalance(InitAccount, _awakenPoolTwoContract.ContractAddress,
                long.Parse(totalPoolReward.Value), DistributeToken);
            ChangeTokenIssuer(DistributeToken, InitAccount);
            var setFarmOne = _awakenPoolTwoContract.SetFarmPoolOne(_awakenFarmContract.Contract);
            setFarmOne.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            SetReDeposit();
        }

        //ELF-ETH
        [TestMethod]
        [DataRow("ALP AAA-BBB", 200, false)]
        [DataRow("ALP BBB-CCC", 300, false)]
        [DataRow("ALP CCC-DDD", 500, false)]
        public void AddPool(string lpToken, long alloc, bool withUpdate)
        {
            var poolLength = _awakenFarmContract.GetPoolLength();
            var startBlock = _awakenFarmContract.GetStartBlockOfDistributeToken();
            var beforeTotalAlloc = _awakenFarmContract.GetTotalAllocPoint();

            _awakenFarmContract.SetAccount(AdminAddress);
            var addResult = _awakenFarmContract.AddPool(alloc, withUpdate, lpToken);
            addResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterBoolLength = _awakenFarmContract.GetPoolLength();
            afterBoolLength.ShouldBe(poolLength + 1);
            var logs = addResult.Logs.First(l => l.Name.Equals("PoolAdded")).NonIndexed;
            var poolAdded = PoolAdded.Parser.ParseFrom(ByteString.FromBase64(logs));
            poolAdded.Pid.ShouldBe((int) afterBoolLength - 1);
            poolAdded.Token.ShouldBe(lpToken);
            poolAdded.AllocationPoint.ShouldBe(alloc);
            poolAdded.PoolType.ShouldBe(0);
            var lastRewardBlock = addResult.BlockNumber > startBlock ? addResult.BlockNumber : startBlock;
            poolAdded.LastRewardBlockHeight.ShouldBe(lastRewardBlock);

            var poolInfo = CheckPoolInfo((int) poolAdded.Pid);
            poolInfo.AllocPoint.ShouldBe(alloc);
            poolInfo.LpToken.ShouldBe(lpToken);
            poolInfo.LastRewardBlock.ShouldBe(lastRewardBlock);
            poolInfo.TotalAmount.ShouldBe(0);
            poolInfo.AccDistributeTokenPerShare.ShouldBe(0);
            poolInfo.AccLockDistributeTokenPerShare.ShouldBe(0);
            poolInfo.AccUsdtPerShare.ShouldBe(0);
            poolInfo.LastAccLockDistributeTokenPerShare.ShouldBe(0);

            var totalAlloc = _awakenFarmContract.GetTotalAllocPoint();
            totalAlloc.ShouldBe(beforeTotalAlloc.Add(alloc));
        }

        [TestMethod]
        [DataRow(0, 100000000, 0)]
        [DataRow(2, 100000000, 0)]
        public void DepositTest(int pid, long amount, long firstDepositHeight)
        {
            //UpdatePool  ClaimRevenue  Deposit
            var depositAddress = TestAddress;
            var originPoolInfo = CheckPoolInfo(pid);
            var originTokenInfo = _tokenContract.GetTokenInfo(DistributeToken);
            var issuedReward = _awakenFarmContract.GetIssuedReward();
            var originUserInfo = _awakenFarmContract.GetUserInfo(pid, depositAddress);
            var startBlock = _awakenFarmContract.GetStartBlockOfDistributeToken();
            var endBlock = _awakenFarmContract.GetEndBlock();
            var symbol = originPoolInfo.LpToken;
            var originUserBalance = _awakenTokenContract.GetBalance(symbol, depositAddress.ConvertAddress());
            var originContractBalance = _awakenTokenContract.GetBalance(symbol, _awakenFarmContract.Contract);
            var originUserDistributeBalance = _tokenContract.GetUserBalance(depositAddress, DistributeToken);
            var originContractDistributeBalance =
                _tokenContract.GetUserBalance(_awakenFarmContract.ContractAddress, DistributeToken);
            var originPending = _awakenFarmContract.Pending(pid, depositAddress);

            ApproveLpToken(symbol, amount, _awakenFarmContract.ContractAddress, depositAddress);
            var pending = _awakenFarmContract.Pending(pid, depositAddress);
            Logger.Info($"Pending: {pending}");
            var lockPending = _awakenFarmContract.PendingLockDistributeToken(pid, depositAddress);
            Logger.Info($"PendingLock: {lockPending}");

            var depositResult = _awakenFarmContract.Deposit(pid, amount, depositAddress);
            depositResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info(depositResult.BlockNumber);
            var depositLogs = depositResult.Logs.First(l => l.Name.Equals("Deposit"));
            foreach (var l in depositLogs.Indexed)
            {
                var d = Deposit.Parser.ParseFrom(ByteString.FromBase64(l));
                if (d.User != null)
                    d.User.ShouldBe(depositAddress.ConvertAddress());
                else
                    d.Pid.ShouldBe(pid);
            }

            var depositAmount = Deposit.Parser.ParseFrom(ByteString.FromBase64(depositLogs.NonIndexed)).Amount;
            depositAmount.ShouldBe(amount);

            var afterTokenInfo = _tokenContract.GetTokenInfo(DistributeToken);
            var afterIssuedReward = _awakenFarmContract.GetIssuedReward();
            var afterUserInfo = _awakenFarmContract.GetUserInfo(pid, depositAddress);
            var afterUserBalance = _awakenTokenContract.GetBalance(symbol, depositAddress.ConvertAddress());
            var afterContractBalance = _awakenTokenContract.GetBalance(symbol, _awakenFarmContract.Contract);
            var afterUserDistributeBalance = _tokenContract.GetUserBalance(depositAddress, DistributeToken);
            var afterContractDistributeBalance =
                _tokenContract.GetUserBalance(_awakenFarmContract.ContractAddress, DistributeToken);

            var afterPoolInfo = CheckPoolInfo(pid);
            // 在startBlock之前进行抵押，LastRewardBlock == startBlock
            afterPoolInfo.LastRewardBlock.ShouldBe(
                originPoolInfo.LastRewardBlock >= depositResult.BlockNumber || originPoolInfo.LastRewardBlock > endBlock
                    ? originPoolInfo.LastRewardBlock
                    : depositResult.BlockNumber);
            afterUserInfo.LastRewardBlock.ShouldBe(
                originPoolInfo.LastRewardBlock >= depositResult.BlockNumber
                    ? originPoolInfo.LastRewardBlock
                    : depositResult.BlockNumber);

            //Pool first deposit 
            if (originPoolInfo.TotalAmount == 0 && afterPoolInfo.AccLockDistributeTokenPerShare.Equals(0)
                || depositResult.BlockNumber <= startBlock)
            {
                afterTokenInfo.Issued.ShouldBe(originTokenInfo.Issued);
                afterIssuedReward.ShouldBe(issuedReward);

                afterPoolInfo.AccDistributeTokenPerShare.ShouldBe(0);
                afterPoolInfo.AccLockDistributeTokenPerShare.ShouldBe(0);
                afterPoolInfo.LastAccLockDistributeTokenPerShare.ShouldBe(0);
                afterPoolInfo.AccUsdtPerShare.ShouldBe(0);

                afterUserInfo.LockPending.ShouldBe(0);
                afterUserInfo.ClaimedAmount.ShouldBe(0);
                afterUserInfo.RewardDistributeTokenDebt.ShouldBe(0);
                afterUserInfo.RewardUsdtDebt.ShouldBe(0);
                afterUserInfo.RewardLockDistributeTokenDebt.ShouldBe(0);

                afterUserDistributeBalance.ShouldBe(originUserDistributeBalance);
                afterContractDistributeBalance.ShouldBe(originContractDistributeBalance);
            }

            //It's not the first time a user has deposited
            if ((originUserInfo.Amount > 0 || !afterPoolInfo.AccLockDistributeTokenPerShare.Equals(0)) &&
                depositResult.BlockNumber > startBlock && originPoolInfo.LastRewardBlock <= endBlock)
            {
                var total = CheckRewardInfo(
                    out var userReward,
                    out var userLockReward,
                    out var currentPeriodUserLockReward,
                    out var lastRewardHeight,
                    originUserInfo, originPoolInfo, depositResult.BlockNumber, firstDepositHeight);

                var stillLockReward = GetUserLockReward(out var claimReward, originUserInfo.LastRewardBlock,
                    lastRewardHeight, depositResult.BlockNumber, currentPeriodUserLockReward);
                stillLockReward.ShouldBe(afterUserInfo.LockPending);

                Logger.Info($"\n UserReward: {userReward}\n " +
                            $"UserLockReward: {currentPeriodUserLockReward}\n " +
                            $"StillLockReward: {stillLockReward}\n " +
                            $"ClaimLockAmount: {claimReward}\n" +
                            $"LockePending: {afterUserInfo.LockPending}");
                if (originPending.DistributeTokenAmount > 0)
                {
                    var claimLogDto = depositResult.Logs.First(l => l.Name.Equals("ClaimRevenue"));
                    foreach (var l in claimLogDto.Indexed)
                    {
                        var d = ClaimRevenue.Parser.ParseFrom(ByteString.FromBase64(l));
                        if (d.User != null)
                            d.User.ShouldBe(depositAddress.ConvertAddress());
                        else
                            d.Pid.ShouldBe(pid);
                    }

                    var claimInfo = ClaimRevenue.Parser.ParseFrom(ByteString.FromBase64(claimLogDto.NonIndexed));
                    userReward.Add(claimReward).ShouldBe(claimInfo.Amount);
                    claimInfo.Amount.ShouldBe(afterUserInfo.ClaimedAmount.Sub(originUserInfo.ClaimedAmount));
                    Logger.Info($"Claim amount: {claimInfo.Amount}");
                    claimInfo.Token.ShouldBe(DistributeToken);

                    afterUserDistributeBalance.ShouldBe(originUserDistributeBalance.Add(claimInfo.Amount));
                    afterContractDistributeBalance.ShouldBe(originContractDistributeBalance.Sub(claimInfo.Amount)
                        .Add(total));
                }

                afterTokenInfo.Issued.ShouldBe(originTokenInfo.Issued.Add(total));
                afterIssuedReward.ShouldBe(issuedReward.Add(total));
                var updatePoolLogDto = depositResult.Logs.First(l => l.Name.Equals("UpdatePool"));
                var updateInfo = UpdatePool.Parser.ParseFrom(ByteString.FromBase64(updatePoolLogDto.NonIndexed));
                updateInfo.Pid.ShouldBe(pid);
                updateInfo.DistributeTokenAmount.ShouldBe(total);
                updateInfo.UpdateBlockHeight.ShouldBe(depositResult.BlockNumber);
            }

            afterPoolInfo.TotalAmount.ShouldBe(originPoolInfo.TotalAmount.Add(amount));
            afterUserInfo.Amount.ShouldBe(originUserInfo.Amount.Add(amount));
            afterUserInfo.RewardDistributeTokenDebt
                .ShouldBe(afterPoolInfo.AccDistributeTokenPerShare.Mul(afterUserInfo.Amount).Div(Multiplier));
            afterUserInfo.RewardLockDistributeTokenDebt
                .ShouldBe(afterPoolInfo.AccLockDistributeTokenPerShare.Mul(afterUserInfo.Amount).Div(Multiplier));

            afterUserBalance.Amount.ShouldBe(originUserBalance.Amount.Sub(amount));
            afterContractBalance.Amount.ShouldBe(originContractBalance.Amount.Add(amount));
        }

        [TestMethod]
        [DataRow(0, 0)]
        [DataRow(1, 0)]
        [DataRow(2, 0)]
        public void WithdrawTest(int pid, long firstDepositHeight)
        {
            var depositAddress = TestAddress;
            var originPoolInfo = CheckPoolInfo(pid);
            var originTokenInfo = _tokenContract.GetTokenInfo(DistributeToken);
            var issuedReward = _awakenFarmContract.GetIssuedReward();
            var symbol = originPoolInfo.LpToken;
            var startBlock = _awakenFarmContract.GetStartBlockOfDistributeToken();
            var endBlock = _awakenFarmContract.GetEndBlock();
            var originUserInfo = _awakenFarmContract.GetUserInfo(pid, depositAddress);
            var originUserBalance = _awakenTokenContract.GetBalance(symbol, depositAddress.ConvertAddress());
            var originContractBalance = _awakenTokenContract.GetBalance(symbol, _awakenFarmContract.Contract);
            var originUserDistributeBalance = _tokenContract.GetUserBalance(depositAddress, DistributeToken);
            var originContractDistributeBalance =
                _tokenContract.GetUserBalance(_awakenFarmContract.ContractAddress, DistributeToken);
            var originUserUsdtBalance = _tokenContract.GetUserBalance(depositAddress, NewRewardToken);
            var originContractUsdtBalance =
                _tokenContract.GetUserBalance(_awakenFarmContract.ContractAddress, NewRewardToken);

            var originPending = _awakenFarmContract.Pending(pid, depositAddress);
            Logger.Info(originPending);

            var withdrawAmount = originUserInfo.Amount;
            var withdrawResult = _awakenFarmContract.Withdraw(pid, withdrawAmount, depositAddress);
            withdrawResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"\nWithdraw amount: {withdrawAmount}");

            var withdrawLogs = withdrawResult.Logs.First(l => l.Name.Equals("Withdraw"));
            foreach (var l in withdrawLogs.Indexed)
            {
                var d = Withdraw.Parser.ParseFrom(ByteString.FromBase64(l));
                if (d.User != null)
                    d.User.ShouldBe(depositAddress.ConvertAddress());
                else
                    d.Pid.ShouldBe(pid);
            }

            var checkWithdrawAmount = Withdraw.Parser.ParseFrom(ByteString.FromBase64(withdrawLogs.NonIndexed)).Amount;
            checkWithdrawAmount.ShouldBe(withdrawAmount);

            var afterTokenInfo = _tokenContract.GetTokenInfo(DistributeToken);
            var afterIssuedReward = _awakenFarmContract.GetIssuedReward();
            var afterUserInfo = _awakenFarmContract.GetUserInfo(pid, depositAddress);
            var afterPoolInfo = CheckPoolInfo(pid);
            var afterUserBalance = _awakenTokenContract.GetBalance(symbol, depositAddress.ConvertAddress());
            var afterContractBalance = _awakenTokenContract.GetBalance(symbol, _awakenFarmContract.Contract);
            var afterUserDistributeBalance = _tokenContract.GetUserBalance(depositAddress, DistributeToken);
            var afterContractDistributeBalance =
                _tokenContract.GetUserBalance(_awakenFarmContract.ContractAddress, DistributeToken);
            var afterUserUsdtBalance = _tokenContract.GetUserBalance(depositAddress, NewRewardToken);
            var afterContractUsdtBalance =
                _tokenContract.GetUserBalance(_awakenFarmContract.ContractAddress, NewRewardToken);


            if (withdrawResult.BlockNumber > startBlock && originUserInfo.LastRewardBlock <= endBlock)
            {
                var total = CheckRewardInfo(
                    out var userReward,
                    out var userLockReward,
                    out var currentPeriodUserLockReward,
                    out var lastRewardHeight,
                    originUserInfo, originPoolInfo, withdrawResult.BlockNumber, firstDepositHeight);
                var stillLockReward = GetUserLockReward(out var claimReward, originUserInfo.LastRewardBlock,
                    lastRewardHeight, withdrawResult.BlockNumber, currentPeriodUserLockReward);
                stillLockReward.ShouldBe(afterUserInfo.LockPending);

                Logger.Info(afterUserInfo.LockPending);
                Logger.Info($"\n UserReward: {userReward}\n " +
                            $"UserLockReward: {currentPeriodUserLockReward}\n " +
                            $"StillLockReward: {stillLockReward}\n " +
                            $"ClaimLockAmount: {claimReward}");

                if (originPending.DistributeTokenAmount > 0)
                {
                    var claimLogDto = withdrawResult.Logs.First(l => l.Name.Equals("ClaimRevenue"));
                    foreach (var l in claimLogDto.Indexed)
                    {
                        var d = ClaimRevenue.Parser.ParseFrom(ByteString.FromBase64(l));
                        if (d.User != null)
                            d.User.ShouldBe(depositAddress.ConvertAddress());
                        else
                            d.Pid.ShouldBe(pid);
                    }

                    var claimInfo = ClaimRevenue.Parser.ParseFrom(ByteString.FromBase64(claimLogDto.NonIndexed));
                    userReward.Add(claimReward).ShouldBe(claimInfo.Amount);
                    claimInfo.Amount.ShouldBe(afterUserInfo.ClaimedAmount.Sub(originUserInfo.ClaimedAmount));
                    Logger.Info($"Claim amount: {claimInfo.Amount}");
                    Logger.Info($"Except claim amount: {userReward.Add(claimReward)}");
                    claimInfo.Token.ShouldBe(DistributeToken);

                    afterUserDistributeBalance.ShouldBe(originUserDistributeBalance.Add(claimInfo.Amount));
                    afterContractDistributeBalance.ShouldBe(originContractDistributeBalance.Sub(claimInfo.Amount)
                    .Add(total));
                }

                if (originPending.UsdtAmount > 0)
                {
                    var claimLogDto = withdrawResult.Logs.Last(l => l.Name.Equals("ClaimRevenue"));
                    foreach (var l in claimLogDto.Indexed)
                    {
                        var d = ClaimRevenue.Parser.ParseFrom(ByteString.FromBase64(l));
                        if (d.User != null)
                            d.User.ShouldBe(depositAddress.ConvertAddress());
                        else
                            d.Pid.ShouldBe(pid);
                    }

                    var claimInfo = ClaimRevenue.Parser.ParseFrom(ByteString.FromBase64(claimLogDto.NonIndexed));
                    Logger.Info($"Usdt claim amount: {claimInfo.Amount}");
                    claimInfo.Token.ShouldBe(NewRewardToken);

                    afterUserUsdtBalance.ShouldBe(originUserUsdtBalance.Add(claimInfo.Amount));
                    afterContractUsdtBalance.ShouldBe(originContractUsdtBalance.Sub(claimInfo.Amount));
                }

                afterTokenInfo.Issued.ShouldBe(originTokenInfo.Issued.Add(total));
                afterIssuedReward.ShouldBe(issuedReward.Add(total));
                var updatePoolLogDto = withdrawResult.Logs.First(l => l.Name.Equals("UpdatePool"));
                var updateInfo = UpdatePool.Parser.ParseFrom(ByteString.FromBase64(updatePoolLogDto.NonIndexed));
                updateInfo.Pid.ShouldBe(pid);
                updateInfo.DistributeTokenAmount.ShouldBe(total);
                updateInfo.UpdateBlockHeight.ShouldBe(withdrawResult.BlockNumber);
            }

            afterPoolInfo.LastRewardBlock.ShouldBe(
                originPoolInfo.LastRewardBlock >= withdrawResult.BlockNumber || originPoolInfo.LastRewardBlock > endBlock
                    ? originPoolInfo.LastRewardBlock
                    : withdrawResult.BlockNumber);
            afterUserInfo.LastRewardBlock.ShouldBe(originPoolInfo.LastRewardBlock >= withdrawResult.BlockNumber
                ? originUserInfo.LastRewardBlock
                : withdrawResult.BlockNumber);

            afterUserInfo.Amount.ShouldBe(originUserInfo.Amount.Sub(withdrawAmount));
            afterPoolInfo.TotalAmount.ShouldBe(originPoolInfo.TotalAmount.Sub(withdrawAmount));
            afterUserInfo.RewardDistributeTokenDebt
                .ShouldBe(afterPoolInfo.AccDistributeTokenPerShare.Mul(afterUserInfo.Amount).Div(Multiplier));
            afterUserInfo.RewardLockDistributeTokenDebt
                .ShouldBe(afterPoolInfo.AccLockDistributeTokenPerShare.Mul(afterUserInfo.Amount).Div(Multiplier));

            afterUserBalance.Amount.ShouldBe(originUserBalance.Amount.Add(withdrawAmount));
            afterContractBalance.Amount.ShouldBe(originContractBalance.Amount.Sub(withdrawAmount));
            Logger.Info($"\n UserDistributeBalance:\n" +
                        $"{originUserDistributeBalance}\n" +
                        $"{afterUserDistributeBalance}\n" +
                        $"change: {afterUserDistributeBalance - originUserDistributeBalance}\n" +
                        "ContractDistributeBalance:\n" +
                        $"{originContractDistributeBalance}\n" +
                        $"{afterContractDistributeBalance}\n" +
                        $"change: {originContractDistributeBalance - afterContractDistributeBalance}");
        }

        [TestMethod]
        [DataRow(1)]
        public void ReDeposit(int pid)
        {
            var depositAddress = TestAddress;
            var symbolA = DistributeToken;
            var symbolB = "ELF";
            var pair = _awakenSwapContract.GetTokenPair(symbolA, symbolB);

            var originUserBalance = _tokenContract.GetUserBalance(depositAddress);
            var originUserDistributeBalance = _tokenContract.GetUserBalance(depositAddress, DistributeToken);
            var originPoolTwoInfo = _awakenPoolTwoContract.GetPoolInfo(0);

            var distributeAmount = originUserDistributeBalance;
            Logger.Info($"User balance : {originUserDistributeBalance}");
            var reDepositLimit = _awakenFarmContract.GetReDepositLimit(pid, depositAddress);
            Logger.Info($"ReDepositLimit: {reDepositLimit}");
            var getRedepositAmount = _awakenFarmContract.GetRedepositAmount(pid, depositAddress);
            Logger.Info($"User redeposit amount: {getRedepositAmount}");

            var amount = distributeAmount > reDepositLimit.Sub(getRedepositAmount)
                ? reDepositLimit.Sub(getRedepositAmount)
                : distributeAmount;
            var totalSupply = _awakenSwapContract.GetTotalSupply(pair);
            var totalSupplyResult = totalSupply.Results.First(t => t.SymbolPair.Equals(pair));
            var elfAmount = amount.Div(10);
            var elfExceptAmount = totalSupplyResult.TotalSupply != 0
                ? _awakenSwapContract.Quote(symbolA, symbolB, amount)
                : elfAmount;
            Logger.Info($"\nDistributeAmount: {amount}" +
                        $"\nElfAmount: {elfAmount}" +
                        $"\nElfExceptAmount: {elfExceptAmount}");

            var approveDistributeToken = _tokenContract.ApproveToken(depositAddress,
                _awakenFarmContract.ContractAddress, amount, DistributeToken);
            var approveElfToken =
                _tokenContract.ApproveToken(depositAddress, _awakenFarmContract.ContractAddress, elfAmount, "ELF");

            var result = _awakenFarmContract.ReDeposit(pid, amount, elfAmount, depositAddress);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var addLiquidityLogs =
                ByteString.FromBase64(result.Logs.First(l => l.Name.Contains("LiquidityAdded")).NonIndexed);
            var liquidityAdded = LiquidityAdded.Parser.ParseFrom(addLiquidityLogs);
            liquidityAdded.SymbolA.ShouldBe(symbolA);
            liquidityAdded.SymbolB.ShouldBe(symbolB);
            liquidityAdded.AmountA.ShouldBe(amount);
            liquidityAdded.AmountB.ShouldBe(elfExceptAmount);
            Logger.Info(liquidityAdded.LiquidityToken);

            var getPoolTwoInfo = _awakenPoolTwoContract.GetPoolInfo(0);
            getPoolTwoInfo.TotalAmount.ShouldBe(originPoolTwoInfo.TotalAmount.Add(liquidityAdded.LiquidityToken));

            var afterUserBalance = _tokenContract.GetUserBalance(depositAddress);
            var afterUserDistributeBalance = _tokenContract.GetUserBalance(depositAddress, DistributeToken);
            var afterRedepositAmount = _awakenFarmContract.GetRedepositAmount(pid, depositAddress);

            afterUserBalance.ShouldBe(originUserBalance.Sub(elfExceptAmount)
                .Sub(approveDistributeToken.GetDefaultTransactionFee())
                .Sub(approveElfToken.GetDefaultTransactionFee()));
            afterUserDistributeBalance.ShouldBe(originUserDistributeBalance.Sub(amount));
            afterRedepositAmount.ShouldBe(getRedepositAmount.Add(amount));
        }


        [TestMethod]
        public void NewReward()
        {
            var distributeStartBlock = _awakenFarmContract.GetStartBlockOfDistributeToken();
            SetToolAddress();
            _awakenFarmContract.SetAccount(ToolAddress);
            long perBlock = 10_000000;
            var usdtAmount = perBlock.Mul(Cycle);
            var startBlock = distributeStartBlock.Add(Block0);

            var toolBalance = _tokenContract.GetUserBalance(ToolAddress, NewRewardToken);
            if (toolBalance < usdtAmount)
            {
                var issuer = _tokenContract.GetTokenInfo(NewRewardToken).Issuer;
                _tokenContract.IssueBalance(issuer.ToBase58(), ToolAddress, usdtAmount, NewRewardToken);
            }

            _tokenContract.ApproveToken(ToolAddress, _awakenFarmContract.ContractAddress, usdtAmount, NewRewardToken);

            var result = _awakenFarmContract.NewReward(ToolAddress, usdtAmount, startBlock, perBlock);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logs = result.Logs.First(l => l.Name.Equals("NewRewardSet")).NonIndexed;
            var newRewardSet = NewRewardSet.Parser.ParseFrom(ByteString.FromBase64(logs));
            newRewardSet.EndBlock.ShouldBe(startBlock.Add(Cycle));
            newRewardSet.StartBlock.ShouldBe(startBlock);
            newRewardSet.UsdtPerBlock.ShouldBe(perBlock);

            var getUsdtEndBlock = _awakenFarmContract.GetUsdtEndBlock();
            var getUsdtPerBlock = _awakenFarmContract.GetUsdtPerBlock();
            var getUsdtStartBlock = _awakenFarmContract.GetUsdtStartBlock();

            getUsdtEndBlock.ShouldBe(startBlock.Add(Cycle));
            getUsdtStartBlock.ShouldBe(startBlock);
            getUsdtPerBlock.ShouldBe(perBlock);
            Logger.Info($"USDT: \n" +
                        $"start block: {startBlock}\n" +
                        $"end block: {getUsdtEndBlock}\n" +
                        $"per block: {perBlock}\n" +
                        $"total amount: {usdtAmount}");
        }

        [TestMethod]
        public void FixEndBlock()
        {
            var endBlock = _awakenFarmContract.GetEndBlock();
            var currentBlockHeight = AsyncHelper.RunSync(() => NodeManager.ApiClient.GetBlockHeightAsync());

            var result = _awakenFarmContract.FixEndBlock(InitAccount, true);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterEndBlock = _awakenFarmContract.GetEndBlock();

            var exceptEndBlock = currentBlockHeight >= endBlock ? endBlock : 0;
            afterEndBlock.ShouldBe(exceptEndBlock);
        }

        [TestMethod]
        public void CheckInitializeState(long totalReward, long currentHeight, long addBlock, Address admin)
        {
            var getAdmin = _awakenFarmContract.GetAdmin();
            var startBlock = _awakenFarmContract.GetStartBlockOfDistributeToken();
            var concentratedMining = _awakenFarmContract.GetDistributeTokenPerBlockConcentratedMining();
            var continuousMining = _awakenFarmContract.GetDistributeTokenPerBlockContinuousMining();
            var halvingPeriod0 = _awakenFarmContract.GetHalvingPeriod0();
            var halvingPeriod1 = _awakenFarmContract.GetHalvingPeriod1();
            var getCycle = _awakenFarmContract.GetCycle();
            var getTotalReward = _awakenFarmContract.GetTotalReward();
            var endBlock = _awakenFarmContract.GetEndBlock();
            Logger.Info($"\nAdmin: {getAdmin.ToBase58()}\n" +
                        $"StartBlock: {startBlock}\n" +
                        $"EndBlock: {endBlock}\n" +
                        $"TotalReward: {totalReward}\n" +
                        $"Concentrate Mining: {concentratedMining}\n" +
                        $"Continuous Mining: {continuousMining}\n" +
                        $"Halving Period0: {halvingPeriod0}\n" +
                        $"Halving Period1: {halvingPeriod1}");
            getAdmin.ShouldBe(admin);
            startBlock.ShouldBe(currentHeight + addBlock);
            concentratedMining.ShouldBe(PerBlock0);
            continuousMining.ShouldBe(PerBlock1);
            halvingPeriod0.ShouldBe(Block0);
            halvingPeriod1.ShouldBe(Block1);
            getCycle.ShouldBe(Cycle);
            getTotalReward.ShouldBe(totalReward);
            endBlock.ShouldBe(startBlock + (Block0 + Block1) * 4);
        }

        [TestMethod]
        public void CheckAllPool()
        {
            var poolLenght = _awakenFarmContract.GetPoolLength();
            for (int i = 0; i < poolLenght; i++)
                CheckPoolInfo(i);
        }

        [TestMethod]
        public void CheckPeriod()
        {
            var height = AsyncHelper.RunSync(() => NodeManager.ApiClient.GetBlockHeightAsync());
            // var height = 0;
            CheckPeriod(height);
        }

        [TestMethod]
        public void CheckPending()
        {
            var depositAddress = TestAddress;
            var poolLength = _awakenFarmContract.GetPoolLength();
            for (var i = 0; i < poolLength; i++)
            {
                Logger.Info($"\nPool id: {i}");
                var pending = _awakenFarmContract.Pending(i, depositAddress);
                Logger.Info($"\nPending: {pending}");
                var pendingLock = _awakenFarmContract.PendingLockDistributeToken(i, depositAddress);
                Logger.Info($"\nPendingLock: {pendingLock}");
                var userInfo = _awakenFarmContract.GetUserInfo(i, depositAddress);
                var poolInfo = CheckPoolInfo(i);
                CheckRewardInfo(out _, out _, out _, out _, userInfo,
                    poolInfo);
            }
        }

        [TestMethod]
        public void SetReDeposit()
        {
            var result = _awakenFarmContract.SetReDeposit
                (AdminAddress, _awakenSwapContract.Contract, _awakenPoolTwoContract.Contract);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void SetToolAddress()
        {
            var result = _awakenFarmContract.SetTool(InitAccount, ToolAddress.ConvertAddress());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void CheckRewardMethod()
        {
            long startBlock = _awakenFarmContract.GetStartBlockOfDistributeToken();
            long onePeriod = Block0.Add(Block1);

            long lastRewardBlock = 15518;
            var currentBlock = lastRewardBlock.Add(100);
            Logger.Info(lastRewardBlock);
            Logger.Info(currentBlock);
            CheckReward(lastRewardBlock, currentBlock, 0);
        }

        [TestMethod]
        public void FinalCheckBalance()
        {
            var totalReward = _awakenFarmContract.GetTotalReward();
            Logger.Info(totalReward);
            long allBalance = 0;
            long allUsdtBalance = 0;
            var accounts = new List<string>
            {
                "ZsRNtZxq6hjkHkCRBYtAMfe1APBxdcY6rhmtQLSGeSpmNTfkf",
                "yLYKdPpCzSew2THWYYh6xSGzxpRAPTCwMWh5iE7xUTFMpzvie",
                "YjyjN2aEH8Fc1FJa6AHB2dfyN6wQZzGcFU5XA8Sp7Mjzr4CPu",
                "Xer4r1PaMe4MxbVdYPoNWpjuaWfM9pPoF5XLBdrtb2wBMjGxd"
            };

            foreach (var a in accounts)
            {
                var balance = _tokenContract.GetUserBalance(a, DistributeToken);
                var usdtBalance = _tokenContract.GetUserBalance(a, NewRewardToken);

                allBalance += balance;
                allUsdtBalance += usdtBalance;
                Logger.Info($"\n{a} : {DistributeToken} {balance}\n" +
                            $"{NewRewardToken}: {usdtBalance}");
            }

            var contractBalance =
                _tokenContract.GetUserBalance(_awakenFarmContract.ContractAddress, DistributeToken);
            var contractUsdtBalance =
                _tokenContract.GetUserBalance(_awakenFarmContract.ContractAddress, NewRewardToken);

            var tokenInfo = _tokenContract.GetTokenInfo(DistributeToken);
            // tokenInfo.Issued.ShouldBe(totalReward);
            Logger.Info($"\nContract {DistributeToken}: {contractBalance}\n" +
                        $"{NewRewardToken}: {contractUsdtBalance}");
            Logger.Info(allBalance);
            Logger.Info(allUsdtBalance);
        }

        private long CheckRewardInfo(out BigIntValue userReward, out BigIntValue userLockReward,
            out BigIntValue currentPeriodBlockUserLockReward, out long lastRewardHeight, UserInfo userInfo,
            PoolInfo poolInfo,
            long depositHeight = 0,
            long firstDepositHeight = 0)
        {
            if (poolInfo.TotalAmount.Equals(0))
            {
                userReward = 0;
                userLockReward = 0;
                currentPeriodBlockUserLockReward = 0;
                lastRewardHeight = userInfo.LastRewardBlock;
                return 0;
            }

            if (depositHeight == 0)
                depositHeight = AsyncHelper.RunSync(() => NodeManager.ApiClient.GetBlockHeightAsync());
            var checkReward = CheckReward(userInfo.LastRewardBlock, depositHeight, firstDepositHeight); //当前抵押所在期
            lastRewardHeight = checkReward["LastRewardBlock"];
            var point = poolInfo.AllocPoint;
            var all = _awakenFarmContract.GetTotalAllocPoint();

            var poolBlockReward = checkReward["BlockReward"].Mul(point).Div(all);
            var previousPoolBlockLockReward = checkReward["PreviousPeriodBlockLockReward"].Mul(point).Div(all);
            var poolLockReward = checkReward["BlockLockReward"].Mul(point).Div(all);
            var total = poolBlockReward + poolLockReward;
            var currentPeriodBlockLockReward = checkReward["CurrentPeriodBlockLockReward"].Mul(point).Div(all);
            currentPeriodBlockUserLockReward = new BigIntValue(currentPeriodBlockLockReward).Mul(userInfo.Amount)
                .Div(poolInfo.TotalAmount);

            //call的时候区块会增加导致结果有偏差
            var checkHeight = checkReward["CurrentPeriod"] == checkReward["LastPeriod"]
                ? checkReward["StartHeight"].Add((Block0 + Block1).Mul(checkReward["CurrentPeriod"]))
                : poolInfo.LastRewardBlock;
            var checkBlockReward = _awakenFarmContract.GetDistributeTokenBlockReward(checkHeight);
            Logger.Info($"\n GetDistributeTokenBlockReward: {checkBlockReward}");

            userReward = new BigIntValue(poolBlockReward.Add(previousPoolBlockLockReward)).Mul(userInfo.Amount)
                .Div(poolInfo.TotalAmount);
            userLockReward = new BigIntValue(poolLockReward).Mul(userInfo.Amount).Div(poolInfo.TotalAmount);
            Logger.Info($"\nPoolBlackReward: {poolBlockReward}\n" +
                        $"PoolLockReward: {poolLockReward}\n" +
                        $"TotalReward: {total}\n" +
                        $"UserReward: {userReward}\n" +
                        $"UserLockReward: {userLockReward}\n" +
                        $"UserCurrentPeriodLockReward: {currentPeriodBlockUserLockReward}");
            return total;
        }

        private PoolInfo CheckPoolInfo(int pid)
        {
            var poolInfo = _awakenFarmContract.GetPoolInfo(pid);
            Logger.Info($"\nAllocPoint: {poolInfo.AllocPoint}\n" +
                        $"LpToken: {poolInfo.LpToken}\n" +
                        $"TotalAmount: {poolInfo.TotalAmount}\n" +
                        $"LastRewardBlock: {poolInfo.LastRewardBlock}\n" +
                        $"AccUsdtPerShare: {poolInfo.AccUsdtPerShare}\n" +
                        $"AccDistributeTokenPerShare: {poolInfo.AccDistributeTokenPerShare}\n" +
                        $"AccLockDistributeTokenPerShare: {poolInfo.AccLockDistributeTokenPerShare}\n" +
                        $"LastAccLockDistributeTokenPerShare: {poolInfo.LastAccLockDistributeTokenPerShare}");
            return poolInfo;
        }

        private Dictionary<string, long> CheckPeriod(long currentHeight)
        {
            var periodInfo = new Dictionary<string, long>();
            var startBlockHeight = _awakenFarmContract.GetStartBlockOfDistributeToken();

            long p = -1;
            string period;
            if (currentHeight > startBlockHeight)
                p = (currentHeight - startBlockHeight - 1) / (Block0 + Block1) > 3
                    ? 3
                    : (currentHeight - startBlockHeight - 1) / (Block0 + Block1);
            if (p != -1)
                period = currentHeight > startBlockHeight.Add((Block0 + Block1).Mul(p)).Add(Block0)
                    ? "Continuous"
                    : "Concentrated";
            else
                period = "NotStart";

            var onePeriod = Block0 + Block1;
            long blockReward = 0;
            long blockLockReward = 0;
            var usdtBlockReward = CheckUsdtReward(currentHeight);
            for (var i = 0; i <= p; i++)
            {
                var halfLevel = (2 << Convert.ToInt32(i)).Div(2);
                if (period == "Concentrated")
                {
                    blockLockReward += PerBlock0.Div(halfLevel)
                        .Mul(currentHeight - onePeriod.Mul(i) - startBlockHeight + 1);
                    blockReward += i == 0 ? 0 : PerBlock1.Div(halfLevel).Mul(Block1);
                }
                else
                {
                    blockLockReward += PerBlock0.Div(halfLevel).Mul(Block0);
                    blockReward += PerBlock1.Div(halfLevel)
                        .Mul(currentHeight - onePeriod.Mul(i) - startBlockHeight - Block0 - 1);
                }
            }

            periodInfo["CurrentHeight"] = currentHeight;
            periodInfo["StartHeight"] = startBlockHeight;
            periodInfo["HalvingPeriod"] = p;
            periodInfo["MiningPeriod"] = period == "Continuous" ? 1 : 0;
            periodInfo["BlockLockReward"] = blockLockReward;
            periodInfo["BlockReward"] = blockReward;
            periodInfo["UsdtReward"] = usdtBlockReward;
            Logger.Info($"\nCurrent Height: {currentHeight}\n" +
                        $"Start Height: {startBlockHeight}\n" +
                        $"HalvingPeriod: {p}\n" +
                        $"MiningPeriod: {period}\n" +
                        $"BlockLockReward: {blockLockReward}\n" +
                        $"BlockReward: {blockReward}\n" +
                        $"UsdtReward: {usdtBlockReward}");
            return periodInfo;
        }

        private long CheckUsdtReward(long currentHeight)
        {
            var usdtStartBlock = _awakenFarmContract.GetUsdtStartBlock();
            var usdtPerBLock = _awakenFarmContract.GetUsdtPerBlock();
            var usdtEndBlock = _awakenFarmContract.GetUsdtEndBlock();

            if (currentHeight < usdtStartBlock)
                return 0;
            var checkHeight = currentHeight > usdtEndBlock ? usdtEndBlock : currentHeight;
            var usdtReward = (checkHeight.Sub(usdtStartBlock)).Mul(usdtPerBLock);
            return usdtReward;
        }

        private Dictionary<string, long> CheckReward(long lastRewardBlock, long currentHeight,
            long firstDepositHeight = 0)
        {
            var periodInfo = new Dictionary<string, long>();
            var startBlockHeight = _awakenFarmContract.GetStartBlockOfDistributeToken();
            //确定周期
            var l = Phase(lastRewardBlock);
            var p = Phase(currentHeight) > 3 ? 3 : Phase(currentHeight);

            var onePeriod = Block0 + Block1;
            long blockReward = 0; //持续挖矿收益
            long allBlockLockReward = 0; //本次高度到开始高度所有集中挖矿收益
            long blockLockReward = 0; //本次高度到上次高度集中挖矿收益/当前期集中挖矿收益
            long previousPeriodBlockLockReward = 0; //上几期集中挖矿收益 --跨期

            //上次抵押/赎回和这次抵押/赎回在同一期
            if (l == p)
            {
                var halfLevel = (2 << Convert.ToInt32(l)).Div(2);
                //集中挖矿和持续挖矿的交接
                var switchBlock = startBlockHeight + Block0 + (Block0 + Block1) * l;
                //当前周期第一个块
                //如果用户在startBlock开始之后进行抵押
                var currentPeriodStartBlock = firstDepositHeight < startBlockHeight + (Block0 + Block1) * l
                    ? startBlockHeight + (Block0 + Block1) * l
                    : firstDepositHeight;
                //在当前和上一期均在期集中挖矿期 
                if (switchBlock >= currentHeight)
                {
                    allBlockLockReward = PerBlock0.Div(halfLevel)
                        .Mul(currentHeight - currentPeriodStartBlock);
                    blockLockReward = PerBlock0.Div(halfLevel)
                        .Mul(currentHeight - lastRewardBlock);
                }
                else
                {
                    //上一次在集中挖矿期，当前在持续挖矿期
                    if (switchBlock >= lastRewardBlock)
                    {
                        allBlockLockReward = PerBlock0.Div(halfLevel)
                            .Mul(switchBlock - currentPeriodStartBlock);
                        blockReward = PerBlock1.Div(halfLevel).Mul(currentHeight - switchBlock);
                        blockLockReward = PerBlock0.Div(halfLevel)
                            .Mul(switchBlock - lastRewardBlock);
                    }
                    //当前期持续挖矿期
                    else
                    {
                        blockReward = PerBlock1.Div(halfLevel).Mul(currentHeight - lastRewardBlock);
                        blockLockReward = PerBlock0.Div(halfLevel)
                            .Mul(switchBlock - currentPeriodStartBlock);
                        allBlockLockReward = PerBlock0.Div(halfLevel)
                            .Mul(switchBlock - currentPeriodStartBlock);
                    }
                }
            }
            //跨期
            else
            {
                for (var i = l; i <= p; i++)
                {
                    //每一期的EndBlock
                    var currentEndBlock = (i + 1).Mul(onePeriod).Add(startBlockHeight);
                    var halfLevel = (2 << Convert.ToInt32(i)).Div(2);
                    //每一期的切换阶段的Block
                    var switchBlock = startBlockHeight + Block0 + (Block0 + Block1) * i;
                    if (i != p)
                    {
                        if (switchBlock > lastRewardBlock)
                        {
                            allBlockLockReward += PerBlock0.Div(halfLevel)
                                .Mul(switchBlock - lastRewardBlock);
                            blockReward += PerBlock1.Div(halfLevel).Mul(Block1);
                        }
                        //持续挖矿阶段
                        else
                        {
                            blockReward += PerBlock1.Div(halfLevel)
                                .Mul(currentEndBlock - lastRewardBlock);
                        }

                        lastRewardBlock = currentEndBlock;
                    }
                    else
                    {
                        var blockHeight = currentHeight > currentEndBlock ? currentEndBlock : currentHeight;
                        if (blockHeight > switchBlock)
                        {
                            previousPeriodBlockLockReward = allBlockLockReward; //跨期之前的所仓得到的分红直接分配
                            allBlockLockReward += PerBlock0.Div(halfLevel)
                                .Mul(switchBlock - lastRewardBlock);
                            blockReward += PerBlock1.Div(halfLevel).Mul(blockHeight - switchBlock);
                            blockLockReward = PerBlock0.Div(halfLevel)
                                .Mul(switchBlock - lastRewardBlock);
                        }
                        else
                        {
                            previousPeriodBlockLockReward = allBlockLockReward; //跨期之前的所仓得到的分红直接分配
                            allBlockLockReward += PerBlock0.Div(halfLevel)
                                .Mul(currentHeight - lastRewardBlock);
                            blockLockReward = PerBlock0.Div(halfLevel)
                                .Mul(currentHeight - lastRewardBlock);
                        }
                    }
                }
            }


            periodInfo["StartHeight"] = startBlockHeight;
            periodInfo["CurrentHeight"] = currentHeight;
            periodInfo["LastRewardBlock"] = lastRewardBlock;
            periodInfo["CurrentPeriod"] = p;
            periodInfo["LastPeriod"] = l;
            periodInfo["BlockLockReward"] = allBlockLockReward;
            periodInfo["BlockReward"] = blockReward;
            periodInfo["PreviousPeriodBlockLockReward"] = previousPeriodBlockLockReward;
            periodInfo["CurrentPeriodBlockLockReward"] = blockLockReward;
            Logger.Info($"\nCurrent Height: {currentHeight}\n" +
                        $"Start Height: {startBlockHeight}\n" +
                        $"CurrentPeriod: {p}\n" +
                        $"LastPeriod: {l}\n" +
                        $"BlockReward: {blockReward}\n" +
                        $"BlockLockReward: {allBlockLockReward}\n" +
                        $"CurrentPeriodBlockLockReward: {blockLockReward}\n" +
                        $"PreviousPeriodBlockLockReward: {previousPeriodBlockLockReward}");
            return periodInfo;
        }

        private BigIntValue GetUserLockReward(out BigIntValue claimReward, long originLastRewardHeight,
            long lastRewardHeight,
            long depositHeight, BigIntValue userLockReward)
        {
            var startBlockHeight = _awakenFarmContract.GetStartBlockOfDistributeToken();
            var endBlock = _awakenFarmContract.GetEndBlock();
            var l = Phase(originLastRewardHeight);
            var p = Phase(depositHeight) > 3 ? 3 : Phase(depositHeight);
            if (l > Phase(endBlock))
            {
                claimReward = 0;
                return 0;
            }

            var switchBlock = startBlockHeight + Block0 + (Block0 + Block1) * p;
            var currentPeriodEndBlock = startBlockHeight + (Block0 + Block1) * (p + 1);
            if (switchBlock > depositHeight)
            {
                claimReward = 0;
                return userLockReward;
            }

            var currentBlock = depositHeight > endBlock ? endBlock : depositHeight;
            var claimedBlock = lastRewardHeight < switchBlock
                ? currentBlock.Sub(switchBlock)
                : currentBlock.Sub(lastRewardHeight);
            var unClaimedBlock = currentPeriodEndBlock.Sub(currentBlock);

            var perBlockReward = userLockReward.Div(Block1);
            claimReward = perBlockReward.Mul(claimedBlock);
            var lockReward = perBlockReward.Mul(unClaimedBlock);
            return lockReward;
        }

        private long Phase(long blockHeight)
        {
            var startBlockHeight = _awakenFarmContract.GetStartBlockOfDistributeToken();
            if (blockHeight <= startBlockHeight) return 0;
            var p = (blockHeight - startBlockHeight - 1) / (Block0 + Block1);
            return p;
        }

        private long GetTotalReward()
        {
            long totalReward = 0;
            for (var i = 0; i < 4; i++)
            {
                var level = 1 << Convert.ToInt32(i);
                var period0 = Block0.Mul(PerBlock0.Div(level));
                var period1 = Block1.Mul(PerBlock1.Div(level));
                var reward = period0 + period1;
                totalReward = reward + totalReward;
            }

            return totalReward;
        }

        private BigIntValue GetPoolTwoTotalReward(BigIntValue perBlock, BigIntValue havingPeriod)
        {
            BigIntValue totalReward = 0;
            for (var i = 0; i < 4; i++)
            {
                var level = 1 << Convert.ToInt32(i);
                var reward = perBlock.Div(level).Mul(havingPeriod);
                totalReward = reward.Add(totalReward);
            }

            return totalReward;
        }

        private void InitializeOtherContract()
        {
            var initializeToken =
                _awakenTokenContract.ExecuteMethodWithResult(AwakenTokenMethod.Initialize,
                    new Awaken.Contracts.Token.InitializeInput {Owner = _awakenSwapContract.Contract});
            initializeToken.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var initializeSwap = _awakenSwapContract.ExecuteMethodWithResult(SwapMethod.Initialize,
                new Awaken.Contracts.Swap.InitializeInput
                {
                    Admin = InitAccount.ConvertAddress(),
                    AwakenTokenContractAddress = _awakenTokenContract.Contract
                });
            initializeSwap.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var admin = _awakenSwapContract.GetAdmin();
            admin.ShouldBe(InitAccount.ConvertAddress());
        }

        private void CreateToken(string symbol, int d, Address issuer)
        {
            var info = _tokenContract.GetTokenInfo(symbol);
            if (!info.Equals(new TokenInfo()))
            {
                Logger.Info($"\nAlready creat Token {symbol}, \n" +
                            $"Issuer: {info.Issuer}");
                return;
            }

            long totalSupply = 100000000;
            var t = totalSupply.Mul(Int64.Parse(new BigIntValue(10).Pow(d).Value));
            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create,
                new CreateInput
                {
                    Symbol = symbol,
                    Decimals = d,
                    Issuer = issuer,
                    TokenName = $"{symbol} token",
                    TotalSupply = t
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        private void ChangeTokenIssuer(string symbol, string issuer)
        {
            _tokenContract.SetAccount(issuer);
            var changeIssue =
                _tokenContract.ExecuteMethodWithResult(TokenMethod.ChangeTokenIssuer,
                    new ChangeTokenIssuerInput
                    {
                        NewTokenIssuer = _awakenFarmContract.Contract,
                        Symbol = symbol
                    });
            changeIssue.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var info = _tokenContract.GetTokenInfo(symbol);
            info.Issuer.ShouldBe(_awakenFarmContract.Contract);
        }

        private void ApproveLpToken(string symbol, long amount, string spender, string owner)
        {
            var result = _awakenTokenContract.ApproveLPToken(spender, owner, amount, symbol);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
    }
}