using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Client.Dto;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Awaken.Contracts.Swap;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Volo.Abp.Threading;
using CreateInput = AElf.Contracts.MultiToken.CreateInput;
using InitializeInput = Awaken.Contracts.Farm.InitializeInput;
using IssueInput = AElf.Contracts.MultiToken.IssueInput;
using TokenInfo = Awaken.Contracts.Token.TokenInfo;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class GandalfFarmRedepositFixTest
    {
        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;
        private AwakenTokenContract _awakenTokenContract;
        private AwakenSwapContract _awakenSwapContract;
        private AwakenFarmContract _awakenFarmContract;
        private AwakenFarmTwoContract _awakenFarmTwoContract;
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }

        private string tokenAddress = "";
        private string swapAddress = "";
        private string farmAddress = "";
        private string farmTwoAddress = "";
        private string InitAccount { get; } = "J6zgLjGwd1bxTBpULLXrGVeV74tnS2n74FFJJz7KNdjTYkDF6";
        private string AdminAddress { get; } = "YUW9zH5GhRboT5JK4vXp5BLAfCDv28rRmTQwo418FuaJmkSg8";
        private string FeeToAccount { get; } = "J6zgLjGwd1bxTBpULLXrGVeV74tnS2n74FFJJz7KNdjTYkDF6";
        private static string RpcUrl { get; } = "http://127.0.0.1:8000";
        private const string DistributeToken = "AWKN";
        private const string LPTOKEN_EU = "ALP ELF-USDT";
        private const string LPTOKEN = "ALP AWKN-ELF";
        private const string NewRewardToken = "USDT";
        private bool isNeedInitialize = false;
        private const long Block0 = 60;
        private const long Block1 = Block0 * 2;
        private const long PerBlock0 = 20000;
        private const long PerBlock1 = PerBlock0 / 2;
        private const long Cycle = Block0 * 4;
        private const long HalvingPeriod = Block0 + Block1;
        private long FeeRate { get; } = 30;

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("AwakenFarmContractTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env1-main");

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
            _awakenFarmTwoContract = farmTwoAddress == ""
                ? new AwakenFarmTwoContract(NodeManager, InitAccount)
                : new AwakenFarmTwoContract(NodeManager, InitAccount, farmTwoAddress);
        }

        [TestMethod]
        public void InitializeTest()
        {
            if (isNeedInitialize)
                InitializeOtherContract();
            var currentHeight = AsyncHelper.RunSync(() => NodeManager.ApiClient.GetBlockHeightAsync());
            var addBlock = 300;
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
                input.StartBlock = currentHeight.Add(addBlock);
                var result = _awakenFarmContract.ExecuteMethodWithResult(FarmMethod.Initialize, input);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }

            // Initialize pool two
            var poolTwoStart = startBlock;
            var redepositStartBlock = poolTwoStart.Add(Block0);
            var poolTwoPerBlock = new BigIntValue(PerBlock1);
            var havingPeriod = Block0.Add(Block1);
            Logger.Info($"\nHavingPeriod: {havingPeriod}" +
                        $"\nPerBlock: {poolTwoPerBlock}" +
                        $"\nStartBlock: {poolTwoStart}");
            var totalPoolReward = GetPoolTwoTotalReward(poolTwoPerBlock, havingPeriod);
            Logger.Info($"totalPoolReward: {totalPoolReward}");
            var initializePoolTwo = _awakenFarmTwoContract.Initialize(DistributeToken, poolTwoPerBlock, havingPeriod,
                poolTwoStart, totalPoolReward, _awakenTokenContract.ContractAddress, redepositStartBlock);
            initializePoolTwo.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Create USDT
            CreateToken(NewRewardToken, 6, InitAccount.ConvertAddress());
            if (_tokenContract.GetUserBalance(AdminAddress) < 1000_000000)
                _tokenContract.TransferBalance(InitAccount, AdminAddress, 1000_000000);

            // Create distribute token
            CreateToken(DistributeToken, 8, InitAccount.ConvertAddress());
            _tokenContract.IssueBalance(InitAccount, AdminAddress, 1000_00000000, DistributeToken);
            _tokenContract.IssueBalance(InitAccount, _awakenFarmTwoContract.ContractAddress,
                long.Parse(totalPoolReward.Value), DistributeToken);

            ChangeTokenIssuer(DistributeToken, InitAccount);

            var setFarmOne = _awakenFarmTwoContract.SetFarmPoolOne(_awakenFarmContract.Contract);
            setFarmOne.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Initialize swap
            InitializeSwapTest();

            // Create pair
            CreatePair("ELF", 8, "USDT", 6);
            CreatePair("AWKN", 8, "ELF", 8);

            // Add liquidity
            AddLiquidity("ELF", "USDT", 400_00000000, 200_000000);
            AddLiquidity("AWKN", "ELF", 400_00000000, 400_00000000);

            SetReDeposit();
        }

        [TestMethod]
        public void InitializeSwapTest()
        {
            var initializeToken =
                _awakenTokenContract.ExecuteMethodWithResult(AwakenTokenMethod.Initialize,
                    new Awaken.Contracts.Token.InitializeInput {Owner = _awakenSwapContract.Contract});
            initializeToken.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var result = _awakenSwapContract.ExecuteMethodWithResult(SwapMethod.Initialize,
                new Awaken.Contracts.Swap.InitializeInput
                {
                    Admin = InitAccount.ConvertAddress(),
                    AwakenTokenContractAddress = _awakenTokenContract.Contract
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var admin = _awakenSwapContract.GetAdmin();
            admin.ShouldBe(InitAccount.ConvertAddress());

            var setFeeTo =
                _awakenSwapContract.ExecuteMethodWithResult(SwapMethod.SetFeeTo, FeeToAccount.ConvertAddress());
            setFeeTo.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var getFeeTo = _awakenSwapContract.GetFeeTo();
            getFeeTo.ShouldBe(FeeToAccount.ConvertAddress());

            var setFeeRate =
                _awakenSwapContract.ExecuteMethodWithResult(SwapMethod.SetFeeRate, new Int64Value {Value = FeeRate});
            setFeeRate.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var getFeeRate = _awakenSwapContract.GetFeeRate();
            getFeeRate.ShouldBe(FeeRate);
        }

        [TestMethod]
        public void FixEndBlockFixedTest()
        {
            InitializeTest();

            var startBlock = _awakenFarmTwoContract.GetStartBlock().Value;
            var halvingPeriod = _awakenFarmTwoContract.GetHalvingPeriod().Value;
            var endBlock = _awakenFarmTwoContract.EndBlock().Value;
            endBlock.ShouldBe(startBlock.Add(halvingPeriod * 4));
            Logger.Info($"\nstartBlock:{startBlock}" +
                        $"\nhalvingPeriod:{halvingPeriod}" +
                        $"\nendBlock:{endBlock}");

            // Add pool
            _awakenFarmContract.SetAccount(AdminAddress);
            var addResult = _awakenFarmContract.AddPool(1, true, LPTOKEN_EU);
            addResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var addPoolOneResult = AddPool(1, LPTOKEN, true);
            var addPoolTwoResult = AddPool(2, LPTOKEN, true);
            addPoolOneResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            addPoolTwoResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Approve
            var approveResult =
                _awakenTokenContract.ApproveLPToken(_awakenFarmContract.ContractAddress, AdminAddress, 100_00000000,
                    LPTOKEN_EU);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            approveResult =
                _awakenTokenContract.ApproveLPToken(_awakenFarmTwoContract.ContractAddress, AdminAddress, 100_00000000,
                    LPTOKEN);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Deposit before startBlock
            var depositResultFarm1 = _awakenFarmContract.Deposit(0, 1_00000000, AdminAddress);
            depositResultFarm1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            _awakenFarmTwoContract.SetAccount(AdminAddress);
            var depositResult = _awakenFarmTwoContract.Deposit(1, 1_00000000);
            depositResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Info before fix endBlock
            var amount = _awakenFarmTwoContract.GetUserInfo(1, AdminAddress).Amount;
            var totalAmount = _awakenFarmTwoContract.GetPoolInfo(1).TotalAmount;
            var distributeTokenBefore = _tokenContract.GetUserBalance(AdminAddress, DistributeToken);
            Logger.Info($"\namount:{amount}" +
                        $"\ntotalAmount:{totalAmount}" +
                        $"\ndistributeTokenBefore:{distributeTokenBefore}");

            while (NodeManager.ApiClient.GetBlockHeightAsync().Result <= startBlock)
            {
                Thread.Sleep(5 * 1000);
            }

            var poolInfoPerShare0 = _awakenFarmTwoContract.GetPoolInfo(0).AccDistributeTokenPerShare;
            poolInfoPerShare0.ShouldBe(0);

            _awakenFarmTwoContract.SetAccount(InitAccount);
            var fixEndBlock = _awakenFarmTwoContract.FixEndBlock(true);
            Logger.Info($"fixEndBlockNumber1 {fixEndBlock.BlockNumber}");
            Logger.Info($"IssuedReward1 {_awakenFarmTwoContract.IssuedReward()}");
            var redepositAdjustFlagBefore = _awakenFarmTwoContract.RedepositAdjustFlag();
            redepositAdjustFlagBefore.ShouldBe(false);

            while (NodeManager.ApiClient.GetBlockHeightAsync().Result <= startBlock.Add(Block0))
            {
                Thread.Sleep(5 * 1000);
            }

            // Redeposit
            ReDeposit(0);

            // Fix endBlock after redeposit
            var result = _awakenFarmTwoContract.FixEndBlock(true);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Info after fix endBlock
            Logger.Info($"fixEndBlockNumber2:{result.BlockNumber}");

            var redepositStartBlock = _awakenFarmTwoContract.RedepositStartBlock();
            var redepositAdjustFlagAfter = _awakenFarmTwoContract.RedepositAdjustFlag();
            var afterEndBlock = _awakenFarmContract.GetEndBlock();
            redepositAdjustFlagAfter.ShouldBe(true);
            redepositStartBlock.ShouldBe(startBlock.Add(Block0));
            // afterEndBlock.ShouldBe(endBlock);
            var toalReward = _awakenFarmTwoContract.GetTotalReward();
            var ph1TotalReward1 = (redepositStartBlock - startBlock) * halvingPeriod * 2 / 3;
            var ph1TotalReward2 = (startBlock + halvingPeriod - redepositStartBlock) * PerBlock1;
            var ph2TotalReward = halvingPeriod * PerBlock1 / 2;
            var ph3TotalReward = halvingPeriod * PerBlock1 / 2 / 2;
            var ph4TotalReward = halvingPeriod * PerBlock1 / 2 / 2;
            var ph5TotalReward = (afterEndBlock - startBlock - PerBlock1 * 4) * PerBlock1 / 2 / 2;
            var expectTotalReward = ph1TotalReward1 + ph1TotalReward2 + ph2TotalReward + ph3TotalReward +
                                    ph4TotalReward + ph5TotalReward;
            toalReward.ShouldBe(expectTotalReward);
            Logger.Info($"endBlockNew:{afterEndBlock}");
            Logger.Info($"IssuedReward2 {_awakenFarmTwoContract.IssuedReward()}");
            Logger.Info($"toalReward {toalReward}");
            Logger.Info($"expectTotalReward {expectTotalReward}");
        }

        [TestMethod]
        [DataRow(0)]
        public void ReDeposit(int pid)
        {
            var depositAddress = AdminAddress;
            var symbolA = DistributeToken;
            var symbolB = "ELF";
            var pair = _awakenSwapContract.GetTokenPair(symbolA, symbolB);

            var originUserBalance = _tokenContract.GetUserBalance(depositAddress);
            var originUserDistributeBalance = _tokenContract.GetUserBalance(depositAddress, DistributeToken);
            var originPoolTwoInfo = _awakenFarmTwoContract.GetPoolInfo(0);

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
            var elfAmount = amount;
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

            // ReDeposit
            var result = _awakenFarmContract.ReDeposit(pid, amount, elfAmount, depositAddress);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var reDepositBlockNumber = result.BlockNumber;
            Logger.Info($"reDepositBlockNumber:{reDepositBlockNumber}");
            var addLiquidityLogs =
                ByteString.FromBase64(result.Logs.First(l => l.Name.Contains("LiquidityAdded")).NonIndexed);
            var liquidityAdded = LiquidityAdded.Parser.ParseFrom(addLiquidityLogs);
            liquidityAdded.SymbolA.ShouldBe(symbolA);
            liquidityAdded.SymbolB.ShouldBe(symbolB);
            liquidityAdded.AmountA.ShouldBe(amount);
            liquidityAdded.AmountB.ShouldBe(elfExceptAmount);
            Logger.Info(liquidityAdded.LiquidityToken);

            var getPoolTwoInfo = _awakenFarmTwoContract.GetPoolInfo(0);
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

        private TransactionResultDto AddPool(long allocPoint, string lpToken, bool with_update)
        {
            var result = _awakenFarmTwoContract.Add(allocPoint, lpToken, with_update);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            return result;
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

        private void CreateToken(string symbol, int d, Address issuer)
        {
            var info = _tokenContract.GetTokenInfo(symbol);
            if (info.Symbol == symbol)
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

        private void CreateLpToken(string symbol, int decimals, Address issuer, long totalSupply)
        {
            var result = _awakenTokenContract.ExecuteMethodWithResult(AwakenTokenMethod.Create, new CreateInput
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

        [TestMethod]
        public void SetReDeposit()
        {
            var result = _awakenFarmContract.SetReDeposit
                (AdminAddress, _awakenSwapContract.Contract, _awakenFarmTwoContract.Contract);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void GetTokenInfo()
        {
            var resultELF = _tokenContract.GetTokenInfo("ELF");
            var resultUSDT = _tokenContract.GetTokenInfo("USDT");
            var resultAWKN = _tokenContract.GetTokenInfo("AWKN");
            Logger.Info($"\nresultELF: {resultELF}" +
                        $"\nresultUSDT: {resultUSDT}" +
                        $"\nresultAWKN: {resultAWKN}");
        }

        [TestMethod]
        public void TransferTest()
        {
            var result = _tokenContract.TransferBalance(InitAccount, AdminAddress, 500_00000000, "ELF");
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void GetLpTokenBalanceInfo()
        {
            // Init balance
            var resultEU = _awakenTokenContract.GetBalance(LPTOKEN_EU, InitAccount.ConvertAddress());
            var resultAE = _awakenTokenContract.GetBalance(LPTOKEN, InitAccount.ConvertAddress());
            Logger.Info($"\nresultEU: {resultEU}" +
                        $"\nresultAE: {resultAE}");

            var resultELF = _tokenContract.GetUserBalance(InitAccount, "ELF");
            var resultUSDT = _tokenContract.GetUserBalance(InitAccount, "USDT");
            var resultAWKN = _tokenContract.GetUserBalance(InitAccount, "AWKN");
            Logger.Info($"\nresultELF: {resultELF}" +
                        $"\nresultUSDT: {resultUSDT}" +
                        $"\nresultAWKN: {resultAWKN}");

            // Admin balance
            resultEU = _awakenTokenContract.GetBalance(LPTOKEN_EU, AdminAddress.ConvertAddress());
            resultAE = _awakenTokenContract.GetBalance(LPTOKEN, AdminAddress.ConvertAddress());
            Logger.Info($"\nresultEU: {resultEU}" +
                        $"\nresultAE: {resultAE}");

            resultELF = _tokenContract.GetUserBalance(AdminAddress, "ELF");
            resultUSDT = _tokenContract.GetUserBalance(AdminAddress, "USDT");
            resultAWKN = _tokenContract.GetUserBalance(AdminAddress, "AWKN");
            Logger.Info($"\nresultELF: {resultELF}" +
                        $"\nresultUSDT: {resultUSDT}" +
                        $"\nresultAWKN: {resultAWKN}");
        }

        [TestMethod]
        public void CreateTokenTest()
        {
            var symbol = "USDT";
            var decimals = 6;
            var issuer = InitAccount.ConvertAddress();
            var totalSupply = 90000000000_000000;
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

        private void ChangeTokenIssuer(string symbol, string issuer)
        {
            _tokenContract.SetAccount(issuer);
            var changeIssue = _tokenContract.ExecuteMethodWithResult(TokenMethod.ChangeTokenIssuer,
                new ChangeTokenIssuerInput
                {
                    NewTokenIssuer = _awakenFarmContract.Contract,
                    Symbol = symbol
                });
            changeIssue.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var info = _tokenContract.GetTokenInfo(symbol);
            info.Issuer.ShouldBe(_awakenFarmContract.Contract);
        }

        [TestMethod]
        [DataRow("ELF", 8, "USDT", 6)]
        [DataRow("AWKN", 8, "ELF", 8)]
        public void CreatePair(string symbolA, int dA, string symbolB, int dB)
        {
            if (CheckToken(symbolA))
                CreateTokenLP(symbolA, dA);
            if (CheckToken(symbolB))
                CreateTokenLP(symbolB, dB);
            var pair = _awakenSwapContract.GetTokenPair(symbolA, symbolB);
            var pairList = _awakenSwapContract.GetPairs();
            if (pairList.Value.Contains(pair))
            {
                Logger.Info($"Already create pair {pair}");
                return;
            }

            var orderPair = _awakenSwapContract.SortSymbols(symbolA, symbolB);
            var pairSymbol = _awakenSwapContract.GetTokenPairSymbol(symbolA, symbolB);
            var create = _awakenSwapContract.CreatePair(pair, out var pairAddress);
            create.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logs = ByteString.FromBase64(create.Logs.First(l => l.Name.Contains("PairCreated")).NonIndexed);
            var pairCreated = PairCreated.Parser.ParseFrom(logs);
            pairCreated.SymbolA.ShouldBe(orderPair[0]);
            pairCreated.SymbolB.ShouldBe(orderPair[1]);
            pairCreated.Pair.ShouldBe(pairAddress);
            Logger.Info(pairAddress);

            var lpLogs = ByteString.FromBase64(create.Logs.First(l => l.Name.Contains("TokenCreated")).NonIndexed);
            var tokenCreated = TokenCreated.Parser.ParseFrom(lpLogs);
            tokenCreated.Decimals.ShouldBe(8);
            tokenCreated.Symbol.ShouldBe(pairSymbol);
            tokenCreated.Issuer.ShouldBe(_awakenSwapContract.Contract);
            tokenCreated.TokenName.ShouldBe($"Awaken {pair} LP Token");
            tokenCreated.IsBurnable.ShouldBeTrue();

            pairList = _awakenSwapContract.GetPairs();
            pairList.Value.ShouldContain(pair);
            var reserves = _awakenSwapContract.GetReserves(pair);
            var reserveResult = reserves.Results.First(r => r.SymbolPair.Equals(pair));
            reserveResult.ReserveA.ShouldBe(0);
            reserveResult.ReserveB.ShouldBe(0);
            var totalSupply = _awakenSwapContract.GetTotalSupply(pair);
            var totalSupplyResult = totalSupply.Results.First(t => t.SymbolPair.Equals(pair));
            totalSupplyResult.TotalSupply.ShouldBe(0);
            var getPairAddress = _awakenSwapContract.GetPairAddress(symbolA, symbolB);
            getPairAddress.ShouldBe(pairAddress);

            var tokenInfo = _awakenTokenContract.GetTokenInfo(pairSymbol);
            tokenInfo.Decimals.ShouldBe(8);
            tokenInfo.Issuer.ShouldBe(_awakenSwapContract.Contract);
            tokenInfo.IsBurnable.ShouldBeTrue();
            Logger.Info(pairSymbol);
        }

        private bool CheckToken(string symbol)
        {
            var tokenInfo = _tokenContract.GetTokenInfo(symbol);
            return tokenInfo.Equals(new AElf.Contracts.MultiToken.TokenInfo());
        }

        private void CreateTokenLP(string symbol, int d)
        {
            long totalSupply = 1_00000000;
            var t = totalSupply.Mul(Int64.Parse(new BigIntValue(10).Pow(d).Value));
            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create,
                new AElf.Contracts.MultiToken.CreateInput
                {
                    Symbol = symbol,
                    Decimals = d,
                    Issuer = InitAccount.ConvertAddress(),
                    TokenName = $"{symbol} token",
                    TotalSupply = t
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        [DataRow("ELF", "USDT", 200_00000000, 200_000000)]
        public void AddLiquidity(string symbolA, string symbolB, long amountA, long amountB)
        {
            var account = AdminAddress;
            var pair = _awakenSwapContract.GetTokenPair(symbolA, symbolB);
            var pairList = _awakenSwapContract.GetPairs();
            if (!pairList.Value.Contains(pair))
            {
                Logger.Info($"No Pair {pair}");
                return;
            }

            var sortPair = _awakenSwapContract.SortSymbols(symbolA, symbolB);
            var newAmountA = sortPair.First().Equals(symbolA) ? amountA : amountB;
            var newAmountB = sortPair.Last().Equals(symbolB) ? amountB : amountA;
            symbolA = sortPair.First();
            symbolB = sortPair.Last();
            amountA = newAmountA;
            amountB = newAmountB;

            var origin = CheckPairData(symbolA, symbolB, account);
            var pairSymbol = _awakenSwapContract.GetTokenPairSymbol(symbolA, symbolB);
            Logger.Info(pairSymbol);
            var pairAddress = _awakenSwapContract.GetPairAddress(symbolA, symbolB);
            var originFeeToLpTokenBalance = _awakenTokenContract.GetBalance(pairSymbol, FeeToAccount.ConvertAddress());
            Logger.Info($"origin FeeTo LPTokenBalance: {originFeeToLpTokenBalance.Amount}");
            var originTokenInfo = _awakenTokenContract.GetTokenInfo(pairSymbol);
            Logger.Info(originTokenInfo);
            if (origin["totalSupply"] != 0)
            {
                amountB = _awakenSwapContract.Quote(symbolA, symbolB, amountA);
                Logger.Info($"new amountB is {amountB}");
            }

            CheckBalance(symbolA, amountA, account);
            CheckBalance(symbolB, amountB, account);
            Approve(account, amountA * 2, symbolA);
            Approve(account, amountB * 2, symbolB);
            origin = CheckPairData(symbolA, symbolB, account);

            _awakenSwapContract.SetAccount(account);
            var result = _awakenSwapContract.AddLiquidity(out var output, symbolA, symbolB, amountA, amountB,
                account.ConvertAddress());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            output.AmountA.ShouldBe(amountA);
            output.AmountB.ShouldBe(amountB);
            output.SymbolA.ShouldBe(symbolA);
            output.SymbolB.ShouldBe(symbolB);

            // var txFee = result.GetResourceTokenFee();
            var logs = ByteString.FromBase64(result.Logs.First(l => l.Name.Contains("LiquidityAdded")).NonIndexed);
            var liquidityAdded = LiquidityAdded.Parser.ParseFrom(logs);
            liquidityAdded.SymbolA.ShouldBe(symbolA);
            liquidityAdded.SymbolB.ShouldBe(symbolB);
            liquidityAdded.AmountA.ShouldBe(amountA);
            liquidityAdded.AmountB.ShouldBe(amountB);

            Logger.Info(liquidityAdded.Pair);
            Logger.Info(liquidityAdded.LiquidityToken);

            var syncLogs = ByteString.FromBase64(result.Logs.First(l => l.Name.Contains("Sync")).NonIndexed);
            var sync = Sync.Parser.ParseFrom(syncLogs);
            CheckSyncEvent(sync, symbolA, symbolB, pairAddress, origin["ReserveA"],
                origin["ReserveB"], liquidityAdded.AmountA, liquidityAdded.AmountB, "add");

            var after = CheckPairData(symbolA, symbolB, account);
            after["UserLPBalance"].ShouldBe(origin["UserLPBalance"] + liquidityAdded.LiquidityToken);
            after["ReserveA"].ShouldBe(origin["ReserveA"] + liquidityAdded.AmountA);
            after["ReserveB"].ShouldBe(origin["ReserveB"] + liquidityAdded.AmountB);

            _awakenSwapContract.SetAccount(account);
            var afterTokenInfo = _awakenTokenContract.GetTokenInfo(pairSymbol);
            Logger.Info(afterTokenInfo);

            if (origin["totalSupply"] == 0)
            {
                after["totalSupply"].ShouldBe(origin["totalSupply"] + liquidityAdded.LiquidityToken + 1);
                afterTokenInfo.Supply.ShouldBe(originTokenInfo.Supply + liquidityAdded.LiquidityToken + 1);

                var feeToLpTokenBalance = _awakenTokenContract.GetBalance(pairSymbol, FeeToAccount.ConvertAddress());
                feeToLpTokenBalance.Amount.ShouldBe(0);
            }
            else
            {
                var feeToLpTokenBalance = _awakenTokenContract.GetBalance(pairSymbol, FeeToAccount.ConvertAddress());
                Logger.Info($"after add liquidity fee to Lp token balance:  {feeToLpTokenBalance.Amount}");
                var fee = feeToLpTokenBalance.Amount - originFeeToLpTokenBalance.Amount;
                Logger.Info($"actual fee: {fee}");
                after["totalSupply"].ShouldBe(origin["totalSupply"] + liquidityAdded.LiquidityToken +
                    feeToLpTokenBalance.Amount - originFeeToLpTokenBalance.Amount);
                afterTokenInfo.Supply.ShouldBe(originTokenInfo.Supply + liquidityAdded.LiquidityToken + fee);
            }

            var afterKLast = CheckKLast(pairAddress);
            afterKLast["KLast"]
                .ShouldBe(new BigIntValue(after["ReserveA"]).Mul(new BigIntValue(after["ReserveB"])).Value);
            after["UserBalanceA"].ShouldBe(origin["UserBalanceA"] - amountA);
            after["UserBalanceB"].ShouldBe(origin["UserBalanceB"] - amountB);
            after["ReserveA"].ShouldBe(origin["ReserveA"] + amountA);
            after["ReserveB"].ShouldBe(origin["ReserveB"] + amountB);
            after["ContractBalanceA"].ShouldBe(origin["ContractBalanceA"] + amountA);
            after["ContractBalanceB"].ShouldBe(origin["ContractBalanceB"] + amountB);
        }

        private Dictionary<string, string> CheckKLast(Address pairAddress)
        {
            var kLastInfo = new Dictionary<string, string>();
            var kLast = _awakenSwapContract.GetKLast(pairAddress);
            Logger.Info($"KLast: {kLast.Value}");
            kLastInfo["KLast"] = kLast.Value;
            return kLastInfo;
        }

        private void CheckSyncEvent(Sync sync, string symbolIn, string symbolOut, Address pairAddress,
            long originReserveA, long originReserveB, long amountIn, long amountOut, string type)
        {
            Logger.Info(sync);
            sync.SymbolA.ShouldBe(symbolIn);
            sync.SymbolB.ShouldBe(symbolOut);
            sync.Pair.ShouldBe(pairAddress);
            switch (type)
            {
                case "swap":
                    sync.ReserveA.ShouldBe(originReserveA + amountIn);
                    sync.ReserveB.ShouldBe(originReserveB - amountOut);
                    break;
                case "add":
                    sync.ReserveA.ShouldBe(originReserveA + amountIn);
                    sync.ReserveB.ShouldBe(originReserveB + amountOut);
                    break;
                case "remove":
                    sync.ReserveA.ShouldBe(originReserveA - amountIn);
                    sync.ReserveB.ShouldBe(originReserveB - amountOut);
                    break;
            }
        }

        private Dictionary<string, long> CheckPairData(string symbolA, string symbolB, string sender = "")
        {
            var account = sender == "" ? InitAccount : sender;
            var resultsList = new Dictionary<string, long>();
            _awakenSwapContract.SetAccount(account);
            var pair = $"{symbolA}-{symbolB}";
            var pairSymbol = _awakenSwapContract.GetTokenPairSymbol(symbolA, symbolB);
            var totalSupply = _awakenSwapContract.GetTotalSupply(pair);
            var result = totalSupply.Results.First(r => r.SymbolPair.Equals(pair));
            resultsList["totalSupply"] = result.TotalSupply;

            var reserves = _awakenSwapContract.GetReserves(pair);
            var reservesResult = reserves.Results.First(r => r.SymbolPair.Equals(pair));

            resultsList["ReserveA"] = reservesResult.SymbolA.Equals(symbolA)
                ? reservesResult.ReserveA
                : reservesResult.ReserveB;
            resultsList["ReserveB"] = reservesResult.SymbolB.Equals(symbolB)
                ? reservesResult.ReserveB
                : reservesResult.ReserveA;

            var symbolList = pair.Split("-").ToList();
            var userBalanceA = _tokenContract.GetUserBalance(account, symbolList.First());
            var userBalanceB = _tokenContract.GetUserBalance(account, symbolList.Last());
            var userLpBalance = _awakenTokenContract.GetBalance(pairSymbol, account.ConvertAddress());
            var contractBalanceA =
                _tokenContract.GetUserBalance(_awakenSwapContract.ContractAddress, symbolList.First());
            var contractBalanceB =
                _tokenContract.GetUserBalance(_awakenSwapContract.ContractAddress, symbolList.Last());

            resultsList["UserBalanceA"] = userBalanceA;
            resultsList["UserBalanceB"] = userBalanceB;
            resultsList["UserLPBalance"] = userLpBalance.Amount;
            resultsList["ContractBalanceA"] = contractBalanceA;
            resultsList["ContractBalanceB"] = contractBalanceB;

            Logger.Info($"\ntotalSupply: {result.TotalSupply}\n" +
                        $"{symbolA} ReserveA: {resultsList["ReserveA"]}\n" +
                        $"{symbolB} ReserveB: {resultsList["ReserveB"]}\n" +
                        $"UserBalanceA: {userBalanceA}\n" +
                        $"UserBalanceB: {userBalanceB}\n" +
                        $"UserLPBalance: {userLpBalance.Amount}\n" +
                        $"ContractBalanceA: {contractBalanceA}\n" +
                        $"ContractBalanceB: {contractBalanceB}");
            return resultsList;
        }

        private void CheckBalance(string symbol, long amount, string account)
        {
            var balance = _tokenContract.GetUserBalance(account, symbol);
            if (balance < amount && symbol != "ELF")
            {
                var result = _tokenContract.IssueBalance(InitAccount, account, amount * 10, symbol);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }
            else if (balance < amount && symbol == "ELF")
            {
                var result = _tokenContract.TransferBalance(InitAccount, account, amount * 10, symbol);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }
        }

        private void Approve(string sender, long amount, string symbol)
        {
            _tokenContract.SetAccount(sender);
            var approve = _tokenContract.ApproveToken(sender, _awakenSwapContract.ContractAddress, amount, symbol);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
    }
}