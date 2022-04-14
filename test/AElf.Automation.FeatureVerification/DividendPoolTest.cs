using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using AElf.Client.Dto;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Gandalf.Contracts.DividendPoolContract;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using log4net.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Shouldly.Configuration;
using UserInfoStruct = Gandalf.Contracts.DividendPoolContract.UserInfoStruct;
using PoolInfoStruct = Gandalf.Contracts.DividendPoolContract.PoolInfoStruct;
using AddPool = Gandalf.Contracts.DividendPoolContract.AddPool;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class DividendPoolTest
    {
        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;
        private DividendPoolContract _dividendPoolContract;
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }
        //private string dividendPoolAddress = "";
        //private string dividendPoolAddress = "2imqjpkCwnvYzfnr61Lp2XQVN2JU17LPkA9AZzmRZzV5LRRWmR";
        private string dividendPoolAddress = "2WFKQSCSfqSC8raW8R1YnL3HxvhVhwmQLjmk31L4ds5R8tikET";
        private string InitAccount { get; } = "J6zgLjGwd1bxTBpULLXrGVeV74tnS2n74FFJJz7KNdjTYkDF6";
        private string UserA { get; } = "2oKcAgFCi2FxwyQFzCVnmNYdKZzJLyA983gEwUmyuuaVUX2d1P";
        private string UserB { get; } = "zptx91dhHVJjJRxf5Wg5KAoMrDrWX6i1H2FAyKAiv2q8VZfbg";
        private static string RpcUrl { get; } = "192.168.66.9:8000";
        private static readonly string[] DISTRIBUTETOKEN = {"APPLE","PEACH","BANANA"};
        private static readonly string[] DEPOSITTOKEN = {"D","ABC"};


        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("DividendPoolContractTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env2-main");

            NodeManager = new NodeManager(RpcUrl);
            AuthorityManager = new AuthorityManager(NodeManager, InitAccount);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
            _dividendPoolContract = dividendPoolAddress == ""
                ? new DividendPoolContract(NodeManager, InitAccount)
                : new DividendPoolContract(NodeManager, InitAccount, dividendPoolAddress);
        }
        
        
        [TestMethod]
        public void InitializeTest()
        {
            var cycle = 500;
            var initializeResult = _dividendPoolContract.Initialize(cycle);
            initializeResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //verify cycle setting
            var cycleSetting = _dividendPoolContract.CallViewMethod<Int64Value>(DividendPoolMethod.Cycle, new Empty());
            cycleSetting.Value.ShouldBe(cycle);
            
            //verify tokenlist
            var tokenListLength =
                _dividendPoolContract.CallViewMethod<Int32Value>(DividendPoolMethod.GetTokenListLength, new Empty());
            tokenListLength.Value.ShouldBe(0);
            
            //verify PoolLength
            var poolLength =
                _dividendPoolContract.CallViewMethod<Int32Value>(DividendPoolMethod.PoolLength, new Empty());
            poolLength.Value.ShouldBe(0);
            
            //verify cycle event
            var setCycleLogStr = initializeResult.Logs.First(l => l.Name.Equals("SetCycle")).NonIndexed;
            var setCycleLog = SetCycle.Parser.ParseFrom(ByteString.FromBase64(setCycleLogStr));
            setCycleLog.Cycle.ShouldBe(cycle);

            //check DEPOSITTOKEN existence and amount
            for (int i = 0; i < DEPOSITTOKEN.Length; i++)
            {
                var depositTokenResult = _tokenContract.GetTokenInfo(DEPOSITTOKEN[i]);
                if (depositTokenResult.Equals(new TokenInfo()))
                {
                    CreateToken(DEPOSITTOKEN[i], 8, InitAccount.ConvertAddress(), 10000000000000000);
                    IssueBalance(DEPOSITTOKEN[i], 1000000000000, UserA.ConvertAddress());
                    IssueBalance(DEPOSITTOKEN[i], 1000000000000, UserB.ConvertAddress());
                }
                if (_tokenContract.GetUserBalance(UserA,DEPOSITTOKEN[i]) <= 1000000000000)
                {
                    IssueBalance(DEPOSITTOKEN[i], 1000000000000, UserA.ConvertAddress());
                }
                if (_tokenContract.GetUserBalance(UserB,DEPOSITTOKEN[i]) <= 1000000000000)
                {
                    IssueBalance(DEPOSITTOKEN[i], 1000000000000, UserB.ConvertAddress());
                }
            }
            
            //check DISTRIBUTETOKEN existence and amount
            for (int i = 0; i < DISTRIBUTETOKEN.Length; i++)
            {
                var distributeTokenResult = _tokenContract.GetTokenInfo(DISTRIBUTETOKEN[i]);
                if (distributeTokenResult.Equals(new TokenInfo()))
                {
                    CreateToken(DISTRIBUTETOKEN[i], 8, InitAccount.ConvertAddress(), 10000000000000000);
                    IssueBalance(DISTRIBUTETOKEN[i], 1000000000000000, InitAccount.ConvertAddress());
                }
                else if (_tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN[i]) <= 1000000000000)
                {
                    IssueBalance(DISTRIBUTETOKEN[i], 1000000000000, InitAccount.ConvertAddress());
                }
            }
            
        }
        
        [TestMethod]
        public void InitializeTwiceTest()
        {
            var cycle = 1000;
            var result = _dividendPoolContract.Initialize(cycle);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("Already initialized");

        }

        [TestMethod]
        public void AddToken()
        {
            //add token0 & token1
            var addToken0 = _dividendPoolContract.AddToken(DISTRIBUTETOKEN[0]);
            addToken0.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var addToken1 = _dividendPoolContract.AddToken(DISTRIBUTETOKEN[1]);
            addToken1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //verify tokenlist
            var tokenList =
                _dividendPoolContract.CallViewMethod<Int32Value>(DividendPoolMethod.GetTokenListLength, new Empty());
            tokenList.Value.ShouldBe(2);
            
            //verify AddToken event
            var addTokenLogStr = addToken0.Logs.First(l => l.Name.Equals("AddToken")).NonIndexed;
            var addTokenLogs =
                Gandalf.Contracts.DividendPoolContract.AddToken.Parser.ParseFrom(ByteString.FromBase64(addTokenLogStr));
            addTokenLogs.Index.ShouldBe(1);
            addTokenLogs.TokenSymbol.ShouldBe(DISTRIBUTETOKEN[0]);
            
        }

        [TestMethod]
        public void FailedToAddToken()
        {
            // add exist token
            var addExsitToken = _dividendPoolContract.AddToken(DISTRIBUTETOKEN[0]);
            addExsitToken.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            
        }
        [TestMethod]
        public void NewReward()
        {
            var newRewardInput = new NewRewardInput();
            var currentBlock = NodeManager.ApiClient.GetBlockHeightAsync().Result;
            newRewardInput.StartBlock = currentBlock.Add(500);
            newRewardInput.Tokens.Add(DISTRIBUTETOKEN[0]);
            newRewardInput.PerBlocks.Add(100000000);
            newRewardInput.Amounts.Add(50000000000);
            
            newRewardInput.Tokens.Add(DISTRIBUTETOKEN[1]);
            newRewardInput.PerBlocks.Add(100000000);
            newRewardInput.Amounts.Add(50000000000);

            for (var i = 0; i < newRewardInput.Tokens.Count; i++)
            {
                long amount = long.Parse(newRewardInput.Amounts[i].Value);
                var token = newRewardInput.Tokens[i];
                var approveResult = _tokenContract.ApproveToken(InitAccount,
                    _dividendPoolContract.ContractAddress, amount,token);
                approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }
            
            var newRewardResult = _dividendPoolContract.NewReward(newRewardInput);
            newRewardResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //verify perblock
            var perBlock = _dividendPoolContract.CallViewMethod<BigIntValue>(DividendPoolMethod.PerBlock,
                new StringValue
                {
                    Value = DISTRIBUTETOKEN[0]
                });
            perBlock.ShouldBe(new BigIntValue(100000000));
            
            //verify startBlock
            var startBlock =
                _dividendPoolContract.CallViewMethod<Int64Value>(DividendPoolMethod.StartBlock, new Empty());
            startBlock.Value.ShouldBe(currentBlock.Add(500));
            
            //verify endBlock
            var cycle = _dividendPoolContract.CallViewMethod<Int64Value>(DividendPoolMethod.Cycle, new Empty()).Value;
            var endBlock = _dividendPoolContract.CallViewMethod<Int64Value>(DividendPoolMethod.EndBlock, new Empty()).Value;
            endBlock.ShouldBe(startBlock.Value.Add(cycle));

            //verify NewReward event
            var newRewardLogStr = newRewardResult.Logs.First(l => l.Name.Equals("NewReward")).NonIndexed;
            var newRewardLogs =
                Gandalf.Contracts.DividendPoolContract.NewReward.Parser.ParseFrom(ByteString.FromBase64(newRewardLogStr));
            newRewardLogs.Token.ShouldBe(DISTRIBUTETOKEN[0]);
            newRewardLogs.StartBlock.ShouldBe(currentBlock.Add(500));
            newRewardLogs.EndBlock.ShouldBe(startBlock.Value.Add(cycle));
            long.Parse(newRewardLogs.Amount.Value).ShouldBe(50000000000);
            long.Parse(newRewardLogs.PerBlocks.Value).ShouldBe(100000000);
            
            
            var newRewardLogSecStr = newRewardResult.Logs.Last(l => l.Name.Equals("NewReward")).NonIndexed;
            var newRewardLogsSec =
                Gandalf.Contracts.DividendPoolContract.NewReward.Parser.ParseFrom(ByteString.FromBase64(newRewardLogSecStr));
            newRewardLogsSec.Token.ShouldBe(DISTRIBUTETOKEN[1]);
            newRewardLogsSec.StartBlock.ShouldBe(currentBlock.Add(500));
            newRewardLogsSec.EndBlock.ShouldBe(startBlock.Value.Add(cycle));
            long.Parse(newRewardLogsSec.Amount.Value).ShouldBe(50000000000);
            long.Parse(newRewardLogsSec.PerBlocks.Value).ShouldBe(100000000);
            
        }

        [TestMethod]
        public void NewReward_InvalidStartBlock()
        {
            //new reward
            _dividendPoolContract.SetAccount(InitAccount);
            var currentblock = NodeManager.ApiClient.GetBlockHeightAsync().Result;
            var newRewardInput = new NewRewardInput();
            newRewardInput.StartBlock = currentblock.Sub(1);
            newRewardInput.Tokens.Add(DISTRIBUTETOKEN[0]);
            newRewardInput.PerBlocks.Add(100000000);
            newRewardInput.Amounts.Add(50000000000);
            
           _tokenContract.ApproveToken(InitAccount,
                _dividendPoolContract.ContractAddress, 50000000000,DISTRIBUTETOKEN[0]);
            
            var newRewardResult = _dividendPoolContract.NewReward(newRewardInput);
            newRewardResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
        
        }
        
        [TestMethod]
        public void NewReward_InvalidStartBlock2()
        {
            var newRewardInput = new NewRewardInput();
            var currentBlock = NodeManager.ApiClient.GetBlockHeightAsync().Result;
            var endblock = _dividendPoolContract.CallViewMethod<Int64Value>(DividendPoolMethod.EndBlock, new Empty()).Value;
            newRewardInput.StartBlock = endblock.Sub(1);
            newRewardInput.Tokens.Add(DISTRIBUTETOKEN[0]);
            newRewardInput.PerBlocks.Add(100000000);
            newRewardInput.Amounts.Add(50000000000);
            
            _tokenContract.ApproveToken(InitAccount,
                _dividendPoolContract.ContractAddress, 50000000000,DISTRIBUTETOKEN[0]);
            
            var newRewardResult = _dividendPoolContract.NewReward(newRewardInput);
            newRewardResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
        }
        
        

        [TestMethod]
        public void NewReward_InvalidToken()
        {
            var newRewardInput = new NewRewardInput();
            var currentBlock = NodeManager.ApiClient.GetBlockHeightAsync().Result;
            newRewardInput.StartBlock = currentBlock.Sub(500);
            newRewardInput.Tokens.Add(DISTRIBUTETOKEN[2]);
            newRewardInput.PerBlocks.Add(100000000);
            newRewardInput.Amounts.Add(50000000000);
            
            _tokenContract.ApproveToken(InitAccount,
                _dividendPoolContract.ContractAddress, 50000000000,DISTRIBUTETOKEN[2]);
            
            var newRewardResult = _dividendPoolContract.NewReward(newRewardInput);
            newRewardResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
        }

        [TestMethod]
        public void AddPool()
        {
            //add pool 0
            var allocationPointPoolOne = 1;
            var tokenSymbolPoolOne = DEPOSITTOKEN[0];
            var addPoolOneResult = _dividendPoolContract.Add(allocationPointPoolOne, tokenSymbolPoolOne);
            addPoolOneResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var startBlock = _dividendPoolContract.StartBlock().Value;
            var lastRewardBlock = addPoolOneResult.BlockNumber > startBlock
                ? addPoolOneResult.BlockNumber
                : startBlock;
            
            //add pool 1
            var allocationPointPoolTwo = 0;
            var tokenSymbolPoolTwo = DEPOSITTOKEN[1];
            var addPoolTwoResult = _dividendPoolContract.Add(allocationPointPoolTwo,tokenSymbolPoolTwo);
            addPoolTwoResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //verify AddPool event
            var poolAddedLogStr = addPoolOneResult.Logs.First(l => l.Name.Equals("AddPool")).NonIndexed;
            //var poolAddedLog = AddPool.Parser.ParseFrom(ByteString.FromBase64(poolAddedLogStr));
            var poolAddedLog =
                Gandalf.Contracts.DividendPoolContract.AddPool.Parser.ParseFrom(ByteString.FromBase64(poolAddedLogStr));
            poolAddedLog.Pid.ShouldBe(0);
            poolAddedLog.Token.ShouldBe(tokenSymbolPoolOne);
            poolAddedLog.AllocPoint.ShouldBe(allocationPointPoolOne);
            poolAddedLog.LastRewardBlock.ShouldBe(lastRewardBlock);

            //validate pool lenght
            var poolLength = _dividendPoolContract.PoolLength().Value;
            poolLength.ShouldBe(2);
            
            //validate pool one info
            var poolOne = _dividendPoolContract.PoolInfo(0);
            poolOne.ShouldBe(new PoolInfoStruct
            {
                LpToken = tokenSymbolPoolOne,
                AllocPoint = allocationPointPoolOne,
                LastRewardBlock = lastRewardBlock,
                TotalAmount = 0
            });
            
        }

        [TestMethod]
        public void OwnerFuncTest()
        {
            _dividendPoolContract.SetAccount(UserA);
            var addtokenresult = _dividendPoolContract.AddToken(DISTRIBUTETOKEN[2]);
            addtokenresult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);

            var newrewardresult = _dividendPoolContract.NewReward(new NewRewardInput
            {
                StartBlock = NodeManager.ApiClient.GetBlockHeightAsync().Result,
                Tokens = {DISTRIBUTETOKEN[0]},
                PerBlocks = {100000000},
                Amounts = {50000000000}
            });
            newrewardresult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);

            var setcycleresult = _dividendPoolContract.ExecuteMethodWithResult(DividendPoolMethod.SetCycle, new Int32Value
            {
                Value = 500
            });
            setcycleresult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);

            var addresult = _dividendPoolContract.Add(1, DEPOSITTOKEN[0]);
            addresult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);

            var setresult = _dividendPoolContract.Set(new SetPoolInput
            {
                Pid = 1,
                AllocationPoint = 3,
                WithUpdate = true
            });
            setresult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);

        }

        [TestMethod]
        public void Deposit_NoApprove()
        {
            var depositresult = _dividendPoolContract.Deposit(1,new BigIntValue(100000000));
            depositresult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
        }

        [TestMethod]
        public void Withdraw_Error()
        {
            _dividendPoolContract.SetAccount(UserA);

            var depositresult = _dividendPoolContract.Deposit(1,new BigIntValue(0));
            depositresult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var amount = _dividendPoolContract.UserInfo(0, UserA).Amount ?? new BigIntValue(0);
            var result = _dividendPoolContract.Withdraw(0, amount.Add(new BigIntValue(100000000)));
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);

            _dividendPoolContract.Withdraw(0, new BigIntValue("InvalidAmount")).Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);

            _dividendPoolContract.Withdraw(100, new BigIntValue(0)).Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
        }

        [TestMethod]
        public void DepositWithdrawWithOneRoundReward()
        {
            //pre-check user amount
            var useraAmountBefore = _dividendPoolContract.UserInfo(1, UserA).Amount ?? new BigIntValue(0);
            var userbAmountBefore = _dividendPoolContract.UserInfo(1, UserB).Amount ?? new BigIntValue(0);
            var useraBalanceBefore = _tokenContract.GetUserBalance(UserA, DEPOSITTOKEN[1]);
            var userbBalanceBefore = _tokenContract.GetUserBalance(UserB, DEPOSITTOKEN[1]);
            
            //usera deposit
            _dividendPoolContract.SetAccount(UserA);
            var useraapproveResult = _tokenContract.ApproveToken(UserA,
                _dividendPoolContract.ContractAddress, 1000000000,DEPOSITTOKEN[1]);
            useraapproveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var useraDeposit = _dividendPoolContract.Deposit(1, 1000000000);
            useraDeposit.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var useraAmountAfterDeposit = _dividendPoolContract.UserInfo(1, UserA).Amount;
            var useraBalanceAfterDeposit = _tokenContract.GetUserBalance(UserA, DEPOSITTOKEN[1]);
            var totalAmountAfterUserADeposit = _dividendPoolContract.PoolInfo(1).TotalAmount;
            var useraDistributeTokenAfterDeposit = _tokenContract.GetUserBalance(UserA, DISTRIBUTETOKEN[0]);

            //newreward
            _dividendPoolContract.SetAccount(InitAccount);
            var newRewardInput = new NewRewardInput();
            var currentBlock = NodeManager.ApiClient.GetBlockHeightAsync().Result;
            newRewardInput.StartBlock = currentBlock.Add(100);
            newRewardInput.Tokens.Add(DISTRIBUTETOKEN[0]);
            newRewardInput.PerBlocks.Add(100000000);
            newRewardInput.Amounts.Add(50000000000);
            var approveResult = _tokenContract.ApproveToken(InitAccount,
                _dividendPoolContract.ContractAddress, 50000000000,DISTRIBUTETOKEN[0]);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var newreward = _dividendPoolContract.NewReward(newRewardInput);
            newreward.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                
            Thread.Sleep(50 * 1000);
            
            //userb deposit    
            _dividendPoolContract.SetAccount(UserB);
            var userbapproveResult = _tokenContract.ApproveToken(UserB,
                _dividendPoolContract.ContractAddress, 1000000000,DEPOSITTOKEN[1]);
            userbapproveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var userbDeposit = _dividendPoolContract.Deposit(1, 1000000000);
            userbDeposit.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //post-check 
            var userbAmountAfterDeposit = _dividendPoolContract.UserInfo(1, UserB).Amount;
            var userbBalanceAfterDeposit = _tokenContract.GetUserBalance(UserB, DEPOSITTOKEN[1]);
            var totalAmountAfterUserbDeposit = _dividendPoolContract.PoolInfo(1).TotalAmount;
            var userbDistributeTokenAfterDeposit = _tokenContract.GetUserBalance(UserB, DISTRIBUTETOKEN[0]);
            
            //usera withdraw
            _dividendPoolContract.SetAccount(UserA);
            var useraAmount = _dividendPoolContract.UserInfo(1, UserA).Amount;
            var useraWithdraw = _dividendPoolContract.Withdraw(1, useraAmount);
            useraWithdraw.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //post-check usera
            var useraAmountAfterWithdraw = _dividendPoolContract.UserInfo(1, UserA).Amount;
            var useraBalanceAfterWithdraw = _tokenContract.GetUserBalance(UserA, DEPOSITTOKEN[1]);
            var useraDistributeTokenAfterWithdraw = _tokenContract.GetUserBalance(UserA, DISTRIBUTETOKEN[0]);
            var totalAmountAfterUseraWithdraw = _dividendPoolContract.PoolInfo(1).TotalAmount;

            //verify accpershare after usera withdraw
            var accpershareafteruserawithdraw = _dividendPoolContract.AccPerShare(1, DISTRIBUTETOKEN[0]);
    
            //wait till the end
            var endlbock = _dividendPoolContract.EndBlock().Value;
            var sleeptime = (int)endlbock.Sub(NodeManager.ApiClient.GetBlockHeightAsync().Result).Div(2);
            Thread.Sleep((sleeptime + 30) * 1000);
            
            //check pending
            var pendingresult = _dividendPoolContract.Pending(1, UserB);

            //userb withdraw
            _dividendPoolContract.SetAccount(UserB);
            var userbAmount = _dividendPoolContract.UserInfo(1, UserB).Amount;
            var userbWithdraw = _dividendPoolContract.Withdraw(1, userbAmount);
            userbWithdraw.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //post-check userb 
            var userbAmountAfterWithdraw = _dividendPoolContract.UserInfo(1, UserB).Amount;
            var userbBalanceAfterWithdraw = _tokenContract.GetUserBalance(UserB, DEPOSITTOKEN[1]);
            var userbDistributeTokenAfterWithdraw = _tokenContract.GetUserBalance(UserB, DISTRIBUTETOKEN[0]);
            
            //verify usera amount and token
            useraAmountAfterDeposit.Sub(useraAmountBefore)
                .ShouldBe(new BigIntValue(useraBalanceBefore.Sub(useraBalanceAfterDeposit)));

            useraAmountAfterDeposit.Sub(useraAmountAfterWithdraw)
                .ShouldBe(new BigIntValue(useraBalanceAfterWithdraw.Sub(useraBalanceAfterDeposit)));
            
            
            //verify userb amount and token
            userbAmountAfterDeposit.Sub(userbAmountBefore)
                .ShouldBe(new BigIntValue(userbBalanceBefore.Sub(userbBalanceAfterDeposit)));

            userbAmountAfterDeposit.Sub(userbAmountAfterWithdraw)
                .ShouldBe(new BigIntValue(userbBalanceAfterWithdraw.Sub(userbBalanceAfterDeposit)));
            
            //verify usera reward
            var useraReward =
                Reward(useraDeposit.BlockNumber, userbDeposit.BlockNumber, DISTRIBUTETOKEN[0],
                    totalAmountAfterUserADeposit, useraAmountAfterDeposit, 1).Add(Reward(userbDeposit.BlockNumber,
                    useraWithdraw.BlockNumber, DISTRIBUTETOKEN[0], totalAmountAfterUserbDeposit,
                    useraAmountAfterDeposit, 1, out var accpershare, out var pendingtotalreward));
            Logger.Info($"usera reward ({useraReward})");
            useraReward.ShouldBe(new BigIntValue(useraDistributeTokenAfterWithdraw.Sub(useraDistributeTokenAfterDeposit)));

            //verfify userb reward
            var userbReward =
                Reward(userbDeposit.BlockNumber, useraWithdraw.BlockNumber, DISTRIBUTETOKEN[0],
                    totalAmountAfterUserbDeposit, userbAmountAfterDeposit, 1).Add(Reward(useraWithdraw.BlockNumber,
                    userbWithdraw.BlockNumber, DISTRIBUTETOKEN[0], totalAmountAfterUseraWithdraw,
                    userbAmountAfterDeposit, 1));
            Logger.Info($"userb reward ({userbReward})");
            userbReward.ShouldBe(new BigIntValue(userbDistributeTokenAfterWithdraw.Sub(userbDistributeTokenAfterDeposit)));
            
            // verify pending
            pendingresult.Amounts[0].ShouldBe(userbReward);

            //verify total reward equal to newreward amount
            useraReward.Add(userbReward).ShouldBeLessThanOrEqualTo(new BigIntValue(50000000000));

            //verify deposit event
            var useraDepositLogStr = useraDeposit.Logs.First(l => l.Name.Equals("Deposit")).NonIndexed;
            var useraDepositLogs = Deposit.Parser.ParseFrom(ByteString.FromBase64(useraDepositLogStr));
            useraDepositLogs.Amount.ShouldBe(new BigIntValue(1000000000));
            useraDepositLogs.Pid.ShouldBe(1);
            useraDepositLogs.User.ShouldBe(UserA.ConvertAddress());
            
            //verify withdraw event
            var useraWithdrawLogStr = useraWithdraw.Logs.First(l => l.Name.Equals("Withdraw")).NonIndexed;
            var useraWithdrawLogs = Withdraw.Parser.ParseFrom(ByteString.FromBase64(useraWithdrawLogStr));
            useraWithdrawLogs.Amount.ShouldBe(useraAmount);
            useraWithdrawLogs.Pid.ShouldBe(1);
            useraWithdrawLogs.User.ShouldBe(UserA.ConvertAddress());
            
            //verify updatepool event
            var updatePoolLogStr = useraWithdraw.Logs.First(l => l.Name.Equals("UpdatePool")).NonIndexed;
            var updatePoolLogs = UpdatePool.Parser.ParseFrom(ByteString.FromBase64(updatePoolLogStr));
            updatePoolLogs.Pid.ShouldBe(1);
            updatePoolLogs.Reward.ShouldBe(pendingtotalreward);
            updatePoolLogs.Token.ShouldBe(DISTRIBUTETOKEN[0]);
            updatePoolLogs.BlockHeigh.ShouldBe(useraWithdraw.BlockNumber);
            updatePoolLogs.AccPerShare.ShouldBe(accpershareafteruserawithdraw);
            
            //verify harvest
            var harvestLogStr = useraWithdraw.Logs.First(l => l.Name.Equals("Harvest")).NonIndexed;
            var harvestLogs = Harvest.Parser.ParseFrom(ByteString.FromBase64(harvestLogStr));
            harvestLogs.Amount.ShouldBe(useraReward);
            harvestLogs.Pid.ShouldBe(1);
            harvestLogs.To.ShouldBe(UserA.ConvertAddress());
            harvestLogs.Token.ShouldBe(DISTRIBUTETOKEN[0]);
        }

        [TestMethod]
        public void DepositWithdrawWithTwoRoundReward()
        {
            //usera deposit
            _dividendPoolContract.SetAccount(UserA);
            var useraapproveResult = _tokenContract.ApproveToken(UserA,
                _dividendPoolContract.ContractAddress, 1000000000,DEPOSITTOKEN[0]);
            useraapproveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var useraDeposit = _dividendPoolContract.Deposit(0, 1000000000);
            useraDeposit.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //postcheck
            var totalAmountAfterUseraDeposit = _dividendPoolContract.PoolInfo(0).TotalAmount;
            var useraAmountAfterDeposit = _dividendPoolContract.UserInfo(0, UserA).Amount;
            var dt0AfterDeposit = _tokenContract.GetUserBalance(UserA, DISTRIBUTETOKEN[0]);
            var dt1AfterDeposit = _tokenContract.GetUserBalance(UserA, DISTRIBUTETOKEN[1]);
            
            //newreward round 1
            _dividendPoolContract.SetAccount(InitAccount);
            var newRewardInput = new NewRewardInput();
            var currentBlock = NodeManager.ApiClient.GetBlockHeightAsync().Result;
            newRewardInput.StartBlock = currentBlock.Add(100);
            newRewardInput.Tokens.Add(DISTRIBUTETOKEN[0]);
            newRewardInput.PerBlocks.Add(100000000);
            newRewardInput.Amounts.Add(50000000000);
            newRewardInput.Tokens.Add(DISTRIBUTETOKEN[1]);
            newRewardInput.PerBlocks.Add(200000000);
            newRewardInput.Amounts.Add(100000000000);

            var dt0ApproveResult = _tokenContract.ApproveToken(InitAccount,
                _dividendPoolContract.ContractAddress, 50000000000,DISTRIBUTETOKEN[0]);
            dt0ApproveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var dt1ApproveResult = _tokenContract.ApproveToken(InitAccount,
                _dividendPoolContract.ContractAddress, 100000000000,DISTRIBUTETOKEN[1]);
            dt1ApproveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var newreward = _dividendPoolContract.NewReward(newRewardInput);
            newreward.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var oricycle = _dividendPoolContract.Cycle().Value;
            var dt0TotalRewardRoundOne = _dividendPoolContract.PerBlock(DISTRIBUTETOKEN[0])
                .Mul(oricycle)
                .Mul(_dividendPoolContract.PoolInfo(0).AllocPoint)
                .Mul(useraAmountAfterDeposit)
                .Div(_dividendPoolContract.PoolInfo(0).TotalAmount.Mul(_dividendPoolContract.TotalAllocPoint()));
            var dt1TotalRewardRoundOne = _dividendPoolContract.PerBlock(DISTRIBUTETOKEN[1])
                .Mul(oricycle)
                .Mul(_dividendPoolContract.PoolInfo(0).AllocPoint)
                .Div(_dividendPoolContract.PoolInfo(0).TotalAmount.Mul(_dividendPoolContract.TotalAllocPoint()));
            //set cycle during round one
            var setcycle = _dividendPoolContract.SetCycle(300);

            //wait till the end
            var endblock = _dividendPoolContract.EndBlock().Value;
            var sleeptime = (int)endblock.Sub(NodeManager.ApiClient.GetBlockHeightAsync().Result).Div(2);
            Thread.Sleep((sleeptime + 30) * 1000);

            var dt0MidReward = Reward(useraDeposit.BlockNumber, endblock, DISTRIBUTETOKEN[0],
                totalAmountAfterUseraDeposit, useraAmountAfterDeposit, 0);
            var dt1MidReward = Reward(useraDeposit.BlockNumber, endblock, DISTRIBUTETOKEN[1],
                totalAmountAfterUseraDeposit, useraAmountAfterDeposit, 0);
            
            //set pool alloc
            var setpool = _dividendPoolContract.Set(new SetPoolInput
            {
                Pid = 1,
                AllocationPoint = 1,
                WithUpdate = true
            });
            
            //newreward round 2
            _dividendPoolContract.SetAccount(InitAccount);
            newRewardInput.StartBlock = NodeManager.ApiClient.GetBlockHeightAsync().Result.Add(100);
            var dt0ApproveResultRound2 = _tokenContract.ApproveToken(InitAccount,
                _dividendPoolContract.ContractAddress, 50000000000,DISTRIBUTETOKEN[0]);
            dt0ApproveResultRound2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var dt1ApproveResultRound2 = _tokenContract.ApproveToken(InitAccount,
                _dividendPoolContract.ContractAddress, 100000000000,DISTRIBUTETOKEN[1]);
            dt1ApproveResultRound2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var newrewardResultRound2 = _dividendPoolContract.NewReward(newRewardInput);
            newrewardResultRound2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var cycle = _dividendPoolContract.Cycle().Value;
            var dt0TotalRewardRoundTwo = _dividendPoolContract.PerBlock(DISTRIBUTETOKEN[0])
                .Mul(cycle)
                .Mul(_dividendPoolContract.PoolInfo(0).AllocPoint)
                .Mul(useraAmountAfterDeposit)
                .Div(_dividendPoolContract.PoolInfo(0).TotalAmount.Mul(_dividendPoolContract.TotalAllocPoint()));
            
            var dt1TotalRewardRoundTwo = _dividendPoolContract.PerBlock(DISTRIBUTETOKEN[1])
                .Mul(cycle)
                .Mul(_dividendPoolContract.PoolInfo(0).AllocPoint)
                .Mul(useraAmountAfterDeposit)
                .Div(_dividendPoolContract.PoolInfo(0).TotalAmount.Mul(_dividendPoolContract.TotalAllocPoint()));
            
            //sleep till the end
            var endblockroundtwo = _dividendPoolContract.EndBlock().Value;
            var sleeptimeroundtwo = (int)endblockroundtwo.Sub(NodeManager.ApiClient.GetBlockHeightAsync().Result).Div(2);
            Thread.Sleep((sleeptimeroundtwo + 30) * 1000);

            //get pending
            Logger.Info(_dividendPoolContract.Pending(1,UserA));
            //usera withdraw
            _dividendPoolContract.SetAccount(UserA);
            var useraAmount = _dividendPoolContract.UserInfo(0, UserA).Amount;
            var useraWithdraw = _dividendPoolContract.Withdraw(0, useraAmount);
            useraWithdraw.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //post-check usera
            var dt0AfterWithdraw = _tokenContract.GetUserBalance(UserA, DISTRIBUTETOKEN[0]);
            var dt1AfterWithdraw = _tokenContract.GetUserBalance(UserA, DISTRIBUTETOKEN[1]);

            //verify reward
            var dt0Reward = dt0MidReward.Add(Reward(newRewardInput.StartBlock,
                useraWithdraw.BlockNumber, DISTRIBUTETOKEN[0], totalAmountAfterUseraDeposit, useraAmountAfterDeposit,
                0));

            var dt0ExactReward = new BigIntValue(dt0AfterWithdraw.Sub(dt0AfterDeposit));
            dt0ExactReward.ShouldBeInRange(dt0Reward.Sub(1), dt0Reward.Add(1));
            dt0ExactReward.ShouldBeInRange(dt0TotalRewardRoundOne.Add(dt0TotalRewardRoundTwo).Sub(2),
                dt0TotalRewardRoundOne.Add(dt0TotalRewardRoundTwo).Add(2));
            
            Logger.Info(dt0Reward);
            Logger.Info(dt0TotalRewardRoundOne);
            Logger.Info(dt0TotalRewardRoundTwo);
            
            
            var dt1Reward = dt1MidReward.Add(Reward(newRewardInput.StartBlock,
                useraWithdraw.BlockNumber, DISTRIBUTETOKEN[1], totalAmountAfterUseraDeposit, useraAmountAfterDeposit,
                0));

            var dt1ExactReward = new BigIntValue(dt1AfterWithdraw.Sub(dt1AfterDeposit));
            dt1ExactReward.ShouldBeInRange(dt1Reward.Sub(1),dt1Reward.Add(1));
            dt1ExactReward.ShouldBeInRange(dt1TotalRewardRoundOne.Add(dt1TotalRewardRoundTwo).Sub(2),
                dt1TotalRewardRoundOne.Add(dt1TotalRewardRoundTwo).Add(2));
            
            Logger.Info(dt1Reward);
            Logger.Info(dt1TotalRewardRoundOne);
            Logger.Info(dt1TotalRewardRoundTwo);
            
            //verify setpool event
            
            var setPoolLogStr = setpool.Logs.First(l => l.Name.Equals("SetPool")).NonIndexed;
            var setPoolLogs = SetPool.Parser.ParseFrom(ByteString.FromBase64(setPoolLogStr));
            setPoolLogs.Pid.ShouldBe(1);
            setPoolLogs.AllocationPoint.ShouldBe(1);
            
            //verify setcycle event
            var setCycleLogStr = setcycle.Logs.First(l => l.Name.Equals("SetCycle")).NonIndexed;
            var setCycleLogs = SetCycle.Parser.ParseFrom(ByteString.FromBase64(setCycleLogStr));
            setCycleLogs.Cycle.ShouldBe(300);

        }

        [TestMethod]
        public void TwoUserDepositWithdrawWithTwoRound()
        {
            //newreward
            _dividendPoolContract.SetAccount(InitAccount);
            var newRewardInput = new NewRewardInput();
            var currentBlock = NodeManager.ApiClient.GetBlockHeightAsync().Result;
            newRewardInput.StartBlock = currentBlock.Add(100);
            newRewardInput.Tokens.Add(DISTRIBUTETOKEN[0]);
            newRewardInput.PerBlocks.Add(100000000);
            newRewardInput.Amounts.Add(20000000000);
            var approveResult = _tokenContract.ApproveToken(InitAccount,
                _dividendPoolContract.ContractAddress, 20000000000,DISTRIBUTETOKEN[0]);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var newreward = _dividendPoolContract.NewReward(newRewardInput);
            newreward.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                
            Thread.Sleep(50 * 1000);
            
            //usera deposit
            _dividendPoolContract.SetAccount(UserA);
            var useraapproveResult = _tokenContract.ApproveToken(UserA,
                _dividendPoolContract.ContractAddress, 1000000000,DEPOSITTOKEN[0]);
            useraapproveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var useraDeposit = _dividendPoolContract.Deposit(0, 1000000000);
            useraDeposit.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var useraAmountAfterDeposit = _dividendPoolContract.UserInfo(0, UserA).Amount;
            var totalAmountAfterUserADeposit = _dividendPoolContract.PoolInfo(0).TotalAmount;
            var useraDistributeTokenAfterDeposit = _tokenContract.GetUserBalance(UserA, DISTRIBUTETOKEN[0]);

            //userb deposit    
            _dividendPoolContract.SetAccount(UserB);
            var userbapproveResult = _tokenContract.ApproveToken(UserB,
                _dividendPoolContract.ContractAddress, 1000000000,DEPOSITTOKEN[0]);
            userbapproveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var userbDeposit = _dividendPoolContract.Deposit(0, 1000000000);
            userbDeposit.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //post-check 
            var userbAmountAfterDeposit = _dividendPoolContract.UserInfo(0, UserB).Amount;
            var totalAmountAfterUserbDeposit = _dividendPoolContract.PoolInfo(0).TotalAmount;
            var userbDistributeTokenAfterDeposit = _tokenContract.GetUserBalance(UserB, DISTRIBUTETOKEN[0]);
            
            //wait till the end
            var endblock = _dividendPoolContract.EndBlock().Value;
            var sleeptime = (int)endblock.Sub(NodeManager.ApiClient.GetBlockHeightAsync().Result).Div(2);
            Thread.Sleep((sleeptime + 30) * 1000);
            
            //reward
            var useraRewardRoundOne =
                Reward(useraDeposit.BlockNumber, userbDeposit.BlockNumber, DISTRIBUTETOKEN[0],
                    totalAmountAfterUserADeposit, useraAmountAfterDeposit, 0).Add(Reward(userbDeposit.BlockNumber,
                    endblock, DISTRIBUTETOKEN[0], totalAmountAfterUserbDeposit, useraAmountAfterDeposit, 0));
            var userbRewardRoundOne = Reward(userbDeposit.BlockNumber, endblock, DISTRIBUTETOKEN[0],
                totalAmountAfterUserbDeposit, userbAmountAfterDeposit, 0);
            
            //newreward round 2
            _dividendPoolContract.SetAccount(InitAccount);
            newRewardInput.StartBlock = NodeManager.ApiClient.GetBlockHeightAsync().Result.Add(100);
            var approveResultRound2 = _tokenContract.ApproveToken(InitAccount,
                _dividendPoolContract.ContractAddress, 20000000000,DISTRIBUTETOKEN[0]);
            approveResultRound2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var newrewardResultRound2 = _dividendPoolContract.NewReward(newRewardInput);
            newrewardResultRound2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            Thread.Sleep(50 * 1000);

            //usera withdraw
            _dividendPoolContract.SetAccount(UserA);
            var useraAmount = _dividendPoolContract.UserInfo(0, UserA).Amount;
            var useraWithdraw = _dividendPoolContract.Withdraw(0, useraAmount);
            useraWithdraw.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //post-check usera
            var useraDistributeTokenAfterWithdraw = _tokenContract.GetUserBalance(UserA, DISTRIBUTETOKEN[0]);
            var totalAmountAfterUseraWithdraw = _dividendPoolContract.PoolInfo(0).TotalAmount;

            //userb withdraw
            _dividendPoolContract.SetAccount(UserB);
            var userbAmount = _dividendPoolContract.UserInfo(0, UserB).Amount;
            var userbWithdraw = _dividendPoolContract.Withdraw(0, userbAmount);
            userbWithdraw.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //post-check userb 
            var userbDistributeTokenAfterWithdraw = _tokenContract.GetUserBalance(UserB, DISTRIBUTETOKEN[0]);

            //reward
            var useraRewardRoundTwo = Reward(_dividendPoolContract.StartBlock().Value, useraWithdraw.BlockNumber,
                DISTRIBUTETOKEN[0], totalAmountAfterUserbDeposit, useraAmountAfterDeposit, 0);
            var userbRewardRoundTwo =
                Reward(_dividendPoolContract.StartBlock().Value, useraWithdraw.BlockNumber,DISTRIBUTETOKEN[0],
                    totalAmountAfterUserbDeposit, userbAmountAfterDeposit, 0).Add(Reward(useraWithdraw.BlockNumber,
                    userbWithdraw.BlockNumber, DISTRIBUTETOKEN[0], totalAmountAfterUseraWithdraw,
                    userbAmountAfterDeposit, 0));
            var useraReward = useraRewardRoundOne.Add(useraRewardRoundTwo);
            var userbReward = userbRewardRoundOne.Add(userbRewardRoundTwo);
            
            //verify reward
            useraReward.ShouldBe(useraDistributeTokenAfterWithdraw.Sub(useraDistributeTokenAfterDeposit));
            userbReward.ShouldBe(userbDistributeTokenAfterWithdraw.Sub(userbDistributeTokenAfterDeposit));
            Logger.Info(useraReward);
            Logger.Info(userbReward);
            
        }

        [TestMethod]
        public void UserRewardDebtTest()
        {
            _dividendPoolContract.SetAccount(UserB);
            _tokenContract.ApproveToken(UserB, _dividendPoolContract.ContractAddress, 100000000, DEPOSITTOKEN[0]);
            
            //first deposit
            Logger.Info(_tokenContract.GetUserBalance(UserB,DEPOSITTOKEN[0]));
            var firstdeposit = _dividendPoolContract.Deposit(0, 100000000);
            firstdeposit.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var totalamountbefore = _dividendPoolContract.PoolInfo(0).TotalAmount;
            var useramountbefore = _dividendPoolContract.UserInfo(0, UserB).Amount;
            var distributeTokenBalanceBefore = _tokenContract.GetUserBalance(UserB, DISTRIBUTETOKEN[0]);

            //new reward
            _dividendPoolContract.SetAccount(InitAccount);
            var newRewardInput = new NewRewardInput();
            var currentBlock = NodeManager.ApiClient.GetBlockHeightAsync().Result;
            newRewardInput.StartBlock = currentBlock.Add(100);
            newRewardInput.Tokens.Add(DISTRIBUTETOKEN[0]);
            newRewardInput.PerBlocks.Add(100000000);
            newRewardInput.Amounts.Add(50000000000);
            newRewardInput.Tokens.Add(DISTRIBUTETOKEN[1]);
            newRewardInput.PerBlocks.Add(200000000);
            newRewardInput.Amounts.Add(100000000000);
            var token0ApproveResult = _tokenContract.ApproveToken(InitAccount,
                _dividendPoolContract.ContractAddress, 50000000000,DISTRIBUTETOKEN[0]);
            
            var token1ApproveResult = _tokenContract.ApproveToken(InitAccount,
                _dividendPoolContract.ContractAddress, 100000000000,DISTRIBUTETOKEN[1]);
            token0ApproveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            token1ApproveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var newreward = _dividendPoolContract.NewReward(newRewardInput);
            newreward.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            Thread.Sleep(60 * 1000);
            
            _dividendPoolContract.SetAccount(UserB);

            //second deposit
            _tokenContract.ApproveToken(UserB, _dividendPoolContract.ContractAddress, 500000000, DEPOSITTOKEN[0]);
            var seconddeposit = _dividendPoolContract.Deposit(0, 500000000);
            seconddeposit.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var midtotalamount = _dividendPoolContract.PoolInfo(0).TotalAmount;
            var miduseramount = _dividendPoolContract.UserInfo(0, UserB).Amount;

            //verify user reward debt
            var debt = _dividendPoolContract.CallViewMethod<BigIntValue>(DividendPoolMethod.RewardDebt, new RewardDebtInput
            {
                Pid = 0,
                User = UserA.ConvertAddress(),
                Token = DISTRIBUTETOKEN[0]
            });
            
            //third deposit
            _tokenContract.ApproveToken(UserB, _dividendPoolContract.ContractAddress, 400000000, DEPOSITTOKEN[0]);
            var thirddeposit = _dividendPoolContract.Deposit(0, 400000000);
            thirddeposit.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var distributeTokenBalanceAfter = _tokenContract.GetUserBalance(UserB, DISTRIBUTETOKEN[0]);
            
            var midreward = Reward(_dividendPoolContract.StartBlock().Value, seconddeposit.BlockNumber,
                DISTRIBUTETOKEN[0], totalamountbefore, useramountbefore, 0);

            var reward = Reward(seconddeposit.BlockNumber, thirddeposit.BlockNumber, DISTRIBUTETOKEN[0], midtotalamount,
                miduseramount, 0);
            
            Logger.Info(reward);
            Logger.Info(midreward);
   
            reward.Add(midreward).ShouldBe(distributeTokenBalanceAfter.Sub(distributeTokenBalanceBefore));
            
        }
        private BigIntValue Reward(long lastrewardblock, long rewardblock, string tokensymbol, BigIntValue totalamount, BigIntValue useramount, int pid, out BigIntValue accpershare, out BigIntValue pendingtotalreward)
        {
            var startblock = _dividendPoolContract.StartBlock().Value;
            lastrewardblock = lastrewardblock > startblock
                ? lastrewardblock
                : startblock;
                 
            var endblock = _dividendPoolContract.EndBlock().Value;
            rewardblock = rewardblock > endblock
                ? endblock
                : rewardblock;

            if (rewardblock.Sub(lastrewardblock) < 0)
            {
                pendingtotalreward = 0;
                accpershare = new BigIntValue(0);
                return new BigIntValue(0);
            }
            var perblock =
                _dividendPoolContract.CallViewMethod<BigIntValue>(DividendPoolMethod.PerBlock,
                    new StringValue
                    {
                        Value = tokensymbol
                    });
            var poolalloc = _dividendPoolContract.PoolInfo(pid).AllocPoint;
            var totalalloc =
                _dividendPoolContract.CallViewMethod<Int64Value>(DividendPoolMethod.TotalAllocPoint, new Empty()).Value;
            accpershare = GetMultiplier(DISTRIBUTETOKEN[0])
                .Mul(new BigIntValue(rewardblock.Sub(lastrewardblock)))
                .Mul(new BigIntValue(poolalloc))
                .Div(totalamount.Mul(totalalloc));
            pendingtotalreward = perblock
                .Mul(new BigIntValue(rewardblock.Sub(lastrewardblock)))
                .Mul(new BigIntValue(poolalloc))
                .Div(new BigIntValue(totalalloc));

            return perblock
                .Mul(new BigIntValue(rewardblock.Sub(lastrewardblock)))
                .Mul(useramount)
                .Mul(new BigIntValue(poolalloc))
                .Div(totalamount.Mul(totalalloc));
        }
        
        private BigIntValue Reward(long lastrewardblock, long rewardblock, string tokensymbol, BigIntValue totalamount, BigIntValue useramount, int pid)
        {
            var startblock = _dividendPoolContract.StartBlock().Value;
            lastrewardblock = lastrewardblock > startblock
                ? lastrewardblock
                : startblock;
                 
            var endblock = _dividendPoolContract.EndBlock().Value;
            rewardblock = rewardblock > endblock
                ? endblock
                : rewardblock;

            if (rewardblock.Sub(lastrewardblock) < 0)
            {
                return new BigIntValue(0);
            }
            var perblock =
                _dividendPoolContract.CallViewMethod<BigIntValue>(DividendPoolMethod.PerBlock,
                    new StringValue
                    {
                        Value = tokensymbol
                    });
            var poolalloc = _dividendPoolContract.PoolInfo(pid).AllocPoint;
            var totalalloc =
                _dividendPoolContract.CallViewMethod<Int64Value>(DividendPoolMethod.TotalAllocPoint, new Empty()).Value;
            Logger.Info(perblock);
            Logger.Info(new BigIntValue(rewardblock.Sub(lastrewardblock)));
            Logger.Info(rewardblock);
            Logger.Info(lastrewardblock);
            Logger.Info(useramount);
            Logger.Info(new BigIntValue(poolalloc));
            Logger.Info(totalamount.Mul(totalalloc));
            Logger.Info(totalamount);
            Logger.Info(totalalloc);
            return perblock
                .Mul(new BigIntValue(rewardblock.Sub(lastrewardblock)))
                .Mul(useramount)
                .Mul(new BigIntValue(poolalloc))
                .Div(totalamount.Mul(totalalloc));
        }
        
        private BigIntValue GetMultiplier(string token)
        {
            var tokenInfo = _tokenContract.GetTokenInfo(token);
            var decimals = tokenInfo.Decimals;
            int multiples = 30;
            return new BigIntValue
            {
                Value = "10"
            }.Pow(multiples.Sub(decimals));
        }
        
        private void CreateToken(string symbol, int decimals, Address issuer, long totalSupply)
        {
            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create,
                new AElf.Contracts.MultiToken.CreateInput
                {
                    Symbol = symbol,
                    Decimals = decimals,
                    Issuer = issuer,
                    TokenName = $"{symbol} token",
                    TotalSupply = totalSupply,
                    IsBurnable = true
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
        
        private void IssueBalance(string symbol, long amount, Address toAddress)
        {
            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Issue, new IssueInput
            {
                Symbol = symbol,
                Amount = amount,
                To = toAddress,
            });
            
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"Successfully issue amount {amount} to {toAddress}");
        }
        
    }
}