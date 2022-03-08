using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Client.Dto;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Awaken.Contracts.Farm;
using Google.Protobuf;
using log4net;
using Shouldly;
using Volo.Abp.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class AwakenFarmOneContractTest
    {
        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;
        private AwakenTokenContract _awakenTokenContract;
        private AwakenSwapContract _awakenSwapContract;
        private AwakenPoolTwoContract _awakenPoolTwoContract;
        private AwakenFarmContract _awakenFarmContract;
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }
        private string tokenAddress = "DHo2K7oUXXq3kJRs1JpuwqBJP56gqoaeSKFfuvr9x8svf3vEJ";
        private string swapAddress = "2hqsqJndRAZGzk96fsEvyuVBTAvoBjcuwTjkuyJffBPueJFrLa";
        private string farmAddress = "SsSqZWLf7Dk9NWyWyvDwuuY5nzn5n99jiscKZgRPaajZP5p8y";
        private string poolTwoAddress = "";
        private string InitAccount { get; } = "nn659b9X1BLhnu5RWmEUbuuV7J9QKVVSN54j9UmeCbF3Dve5D";
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
        private const string DistributeToken = "ATOKEN";
        private bool isNeedInitialize = false;

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("AwakenFarmContractTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-new-env-main");

            NodeManager = new NodeManager(RpcUrl);
            AuthorityManager = new AuthorityManager(NodeManager, InitAccount);
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
            // _awakenPoolTwoContract = poolTwoAddress == ""
            //     ? new AwakenPoolTwoContract(NodeManager, InitAccount)
            //     : new AwakenPoolTwoContract(NodeManager, InitAccount, poolTwoAddress);
            if (isNeedInitialize)
                InitializeOtherContract();
            CreateToken(DistributeToken, 8);
            if (_tokenContract.GetUserBalance(AdminAddress) < 100_00000000)
                _tokenContract.TransferBalance(InitAccount, AdminAddress, 100000_00000000);
            if (_tokenContract.GetUserBalance(TestAddress) < 100_00000000)
                _tokenContract.TransferBalance(InitAccount, TestAddress, 100000_00000000);
        }

        [TestMethod]
        public void InitializeTest()
        {
            var currentHeight = AsyncHelper.RunSync(() => NodeManager.ApiClient.GetBlockHeightAsync());
            var addBlock = 1000;
            var totalReward = GetTotalReward();
            Logger.Info($"TotalReward: {totalReward}, CurrentHeight: {currentHeight}");
            var admin = AdminAddress.ConvertAddress();
            var input = new InitializeInput
            {
                Admin = admin,
                LpTokenContract = _awakenTokenContract.Contract,
                StartBlock = currentHeight.Add(addBlock),
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
        }

        //ELF-ETH
        [TestMethod]
        [DataRow("ALP AAA-BBB", 200, false)]
        [DataRow("ALP BBB-CCC", 300, false)]
        [DataRow("ALP CCC-DDD", 500, false)]
        public void AddPool(string LPToken, long alloc, bool withUpdate)
        {
            var poolLength = _awakenFarmContract.GetPoolLength();
            var startBlock = _awakenFarmContract.GetStartBlockOfDistributeToken();
            var beforeTotalAlloc = _awakenFarmContract.GetTotalAllocPoint();

            _awakenFarmContract.SetAccount(AdminAddress);
            var addResult = _awakenFarmContract.AddPool(alloc, withUpdate, LPToken);
            addResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterBoolLength = _awakenFarmContract.GetPoolLength();
            afterBoolLength.ShouldBe(poolLength + 1);
            var logs = addResult.Logs.First(l => l.Name.Equals("PoolAdded")).NonIndexed;
            var poolAdded = PoolAdded.Parser.ParseFrom(ByteString.FromBase64(logs));
            poolAdded.Pid.ShouldBe(afterBoolLength - 1);
            poolAdded.Token.ShouldBe(LPToken);
            poolAdded.AllocationPoint.ShouldBe(alloc);
            poolAdded.PoolType.ShouldBe(0);
            var lastRewardBlock = addResult.BlockNumber > startBlock ? addResult.BlockNumber : startBlock;
            poolAdded.LastRewardBlockHeight.ShouldBe(lastRewardBlock);

            var poolInfo = CheckPoolInfo((int) poolAdded.Pid);
            poolInfo.AllocPoint.ShouldBe(alloc);
            poolInfo.LpToken.ShouldBe(LPToken);
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
        [DataRow(0, 1000000)]
        [DataRow(1, 1000000)]
        [DataRow(2, 1000000)]
        public void DepositTest(int pid, long amount)
        {
            //UpdatePool  ClaimRevenue  Deposit
            var depositAddress = TestAddress;
            var originPoolInfo = CheckPoolInfo(pid);
            var originTokenInfo = _tokenContract.GetTokenInfo(DistributeToken);
            var issuedReward = _awakenFarmContract.GetIssuedReward();
            var originUserInfo = _awakenFarmContract.GetUserInfo(pid, depositAddress);
            var startBlock = _awakenFarmContract.GetStartBlockOfDistributeToken();
            var symbol = originPoolInfo.LpToken;
            var originUserBalance = _awakenTokenContract.GetBalance(symbol, depositAddress.ConvertAddress());
            var originContractBalance = _awakenTokenContract.GetBalance(symbol, _awakenFarmContract.Contract);
            var originUserDistributeBalance = _tokenContract.GetUserBalance(depositAddress, DistributeToken);
            var originContractDistributeBalance = _tokenContract.GetUserBalance(_awakenFarmContract.ContractAddress, DistributeToken);

            ApproveLpToken(symbol, amount, _awakenFarmContract.ContractAddress, depositAddress);
            var pending = _awakenFarmContract.Pending(pid, depositAddress);
            Logger.Info($"Pending: {pending}");
            var lockPending = _awakenFarmContract.PendingLockDistributeToken(pid, depositAddress);
            Logger.Info($"PendingLock: {lockPending}");
            
            var depositResult = _awakenFarmContract.Deposit(pid, amount, depositAddress);
            depositResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var switchBlock = startBlock.Add(Block0).Add((Block0 + Block1).Mul(Phase(depositResult.BlockNumber)));

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
            var afterContractDistributeBalance = _tokenContract.GetUserBalance(_awakenFarmContract.ContractAddress, DistributeToken);
            
            var afterPoolInfo = CheckPoolInfo(pid);
            // 在startBlock之前进行抵押，LastRewardBlock == startBlock
            afterPoolInfo.LastRewardBlock.ShouldBe(originPoolInfo.LastRewardBlock >= depositResult.BlockNumber
                ? originPoolInfo.LastRewardBlock
                : depositResult.BlockNumber);
            afterUserInfo.LastRewardBlock.ShouldBe(originPoolInfo.LastRewardBlock >= depositResult.BlockNumber
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
            if (originUserInfo.Amount > 0 && depositResult.BlockNumber > startBlock)
            {
                var total = CheckRewardInfo(out var userReward, out var userLockReward, out var allPoolLockReward,
                    originUserInfo, originPoolInfo, depositResult.BlockNumber);
                userReward
                    .ShouldBe(afterPoolInfo.AccDistributeTokenPerShare
                        .Mul(originUserInfo.Amount).Div(Multiplier)
                        .Sub(originUserInfo.RewardDistributeTokenDebt));
                userLockReward
                    .ShouldBe(afterPoolInfo.AccLockDistributeTokenPerShare
                        .Mul(originUserInfo.Amount).Div(Multiplier)
                        .Sub(originUserInfo.RewardLockDistributeTokenDebt));
                var lockReward = userLockReward.Equals(0) || depositResult.BlockNumber < switchBlock ? allPoolLockReward : userLockReward;
                var stillLockReward = GetUserLockReward(depositResult.BlockNumber, lockReward, originPoolInfo,
                    originUserInfo);
                stillLockReward.ShouldBe(afterUserInfo.LockPending.Add(1));
               
                Logger.Info($"\n UserReward: {userReward}\n " +
                            $"UserLockReward: {userLockReward}\n " +
                            $"StillLockReward: {stillLockReward}");
                if (depositResult.BlockNumber > switchBlock)
                {
                    var claimLockAmount = lockReward.Div(Block1)
                        .Mul(afterUserInfo.LastRewardBlock - originUserInfo.LastRewardBlock)
                        .Mul(originUserInfo.Amount).Div(originPoolInfo.TotalAmount);
                    Logger.Info($"Claim lock amount: {claimLockAmount}");
                    
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
                    userReward.Add(claimLockAmount).ShouldBe(claimInfo.Amount);
                    claimInfo.Amount.ShouldBe(afterUserInfo.ClaimedAmount.Sub(originUserInfo.ClaimedAmount));
                    Logger.Info($"Claim amount: {claimInfo.Amount}");
                    claimInfo.Token.ShouldBe(DistributeToken);

                    afterUserDistributeBalance.ShouldBe(originUserDistributeBalance.Add(claimInfo.Amount));
                    afterContractDistributeBalance.ShouldBe(originContractDistributeBalance.Sub(claimInfo.Amount).Add(total));
                }
                
                afterTokenInfo.Issued.ShouldBe(originTokenInfo.Issued.Add(total));
                afterIssuedReward.ShouldBe(issuedReward.Add(total));
                var updatePoolLogDto =  depositResult.Logs.First(l => l.Name.Equals("UpdatePool"));
                var updateInfo = UpdatePool.Parser.ParseFrom(ByteString.FromBase64(updatePoolLogDto.NonIndexed));
                updateInfo.Pid.ShouldBe(pid);
                updateInfo.DistributeTokenAmount.ShouldBe(total);
                updateInfo.UpdateBlockHeight.ShouldBe(depositResult.BlockNumber);
            }

            afterPoolInfo.TotalAmount.ShouldBe(originPoolInfo.TotalAmount.Add(amount));
            afterUserInfo.Amount.ShouldBe(originUserInfo.Amount.Add(amount));
            afterUserInfo.LastRewardBlock.ShouldBe(afterPoolInfo.LastRewardBlock);
            afterUserInfo.RewardDistributeTokenDebt
                .ShouldBe(afterPoolInfo.AccDistributeTokenPerShare.Mul(afterUserInfo.Amount).Div(Multiplier));
            afterUserInfo.RewardLockDistributeTokenDebt
                .ShouldBe(afterPoolInfo.AccLockDistributeTokenPerShare.Mul(afterUserInfo.Amount).Div(Multiplier));

            afterUserBalance.Amount.ShouldBe(originUserBalance.Amount.Sub(amount));
            afterContractBalance.Amount.ShouldBe(originContractBalance.Amount.Add(amount));
        }

        [TestMethod]
        [DataRow(0)]
        public void WithdrawTest(int pid)
        {
            var depositAddress = TestAddress;
            var originPoolInfo = CheckPoolInfo(pid);
            var originTokenInfo = _tokenContract.GetTokenInfo(DistributeToken);
            var issuedReward = _awakenFarmContract.GetIssuedReward();
            var symbol = originPoolInfo.LpToken;
            var startBlock = _awakenFarmContract.GetStartBlockOfDistributeToken();
            var originUserInfo = _awakenFarmContract.GetUserInfo(pid, depositAddress);
            var originUserBalance = _awakenTokenContract.GetBalance(symbol, depositAddress.ConvertAddress());
            var originContractBalance = _awakenTokenContract.GetBalance(symbol, _awakenFarmContract.Contract);
            var originUserDistributeBalance = _tokenContract.GetUserBalance(depositAddress, DistributeToken);
            var originContractDistributeBalance =
                _tokenContract.GetUserBalance(_awakenFarmContract.ContractAddress, DistributeToken);

            var withdrawAmount = originUserInfo.Amount;
            var withdrawResult = _awakenFarmContract.Withdraw(pid, withdrawAmount, depositAddress);
            withdrawResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info(withdrawAmount);
            var withdrawLogs = withdrawResult.Logs.First(l => l.Name.Equals("Withdraw"));
            foreach (var l in withdrawLogs.Indexed)
            {
                var d = Withdraw.Parser.ParseFrom(ByteString.FromBase64(l));
                if (d.User != null)
                    d.User.ShouldBe(depositAddress.ConvertAddress());
                else
                    d.Pid.ShouldBe(pid);
            }
            var switchBlock = startBlock.Add(Block0).Add((Block0 + Block1).Mul(Phase(withdrawResult.BlockNumber)));
            var checkWithdrawAmount = Withdraw.Parser.ParseFrom(ByteString.FromBase64(withdrawLogs.NonIndexed)).Amount;
            checkWithdrawAmount.ShouldBe(withdrawAmount);
            
            var afterTokenInfo = _tokenContract.GetTokenInfo(DistributeToken);
            var afterIssuedReward = _awakenFarmContract.GetIssuedReward();
            var afterUserInfo =  _awakenFarmContract.GetUserInfo(pid, depositAddress);
            var afterPoolInfo = CheckPoolInfo(pid);
            var afterUserBalance = _awakenTokenContract.GetBalance(symbol, depositAddress.ConvertAddress());
            var afterContractBalance = _awakenTokenContract.GetBalance(symbol, _awakenFarmContract.Contract);
            var afterUserDistributeBalance = _tokenContract.GetUserBalance(depositAddress, DistributeToken);
            var afterContractDistributeBalance =
                _tokenContract.GetUserBalance(_awakenFarmContract.ContractAddress, DistributeToken);
            
            if (withdrawResult.BlockNumber > startBlock)
            {
                var total = CheckRewardInfo(out var userReward, out var userLockReward, out var allPoolLockReward,
                    originUserInfo, originPoolInfo, withdrawResult.BlockNumber);
                userReward
                    .ShouldBe(afterPoolInfo.AccDistributeTokenPerShare
                        .Mul(originUserInfo.Amount).Div(Multiplier)
                        .Sub(originUserInfo.RewardDistributeTokenDebt));
                userLockReward
                    .ShouldBe(afterPoolInfo.AccLockDistributeTokenPerShare
                        .Mul(originUserInfo.Amount).Div(Multiplier)
                        .Sub(originUserInfo.RewardLockDistributeTokenDebt));
                var lockReward = userLockReward.Equals(0)|| withdrawResult.BlockNumber < switchBlock ? allPoolLockReward : userLockReward;
                var stillLockReward = GetUserLockReward(withdrawResult.BlockNumber, lockReward, originPoolInfo,
                    originUserInfo);
                stillLockReward.ShouldBe(afterUserInfo.LockPending.Add(1));

                Logger.Info($"\n UserReward: {userReward}\n " +
                            $"UserLockReward: {userLockReward}\n " +
                            $"StillLockReward: {stillLockReward}");
                if (withdrawResult.BlockNumber > switchBlock)
                {
                    var claimLockAmount = lockReward.Div(Block1)
                        .Mul(afterUserInfo.LastRewardBlock - originUserInfo.LastRewardBlock)
                        .Mul(originUserInfo.Amount).Div(originPoolInfo.TotalAmount);
                    Logger.Info($"Claimed lock amount: {claimLockAmount}");

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
                    userReward.Add(claimLockAmount).ShouldBe(claimInfo.Amount);
                    claimInfo.Amount.ShouldBe(afterUserInfo.ClaimedAmount.Sub(originUserInfo.ClaimedAmount));
                    Logger.Info($"Claim amount: {claimInfo.Amount}");
                    claimInfo.Token.ShouldBe(DistributeToken);

                    afterUserDistributeBalance.ShouldBe(originUserDistributeBalance.Add(claimInfo.Amount));
                    afterContractDistributeBalance.ShouldBe(originContractDistributeBalance.Sub(claimInfo.Amount).Add(total));
                }
                
                afterTokenInfo.Issued.ShouldBe(originTokenInfo.Issued.Add(total));
                afterIssuedReward.ShouldBe(issuedReward.Add(total));
                var updatePoolLogDto =  withdrawResult.Logs.First(l => l.Name.Equals("UpdatePool"));
                var updateInfo = UpdatePool.Parser.ParseFrom(ByteString.FromBase64(updatePoolLogDto.NonIndexed));
                updateInfo.Pid.ShouldBe(pid);
                updateInfo.DistributeTokenAmount.ShouldBe(total);
                updateInfo.UpdateBlockHeight.ShouldBe(withdrawResult.BlockNumber);
            }

            afterPoolInfo.LastRewardBlock.ShouldBe(originPoolInfo.LastRewardBlock >= withdrawResult.BlockNumber
                ? originPoolInfo.LastRewardBlock
                : withdrawResult.BlockNumber);
            afterUserInfo.LastRewardBlock.ShouldBe(originPoolInfo.LastRewardBlock >= withdrawResult.BlockNumber
                ? originPoolInfo.LastRewardBlock
                : withdrawResult.BlockNumber);
            
            afterUserInfo.Amount.ShouldBe(originUserInfo.Amount.Sub(withdrawAmount));
            afterPoolInfo.TotalAmount.ShouldBe(originPoolInfo.TotalAmount.Sub(withdrawAmount));
            afterUserInfo.RewardDistributeTokenDebt
                .ShouldBe(afterPoolInfo.AccDistributeTokenPerShare.Mul(afterUserInfo.Amount).Div(Multiplier));
            afterUserInfo.RewardLockDistributeTokenDebt
                .ShouldBe(afterPoolInfo.AccLockDistributeTokenPerShare.Mul(afterUserInfo.Amount).Div(Multiplier));
            
            afterUserBalance.Amount.ShouldBe(originUserBalance.Amount.Add(withdrawAmount));
            afterContractBalance.Amount.ShouldBe(originContractBalance.Amount.Sub(withdrawAmount));
            
        }


        #region Abnormal Test

        [TestMethod]
        public void AuthorityTest()
        {
            //"No permission."
        }

        #endregion

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
            for (int i = 0; i < poolLength; i++)
            {
                Logger.Info($"\nPool id: {i}");
                var pending = _awakenFarmContract.Pending(i, depositAddress);
                Logger.Info($"\nPending: {pending}");
                var pendingLock = _awakenFarmContract.PendingLockDistributeToken(i, depositAddress);
                Logger.Info($"\nPendingLock: {pendingLock}");
                var userInfo = _awakenFarmContract.GetUserInfo(i, depositAddress);
                var poolInfo = CheckPoolInfo(i);
                CheckRewardInfo(out var userReward, out var userLockReward,out var allPoolLockReward, userInfo, poolInfo);
            }
        }

        private long CheckRewardInfo(out BigIntValue userReward, out BigIntValue userLockReward, out long allPoolLockReward, UserInfo userInfo, PoolInfo poolInfo, long depositHeight = 0)
        {
            if (poolInfo.TotalAmount.Equals(0))
            {
                userReward = 0;
                userLockReward = 0;
                allPoolLockReward = 0;
                return 0;
            }

            if (depositHeight == 0)
                depositHeight = AsyncHelper.RunSync(() => NodeManager.ApiClient.GetBlockHeightAsync());
            var checkReward = CheckReward(poolInfo.LastRewardBlock, depositHeight); //当前抵押所在期

            long checkHeight = 0;
            checkHeight = checkReward["CurrentPeriod"] == checkReward["LastPeriod"] 
                ? checkReward["StartHeight"].Add((Block0 + Block1).Mul(checkReward["CurrentPeriod"])) 
                : poolInfo.LastRewardBlock;

            //call的时候区块会增加导致结果有偏差
            var checkBlockReward = _awakenFarmContract.GetDistributeTokenBlockReward(checkHeight);
            checkBlockReward.BlockReward.ShouldBeGreaterThanOrEqualTo(checkReward["BlockReward"]);
            checkBlockReward.BlockLockReward.ShouldBeGreaterThanOrEqualTo(
                checkHeight == checkReward["StartHeight"].Add((Block0 + Block1).Mul(checkReward["CurrentPeriod"]))
                    ? checkReward["AllBlockLockReward"]
                    : checkReward["BlockLockReward"]);

            Logger.Info($"\n GetDistributeTokenBlockReward: {checkBlockReward}");
            
            var point = poolInfo.AllocPoint;
            var all = _awakenFarmContract.GetTotalAllocPoint();

            var poolBlockReward = checkReward["BlockReward"].Mul(point).Div(all);
            var poolLockReward = checkReward["BlockLockReward"].Mul(point).Div(all);
            var total = poolBlockReward + poolLockReward;
            allPoolLockReward = checkReward["AllBlockLockReward"].Mul(point).Div(all);
            
            var period = CheckPeriod(depositHeight);
            var perShare = new BigIntValue(period["BlockReward"])
                .Mul(Multiplier).Div(poolInfo.TotalAmount).Mul(point).Div(all);
            
            var perLockShare = new BigIntValue(period["BlockLockReward"])
                .Mul(Multiplier).Div(poolInfo.TotalAmount).Mul(point).Div(all);
            
            userReward = new BigIntValue(poolBlockReward).Mul(userInfo.Amount).Div(poolInfo.TotalAmount);
            userLockReward = new BigIntValue(poolLockReward).Mul(userInfo.Amount).Div(poolInfo.TotalAmount);
            Logger.Info($"\nPoolBlackReward: {poolBlockReward}\n" +
                        $"PoolLockReward: {poolLockReward}\n" +
                        $"TotalReward: {total}\n" +
                        $"CalculatedAccDistributeTokenPerShare: {perShare}\n" +
                        $"CalculatedAccLockDistributeTokenPerShare: {perLockShare}\n" +
                        $"UserReward: {userReward}\n" +
                        $"UserLockReward: {userLockReward}");
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
            if (currentHeight > startBlockHeight)
                p = (currentHeight - startBlockHeight - 1) / (Block0 + Block1);
            var period = currentHeight > startBlockHeight.Add((Block0 + Block1).Mul(p)).Add(Block0)
                ? "Continuous"
                : "Concentrated";
            var onePeriod = Block0 + Block1;
            long blockReward = 0;
            long blockLockReward = 0;
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
            Logger.Info($"\nCurrent Height: {currentHeight}\n" +
                        $"Start Height: {startBlockHeight}\n" +
                        $"HalvingPeriod: {p}\n" +
                        $"MiningPeriod: {period}\n" +
                        $"BlockLockReward: {blockLockReward}\n" +
                        $"BlockReward: {blockReward}");
            return periodInfo;
        }

        private Dictionary<string, long> CheckReward(long lastRewardBlock, long currentHeight)
        {
            var periodInfo = new Dictionary<string, long>();
            var startBlockHeight = _awakenFarmContract.GetStartBlockOfDistributeToken();
            //确定周期
            var l = Phase(lastRewardBlock);
            var p = Phase(currentHeight);

            var onePeriod = Block0 + Block1;
            long blockReward = 0;
            long blockLockReward = 0;
            long allBlockLockReward = 0;
            //在同一期
            if (l == p)
            {
                var halfLevel = (2 << Convert.ToInt32(l)).Div(2);
                var switchBlock = startBlockHeight + Block0 - 1 + (Block0 + Block1) * l;
                var switchHalfBlock = startBlockHeight + (Block0 + Block1) * l;
                if (switchBlock >= currentHeight)
                {
                    blockLockReward += PerBlock0.Div(halfLevel)
                        .Mul(currentHeight - lastRewardBlock);
                    allBlockLockReward += PerBlock0.Div(halfLevel)
                        .Mul(currentHeight - switchHalfBlock);
                }
                else
                {
                    if (switchBlock > lastRewardBlock)
                    {
                        blockLockReward += PerBlock0.Div(halfLevel)
                            .Mul(switchBlock - lastRewardBlock);
                        blockReward += PerBlock1.Div(halfLevel).Mul(currentHeight - switchBlock);
                    }
                    else
                    {
                        blockReward += PerBlock1.Div(halfLevel).Mul(currentHeight - lastRewardBlock);
                        allBlockLockReward += PerBlock0.Div(halfLevel)
                            .Mul(switchBlock - switchHalfBlock + 1);
                    }
                }
            }
            //跨期
            else
            {
                for (var i = l; i <= p; i++)
                {
                    var r = (i+1).Mul(onePeriod).Add(startBlockHeight);
                    var halfLevel = (2 << Convert.ToInt32(i)).Div(2);
                    var switchBlock = startBlockHeight + Block0 - 1 + (Block0 + Block1) * i;
                    if (switchBlock > lastRewardBlock)
                    {
                        blockLockReward += PerBlock0.Div(halfLevel)
                            .Mul(switchBlock - lastRewardBlock);
                        blockReward += PerBlock1.Div(halfLevel).Mul(r - switchBlock);
                    }
                    else
                    {
                        blockReward += PerBlock1.Div(halfLevel)
                            .Mul(r - lastRewardBlock);
                    }

                    lastRewardBlock = r;
                }
            }


            periodInfo["StartHeight"] = startBlockHeight;
            periodInfo["CurrentHeight"] = currentHeight;
            periodInfo["LastRewardBlock"] = lastRewardBlock;
            periodInfo["CurrentPeriod"] = p;
            periodInfo["LastPeriod"] = l;
            periodInfo["BlockLockReward"] = blockLockReward;
            periodInfo["BlockReward"] = blockReward;
            periodInfo["AllBlockLockReward"] = allBlockLockReward;
            Logger.Info($"\nCurrent Height: {currentHeight}\n" +
                        $"Start Height: {startBlockHeight}\n" +
                        $"CurrentPeriod: {p}\n" +
                        $"LastPeriod: {l}\n" +
                        $"AllBlockLockReward: {allBlockLockReward}\n" +
                        $"AddBlockLockReward: {blockLockReward}\n" +
                        $"BlockReward: {blockReward}");
            return periodInfo;
        }

        private BigIntValue GetUserLockReward(long depositHeight, BigIntValue userLockReward, PoolInfo poolInfo, UserInfo userInfo)
        {
            var startBlockHeight = _awakenFarmContract.GetStartBlockOfDistributeToken();
            var endBlock = _awakenFarmContract.GetEndBlock();
            var p = Phase(depositHeight);
            var l = Phase(poolInfo.LastRewardBlock);
            var onePeriod = Block0 + Block1;
            var all = _awakenFarmContract.GetTotalAllocPoint();
         
            if(p > Phase(endBlock)){
                return 0;
            }

            if (p == l)
            {
                var r = (l+1).Mul(onePeriod).Add(startBlockHeight);
                var switchBlock = startBlockHeight + Block0 - 1 + (Block0 + Block1) * l;
                return switchBlock >= depositHeight 
                    ? userLockReward 
                    :  new BigIntValue(userLockReward).Div(Block1).Mul(r.Sub(depositHeight));
            }
            else
            {
                var halfLevel = (2 << Convert.ToInt32(p)).Div(2);
                var switchBlock = startBlockHeight + Block0 - 1 + (Block0 + Block1) * p;
                if (switchBlock >= depositHeight)
                {
                    var block = depositHeight - (startBlockHeight + (Block0 + Block1) * p);
                    var reward = new BigIntValue(block).Mul(PerBlock0.Div(halfLevel)).Mul(poolInfo.AllocPoint).Div(all)
                        .Mul(userInfo.Amount).Div(poolInfo.TotalAmount);
                    return reward;
                }
                else
                {
                    var reward = new BigIntValue(Block0).Mul(PerBlock0.Div(halfLevel)).Mul(poolInfo.AllocPoint).Div(all)
                        .Mul(userInfo.Amount).Div(poolInfo.TotalAmount);
                    var r = p.Mul(onePeriod).Add(startBlockHeight);
                    var lockReward = reward.Div(Block1).Mul(r.Sub(depositHeight));
                    return lockReward;
                }
            }
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

            // var initializePoolTwo = _awakenPoolTwoContract.ExecuteMethodWithResult(PoolTwoMethod.Initialize,
            //     new Gandalf.Contracts.PoolTwoContract.InitializeInput
            //     {
            //         
            //     });
            // initializePoolTwo.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        private void CreateToken(string symbol, int d)
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
                    Issuer = _awakenFarmContract.Contract,
                    TokenName = $"{symbol} token",
                    TotalSupply = t
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        private void CheckEvent(TransactionResultDto resultDto)
        {
            var logs = resultDto.Logs.First(l => l.Name.Equals("Deposit"));
        }

        private void ApproveLpToken(string symbol, long amount, string spender, string owner)
        {
            var result = _awakenTokenContract.ApproveLPToken(spender, owner, amount, symbol);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
    }
}