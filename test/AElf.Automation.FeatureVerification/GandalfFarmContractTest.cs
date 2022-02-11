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
using GetBalanceOutput = AElf.Client.MultiToken.GetBalanceOutput;

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
        private string farmPoolTwoAddress = "";
        //private string farmPoolTwoAddress = "KRqiqama8Kzv3UfDeJMdWfsDca4SqAbp1DoSTqJJHLAoJYoJN";
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
            Int64 halvingPeriod = 600;
            Int64 startBlock = NodeManager.ApiClient.GetBlockHeightAsync().Result.Add(500);
            var totalReward = new BigIntValue(9000);
            
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
            
            if (_tokenContract.GetUserBalance(_gandalfFarmContract.ContractAddress,DISTRIBUTETOKEN) == 0)
                IssueBalance(DISTRIBUTETOKEN,1000000,_gandalfFarmContract.ContractAddress.ConvertAddress(),$"issue balance{DISTRIBUTETOKEN}");
            
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
            var userAmountBeforeDeposit = GetUserAmount(1, InitAccount);

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
            var userAmountAfterDeposit = GetUserAmount(1,InitAccount);
            userAmountAfterDeposit.ShouldBe(userAmountBeforeDeposit.Add(100));

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
            var approveResult = _tokenContract.ApproveToken(InitAccount, farmPoolTwoAddress, 100, LPTOKEN);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //get approve fee 
            //var fee = approveResult.GetResourceTokenFee();
            //var amount = fee["ELF"];

            //pre-check user amount
            var userAmountBeforeDeposit = GetUserAmount(1,InitAccount);
            Logger.Info($"userAmountBeforeDeposit is {userAmountBeforeDeposit}");
           
            var poolTotalAmount = _gandalfFarmContract.GetPoolInfo(1).TotalAmount;
            
            //pre-check distributetoken in farm contract before deposit
            Logger.Info(_tokenContract.GetUserBalance(_gandalfFarmContract.ContractAddress, DISTRIBUTETOKEN));

            //pre-check user balance
            var balanceBeforeDeposit = _tokenContract.GetUserBalance(InitAccount, LPTOKEN);
            Logger.Info($"balanceBeforeDeposit is {balanceBeforeDeposit}");

            //pre-check disributetoken
            var distributeTokenBefore = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            Logger.Info($"distributeTokenBefore {distributeTokenBefore}");
            
            //get last reward block
            var lastRewardBlock = GetLastRewardBlock(1);

            //Do deposit
            var depositResult = _gandalfFarmContract.Deposit(1, 100);
            depositResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var depositBlock = depositResult.BlockNumber;
            
            //post-check user balance
            var balanceAfterDeposit = _tokenContract.GetUserBalance(InitAccount, LPTOKEN);
            balanceAfterDeposit.ShouldBe(balanceBeforeDeposit - 100);
            
            //post-check user amount
            var userAmountAfterDeposit = GetUserAmount(1, InitAccount);
            userAmountAfterDeposit.ShouldBe(userAmountBeforeDeposit.Add(100));
            
            Logger.Info($"balanceAfterDeposit is {balanceAfterDeposit}");
            Logger.Info($"userAmountAfterDeposit is {userAmountAfterDeposit}");
            
            var transferDistritubeToken = GetTransferDistributeToken(1, lastRewardBlock, 
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
        
        private BigIntValue GetTransferDistributeToken(int pid, long lastrewardblock, long txblock, BigIntValue useramount, BigIntValue poolTotalAmount)
        {
            if (poolTotalAmount.Equals(new BigIntValue(0)))
            {
                return new BigIntValue(0);   
            }

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
            
            return transferDistributeToken;
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
        public void Withdraw()
        {
            //pre-check user amount
            var userAmountBeforeWithdraw = GetUserAmount(1,InitAccount);
            Logger.Info($"userAmountBeforeWithdraw is {userAmountBeforeWithdraw}");
            
            //pre-check distributetoken in farm contract before deposit
            Logger.Info(_tokenContract.GetUserBalance(_gandalfFarmContract.ContractAddress, DISTRIBUTETOKEN));

            //pre-check user balance
            var balanceBeforeWithdraw = _tokenContract.GetUserBalance(InitAccount, LPTOKEN);
            Logger.Info($"balanceBeforeWithdraw is {balanceBeforeWithdraw}");

            //pre-check disributetoken
            var distributeTokenBefore = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            Logger.Info($"distributeTokenBefore {distributeTokenBefore}");
            
            //pre get
            var lastRewardBlock = GetLastRewardBlock(1);
            var poolTotalAmount = _gandalfFarmContract.GetPoolInfo(1).TotalAmount;

            //Do withdraw
            var withdrawResult = _gandalfFarmContract.Withdraw(1, userAmountBeforeWithdraw);
            withdrawResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var withdrawBlock = withdrawResult.BlockNumber;
            
            //post-check user balance
            var balanceAfterWithdraw = _tokenContract.GetUserBalance(InitAccount, LPTOKEN);
            new BigIntValue(balanceAfterWithdraw).ShouldBe(userAmountBeforeWithdraw.Add(new BigIntValue(balanceBeforeWithdraw)));
            
            //post-check user amount
            var userAmountAfterWithdraw = GetUserAmount(1, InitAccount);
            userAmountAfterWithdraw.ShouldBe(new BigIntValue(0));
            
            Logger.Info($"balanceAfterDeposit is {balanceAfterWithdraw}");
            Logger.Info($"userAmountAfterDeposit is {userAmountAfterWithdraw}");
            
            var transferDistritubeToken = GetTransferDistributeToken(1, lastRewardBlock, 
                withdrawBlock,userAmountBeforeWithdraw, poolTotalAmount);
            Logger.Info($"transferDistritubeToken {transferDistritubeToken}");
            
            //post-check Distribute Token Transfer
            var distributeTokenAfter = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            Logger.Info($"distributeTokenAfter {distributeTokenAfter}");
            new BigIntValue(distributeTokenAfter).ShouldBe(transferDistritubeToken.Add(new BigIntValue(distributeTokenBefore)));

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
        
        [TestMethod]
        public void DepositWithdrawTest()
        {
            //Do approve
            var approveResult = _tokenContract.ApproveToken(InitAccount, farmPoolTwoAddress, 100, LPTOKEN);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //pre-check distributetoken in farm contract before deposit
            Logger.Info(_tokenContract.GetUserBalance(_gandalfFarmContract.ContractAddress, DISTRIBUTETOKEN));

            //Do deposit
            var depositResult = _gandalfFarmContract.Deposit(1, 100);
            depositResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //var depositBlock = depositResult.BlockNumber;
            
            //pre-check user amount
            var userAmountBeforeWithdraw = GetUserAmount(1,InitAccount);
            Logger.Info($"userAmountBeforeWithdraw is {userAmountBeforeWithdraw}");
           
            //get totalamount before withdraw
            var poolTotalAmount = _gandalfFarmContract.GetPoolInfo(1).TotalAmount;
            
            //pre-check user balance
            var balanceBeforeWithdraw = _tokenContract.GetUserBalance(InitAccount, LPTOKEN);
            Logger.Info($"balanceBeforeWithdraw is {balanceBeforeWithdraw}");

            //pre-check disributetoken
            var distributeTokenBefore = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            Logger.Info($"distributeTokenBefore {distributeTokenBefore}");
            var lastRewardBlock = GetLastRewardBlock(1);
            //do withdraw
            var result = _gandalfFarmContract.Withdraw(1, userAmountBeforeWithdraw);
            var withdrawBlock = result.BlockNumber;
            
            //post-check user balance
            var balanceAfterWithdraw = _tokenContract.GetUserBalance(InitAccount, LPTOKEN);
            new BigIntValue(balanceAfterWithdraw).ShouldBe(userAmountBeforeWithdraw.Add(new BigIntValue(balanceBeforeWithdraw)));
            
            //post-check user amount
            var userAmountAfterWithdraw = GetUserAmount(1, InitAccount);
            userAmountAfterWithdraw.ShouldBe(0);
            
            Logger.Info($"balanceAfterWithdraw is {balanceAfterWithdraw}");
            Logger.Info($"userAmountAfterWithdraw is {userAmountAfterWithdraw}");

            var transferDistritubeToken = GetTransferDistributeToken(1, lastRewardBlock, 
                withdrawBlock,userAmountBeforeWithdraw, poolTotalAmount);
            Logger.Info($"transferDistritubeToken {transferDistritubeToken}");
            
            //post-check Distribute Token Transfer
            
            var distributeTokenAfter = _tokenContract.GetUserBalance(InitAccount, DISTRIBUTETOKEN);
            Logger.Info($"distributeTokenAfter {distributeTokenAfter}");
            //new BigIntValue(distributeTokenAfter).ShouldBe(transferDistritubeToken.Add(new BigIntValue(distributeTokenBefore)));
            new BigIntValue(distributeTokenAfter.Sub(distributeTokenBefore)).ShouldBe(transferDistritubeToken);
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
            var blocknumber = NodeManager.ApiClient.GetBlockHeightAsync().Result;
            //var blocknumber = 716989;
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
        
    }
}