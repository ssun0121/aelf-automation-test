using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Client.Dto;
using AElf.Contracts.Genesis;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Awaken.Contracts.PoolTwoContract;
using Awaken.Contracts.Token;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using log4net.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Shouldly.Configuration;
using CreateInput = AElf.Contracts.MultiToken.CreateInput;
using GetBalanceOutput = AElf.Client.MultiToken.GetBalanceOutput;
using IssueInput = AElf.Contracts.MultiToken.IssueInput;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class GandalfFarmContractTest
    {
        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;
        private GandalfFarmContract _gandalfFarmContract;
        private AwakenTokenContract _awakenTokenContract;
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }
        //private string farmPoolTwoAddress = "";
        private string farmPoolTwoAddress = "uBvnFUUKG43qfnjPqoXB8S4nHkHaPXYgjMDn5B2CRPigUeM7B";
        //private string tokenAddress = "";
        private string tokenAddress = "2eKvgivaCmqPXhkvpS2UVo2qpYskPzFVSYsw6s93jTa9kUh44h";
        private string InitAccount { get; } = "nn659b9X1BLhnu5RWmEUbuuV7J9QKVVSN54j9UmeCbF3Dve5D";
        private string UserB { get; } = "YUW9zH5GhRboT5JK4vXp5BLAfCDv28rRmTQwo418FuaJmkSg8";
        private static string RpcUrl { get; } = "192.168.67.166:8000";
        private const string DISTRIBUTETOKEN = "XXX";
        private const string LPTOKEN = "ALP ABC-ELF";
        private const int SleepMillisecond = 250;
        private const int WaitStartPeriod = 500;


        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("GandalfFarmContractTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-new-env-main");

            NodeManager = new NodeManager(RpcUrl);
            AuthorityManager = new AuthorityManager(NodeManager, InitAccount);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
            _awakenTokenContract = tokenAddress == ""
                ? new AwakenTokenContract(NodeManager, InitAccount)
                : new AwakenTokenContract(NodeManager, InitAccount, tokenAddress);
            _gandalfFarmContract = farmPoolTwoAddress == ""
                ? new GandalfFarmContract(NodeManager, InitAccount)
                : new GandalfFarmContract(NodeManager, InitAccount, farmPoolTwoAddress);
        }

        [TestMethod]
        public void InitializeWithInvalidStartBlockTest()
        {
            string tokenSymbol = DISTRIBUTETOKEN;
            var tokenPerBlock = new BigIntValue(8);
            Int64 halvingPeriod = 50;
            Int64 startBlock = NodeManager.ApiClient.GetBlockHeightAsync().Result.Sub(500);
            var totalReward = new BigIntValue(750);
            
            var result = _gandalfFarmContract.Initialize(tokenSymbol, tokenPerBlock, halvingPeriod, startBlock, totalReward, _awakenTokenContract.ContractAddress);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("Invalid StartBlock");

        }
        
        [TestMethod]
        public void InitializeTest()
        {
            string tokenSymbol = DISTRIBUTETOKEN;
            var tokenPerBlock = new BigIntValue(1600000000);
            Int64 halvingPeriod = 50;
            Int64 startBlock = NodeManager.ApiClient.GetBlockHeightAsync().Result.Add(WaitStartPeriod);
            var totalReward = new BigIntValue(150000000000);
            
            var result = _gandalfFarmContract.Initialize(tokenSymbol, tokenPerBlock, halvingPeriod, startBlock, totalReward, _awakenTokenContract.ContractAddress);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            if (!IsLpTokenExist(LPTOKEN))
            {
                CreateLpToken(LPTOKEN, 8, InitAccount.ConvertAddress(), 10000000000000000);
                IssueLpBalance(LPTOKEN,1000000000000000,InitAccount.ConvertAddress());
            }

            if (!IsTokenExist(DISTRIBUTETOKEN))
            {
                CreateToken(DISTRIBUTETOKEN, 8, InitAccount.ConvertAddress(), 10000000000000000);
                IssueBalance(DISTRIBUTETOKEN,1000000000000000,_gandalfFarmContract.ContractAddress.ConvertAddress());
            }
            
            if (_tokenContract.GetUserBalance(_gandalfFarmContract.ContractAddress,DISTRIBUTETOKEN) == 0)
                IssueBalance(DISTRIBUTETOKEN,1000000000000000,_gandalfFarmContract.ContractAddress.ConvertAddress());
            
            Logger.Info($"start/end block {_gandalfFarmContract.GetStartBlock().Value}/{_gandalfFarmContract.EndBlock().Value}");
            Logger.Info($"total reward {_gandalfFarmContract.TotalReward().Value}");
            Logger.Info($"halving period {_gandalfFarmContract.GetHalvingPeriod().Value}");
            Logger.Info($"reward per block {_gandalfFarmContract.GetDistributeTokenBlockReward().Value}");
        }
        
        [TestMethod]
        public void InitializeTwiceTest()
        {
            string tokenSymbol = DISTRIBUTETOKEN;
            var tokenPerBlock = new BigIntValue(8);
            Int64 halvingPeriod = 50;
            Int64 startBlock = NodeManager.ApiClient.GetBlockHeightAsync().Result.Add(500);
            var totalReward = new BigIntValue(750);
            
            var result = _gandalfFarmContract.Initialize(tokenSymbol, tokenPerBlock, halvingPeriod, startBlock, totalReward, _awakenTokenContract.ContractAddress);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("Already initialized");

        }

        [TestMethod]
        public void AddTest()
        {
            //add pool 0
            var addPoolOneResult = AddPool(0,LPTOKEN,false);
            //add pool 1
            var addPoolTwoResult = AddPool(1,LPTOKEN,false);
            var lastRewardBlock = addPoolTwoResult.BlockNumber > _gandalfFarmContract.GetStartBlock().Value
                ? addPoolTwoResult.BlockNumber
                : _gandalfFarmContract.GetStartBlock().Value;

            //verify PoolAdded event
            var poolAddedLogStr = addPoolTwoResult.Logs.First(l => l.Name.Equals("PoolAdded")).NonIndexed;
            var poolAddedLog = PoolAdded.Parser.ParseFrom(ByteString.FromBase64(poolAddedLogStr));
            poolAddedLog.Pid.ShouldBe(1);
            poolAddedLog.Token.ShouldBe(LPTOKEN);
            poolAddedLog.AllocationPoint.ShouldBe(1);
            poolAddedLog.PoolType.ShouldBe(1);
            poolAddedLog.LastRewardBlockHeight.ShouldBe(lastRewardBlock);
            
            //validate pool lenght
            var poolLength = GetPoolLength();
            poolLength.ShouldBe(2);
            
            //validate pool 0 info
            var poolOne = _gandalfFarmContract.GetPoolInfo(0);
            poolOne.AllocPoint.ShouldBe(0);
            poolOne.LpToken.ShouldBe(LPTOKEN);

            //validate pool 1 info
            var poolTwo = _gandalfFarmContract.GetPoolInfo(1);
            poolTwo.AllocPoint.ShouldBe(1);
            poolTwo.LpToken.ShouldBe(LPTOKEN);
            

        }
        
        
        [TestMethod]
        public void DepositWithNoApprovementTest()
        {
            //verify deposit with no aprrovement
            var depositResultA = _gandalfFarmContract.Deposit(1, 10000000000);
            depositResultA.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            depositResultA.Error.ShouldContain("Insufficient allowance.");

        }

        [TestMethod]
        public void DepositToInvalidPoolPidTest()
        {
            
            var depositResult = _gandalfFarmContract.Deposit(0, 10000000000000);
            depositResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            depositResult.Error.ShouldContain("Invalid pid");
            
            var result = _gandalfFarmContract.Deposit(10, 10000000000000);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("Invalid pid");
        }

        [TestMethod]
        public void CallAdminFuncByUserTest()
        {
            _gandalfFarmContract.SetAccount(UserB);

            //Add
            var poolLength = GetPoolLength();
            var addResult = _gandalfFarmContract.Add(10, LPTOKEN, false);
            addResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addResult.Error.ShouldContain("Not Owner");
            GetPoolLength().ShouldBe(poolLength);

            //Set
            var poolInfo = _gandalfFarmContract.GetPoolInfo(1);
            var rewardperblock = _gandalfFarmContract.GetDistributeTokenBlockReward(); 
            var setResult = _gandalfFarmContract.Set(1, 10, 16, true);
            setResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            setResult.Error.ShouldContain("Not Owner");
            _gandalfFarmContract.GetPoolInfo(1).AllocPoint.ShouldBe(poolInfo.AllocPoint);
            _gandalfFarmContract.GetDistributeTokenBlockReward().ShouldBe(rewardperblock);

            //SetDistributeTokenPerBlock
            var setDistributeTokenPerBlockResult = _gandalfFarmContract.SetDistributeTokenPerBlock(16);
            setDistributeTokenPerBlockResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            setDistributeTokenPerBlockResult.Error.ShouldContain("Not Owner");
            _gandalfFarmContract.GetDistributeTokenBlockReward().ShouldBe(rewardperblock);

            //SetHalvingPeriod
            var halvingperiod = _gandalfFarmContract.GetHalvingPeriod();
            var setHalvingPeriodResult = _gandalfFarmContract.SetHalvingPeriod(1000);
            setHalvingPeriodResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            _gandalfFarmContract.GetHalvingPeriod().ShouldBe(halvingperiod);

            //SetFarmPoolOne
            var farmpoolone = _gandalfFarmContract.FarmPoolOne();
            var setFarmPoolOneResult =
                _gandalfFarmContract.SetFarmPoolOne(
                    "2SCJJtEXhU7wmRD9941WMK9CT5iYFvbgExbNTLKSxFh8H5pxQ8".ConvertAddress());
            setFarmPoolOneResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            setFarmPoolOneResult.Error.ShouldContain("Not Owner");
            _gandalfFarmContract.FarmPoolOne().ShouldBe(farmpoolone);
            
            //FixEndBlock
            var endblock = _gandalfFarmContract.EndBlock();
            var fixEndBlockResult = _gandalfFarmContract.FixEndBlock(true);
            fixEndBlockResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            fixEndBlockResult.Error.ShouldContain("Not Owner");
            _gandalfFarmContract.EndBlock().ShouldBe(endblock);

            _gandalfFarmContract.SetAccount(InitAccount);
        }

        [TestMethod]
        public void DepositBeforeStartBlock()
        {
            var isBeforeStartBlock = NodeManager.ApiClient.GetBlockHeightAsync().Result < _gandalfFarmContract.GetStartBlock().Value;
            Assert(isBeforeStartBlock,"Case needs to be executed before mining");
            
            //pre-check user amount
            var userAmountBeforeDeposit = GetUserAmount(1, InitAccount);

            Logger.Info($"userAmountBeforeDeposit is {userAmountBeforeDeposit}");
            
            //approve
            var approveResult = _awakenTokenContract.ApproveLPToken(farmPoolTwoAddress, InitAccount , 10000000000, LPTOKEN);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //pre-check user balance
            var balanceBeforeDeposit = _awakenTokenContract.GetBalance(LPTOKEN, InitAccount.ConvertAddress()).Amount;
            Logger.Info($"balanceBeforeDeposit is {balanceBeforeDeposit}");
            
            //pre-check Distribute Token Transfer
            var distributeTokenBefore = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            
            //do deposit
            var result = _gandalfFarmContract.Deposit(1, 10000000000);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            
            //post-check user balance
            var balanceAfterDeposit = _awakenTokenContract.GetBalance( LPTOKEN,InitAccount.ConvertAddress()).Amount;
            balanceAfterDeposit.ShouldBe(balanceBeforeDeposit - 10000000000);
            
            //post-check user amount
            var userAmountAfterDeposit = GetUserAmount(1,InitAccount);
            userAmountAfterDeposit.ShouldBe(userAmountBeforeDeposit.Add(10000000000));

            Logger.Info($"balanceAfterDeposit is {balanceAfterDeposit}");
            Logger.Info($"userAmountAfterDeposit is {userAmountAfterDeposit}");

            //validate issued reward
            var issuedReward = _gandalfFarmContract.IssuedReward();
            issuedReward.ShouldBe(0);
            
            //post-check Distribute Token Transfer
            var distributeTokenAfter = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            distributeTokenAfter.ShouldBe(distributeTokenBefore);

        }
        
        [TestMethod]
        public void DepositAfterStartBlock()
        {
            //Do approve
            var approveResult = _awakenTokenContract.ApproveLPToken(_gandalfFarmContract.ContractAddress, InitAccount, 10000000000, LPTOKEN);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //get approve fee 
            //var fee = approveResult.GetResourceTokenFee();
            //var amount = fee["ELF"];

            //pre-check
            var userAmountBeforeDeposit = GetUserAmount(1,InitAccount);
            var poolTotalAmount = _gandalfFarmContract.GetPoolInfo(1).TotalAmount;
            var balanceBeforeDeposit = _awakenTokenContract.GetBalance(LPTOKEN, InitAccount.ConvertAddress()).Amount;
            var distributeTokenBefore = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            Logger.Info($"farmpooltwo balance {_tokenContract.GetUserBalance(_gandalfFarmContract.ContractAddress, DISTRIBUTETOKEN)}");
            Logger.Info($"userAmountBeforeDeposit is {userAmountBeforeDeposit}");
            Logger.Info($"poolTotalAmount is {poolTotalAmount}");
            Logger.Info($"balanceBeforeDeposit is {balanceBeforeDeposit}");
            Logger.Info($"distributeTokenBefore {distributeTokenBefore}");
            
            //get last reward block
            var lastRewardBlock = GetLastRewardBlock(1);

            //Do deposit
            var depositResult = _gandalfFarmContract.Deposit(1, 10000000000);
            depositResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var depositBlock = depositResult.BlockNumber;
            
            //post-check
            var balanceAfterDeposit = _awakenTokenContract.GetBalance( LPTOKEN,InitAccount.ConvertAddress()).Amount;
            var userAmountAfterDeposit = GetUserAmount(1, InitAccount);
            var transferDistritubeToken = GetTransferDistributeToken(depositResult, 1, lastRewardBlock, 
                depositBlock,userAmountBeforeDeposit, poolTotalAmount);
            var distributeTokenAfter = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            Logger.Info($"balanceAfterDeposit is {balanceAfterDeposit}");
            Logger.Info($"userAmountAfterDeposit is {userAmountAfterDeposit}");
            Logger.Info($"transferDistritubeToken {transferDistritubeToken}");
            Logger.Info($"distributeTokenAfter {distributeTokenAfter}");
            
            // verify Deposit Event
            var logs = depositResult.Logs.First(l => l.Name.Equals("Deposit"));
            for (int i = 0; i < logs.Indexed.Length; i++)
            {
                var indexedByteString = ByteString.FromBase64(logs.Indexed[i]);
                var log = Deposit.Parser.ParseFrom(indexedByteString);
                if (i == 0)
                    log.User.ShouldBe(InitAccount.ConvertAddress());
                else
                    log.Pid.ShouldBe(1);
            }
            var nonIndexedByteString = ByteString.FromBase64(logs.NonIndexed);
            var depositLog = Deposit.Parser.ParseFrom(nonIndexedByteString);
            depositLog.Amount.ShouldBe(10000000000);
            Logger.Info($"Deposit log {logs}");
            
            // verify ClaimRevenue Event
            if (!transferDistritubeToken.Equals(new BigIntValue(0)))
            {
                var claimRevenueLogStr = depositResult.Logs.First(l => l.Name.Equals("ClaimRevenue")).NonIndexed;
                var claimRevenueLog = ClaimRevenue.Parser.ParseFrom(ByteString.FromBase64(claimRevenueLogStr));
                claimRevenueLog.Amount.ShouldBe(transferDistritubeToken);
                claimRevenueLog.Pid.ShouldBe(1);
                claimRevenueLog.User.ShouldBe(InitAccount.ConvertAddress());
                claimRevenueLog.TokenSymbol.ShouldBe(DISTRIBUTETOKEN);
            }

            //verify user balance, user amount, transferdistributetoken
            balanceAfterDeposit.ShouldBe(balanceBeforeDeposit - 10000000000);
            userAmountAfterDeposit.ShouldBe(userAmountBeforeDeposit.Add(10000000000));
            new BigIntValue(distributeTokenAfter).ShouldBe(transferDistritubeToken.Add(new BigIntValue(distributeTokenBefore)));
            
        }

        [TestMethod]
        public void WithdrawTest()
        {
            //pre-check user amount
            var userAmountBeforeWithdraw = GetUserAmount(1,InitAccount);
            var balanceBeforeWithdraw = _awakenTokenContract.GetBalance( LPTOKEN,InitAccount.ConvertAddress()).Amount;
            var distributeTokenBefore = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            var lastRewardBlock = GetLastRewardBlock(1);
            var poolTotalAmount = _gandalfFarmContract.GetPoolInfo(1).TotalAmount;
            Logger.Info($"farmpooltwo balance {_tokenContract.GetUserBalance(_gandalfFarmContract.ContractAddress, DISTRIBUTETOKEN)}");
            Logger.Info($"userAmountBeforeWithdraw is {userAmountBeforeWithdraw}");
            Logger.Info($"balanceBeforeWithdraw is {balanceBeforeWithdraw}");
            Logger.Info($"distributeTokenBefore {distributeTokenBefore}");

            //Do withdraw
            var withdrawResult = _gandalfFarmContract.Withdraw(1, userAmountBeforeWithdraw);
            withdrawResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var withdrawBlock = withdrawResult.BlockNumber;
            
            //post-check
            var balanceAfterWithdraw = _awakenTokenContract.GetBalance( LPTOKEN,InitAccount.ConvertAddress()).Amount;
            var userAmountAfterWithdraw = GetUserAmount(1, InitAccount);
            var transferDistritubeToken = GetTransferDistributeToken(withdrawResult,1, lastRewardBlock, 
                withdrawBlock,userAmountBeforeWithdraw, poolTotalAmount);   
            var distributeTokenAfter = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            Logger.Info($"balanceAfterDeposit is {balanceAfterWithdraw}");
            Logger.Info($"userAmountAfterDeposit is {userAmountAfterWithdraw}");
            Logger.Info($"transferDistritubeToken {transferDistritubeToken}");
            Logger.Info($"distributeTokenAfter {distributeTokenAfter}");

            // verify Withdraw Event
            var logs = withdrawResult.Logs.First(l => l.Name.Equals("Withdraw"));
            for (int i = 0; i < logs.Indexed.Length; i++)
            {
                var indexedByteString = ByteString.FromBase64(logs.Indexed[i]);
                var log = Awaken.Contracts.PoolTwoContract.Withdraw.Parser.ParseFrom(indexedByteString);
                if (i == 0)
                    log.User.ShouldBe(InitAccount.ConvertAddress());
                else
                    log.Pid.ShouldBe(1);
            }
            var nonIndexedByteString = ByteString.FromBase64(logs.NonIndexed);
            var withdrawLog = Deposit.Parser.ParseFrom(nonIndexedByteString);
            withdrawLog.Amount.ToString().ShouldBe(userAmountBeforeWithdraw.Value);
            Logger.Info($"Withdraw log {logs}");
            
            // verify ClaimRevenue Event
            if (!transferDistritubeToken.Equals(new BigIntValue(0)))
            {
                var claimRevenueLogStr = withdrawResult.Logs.First(l => l.Name.Equals("ClaimRevenue")).NonIndexed;
                var claimRevenueLog = ClaimRevenue.Parser.ParseFrom(ByteString.FromBase64(claimRevenueLogStr));
                claimRevenueLog.Amount.ShouldBe(transferDistritubeToken);
                claimRevenueLog.Pid.ShouldBe(1);
                claimRevenueLog.User.ShouldBe(InitAccount.ConvertAddress());
                //claimRevenueEventLog.TokenSymbol.ShouldBe(DISTRIBUTETOKEN); bug
            }
 
            new BigIntValue(distributeTokenAfter).ShouldBe(transferDistritubeToken.Add(new BigIntValue(distributeTokenBefore)));
            new BigIntValue(balanceAfterWithdraw).ShouldBe(userAmountBeforeWithdraw.Add(new BigIntValue(balanceBeforeWithdraw)));
            userAmountAfterWithdraw.ShouldBe(new BigIntValue(0));

        }

        [TestMethod]
        public void WithdrawWithInsufficientAmount()
        {
            //pre-check user amount
            var userAmount = GetUserAmount(1, InitAccount);
            
            //do withdraw
            var result = _gandalfFarmContract.Withdraw(1, userAmount.Add(100));
            
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("Insufficient amount");

            //post-check user amount
            GetUserAmount(1,InitAccount).ShouldBe(userAmount);
        }
                
        [TestMethod]
        public void WithdrawAmountZeroTest()
        {
            //do approve
            var approveResult = _tokenContract.ApproveToken(InitAccount, farmPoolTwoAddress, 10000000000, LPTOKEN);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //do Deposit 
            var depositResult = _gandalfFarmContract.Deposit(1, 10000000000);
            depositResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var balancebefore = _tokenContract.GetUserBalance(InitAccount, LPTOKEN);
            var distributetokenbefore = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            var amount = _tokenContract.GetUserBalance(InitAccount, LPTOKEN);
            var totoalamount = _gandalfFarmContract.GetPoolInfo(1).TotalAmount;
            
            //do withdraw
            var withdrawResult =
                _gandalfFarmContract.Withdraw(1, 0);
            withdrawResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var distributetokenafter = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            
            //verify LPToken balance
            _tokenContract.GetUserBalance(InitAccount,LPTOKEN).ShouldBe(balancebefore);
            
            //verify transferdistributetoken
            GetTransferDistributeToken(withdrawResult, 1, depositResult.BlockNumber, withdrawResult.BlockNumber, amount,
                    totoalamount)
                .ShouldBe(new BigIntValue(distributetokenafter).Sub(new BigIntValue(distributetokenbefore)));

        }
        
        [TestMethod]
        public void DepositWithdrawTest()
        {
            //Do approve
            var approveResult = _awakenTokenContract.ApproveLPToken( farmPoolTwoAddress,InitAccount, 10000000000, LPTOKEN);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //pre-check distributetoken in farm contract before deposit
            Logger.Info(_tokenContract.GetUserBalance(_gandalfFarmContract.ContractAddress, DISTRIBUTETOKEN));

            //Do deposit
            var depositResult = _gandalfFarmContract.Deposit(1, 10000000000);
            depositResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //var depositBlock = depositResult.BlockNumber;
            
            //pre-check user amount
            var userAmountBeforeWithdraw = GetUserAmount(1,InitAccount);
            Logger.Info($"userAmountBeforeWithdraw is {userAmountBeforeWithdraw}");
           
            //get totalamount before withdraw
            var poolTotalAmount = _gandalfFarmContract.GetPoolInfo(1).TotalAmount;
            
            //pre-check user balance
            var balanceBeforeWithdraw = _awakenTokenContract.GetBalance(LPTOKEN,InitAccount.ConvertAddress()).Amount;
            Logger.Info($"balanceBeforeWithdraw is {balanceBeforeWithdraw}");

            //pre-check disributetoken
            var distributeTokenBefore = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            Logger.Info($"distributeTokenBefore {distributeTokenBefore}");
            var lastRewardBlock = GetLastRewardBlock(1);
            //do withdraw
            var result = _gandalfFarmContract.Withdraw(1, userAmountBeforeWithdraw);
            var withdrawBlock = result.BlockNumber;
            
            //post-check user balance
            var balanceAfterWithdraw = _awakenTokenContract.GetBalance( LPTOKEN,InitAccount.ConvertAddress()).Amount;
            new BigIntValue(balanceAfterWithdraw).ShouldBe(userAmountBeforeWithdraw.Add(new BigIntValue(balanceBeforeWithdraw)));
            
            //post-check user amount
            var userAmountAfterWithdraw = GetUserAmount(1, InitAccount);
            userAmountAfterWithdraw.ShouldBe(0);
            
            Logger.Info($"balanceAfterWithdraw is {balanceAfterWithdraw}");
            Logger.Info($"userAmountAfterWithdraw is {userAmountAfterWithdraw}");

            var transferDistritubeToken = GetTransferDistributeToken(result,1, lastRewardBlock, 
                withdrawBlock,userAmountBeforeWithdraw, poolTotalAmount);
            Logger.Info($"transferDistritubeToken {transferDistritubeToken}");
            
            //post-check Distribute Token Transfer
            
            var distributeTokenAfter = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            Logger.Info($"distributeTokenAfter {distributeTokenAfter}");
            //new BigIntValue(distributeTokenAfter).ShouldBe(transferDistritubeToken.Add(new BigIntValue(distributeTokenBefore)));
            new BigIntValue(distributeTokenAfter.Sub(distributeTokenBefore)).ShouldBe(transferDistritubeToken);
        }
        
        
        [TestMethod]
        public void NoRewardAfterWithdrawAllTest()
        {
        
            //Do approve
            var approveResult = _awakenTokenContract.ApproveLPToken( farmPoolTwoAddress,InitAccount, 10000000000, LPTOKEN);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //pre-check distributetoken in farm contract before deposit
            Logger.Info(_tokenContract.GetUserBalance(_gandalfFarmContract.ContractAddress, DISTRIBUTETOKEN));

            //Do deposit
            var depositResult = _gandalfFarmContract.Deposit(1, 10000000000);
            depositResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //do withdraw
            var result = _gandalfFarmContract.Withdraw(1, _gandalfFarmContract.GetUserInfo(1,InitAccount).Amount);
            var withdrawBlock = result.BlockNumber;
            var distributetokenafter = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            
            //Do deposit
            var deposit = _gandalfFarmContract.Deposit(1, 10000000000);
            deposit.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //verify no reward after withdraw all
            _tokenContract.GetUserBalance(InitAccount,DISTRIBUTETOKEN).ShouldBe(distributetokenafter);
            
        }

        [TestMethod]
        public void PendingTest()
        {
            //do Approve
            var approveResult = _awakenTokenContract.ApproveLPToken( farmPoolTwoAddress,InitAccount, 10000000000, LPTOKEN);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //do deposit
            var depositResult = _gandalfFarmContract.Deposit(1, 10000000000);
            depositResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var amount = _gandalfFarmContract.GetUserInfo(1, InitAccount).Amount;
            var totalamout = _gandalfFarmContract.GetPoolInfo(1).TotalAmount;
            
            Thread.Sleep(250 * 1000);
            
            //see pending
            var pendingResult = _gandalfFarmContract.GetPendingTest(1, InitAccount);
            pendingResult.Amount.ShouldBe(GetTransferDistributeToken(1, depositResult.BlockNumber,
                pendingResult.PendingBlockHeight, amount, totalamout));

        }
        
        [TestMethod]
        public void TwoUsersDepositBeforePhase0WithdrawAfterPhase3()
        {
            var isBeforeStartBlock = NodeManager.ApiClient.GetBlockHeightAsync().Result < _gandalfFarmContract.GetStartBlock().Value;
            Assert(isBeforeStartBlock,"Case needs to be executed before mining");
            
            if (_awakenTokenContract.GetBalance(LPTOKEN,UserB.ConvertAddress()).Amount < 150000000000)
                IssueLpBalance(LPTOKEN, 150000000000, UserB.ConvertAddress());
            
            //usera approve
            var approveUserA = _awakenTokenContract.ApproveLPToken( farmPoolTwoAddress,InitAccount, 10000000000, LPTOKEN);
            approveUserA.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //userb approve
            var approveUserB = _awakenTokenContract.ApproveLPToken( farmPoolTwoAddress,UserB, 5000000000, LPTOKEN);
            approveUserB.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //usera deposit
            var depositUserA = _gandalfFarmContract.Deposit(1, 10000000000);
            depositUserA.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var depositBlockUserA = depositUserA.BlockNumber;
            var totalAmountAfterDepositUserA = _gandalfFarmContract.GetPoolInfo(1).TotalAmount;
            var userAmountAfterDepositUserA = _gandalfFarmContract.GetUserInfo(1, InitAccount).Amount;
            var rewardDebtAfterDepositUserA = _gandalfFarmContract.GetUserInfo(1, InitAccount).RewardDebt;
            //validate distribute token 0 reward
            var distributeTokenAfterDepositUserA = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            Logger.Info($"distributeTokenAfterDepositUserA {distributeTokenAfterDepositUserA}");
            
            Logger.Info($"total/user/block/debt:{totalAmountAfterDepositUserA},{userAmountAfterDepositUserA},{depositBlockUserA}{rewardDebtAfterDepositUserA}");

            //userb deposit
            _gandalfFarmContract.SetAccount(UserB);
            var depositUserB = _gandalfFarmContract.Deposit(1, 5000000000);
            depositUserB.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var depositBlockUserB = depositUserB.BlockNumber;
            var totalAmountAfterDepositUserB = _gandalfFarmContract.GetPoolInfo(1).TotalAmount;
            var userAmountAfterDepositUserB = _gandalfFarmContract.GetUserInfo(1, UserB).Amount;
            var rewardDebtAfterDepositUserB = _gandalfFarmContract.GetUserInfo(1, UserB).RewardDebt;
            //validate distribute token userb 0 reward
            var distributeTokenAfterDepositUserB = _tokenContract.GetUserBalance(UserB, DISTRIBUTETOKEN);
            Logger.Info($"distributeTokenAfterDepositUserB {distributeTokenAfterDepositUserB}");
            Logger.Info($"total/user/block/debt:{totalAmountAfterDepositUserB},{userAmountAfterDepositUserB},{depositBlockUserB}{rewardDebtAfterDepositUserB}");
            
            var endblock = _gandalfFarmContract.EndBlock().Value;
            var currentblock = NodeManager.ApiClient.GetBlockHeightAsync().Result;
            var sleep = (int)endblock.Sub(currentblock).Div(2);
            Thread.Sleep( sleep * 1000);

            while (NodeManager.ApiClient.GetBlockHeightAsync().Result <= endblock)
            {
                Thread.Sleep(1 * 1000);
            }
            
            //usera withdraw
            _gandalfFarmContract.SetAccount(InitAccount);
            var withdrawUserA = _gandalfFarmContract.Withdraw(1, _gandalfFarmContract.GetUserInfo(1,InitAccount).Amount);
            withdrawUserA.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var withdrawUserABlock = withdrawUserA.BlockNumber;
            var totalAmountAfterWithdrawUserA = _gandalfFarmContract.GetPoolInfo(1).TotalAmount;
            var userAmountAfterWithdrawUserA = _gandalfFarmContract.GetUserInfo(1, InitAccount).Amount;
            var rewardDebtAfterWithdrawUserA = _gandalfFarmContract.GetUserInfo(1, InitAccount).RewardDebt;
            //validate distribute token 
            var distributeTokenAfterWithdrawUserA = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            Logger.Info($"distributeTokenAfterWithdrawUserA {distributeTokenAfterWithdrawUserA}");

            Logger.Info($"total/user/block/debt:{totalAmountAfterWithdrawUserA},{userAmountAfterWithdrawUserA},{withdrawUserABlock}{rewardDebtAfterWithdrawUserA}");
       
            var transferDistributeTokenUserA = GetTransferDistributeToken(depositUserB,1, depositBlockUserA, depositBlockUserB,
                userAmountAfterDepositUserA, totalAmountAfterDepositUserA).Add(GetTransferDistributeToken(withdrawUserA,1,
                depositBlockUserB, withdrawUserABlock, userAmountAfterDepositUserA, totalAmountAfterDepositUserB));
            transferDistributeTokenUserA.ShouldBe(new BigIntValue(distributeTokenAfterWithdrawUserA - distributeTokenAfterDepositUserA));

            
            //userb withdraw
            _gandalfFarmContract.SetAccount(UserB);
            var withdrawUserB = _gandalfFarmContract.Withdraw(1, _gandalfFarmContract.GetUserInfo(1, UserB).Amount);
            withdrawUserB.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var withdrawUserBBlock = withdrawUserB.BlockNumber;
            var totalAmountAfterWithdrawUserB = _gandalfFarmContract.GetPoolInfo(1).TotalAmount;
            var userAmountAfterWithdrawUserB = _gandalfFarmContract.GetUserInfo(1, UserB).Amount;
            var rewardDebtAfterWithdrawUserB = _gandalfFarmContract.GetUserInfo(1, UserB).RewardDebt;
            //validate distribute token
            var distributeTokenAfterWithdrawUserB = _tokenContract.GetUserBalance(UserB, DISTRIBUTETOKEN);
            Logger.Info($"distributeTokenAfterWithdrawUserB {distributeTokenAfterWithdrawUserB}");
            
            Logger.Info($"total/user/block/debt:{totalAmountAfterWithdrawUserB},{userAmountAfterWithdrawUserB},{withdrawUserBBlock}{rewardDebtAfterWithdrawUserB}");
            var transferDistributeTokenUserB =
                GetTransferDistributeToken(withdrawUserA,1, depositBlockUserB, withdrawUserABlock, userAmountAfterDepositUserB,
                    totalAmountAfterDepositUserB).Add(GetTransferDistributeToken(withdrawUserB,1, withdrawUserABlock,
                    withdrawUserBBlock, userAmountAfterDepositUserB, totalAmountAfterWithdrawUserA));
            transferDistributeTokenUserB.ShouldBe(new BigIntValue(distributeTokenAfterWithdrawUserB - distributeTokenAfterDepositUserB));
            
            Logger.Info($"transferDistributeTokenUserB/transferDistributeTokenUserA:{transferDistributeTokenUserB},{transferDistributeTokenUserA}");
            transferDistributeTokenUserB.Add(transferDistributeTokenUserA).ShouldBe(_gandalfFarmContract.IssuedReward());
            _gandalfFarmContract.IssuedReward().ShouldBe(_gandalfFarmContract.TotalReward()
                .Mul(new BigIntValue(_gandalfFarmContract.GetPoolInfo(1).AllocPoint))
                .Div(new BigIntValue(_gandalfFarmContract.TotalAllocPoint().Value)));
        }

        [TestMethod]
        public void SetPoolAllocPointTest()
        {
            var allocpoint = 3;
            var perblock = _gandalfFarmContract.GetDistributeTokenBlockReward();
            var poolAllocPoint = _gandalfFarmContract.GetPoolInfo(1).AllocPoint;
            var totalAllocPoint = _gandalfFarmContract.TotalAllocPoint().Value;
            
            //approve
            var approveResult = _awakenTokenContract.ApproveLPToken( farmPoolTwoAddress,InitAccount, 10000000000, LPTOKEN);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //do Deposit first before Set
            var depositResult = _gandalfFarmContract.Deposit(1, 10000000000);
            depositResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var amount = _gandalfFarmContract.GetUserInfo(1, InitAccount).Amount;
            var totalamount = _gandalfFarmContract.GetPoolInfo(1).TotalAmount;
            var distributeTokenBefore = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            
            Thread.Sleep(30 * 1000);
            
            //do Set
            var setResult = _gandalfFarmContract.Set(1, allocpoint, perblock, true);
            setResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //verify WeightSet event
            var weightSetLogStr = setResult.Logs.First(l => l.Name.Equals("WeightSet")).NonIndexed;
            var weightSetLog = WeightSet.Parser.ParseFrom(ByteString.FromBase64(weightSetLogStr));
            weightSetLog.Pid.ShouldBe(1);
            weightSetLog.NewAllocationPoint.ShouldBe(allocpoint);

            //verify DistributeTokenPerBlockSet event
            var distributeTokenPerBlockSetLogStr =
                setResult.Logs.First(l => l.Name.Equals("DistributeTokenPerBlockSet")).NonIndexed;
            var distributeTokenPerBlockSetLog =
                DistributeTokenPerBlockSet.Parser.ParseFrom(ByteString.FromBase64(distributeTokenPerBlockSetLogStr));
            distributeTokenPerBlockSetLog.NewDistributeTokenPerBlock.ShouldBe(perblock);
            
            Thread.Sleep(30 * 1000);
            
            //do Withdraw
            var withdrawResult =
                _gandalfFarmContract.Withdraw(1, _gandalfFarmContract.GetUserInfo(1, InitAccount).Amount);
            withdrawResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var distributeTokenAfter = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            
            var transferdistributeToken = GetTransferDistributeToken(depositResult.BlockNumber,
                setResult.BlockNumber, amount, totalamount, poolAllocPoint, totalAllocPoint).Add(GetTransferDistributeToken(withdrawResult,
                1, setResult.BlockNumber, withdrawResult.BlockNumber, amount, totalamount));
            transferdistributeToken.ShouldBe(new BigIntValue(distributeTokenAfter).Sub(new BigIntValue(distributeTokenBefore)));
            
        }

        [TestMethod]
        public void SetPoolDistributeTokenPerBlockTest()
        {
            var allocpoint = _gandalfFarmContract.GetPoolInfo(1).AllocPoint;
            var newperblock = new BigIntValue(16);
            var distributetokenperblock = _gandalfFarmContract.GetDistributeTokenBlockReward();
            
            //do Approve
            var approveResult = _awakenTokenContract.ApproveLPToken( farmPoolTwoAddress,InitAccount, 10000000000, LPTOKEN);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //do Deposit
            var depositResult = _gandalfFarmContract.Deposit(1, 10000000000);
            depositResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var amount = _gandalfFarmContract.GetUserInfo(1, InitAccount).Amount;
            var totalamount = _gandalfFarmContract.GetPoolInfo(1).TotalAmount;
            var transferDistributeTokenBefore = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            
            Thread.Sleep(200 * 1000);

            //do Set
            var setResult = _gandalfFarmContract.Set(1, allocpoint, newperblock, true);
            setResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            Thread.Sleep(30 * 10000);
            
            //do Withdraw
            var withdrawResult =
                _gandalfFarmContract.Withdraw(1, _gandalfFarmContract.GetUserInfo(1, InitAccount).Amount.Div(new BigIntValue(2)));
            withdrawResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var transferDistributeTokenAfter = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            
            //verify transferditributetoken
            var transferDistributeToken = GetTransferDistributeToken(setResult, 1, depositResult.BlockNumber,
                setResult.BlockNumber, amount, totalamount, distributetokenperblock)
                .Add(GetTransferDistributeToken(withdrawResult, 1, setResult.BlockNumber, withdrawResult.BlockNumber, 
                    amount, totalamount));
            
            transferDistributeToken.ShouldBe(new BigIntValue(transferDistributeTokenAfter).Sub(new BigIntValue(transferDistributeTokenBefore)));
            
        }

        [TestMethod]
        public void FixEndBlockTest()
        {
            var isBeforeStartBlock = NodeManager.ApiClient.GetBlockHeightAsync().Result < _gandalfFarmContract.GetStartBlock().Value;
            Assert(isBeforeStartBlock,"Case needs to be executed before mining");
            
            var allocpoint = _gandalfFarmContract.GetPoolInfo(1).AllocPoint;
            var newperblock = new BigIntValue(2400000000);
            
            //do Approve
            var approveResult = _awakenTokenContract.ApproveLPToken( farmPoolTwoAddress,InitAccount, 10000000000, LPTOKEN);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //do deposit
            var depositResult = _gandalfFarmContract.Deposit(1, 10000000000);
            depositResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var amount = _gandalfFarmContract.GetUserInfo(1, InitAccount).Amount;
            var totalamount = _gandalfFarmContract.GetPoolInfo(1).TotalAmount;
            var distributeTokenBefore = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            Logger.Info(distributeTokenBefore);

            var startblock = _gandalfFarmContract.GetStartBlock();
            while (NodeManager.ApiClient.GetBlockHeightAsync().Result <= startblock.Value)
            {
                Thread.Sleep(5 * 1000);    
            }
            
            //set
            var setResult = _gandalfFarmContract.Set(1, allocpoint, newperblock, true);
            setResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info(_gandalfFarmContract.GetPhase(NodeManager.ApiClient.GetBlockHeightAsync().Result));
            var distributeTokenAfterSet = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            Logger.Info(distributeTokenAfterSet);

            //do fixendblock
            var fixEndBlockResult = _gandalfFarmContract.FixEndBlock(true);
            fixEndBlockResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var distributeTokenAfterFix = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            Logger.Info(distributeTokenAfterFix);
            var issuedAfterFix = _gandalfFarmContract.IssuedReward();

            var endblock = _gandalfFarmContract.EndBlock().Value;
            var currentblock = NodeManager.ApiClient.GetBlockHeightAsync().Result;
            var sleep = (int)endblock.Sub(currentblock).Div(2);
            Logger.Info(_gandalfFarmContract.GetPhase(NodeManager.ApiClient.GetBlockHeightAsync().Result));
            Logger.Info($"new start block {fixEndBlockResult.BlockNumber}");
            Logger.Info($"fixed end block {endblock}");
            Logger.Info(
                $"total reward {_gandalfFarmContract.TotalReward().Sub(_gandalfFarmContract.IssuedReward())}");

            if (sleep > 0)
            {
                Thread.Sleep((sleep + 50) * 1000);    
            }
            
            
            //do withdraw
            var withdrawResult = _gandalfFarmContract.Withdraw(1, amount);
            withdrawResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var distributeTokenAfter = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            Logger.Info(distributeTokenAfter);

            Logger.Info(distributeTokenAfter - distributeTokenBefore);
            Logger.Info(_gandalfFarmContract.TotalReward());
            Logger.Info(_gandalfFarmContract.TotalReward().Sub(issuedAfterFix));
            Logger.Info(GetTransferDistributeToken(1,
                fixEndBlockResult.BlockNumber, _gandalfFarmContract.EndBlock().Value, amount, totalamount));
            
            new BigIntValue(distributeTokenAfter - distributeTokenBefore).ShouldBe(_gandalfFarmContract.TotalReward());
            _gandalfFarmContract.TotalReward().Sub(issuedAfterFix).ShouldBe(GetTransferDistributeToken(1,
                fixEndBlockResult.BlockNumber, _gandalfFarmContract.EndBlock().Value, amount, totalamount));

        }

        private void Assert(bool asserted, string message)
        {
            if (!asserted) 
                throw new Exception(message);
        }
        
        private TransactionResultDto AddPool(long allocPoint, string lpToken, bool with_update)
        {
            var result = _gandalfFarmContract.Add(allocPoint, lpToken, with_update);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            return result;
        }
        
        private long GetPoolLength()
        {
            var result = _gandalfFarmContract.CallViewMethod<Int64Value>(FarmMethod.PoolLength, new Empty());
            Logger.Info($"Pool length {result.Value}");
            return result.Value;
        }
        
        private bool IsLpTokenExist(string symbol)
        {
            var getTokenResult = _awakenTokenContract.GetTokenInfo(symbol);
            return !getTokenResult.Equals(new TokenInfo());
        }
        
        private bool IsTokenExist(string symbol)
        {
            var getTokenResult = _tokenContract.GetTokenInfo(symbol);
            return !getTokenResult.Equals(new AElf.Contracts.MultiToken.TokenInfo());
        }
      
        private BigIntValue GetTransferDistributeToken(int pid, long lastrewardblock, 
            long txblock, BigIntValue useramount, BigIntValue poolTotalAmount)
        {
            if (poolTotalAmount.Equals(new BigIntValue(0)))
            {
                return new BigIntValue(0);   
            }

            var startBlock = _gandalfFarmContract.GetStartBlock().Value;

            if (txblock < startBlock)
            {
                return new BigIntValue(0);
            }
            
            lastrewardblock = lastrewardblock > startBlock ? lastrewardblock : startBlock;
            
            var endBlock = _gandalfFarmContract.EndBlock().Value;
            var rewardblock = txblock > endBlock
                ? endBlock
                : txblock;
            
            if (rewardblock <= lastrewardblock)
            {
                return new BigIntValue(0);
            }

            var blockreward = new BigIntValue(0);
            var n = _gandalfFarmContract.GetPhase(lastrewardblock).Value;
            var m = _gandalfFarmContract.GetPhase(rewardblock).Value;
            var halvingperiod = _gandalfFarmContract.GetHalvingPeriod().Value;
            var startblock = _gandalfFarmContract.GetStartBlock().Value;
            
            while (n < m)
            {
                n++;
                var r = n.Mul(halvingperiod).Add(startblock);
                var reward = _gandalfFarmContract.Reward(new Int64Value
                {
                    Value = r
                });
                blockreward = blockreward.Add(reward.Mul(r.Sub(lastrewardblock)));
                lastrewardblock = r;
            }

            blockreward = blockreward.Add(_gandalfFarmContract.Reward(new Int64Value
                {
                    Value = rewardblock
                })
                .Mul(rewardblock.Sub(lastrewardblock)));
            
            var poolAllocPoint = _gandalfFarmContract.GetPoolInfo(pid).AllocPoint;
            var totalAllocPoint = _gandalfFarmContract.TotalAllocPoint().Value;
            var transferDistributeToken = useramount
                    .Mul(blockreward)
                    .Mul(poolAllocPoint)
                    .Div(poolTotalAmount.Mul(totalAllocPoint));
            Logger.Info($"useramount/blockreward/poolTotalAmount/transferDistributeToken:{useramount}{blockreward}{poolTotalAmount}{transferDistributeToken}");
            
            return transferDistributeToken;
        }
        private BigIntValue GetTransferDistributeToken(TransactionResultDto transactionResult, int pid, long lastrewardblock, 
            long txblock, BigIntValue useramount, BigIntValue poolTotalAmount)
        {
            if (poolTotalAmount.Equals(new BigIntValue(0)))
            {
                return new BigIntValue(0);   
            }

            var startBlock = _gandalfFarmContract.GetStartBlock().Value;

            if (txblock < startBlock)
            {
                return new BigIntValue(0);
            }
            
            lastrewardblock = lastrewardblock > startBlock ? lastrewardblock : startBlock;
            
            var endBlock = _gandalfFarmContract.EndBlock().Value;
            var rewardblock = txblock > endBlock
                ? endBlock
                : txblock;
            
            if (rewardblock <= lastrewardblock)
            {
                return new BigIntValue(0);
            }

            var blockreward = new BigIntValue(0);
            var n = _gandalfFarmContract.GetPhase(lastrewardblock).Value;
            var m = _gandalfFarmContract.GetPhase(rewardblock).Value;
            var halvingperiod = _gandalfFarmContract.GetHalvingPeriod().Value;
            var startblock = _gandalfFarmContract.GetStartBlock().Value;
            
            while (n < m)
            {
                n++;
                var r = n.Mul(halvingperiod).Add(startblock);
                var reward = _gandalfFarmContract.Reward(new Int64Value
                {
                    Value = r
                });
                blockreward = blockreward.Add(reward.Mul(r.Sub(lastrewardblock)));
                lastrewardblock = r;
            }

            blockreward = blockreward.Add(_gandalfFarmContract.Reward(new Int64Value
                {
                    Value = rewardblock
                })
                .Mul(rewardblock.Sub(lastrewardblock)));
            
            var poolAllocPoint = _gandalfFarmContract.GetPoolInfo(pid).AllocPoint;
            var totalAllocPoint = _gandalfFarmContract.TotalAllocPoint().Value;
            var transferDistributeToken = useramount
                    .Mul(blockreward)
                    .Mul(poolAllocPoint)
                    .Div(poolTotalAmount.Mul(totalAllocPoint));
            Logger.Info($"useramount/blockreward/poolTotalAmount/transferDistributeToken:{useramount}{blockreward}{poolTotalAmount}{transferDistributeToken}");
            
            // verify UpdatePool Event
            var updatePoolLogStr = transactionResult.Logs.First(l => l.Name.Equals("UpdatePool")).NonIndexed;
            var updatePoolLog = UpdatePool.Parser.ParseFrom(ByteString.FromBase64(updatePoolLogStr));
            updatePoolLog.Pid.ShouldBe(1);
            updatePoolLog.UpdateBlockHeight.ShouldBe(txblock);
            updatePoolLog.DistributeTokenAmount.ShouldBe(blockreward.Mul(poolAllocPoint).Div(totalAllocPoint));
            Logger.Info($"DistributeTokenAmount in updatepool event/self func {updatePoolLog.DistributeTokenAmount}, {blockreward.Mul(poolAllocPoint).Div(totalAllocPoint)}");
            return transferDistributeToken;
        }
        
        private BigIntValue GetTransferDistributeToken(long lastrewardblock, long txblock, BigIntValue useramount, BigIntValue poolTotalAmount, long poolAllocPoint, long totalAllocPoint)
        {
            if (poolTotalAmount.Equals(new BigIntValue(0)))
            {
                return new BigIntValue(0);   
            }

            var startBlock = _gandalfFarmContract.GetStartBlock().Value;

            if (txblock < startBlock)
            {
                return new BigIntValue(0);
            }
            
            lastrewardblock = lastrewardblock > startBlock ? lastrewardblock : startBlock;
            
            var endBlock = _gandalfFarmContract.EndBlock().Value;
            var rewardblock = txblock > endBlock
                ? endBlock
                : txblock;
            
            if (rewardblock <= lastrewardblock)
            {
                return new BigIntValue(0);
            }

            var blockreward = new BigIntValue(0);
            var n = _gandalfFarmContract.GetPhase(lastrewardblock).Value;
            var m = _gandalfFarmContract.GetPhase(rewardblock).Value;
            var halvingperiod = _gandalfFarmContract.GetHalvingPeriod().Value;
            var startblock = _gandalfFarmContract.GetStartBlock().Value;
            
            while (n < m)
            {
                n++;
                var r = n.Mul(halvingperiod).Add(startblock);
                var reward = _gandalfFarmContract.Reward(new Int64Value
                {
                    Value = r
                });
                blockreward = blockreward.Add(reward.Mul(r.Sub(lastrewardblock)));
                lastrewardblock = r;
            }

            blockreward = blockreward.Add(_gandalfFarmContract.Reward(new Int64Value
                {
                    Value = rewardblock
                })
                .Mul(rewardblock.Sub(lastrewardblock)));
            
            var transferDistributeToken = useramount
                    .Mul(blockreward)
                    .Mul(poolAllocPoint)
                    .Div(poolTotalAmount.Mul(totalAllocPoint));
            Logger.Info($"useramount/blockreward/poolTotalAmount/transferDistributeToken:{useramount}{blockreward}{poolTotalAmount}{transferDistributeToken}");
            return transferDistributeToken;
        }
        
        private BigIntValue GetTransferDistributeToken(TransactionResultDto transactionResult, int pid, long lastrewardblock, 
            long txblock, BigIntValue useramount, BigIntValue poolTotalAmount, BigIntValue distributetokenperblock)
        {
            if (poolTotalAmount.Equals(new BigIntValue(0)))
            {
                return new BigIntValue(0);   
            }

            var startBlock = _gandalfFarmContract.GetStartBlock().Value;

            if (txblock < startBlock)
            {
                return new BigIntValue(0);
            }
            
            lastrewardblock = lastrewardblock > startBlock ? lastrewardblock : startBlock;
            
            var endBlock = _gandalfFarmContract.EndBlock().Value;
            var rewardblock = txblock > endBlock
                ? endBlock
                : txblock;
            
            if (rewardblock <= lastrewardblock)
            {
                return new BigIntValue(0);
            }

            var blockreward = new BigIntValue(0);
            var n = _gandalfFarmContract.GetPhase(lastrewardblock).Value;
            var m = _gandalfFarmContract.GetPhase(rewardblock).Value;
            var halvingperiod = _gandalfFarmContract.GetHalvingPeriod().Value;
            var startblock = _gandalfFarmContract.GetStartBlock().Value;
            
            while (n < m)
            {
                n++;
                var r = n.Mul(halvingperiod).Add(startblock);
                /*var reward = _gandalfFarmContract.Reward(new Int64Value
                {
                    Value = r
                });*/
                var phase = _gandalfFarmContract.GetPhase(r);
                var reward = distributetokenperblock.Div(1 << Convert.ToInt32(phase.Value));
                
                blockreward = blockreward.Add(reward.Mul(r.Sub(lastrewardblock)));
                lastrewardblock = r;
            }
            
            blockreward = blockreward.Add(distributetokenperblock.Div(1 << Convert.ToInt32(m))
                .Mul(rewardblock.Sub(lastrewardblock)));
            
            var poolAllocPoint = _gandalfFarmContract.GetPoolInfo(pid).AllocPoint;
            var totalAllocPoint = _gandalfFarmContract.TotalAllocPoint().Value;
            var transferDistributeToken = useramount
                    .Mul(blockreward)
                    .Mul(poolAllocPoint)
                    .Div(poolTotalAmount.Mul(totalAllocPoint));
            Logger.Info($"useramount/blockreward/poolTotalAmount/transferDistributeToken:{useramount}{blockreward}{poolTotalAmount}{transferDistributeToken}");
            
            // verify UpdatePool Event
            var updatePoolLogStr = transactionResult.Logs.First(l => l.Name.Equals("UpdatePool")).NonIndexed;
            var updatePoolLog = UpdatePool.Parser.ParseFrom(ByteString.FromBase64(updatePoolLogStr));
            updatePoolLog.Pid.ShouldBe(1);
            updatePoolLog.UpdateBlockHeight.ShouldBe(txblock);
            updatePoolLog.DistributeTokenAmount.ShouldBe(blockreward.Mul(poolAllocPoint).Div(totalAllocPoint));
            Logger.Info($"DistributeTokenAmount in updatepool event/self func {updatePoolLog.DistributeTokenAmount}, {blockreward.Mul(poolAllocPoint).Div(totalAllocPoint)}");
            return transferDistributeToken;
        }

        private BigIntValue GetUserAmount(int pid, string user)
        {
            var result = _gandalfFarmContract.GetUserInfo(pid,user);
            BigIntValue userAmount = 0;
            if (!result.Equals(new UserInfoStruct()))
            {
                userAmount = result.Amount;
            }
            Logger.Info($"User {user} info {result}");
            return userAmount;
        }

        private long GetLastRewardBlock(int pid)
        {
            return _gandalfFarmContract.GetPoolInfo(pid).LastRewardBlock;
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
        
        private void IssueLpBalance(string symbol, long amount, Address toAddress)
        {
            var result = _awakenTokenContract.ExecuteMethodWithResult(AwakenTokenMethod.Issue, new IssueInput
            {
                Symbol = symbol,
                Amount = amount,
                To = toAddress
            });
            
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"Successfully issue amount {amount} to {toAddress}");
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
        
        private void CreateLpToken(string symbol, int decimals, Address issuer, long totalSupply)
        {
            var result = _awakenTokenContract.ExecuteMethodWithResult(AwakenTokenMethod.Create,new CreateInput
            {
                Symbol = symbol,
                Decimals = decimals,
                Issuer = InitAccount.ConvertAddress(),
                TokenName = $"{issuer} token",
                TotalSupply = totalSupply,
                IsBurnable = true
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            Logger.Info($"Sucessfully create lp token {symbol}");
        }
        
    }
}