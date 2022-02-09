using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Gandalf.Contracts.PoolTwoContract;
using Gandalf.Contracts.Swap;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Shouldly.Configuration;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class GandalfFarmContractTest
    {
        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;
        private GandalfFarmContract _gandalfFarmContract;
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }
        //private string farmPoolTwoAddress = "";
        private string farmPoolTwoAddress = "2QtXdKR1ap9Sxgvz3ksiozXx88xf12rfQhk7kNGYuamveDh1ZX";
        private string InitAccount { get; } = "nn659b9X1BLhnu5RWmEUbuuV7J9QKVVSN54j9UmeCbF3Dve5D";
        private string FeeToAccount { get; } = "Zz4iuCCCktGjZGmQ9vMcxh2JT9pDTFA4XSR7WDNrndYcEXtRx";
        private static string RpcUrl { get; } = "192.168.67.166:8000";
        private long FeeRate { get; } = 30;

        private const string DISTRIBUTETOKEN = "ISTAR";
        private const string LPTOKEN = "ELF";


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
            _gandalfFarmContract = farmPoolTwoAddress == ""
                ? new GandalfFarmContract(NodeManager, InitAccount)
                : new GandalfFarmContract(NodeManager, InitAccount, farmPoolTwoAddress);
        }

        [TestMethod]
        public void InitializeTest()
        {
            string tokenSymbol = DISTRIBUTETOKEN;
            var tokenPerBlock = new BigIntValue(8);
            Int64 halvingPeriod = 10800;
            Int64 startBlock = NodeManager.ApiClient.GetBlockHeightAsync().Result.Add(500);
            var totalReward = new BigIntValue(162000);
            
            var result = _gandalfFarmContract.Initialize(tokenSymbol, tokenPerBlock, halvingPeriod, startBlock, totalReward);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            if (!IsTokenExist(LPTOKEN))
            {
                CreateToken(LPTOKEN, 8, InitAccount.ConvertAddress(), 1000000);
                IssueBalance(LPTOKEN,1000000,InitAccount.ConvertAddress(),$"issue balance{LPTOKEN}");
            }

            if (!IsTokenExist(DISTRIBUTETOKEN))
            {
                CreateToken(DISTRIBUTETOKEN, 8, InitAccount.ConvertAddress(), 1000000);
                IssueBalance(DISTRIBUTETOKEN,1000000,_gandalfFarmContract.ContractAddress.ConvertAddress(),$"issue balance{DISTRIBUTETOKEN}");
            }


        }

        [TestMethod]
        public void AddTest()
        {
            //add pool 0
            AddPool(2,LPTOKEN,false);
            //add pool 1
            AddPool(1,LPTOKEN,false);

            //validate pool lenght
            var poolLength = GetPoolLength();
            poolLength.ShouldBe(2);
            
            //validate pool 0 info
            var poolOne = _gandalfFarmContract.GetPoolInfo(0);
            poolOne.AllocPoint.ShouldBe(2);
            poolOne.LpToken.ShouldBe(LPTOKEN);

            //validate pool 1 info
            var poolTwo = _gandalfFarmContract.GetPoolInfo(1);
            poolTwo.AllocPoint.ShouldBe(1);
            poolTwo.LpToken.ShouldBe(LPTOKEN);

        }
        
        private void AddPool(long allocPoint, string lpToken, bool with_update)
        {
            var result = _gandalfFarmContract.Add(allocPoint, lpToken, with_update);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
        
        private long GetPoolLength()
        {
            var result = _gandalfFarmContract.CallViewMethod<Int64Value>(FarmMethod.PoolLength, new Empty());
            Logger.Info($"Pool length {result.Value}");
            return result.Value;
        }
        
        

        [TestMethod]
        public void DepositBeforeStartBlock()
        {
            var isBeforeStartBlock = NodeManager.ApiClient.GetBlockHeightAsync().Result < _gandalfFarmContract.GetStartBlock().Value;
            Assert(isBeforeStartBlock,"Case needs to be executed before mining");
            
            //pre-check user amount
            var userInfoBeforeDeposit = _gandalfFarmContract.GetUserInfo(1,InitAccount.ConvertAddress());
            BigIntValue userAmountBeforeDeposit = 0;
            if (!userInfoBeforeDeposit.Equals(new UserInfoStruct()))
            {
                userAmountBeforeDeposit = userInfoBeforeDeposit.Amount;
            }

            Logger.Info($"userAmountBeforeDeposit is {userAmountBeforeDeposit}");
            
            //approve
            var approveResult = _tokenContract.ApproveToken(InitAccount, farmPoolTwoAddress, 100, LPTOKEN);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //pre-check user balance
            var balanceBeforeDeposit = _tokenContract.GetUserBalance(InitAccount, LPTOKEN);
            Logger.Info($"balanceBeforeDeposit is {balanceBeforeDeposit}");
            
            //pre-check Distribute Token Transfer
            var distributeTokenBefore = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            
            //do deposit
            var result = _gandalfFarmContract.Deposit(1, 100);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //post-check user balance
            var balanceAfterDeposit = _tokenContract.GetUserBalance(InitAccount, LPTOKEN);
            balanceAfterDeposit.ShouldBe(balanceBeforeDeposit - 100);
            
            //post-check user amount
            var userInfoAfterDeposit = _gandalfFarmContract.GetUserInfo(1,InitAccount.ConvertAddress());
            userInfoAfterDeposit.Amount.ShouldBe(userAmountBeforeDeposit.Add(100));
            
            Logger.Info($"balanceAfterDeposit is {balanceAfterDeposit}");
            Logger.Info($"userAmountAfterDeposit is {userInfoAfterDeposit}");

            //validate issued reward
            var issuedReward = _gandalfFarmContract.IssuedReward();
            issuedReward.ShouldBe(0);
            
            //post-check Distribute Token Transfer
            var distributeTokenAfter = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            distributeTokenAfter.ShouldBe(distributeTokenBefore);

        }
        
        [TestMethod]
        public void DepositInPhase0()
        {
            var currentHeight = NodeManager.ApiClient.GetBlockHeightAsync().Result;
            var startBlockHeight = _gandalfFarmContract.GetStartBlock().Value;
            var havlingPeriod = _gandalfFarmContract.GetHalvingPeriod().Value;
            Assert((currentHeight > startBlockHeight
                    && currentHeight < startBlockHeight + havlingPeriod), "Case needs to be executed in Phase0");

            var farmDistributeToken =
                _tokenContract.GetUserBalance(_gandalfFarmContract.ContractAddress, DISTRIBUTETOKEN);
            if (farmDistributeToken == 0)
                IssueBalance(DISTRIBUTETOKEN,1000000,_gandalfFarmContract.ContractAddress.ConvertAddress(),$"issue balance{DISTRIBUTETOKEN}");

            //Do approve
            var approveResult = _tokenContract.ApproveToken(InitAccount, farmPoolTwoAddress, 100, LPTOKEN);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //get approve fee 
            //var fee = approveResult.GetResourceTokenFee();
            //var amount = fee["ELF"];

            //pre-check user amount
            var userInfoBeforeDeposit = _gandalfFarmContract.GetUserInfo(1,InitAccount.ConvertAddress());
            BigIntValue userAmountBeforeDeposit = 0;
            if (!userInfoBeforeDeposit.Equals(new UserInfoStruct()))
            {
                userAmountBeforeDeposit = userInfoBeforeDeposit.Amount;
            }
            var poolTotalAmount = _gandalfFarmContract.GetPoolInfo(1).TotalAmount;
            Logger.Info($"userAmountBeforeDeposit is {userAmountBeforeDeposit}");
            
            //pre-check distributetoken in farm contract before deposit
            Logger.Info(_tokenContract.GetUserBalance(_gandalfFarmContract.ContractAddress, DISTRIBUTETOKEN));

            //pre-check user balance
            var balanceBeforeDeposit = _tokenContract.GetUserBalance(InitAccount, LPTOKEN);
            Logger.Info($"balanceBeforeDeposit is {balanceBeforeDeposit}");

            //pre-check disributetoken
            var distributeTokenBefore = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            Logger.Info($"distributeTokenBefore {distributeTokenBefore}");

            //Do deposit
            var depositResult = _gandalfFarmContract.Deposit(1, 100);
            depositResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var depositBlock = depositResult.BlockNumber;
            
            //post-check user balance
            var balanceAfterDeposit = _tokenContract.GetUserBalance(InitAccount, LPTOKEN);
            balanceAfterDeposit.ShouldBe(balanceBeforeDeposit - 100);
            
            //post-check user amount
            var userInfoAfterDeposit = _gandalfFarmContract.GetUserInfo(1,InitAccount.ConvertAddress());
            userInfoAfterDeposit.Amount.ShouldBe(userAmountBeforeDeposit.Add(100));
            
            Logger.Info($"balanceAfterDeposit is {balanceAfterDeposit}");
            Logger.Info($"userAmountAfterDeposit is {userInfoAfterDeposit}");

            var transferDistritubeToken = GetTransferDistributeToken(1, startBlockHeight, 
                depositBlock,userAmountBeforeDeposit, poolTotalAmount);
            Logger.Info($"transferDistritubeToken {transferDistritubeToken}");
            
            //post-check Distribute Token Transfer
            var distributeTokenAfter = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            Logger.Info($"distributeTokenAfter {distributeTokenAfter}");
            new BigIntValue(distributeTokenAfter).ShouldBe(transferDistritubeToken.Add(new BigIntValue(distributeTokenBefore)));
            
            
        }
        private bool IsTokenExist(string symbol)
        {
            var getTokenResult = _tokenContract.GetTokenInfo(symbol);
            return !getTokenResult.Equals(new TokenInfo());
        }
        
        private BigIntValue GetTransferDistributeToken(int pid, long lastrewardblock, long rewardblock, BigIntValue useramount, BigIntValue poolTotalAmount)
        {
            var poolInfo = _gandalfFarmContract.GetPoolInfo(pid);
            var userAmount = useramount;
            var totalAmount = poolTotalAmount;
            var totalAllocPoint = _gandalfFarmContract.TotalAllocPoint().Value;
            var poolAllocPoint = poolInfo.AllocPoint;

            var blocks = rewardblock - lastrewardblock;
            var reward = _gandalfFarmContract.Reward(rewardblock);

            var transferDistributeToken =
                userAmount
                    .Mul(reward)
                    .Mul(blocks)
                    .Mul(poolAllocPoint)
                    .Div(totalAmount.Mul(totalAllocPoint));
            return transferDistributeToken;
            
        }
        
        [TestMethod]
        public void DepositTwiceTest()
        {
            
            //_tokenContract.IssueBalance(InitAccount, _gandalfFarmContract.ContractAddress, 100000000000, "ISTAR");
            //Logger.Info(_tokenContract.GetUserBalance(_gandalfFarmContract.ContractAddress, "ISTAR"));
            
           /* var pending = _gandalfFarmContract.CallViewMethod<BigIntValue>(FarmMethod.Pending, new PendingInput
            {
                Pid = 1,
                User = InitAccount.ConvertAddress()
            });
            var pendingAmount = Convert.ToInt64(pending.Value);
            Logger.Info(pendingAmount);*/
           
            
            //Do approve
            var approveResult = _tokenContract.ExecuteMethodWithResult(TokenMethod.Approve, new ApproveInput
            {
                Symbol = "ELF",
                Amount = 200,
                Spender = farmPoolTwoAddress.ConvertAddress()
            });
            
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var depositFirst = _gandalfFarmContract.Deposit(1, 50);
            depositFirst.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //var userInfoAfterFirstDeposit = _gandalfFarmContract.GetUserInfo(1,InitAccount.ConvertAddress());
            //userInfoAfterFirstDeposit.Amount.ShouldBe(50);


            var depositSecond = _gandalfFarmContract.Deposit(1, 150);
            depositSecond.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

           // var userInfoAfterSecondDeposit = _gandalfFarmContract.GetUserInfo(1,InitAccount.ConvertAddress());
           // userInfoAfterSecondDeposit.Amount.ShouldBe(200);
        }

        [TestMethod]
        public void WithdrawTest()
        {
            //_tokenContract.IssueBalance(InitAccount, _gandalfFarmContract.ContractAddress, 100000000000, "ISTAR");
            //Logger.Info(_tokenContract.GetUserBalance(_gandalfFarmContract.ContractAddress, "ISTAR"));

            var pending = _gandalfFarmContract.CallViewMethod<BigIntValue>(FarmMethod.Pending, new PendingInput
            {
                Pid = 1,
                User = InitAccount.ConvertAddress()
            });
            var pendingAmount = Convert.ToInt64(pending.Value);
            Logger.Info(pendingAmount);
            
            var result = _gandalfFarmContract.Withdraw(1, 100);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
        
        [TestMethod]
        public void DepositWithdrawTest()
        {
            _tokenContract.IssueBalance(InitAccount, _gandalfFarmContract.ContractAddress, 100000000000, "ISTAR");
            Logger.Info(_tokenContract.GetUserBalance(_gandalfFarmContract.ContractAddress, "ISTAR"));

            var pending = _gandalfFarmContract.CallViewMethod<BigIntValue>(FarmMethod.Pending, new PendingInput
            {
                Pid = 1,
                User = InitAccount.ConvertAddress()
            });
            var pendingAmount = Convert.ToInt64(pending.Value);
            Logger.Info(pendingAmount);
            
            var getTokenResult = _tokenContract.GetTokenInfo(DISTRIBUTETOKEN);
            if (getTokenResult.Equals(new TokenInfo()))
            {
                CreateToken(DISTRIBUTETOKEN, 8, InitAccount.ConvertAddress(), 1000000);
            }
            
            //Do approve
            var approveResult = _tokenContract.ExecuteMethodWithResult(TokenMethod.Approve, new ApproveInput
            {
                Symbol = "ELF",
                Amount = 200,
                Spender = farmPoolTwoAddress.ConvertAddress()
            });
            
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var depositFirst = _gandalfFarmContract.Deposit(1, 50);
            depositFirst.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var userInfoAfterFirstDeposit = _gandalfFarmContract.GetUserInfo(1,InitAccount.ConvertAddress());
            userInfoAfterFirstDeposit.Amount.ShouldBe(50);


            var depositSecond = _gandalfFarmContract.Deposit(1, 150);
            depositSecond.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var userInfoAfterSecondDeposit = _gandalfFarmContract.GetUserInfo(1,InitAccount.ConvertAddress());
            userInfoAfterSecondDeposit.Amount.ShouldBe(200);
        }
        private void CreateToken(string symbol, int decimals, Address issuer, long totalSupply)
        {
            var t = totalSupply.Mul(Int64.Parse(new BigIntValue(10).Pow(decimals).Value));
            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
            {
                Symbol = symbol,
                Decimals = decimals,
                Issuer = InitAccount.ConvertAddress(),
                TokenName = $"{issuer} token",
                TotalSupply = t
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            Logger.Info($"Sucessfully create token {symbol}");
        }
        
        private void IssueBalance(string symbol, long amount, Address toAddress, string memo)
        {
            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Issue, new IssueInput
            {
                Symbol = symbol,
                Amount = amount,
                To = toAddress,
                Memo = memo
            });
            
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"Successfully issue amount {amount} to {toAddress}");
        }

        [TestMethod]
        public void GetPhaseTest()
        {
            var blocknumber = 93934;
            var result = _gandalfFarmContract.GetPhase(blocknumber);
            Logger.Info($"Block {blocknumber} Phase is {result}");
        }

        [TestMethod]
        public void GetStartBlockTest()
        {
            var result = _gandalfFarmContract.GetStartBlock();
            Logger.Info($"Start Block is {result}");
        }

        [TestMethod]
        public void GetHalvingPeriodTest()
        {
            var result = _gandalfFarmContract.GetHalvingPeriod();
            Logger.Info($"Halving Period is {result}");
        }
        
        [TestMethod]
        public void GetTotalRewardTest()
        {
            var result = _gandalfFarmContract.GetTotalReward();
            Logger.Info($"Total Reward is {result}");
        }
 
        [TestMethod]
        public void GetDistributeTokenBlockRewardTest()
        {
            var result = _gandalfFarmContract.GetDistributeTokenBlockReward();
            Logger.Info($"Distribute Token Block Reward is {result}");
        }

        [TestMethod]
        public void SetFarmPoolOneTest()
        {
            var result = _gandalfFarmContract.ExecuteMethodWithResult(FarmMethod.SetFarmPoolOne, new Address());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void RedepositTest()
        {
            var amount = 1000;
            Address user = InitAccount.ConvertAddress();

            var result = _gandalfFarmContract.Redeposit(amount, user);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
        }

        [TestMethod]
        public void GetUserInfo()
        {
            var result = _gandalfFarmContract.GetUserInfo(1,InitAccount.ConvertAddress());
            Logger.Info($"User {InitAccount} info {result}");
        }

        [TestMethod]
        public void GetPendingAmountTest()
        {
        
            var result = _gandalfFarmContract.PendingAmount(1, InitAccount.ConvertAddress());
            Logger.Info($"User {InitAccount} in pool 1 pending amount is:{result.Value}");
        }

        [TestMethod]
        public void GetEndBlock()
        {
            var result = _gandalfFarmContract.EndBlock();
            Logger.Info($"end block is {result.Value}");
        }

        [TestMethod]
        public void GetTotoalReward()
        {
            var result = _gandalfFarmContract.TotalReward();
            Logger.Info($"total reward is {result.Value}");

        }

        [TestMethod]
        public void GetPoolInfo()
        {
            var result = _gandalfFarmContract.GetPoolInfo(1);
            Logger.Info($"pool info {result}");
            
        }
        
        private void Assert(bool asserted, string message)
        {
            if (!asserted) 
                throw new Exception(message);
        }

//87992445650683044
//87992445650682944
//87992445625047944

//87992445545392844
//87992445545392744
//87992445519757744
//87992445519757644
//87992445467612644
//87992445467612544
//87992407925607544
//87992407925607444
//87992034046972444
//87992034046972344
//87992064770048258
//87992064744413258
//87992064744413158
//87992064744413158
//87992064718778158
//87992064718778058
//87992064718778058
//87992064693143058
//87992064693142958
//87992064667507958
//87992064667507958
//87992064667507858
//87992064641872858
    }
}