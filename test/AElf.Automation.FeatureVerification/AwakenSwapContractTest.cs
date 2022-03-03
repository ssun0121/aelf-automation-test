using System;
using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Awaken.Contracts.Swap;
using Awaken.Contracts.Token;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using InitializeInput = Awaken.Contracts.Swap.InitializeInput;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class GandalfSwapContractTest
    {
        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;
        private AwakenTokenContract _awakenTokenContract;
        private AwakenSwapContract _awakenSwapContract;
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }
        private string tokenAddress = "VZCyHSPayr4PPyHqDKUTSbpR2o7MJgjXkHqMUVv9SEbTYoWqw";
        private string swapAddress = "2wGCD2xYsXyAuHaU33PPiUqCT9LdzA6RuMCNnE4dozpxVKFWSR";
        private string InitAccount { get; } = "nn659b9X1BLhnu5RWmEUbuuV7J9QKVVSN54j9UmeCbF3Dve5D";
        private string FeeToAccount { get; } = "Zz4iuCCCktGjZGmQ9vMcxh2JT9pDTFA4XSR7WDNrndYcEXtRx";
        private static string RpcUrl { get; } = "http://192.168.67.166:8000";
        private long FeeRate { get; } = 30;


        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("AwakenSwapContractTest");
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
        }

        [TestMethod]
        public void InitializeTest()
        {
            var initializeToken =
                _awakenTokenContract.ExecuteMethodWithResult(AwakenTokenMethod.Initialize,
                    new Awaken.Contracts.Token.InitializeInput {Owner = _awakenSwapContract.Contract});
            initializeToken.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var result = _awakenSwapContract.ExecuteMethodWithResult(SwapMethod.Initialize, new InitializeInput
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
        public void InitializeTest_ERROR()
        {
            var result = _awakenSwapContract.ExecuteMethodWithResult(SwapMethod.Initialize, new Empty());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("Already initialized.");

            _awakenSwapContract.SetAccount(FeeToAccount);
            var setFeeTo =
                _awakenSwapContract.ExecuteMethodWithResult(SwapMethod.SetFeeTo, FeeToAccount.ConvertAddress());
            setFeeTo.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            setFeeTo.Error.ShouldContain("No permission");

            var setFeeRate =
                _awakenSwapContract.ExecuteMethodWithResult(SwapMethod.SetFeeRate, new Int64Value {Value = FeeRate});
            setFeeRate.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            setFeeRate.Error.ShouldContain("No permission");
        }

        //USDT-ELF：vfrEQcv4VKHzndF7JDog5Uj7Kc3eUhnbXDVvs9ivUKqFH6tzN
        //ABC-USDT: BxXZuCEoyRBDS236q8xgzNXQ9Wj8urufZsGGDzKLikDgewF6U
        //ETH-USDT：2TrLtiVs4ptRJqCiZEMc23uQ6j7VTrVUfaEATmu6iFByqNnBEV
        //ABC-TEST:d7QZjmCsrN4hQmFYmiN2LSsM3VLvvdTB4T9rK9ws7rqjN4UxM
        [TestMethod]
        [DataRow("ELF",8,"USDT",6)]
        [DataRow("ETH",8,"USDT",6)]
        public void CreatePair(string symbolA, int dA, string symbolB, int dB)
        {
            if (CheckToken(symbolA))
                CreateToken(symbolA, dA);
            if (CheckToken(symbolB))
                CreateToken(symbolB, dB);
            var pair = GetTokenPair(symbolA, symbolB);
            var pairList = _awakenSwapContract.GetPairs();
            if (pairList.Value.Contains(pair))
            {
                Logger.Info($"Already create pair {pair}");
                return;
            }

            var orderPair = SortSymbols(symbolA, symbolB);
            var pairSymbol = GetTokenPairSymbol(symbolA, symbolB);
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
            tokenCreated.Decimals.ShouldBe(0);
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
            tokenInfo.Decimals.ShouldBe(0);
            tokenInfo.Issuer.ShouldBe(_awakenSwapContract.Contract);
            tokenInfo.IsBurnable.ShouldBeTrue();
        }

        [TestMethod]
        public void CreatePair_ERROR()
        {
            {
                var symbolA = "ELF";
                var symbolB = "USDT";
                var pair = GetTokenPair(symbolA, symbolB);
                var create = _awakenSwapContract.CreatePair(pair, out var pairAddress);
                create.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                create.Error.ShouldContain($"Pair {pair} Already Exist.");
            }
            {
                var symbolA = "ELF";
                var symbolB = "ABC";
                var pair = $"{symbolA}-{symbolB}";
                var create = _awakenSwapContract.CreatePair(pair, out var pairAddress);
                create.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                create.Error.ShouldContain("Token ABC not exists.");
            }
            {
                var symbolA = "ABC";
                var symbolB = "ELF";
                var pair = $"{symbolA}-{symbolB}";
                var create = _awakenSwapContract.CreatePair(pair, out var pairAddress);
                create.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                create.Error.ShouldContain("Token ABC not exists.");
            }
            {
                var symbolA = "ELF";
                var symbolB = "ELF";
                var pair = $"{symbolA}-{symbolB}";
                var create = _awakenSwapContract.CreatePair(pair, out var pairAddress);
                create.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                create.Error.ShouldContain("Identical Tokens");
            }
            {
                var symbolA = "ELF";
                var symbolB = "ETH";
                var pair = $"{symbolA}{symbolB}";
                var create = _awakenSwapContract.CreatePair(pair, out var pairAddress);
                create.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                create.Error.ShouldContain("PairVirtualAddressMap");
            }
        }

        [TestMethod]
        [DataRow("TEST","ABC",2000_00000000,1000_00000000)]
        [DataRow("ABC","USDT",2000_00000000,1000_000000)]
        [DataRow("ELF","USDT",10000_00000000,3000_000000)]
        [DataRow("ETH","USDT",10_00000000,20000_000000)]
        public void AddLiquidity(string symbolA, string symbolB, long amountA, long amountB)
        {
            var account = InitAccount;
            var pair = GetTokenPair(symbolA, symbolB);
            var pairList = _awakenSwapContract.GetPairs();
            if (!pairList.Value.Contains(pair))
            {
                Logger.Info($"No Pair {pair}");
                return;
            }

            var sortPair = SortSymbols(symbolA, symbolB);
            var newAmountA = sortPair.First().Equals(symbolA) ? amountA : amountB;
            var newAmountB = sortPair.Last().Equals(symbolB) ? amountB : amountA;
            symbolA = sortPair.First();
            symbolB = sortPair.Last();
            amountA = newAmountA;
            amountB = newAmountB;

            var origin = CheckPairData(symbolA, symbolB);
            var pairSymbol = GetTokenPairSymbol(symbolA, symbolB);
            Logger.Info(pairSymbol);
            var pairAddress = _awakenSwapContract.GetPairAddress(symbolA, symbolB);
            var originFeeToLpTokenBalance = _awakenTokenContract.GetBalance(pairSymbol, FeeToAccount.ConvertAddress());
            Logger.Info($"origin FeeTo LPTokenBalance: {originFeeToLpTokenBalance.Amount}");

            if (origin["totalSupply"] != 0)
            {
                amountB = _awakenSwapContract.Quote(symbolA, symbolB, amountA);
                Logger.Info($"new amountB is {amountB}");
            }

            CheckBalance(symbolA, amountA, account);
            CheckBalance(symbolB, amountB, account);
            Approve(account, amountA, symbolA);
            Approve(account, amountB, symbolB);
            origin = CheckPairData(symbolA, symbolB);

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

            var after = CheckPairData(symbolA, symbolB);
            after["UserLPBalance"].ShouldBe(origin["UserLPBalance"] + liquidityAdded.LiquidityToken);
            after["ReserveA"].ShouldBe(origin["ReserveA"] + liquidityAdded.AmountA);
            after["ReserveB"].ShouldBe(origin["ReserveB"] + liquidityAdded.AmountB);

            if (origin["totalSupply"] == 0)
            {
                after["totalSupply"].ShouldBe(origin["totalSupply"] + liquidityAdded.LiquidityToken + 1);
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

            _awakenSwapContract.SetAccount(account);
            var accountAssets = _awakenSwapContract.GetAccountAssets();
            accountAssets.Value.ShouldContain(pair);
        }

        [TestMethod]
        public void AddLiquidity_AfterSwap()
        {
            //swap
            var symbolIn = "TEST";
            var symbolOut = "ABC";
            var account = InitAccount;
            var amountIn = 200000000;
            var pairAddress = _awakenSwapContract.GetPairAddress(symbolIn, symbolOut);
            var origin = CheckPairData(symbolIn, symbolOut);

            Approve(account, amountIn, symbolIn);
            CheckBalance(symbolIn, amountIn, account);

            var path = new List<string> {symbolIn, symbolOut};
            _awakenSwapContract.SetAccount(account);
            var swapResult =
                _awakenSwapContract.SwapExactTokensForTokens(out var swapOutput, account.ConvertAddress(), path,
                    amountIn);
            swapResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info(swapOutput);
            var swapLogs = ByteString.FromBase64(swapResult.Logs.First(l => l.Name.Contains("Swap")).NonIndexed);
            var swap = Swap.Parser.ParseFrom(swapLogs);
            Logger.Info(swap);
            var totalFee = swap.TotalFee;

            long amountA = 2000_00000000;
            long amountB = 1000_00000000;
            var pair = GetTokenPair(symbolIn, symbolOut);
            var sortPair = SortSymbols(symbolIn, symbolOut);
            var newAmountA = sortPair.First().Equals(symbolIn) ? amountA : amountB;
            var symbolA = sortPair.First();
            var symbolB = sortPair.Last();
            amountA = newAmountA;
            var pairSymbol = GetTokenPairSymbol(symbolA, symbolB);
            Logger.Info(pairSymbol);
            var originFeeToLpTokenBalance = _awakenTokenContract.GetBalance(pairSymbol, FeeToAccount.ConvertAddress());
            Logger.Info($"origin FeeTo LPTokenBalance: {originFeeToLpTokenBalance.Amount}");
            amountB = _awakenSwapContract.Quote(symbolA, symbolB, amountA);
            Logger.Info($"amountB is {amountB}");

            CheckBalance(symbolA, amountA, account);
            CheckBalance(symbolB, amountB, account);
            Approve(account, amountA, symbolA);
            Approve(account, amountB, symbolB);
            origin = CheckPairData(symbolA, symbolB);

            _awakenSwapContract.SetAccount(account);
            var result = _awakenSwapContract.AddLiquidity(out var output, symbolA, symbolB, amountA, amountB,
                account.ConvertAddress());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            output.AmountA.ShouldBe(amountA);
            output.AmountB.ShouldBe(amountB);
            output.SymbolA.ShouldBe(symbolA);
            output.SymbolB.ShouldBe(symbolB);
            var logs = ByteString.FromBase64(result.Logs.First(l => l.Name.Contains("LiquidityAdded")).NonIndexed);
            var liquidityAdded = LiquidityAdded.Parser.ParseFrom(logs);
            liquidityAdded.SymbolA.ShouldBe(symbolA);
            liquidityAdded.SymbolB.ShouldBe(symbolB);
            liquidityAdded.AmountA.ShouldBe(amountA);
            liquidityAdded.AmountB.ShouldBe(amountB);

            Logger.Info(liquidityAdded.Pair);
            Logger.Info(liquidityAdded.LiquidityToken);
            var after = CheckPairData(symbolA, symbolB);
            after["UserLPBalance"].ShouldBe(origin["UserLPBalance"] + liquidityAdded.LiquidityToken);
            after["ReserveA"].ShouldBe(origin["ReserveA"] + liquidityAdded.AmountA);
            after["ReserveB"].ShouldBe(origin["ReserveB"] + liquidityAdded.AmountB);

            var feeToLpTokenBalance = _awakenTokenContract.GetBalance(pairSymbol, FeeToAccount.ConvertAddress());
            Logger.Info($"after add liquidity fee to Lp token balance:  {feeToLpTokenBalance.Amount}");
            var fee = feeToLpTokenBalance.Amount - originFeeToLpTokenBalance.Amount;
            Logger.Info($"fee: {fee}");
            after["totalSupply"].ShouldBe(origin["totalSupply"] + liquidityAdded.LiquidityToken +
                feeToLpTokenBalance.Amount - originFeeToLpTokenBalance.Amount);
            var reserveIn = symbolIn.Equals(symbolA) ? origin["ReserveA"] : origin["ReserveB"]; 
            var reserveOut = reserveIn.Equals(origin["ReserveA"]) ? origin["ReserveB"] : origin["ReserveA"];
            var checkFee = CheckFee(totalFee, reserveIn, reserveOut, origin["totalSupply"]);
            checkFee.ShouldBeGreaterThanOrEqualTo(fee);

            var afterKLast = CheckKLast(pairAddress);
            afterKLast["KLast"]
                .ShouldBe(new BigIntValue(after["ReserveA"]).Mul(new BigIntValue(after["ReserveB"])).Value);
            
            after["UserBalanceA"].ShouldBe(origin["UserBalanceA"] - amountA);
            after["UserBalanceB"].ShouldBe(origin["UserBalanceB"] - amountB);
            after["ReserveA"].ShouldBe(origin["ReserveA"] + amountA);
            after["ReserveB"].ShouldBe(origin["ReserveB"] + amountB);
            after["ContractBalanceA"].ShouldBe(origin["ContractBalanceA"] + amountA);
            after["ContractBalanceB"].ShouldBe(origin["ContractBalanceB"] + amountB);

            _awakenSwapContract.SetAccount(account);
            var accountAssets = _awakenSwapContract.GetAccountAssets();
            accountAssets.Value.ShouldContain(pair);
        }
    

        [TestMethod]
        public void AddLiquidity_ERROR()
        {
            {
                var symbolA = "USDT";
                var symbolB = "ELF";
                var amountA = 10_000000;
                var amountB = 300_00000000;
                var pair = $"{symbolA}-{symbolB}";
                var result =
                    _awakenSwapContract.AddLiquidity(out var output, symbolA, symbolB, amountA, amountB,
                        InitAccount.ConvertAddress(), -1, -1);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Invalid Input");
            }
            {
                var symbolA = "ABC";
                var symbolB = "ETH";
                var amountA = 10_000000;
                var amountB = 300_00000000;
                var result = _awakenSwapContract.AddLiquidity(out var output, symbolA, symbolB, amountA, amountB,
                    InitAccount.ConvertAddress());
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Pair ABC-ETH does not exist.");
            }
            {
                var symbolA = "USDT";
                var symbolB = "ELF";
                var amountA = 10_000000;
                var amountB = 300_00000000;
                var timestamp = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-1));
                var result = _awakenSwapContract.AddLiquidity(out var output, symbolA, symbolB, amountA, amountB,
                    InitAccount.ConvertAddress(), 1, 1
                    , "", timestamp);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Expired");
            }
            {
                var symbolA = "ETH";
                var symbolB = "ELF";
                var amountA = 10_000000000;
                var amountBOptimal = _awakenSwapContract.Quote(symbolA, symbolB, amountA);
                var amountBMin = amountBOptimal + 100000000000;
                var result = _awakenSwapContract.AddLiquidity(out var output, symbolA, symbolB, amountA,
                    amountBOptimal, InitAccount.ConvertAddress(), 1, amountBMin);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Insufficient amount of tokenB");
            }
            {
                var symbolA = "ETH";
                var symbolB = "ELF";
                var amountA = 10000;
                var amountBOptimal = _awakenSwapContract.Quote(symbolA, symbolB, amountA);
                var amountAMin = amountA.Add(1000000);
                var result = _awakenSwapContract.AddLiquidity(out var output, symbolA, symbolB, amountAMin,
                    amountBOptimal, InitAccount.ConvertAddress(), amountAMin);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Insufficient amount of tokenA");
            }
        }

        [TestMethod]
        [DataRow("ELF","USDT", true)]
        public void RemoveLiquidity(string symbolA, string symbolB, bool isAll)
        {
            var account = InitAccount;
            var pair = GetTokenPair(symbolA, symbolB);
            var sortPair = SortSymbols(symbolA, symbolB);
            symbolA = sortPair.First();
            symbolB = sortPair.Last();
            var pairSymbol = GetTokenPairSymbol(symbolA, symbolB);
            Logger.Info(pairSymbol);
            var origin = CheckPairData(symbolA, symbolB, account);
            var originFeeToLpTokenBalance = _awakenTokenContract.GetBalance(pairSymbol, FeeToAccount.ConvertAddress());
            Logger.Info($"origin FeeTo LPTokenBalance: {originFeeToLpTokenBalance.Amount}");

            var tokenInfo = _awakenTokenContract.GetTokenInfo(pairSymbol);
            var pairAddress = _awakenSwapContract.GetPairAddress(symbolA, symbolB);
            BigIntValue KLast = 0;
            KLast = _awakenSwapContract.GetKLast(pairAddress);
            Logger.Info(KLast);

            var removeLpTokenAmount = isAll? origin["UserLPBalance"] : origin["UserLPBalance"] / 2;
            var approveResult = _awakenTokenContract.ApproveLPToken(_awakenSwapContract.ContractAddress, account,
                removeLpTokenAmount,
                pairSymbol);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var approveFee = approveResult.GetResourceTokenFee();

            _awakenSwapContract.SetAccount(account);
            var result = _awakenSwapContract.RemoveLiquidity(out var output, symbolA, symbolB, removeLpTokenAmount,
                account.ConvertAddress());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logs = ByteString.FromBase64(result.Logs.First(l => l.Name.Contains("LiquidityRemoved")).NonIndexed);
            var liquidityRemoved = LiquidityRemoved.Parser.ParseFrom(logs);
            liquidityRemoved.SymbolA.ShouldBe(symbolA);
            liquidityRemoved.SymbolB.ShouldBe(symbolB);
            liquidityRemoved.LiquidityToken.ShouldBe(removeLpTokenAmount);
            output.AmountA.ShouldBe(liquidityRemoved.AmountA);
            output.AmountB.ShouldBe(liquidityRemoved.AmountB);
            Logger.Info(output);
            Logger.Info(liquidityRemoved.Pair);
            Logger.Info(liquidityRemoved.LiquidityToken);

            var syncLogs = ByteString.FromBase64(result.Logs.First(l => l.Name.Contains("Sync")).NonIndexed);
            var sync = Sync.Parser.ParseFrom(syncLogs);
            CheckSyncEvent(sync, symbolA, symbolB, pairAddress,
                origin["ReserveA"], origin["ReserveB"],
                liquidityRemoved.AmountA, liquidityRemoved.AmountB,
                "remove");

            var after = CheckPairData(symbolA, symbolB, account);
            after["UserLPBalance"].ShouldBe(origin["UserLPBalance"] - liquidityRemoved.LiquidityToken);
            if (isAll) after["UserLPBalance"].ShouldBe(0);
            after["ReserveA"].ShouldBe(origin["ReserveA"] - liquidityRemoved.AmountA);
            after["ReserveB"].ShouldBe(origin["ReserveB"] - liquidityRemoved.AmountB);

            var feeToLpTokenBalance = _awakenTokenContract.GetBalance(pairSymbol, FeeToAccount.ConvertAddress());
            Logger.Info($"after add liquidity {feeToLpTokenBalance.Amount}");
            var fee = originFeeToLpTokenBalance.Amount - feeToLpTokenBalance.Amount;
            Logger.Info(fee);

            after["totalSupply"].ShouldBe(origin["totalSupply"] - liquidityRemoved.LiquidityToken +
                feeToLpTokenBalance.Amount - originFeeToLpTokenBalance.Amount);
            var afterKLast = CheckKLast(pairAddress);
            afterKLast["KLast"]
                .ShouldBe(new BigIntValue(after["ReserveA"]).Mul(new BigIntValue(after["ReserveB"])).Value);
            long addATxFee = 0;
            long addBTxFee = 0;
            if (approveFee.Keys.Contains(symbolA))
                addATxFee = approveFee.First(t => t.Key.Equals(symbolA)).Value;
            else if (approveFee.Keys.Contains(symbolB))
                addBTxFee = approveFee.First(t => t.Key.Equals(symbolB)).Value;
            after["UserBalanceA"].ShouldBe(origin["UserBalanceA"] + liquidityRemoved.AmountA - addATxFee);
            after["UserBalanceB"].ShouldBe(origin["UserBalanceB"] + liquidityRemoved.AmountB - addBTxFee);
            after["ContractBalanceA"].ShouldBe(origin["ContractBalanceA"] - liquidityRemoved.AmountA);
            after["ContractBalanceB"].ShouldBe(origin["ContractBalanceB"] - liquidityRemoved.AmountB);

            var afterTokenInfo = _awakenTokenContract.GetTokenInfo(pairSymbol);
            afterTokenInfo.Supply.ShouldBe(tokenInfo.Supply - removeLpTokenAmount + fee);
            var accountAsset = _awakenSwapContract.GetAccountAssets();
            if (isAll)
                accountAsset.Value.ShouldNotContain(pair);
            else
                accountAsset.Value.ShouldContain(pair);
        }

        //TokenA - TokenB
        [TestMethod]
        public void SwapExactTokenForToken()
        {
            var symbolIn = "USDT";
            var symbolOut = "ELF";
            var account = InitAccount;
            var amountIn = 2000000;
            var pairAddress = _awakenSwapContract.GetPairAddress(symbolIn, symbolOut);
            Approve(account, amountIn, symbolIn);
            CheckBalance(symbolIn, amountIn, account);

            var origin = CheckPairData(symbolIn, symbolOut, account);
            var exceptAmountOut = _awakenSwapContract.GetAmountOut(symbolIn, symbolOut, amountIn);
            Logger.Info($"except amount out: {exceptAmountOut}");
            var rate = CheckFeeRate(amountIn, origin["ReserveA"], origin["ReserveB"], symbolIn, symbolOut);
            rate.Add(1).ShouldBeGreaterThanOrEqualTo(new BigIntValue(10000 - FeeRate));
            var check = CheckAmountOut(amountIn, origin["ReserveA"], origin["ReserveB"], symbolIn, symbolOut);
            Logger.Info(check);
            check.ShouldBeTrue();

            var path = new List<string> {symbolIn, symbolOut};
            _awakenSwapContract.SetAccount(account);
            var result =
                _awakenSwapContract.SwapExactTokensForTokens(out var output, account.ConvertAddress(), path, amountIn);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info(output);
            output.Amount[0].ShouldBe(amountIn);
            output.Amount[1].ShouldBe(exceptAmountOut);

            var logs = ByteString.FromBase64(result.Logs.First(l => l.Name.Contains("Swap")).NonIndexed);
            var swap = Swap.Parser.ParseFrom(logs);
            Logger.Info(swap);
            swap.Pair.ShouldBe(pairAddress);
            swap.SymbolIn.ShouldBe(symbolIn);
            swap.SymbolOut.ShouldBe(symbolOut);
            swap.AmountIn.ShouldBe(amountIn);
            swap.TotalFee.ShouldBe(swap.AmountIn.Mul(FeeRate).Div(10000));
            swap.AmountOut.ShouldBe(exceptAmountOut);
            var amountOut = swap.AmountOut;

            var syncLogs = ByteString.FromBase64(result.Logs.First(l => l.Name.Contains("Sync")).NonIndexed);
            var sync = Sync.Parser.ParseFrom(syncLogs);
            CheckSyncEvent(sync, symbolIn, symbolOut, pairAddress, origin["ReserveA"], origin["ReserveB"], amountIn,
                amountOut, "swap");

            var after = CheckPairData(symbolIn, symbolOut);
            after["ReserveA"].ShouldBe(origin["ReserveA"] + amountIn);
            after["ReserveB"].ShouldBe(origin["ReserveB"] - amountOut);
            after["UserBalanceA"].ShouldBe(origin["UserBalanceA"] - amountIn);
            after["UserBalanceB"].ShouldBe(origin["UserBalanceB"] + amountOut);
            after["ContractBalanceA"].ShouldBe(origin["ContractBalanceA"] + amountIn);
            after["ContractBalanceB"].ShouldBe(origin["ContractBalanceB"] - amountOut);
        }

        //TokenA - TokenB - TokenC
        [TestMethod]
        public void SwapExactTokensForTokens()
        {
            var symbolIn = "ABC";
            var midSymbol = "USDT";
            var symbolOut = "ELF";
            var account = InitAccount;
            var amountIn = 100000000;
            var path = new List<string> {symbolIn, midSymbol, symbolOut};
            var originList = new List<Dictionary<string, long>>();
            Approve(account, amountIn, symbolIn);

            var exceptAmountOut = _awakenSwapContract.GetAmountsOut(path, amountIn);
            Logger.Info($"except amount out: {exceptAmountOut.Amount}");
            exceptAmountOut.Amount.First().ShouldBe(amountIn);

            for (var i = 0; i < exceptAmountOut.Amount.Count - 1; i++)
            {
                var inSymbol = path[i];
                var outSymbol = path[i + 1];
                var origin = CheckPairData(inSymbol, outSymbol, account);
                var rate = CheckFeeRate(exceptAmountOut.Amount[i], origin["ReserveA"], origin["ReserveB"], inSymbol,
                    outSymbol);
                // 精度损失。保留整数后可能会得到9969
                rate.Add(1).ShouldBeGreaterThanOrEqualTo(new BigIntValue(10000 - FeeRate));
                var check = CheckAmountOut(exceptAmountOut.Amount[i], origin["ReserveA"], origin["ReserveB"], inSymbol,
                    outSymbol);
                Logger.Info(check);
                // check.ShouldBeTrue();
                originList.Add(origin);
            }

            _awakenSwapContract.SetAccount(account);
            var result =
                _awakenSwapContract.SwapExactTokensForTokens(out var output, account.ConvertAddress(), path, amountIn);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info(output);
            output.Amount[0].ShouldBe(exceptAmountOut.Amount[0]);
            output.Amount[1].ShouldBe(exceptAmountOut.Amount[1]);
            output.Amount[2].ShouldBe(exceptAmountOut.Amount[2]);

            var swapLogs = result.Logs.Where(l => l.Name.Contains("Swap")).ToList();
            swapLogs.Count.ShouldBe(path.Count - 1);
            for (var i = 0; i < swapLogs.Count; i++)
            {
                var byteString = ByteString.FromBase64(swapLogs[i].NonIndexed);
                var swap = Swap.Parser.ParseFrom(byteString);
                var inSymbol = path[i];
                var outSymbol = path[i + 1];
                var pairAddress = _awakenSwapContract.GetPairAddress(inSymbol, outSymbol);
                swap.Pair.ShouldBe(pairAddress);
                swap.SymbolIn.ShouldBe(inSymbol);
                swap.SymbolOut.ShouldBe(outSymbol);
                swap.AmountIn.ShouldBe(exceptAmountOut.Amount[i]);
                swap.TotalFee.ShouldBe(swap.AmountIn.Mul(FeeRate).Div(10000));
                swap.AmountOut.ShouldBe(exceptAmountOut.Amount[i + 1]);
            }

            var syncLogs = result.Logs.Where(l => l.Name.Contains("Sync")).ToList();
            syncLogs.Count.ShouldBe(path.Count - 1);
            for (var i = 0; i < syncLogs.Count; i++)
            {
                var byteString = ByteString.FromBase64(syncLogs[i].NonIndexed);
                var sync = Sync.Parser.ParseFrom(byteString);
                var inSymbol = path[i];
                var outSymbol = path[i + 1];
                var pairAddress = _awakenSwapContract.GetPairAddress(inSymbol, outSymbol);

                CheckSyncEvent(sync, inSymbol, outSymbol, pairAddress, originList[i]["ReserveA"],
                    originList[i]["ReserveB"], exceptAmountOut.Amount[i], exceptAmountOut.Amount[i + 1], "swap");
            }

            for (var i = 0; i < path.Count - 1; i++)
            {
                var inSymbol = path[i];
                var outSymbol = path[i + 1];
                var after = CheckPairData(inSymbol, outSymbol, account);

                after["ReserveA"].ShouldBe(originList[i]["ReserveA"] + exceptAmountOut.Amount[i]);
                after["ReserveB"].ShouldBe(originList[i]["ReserveB"] - exceptAmountOut.Amount[i + 1]);
                if (i == 0)
                {
                    after["UserBalanceA"].ShouldBe(originList[i]["UserBalanceA"] - exceptAmountOut.Amount[i]);
                    after["UserBalanceB"].ShouldBe(originList[i]["UserBalanceB"]);
                    after["ContractBalanceA"].ShouldBe(originList[i]["ContractBalanceA"] + exceptAmountOut.Amount[i]);
                    after["ContractBalanceB"].ShouldBe(originList[i]["ContractBalanceB"]);
                }
                else if (i == path.Count - 2)
                {
                    after["UserBalanceA"].ShouldBe(originList[i]["UserBalanceA"]);
                    after["UserBalanceB"].ShouldBe(originList[i]["UserBalanceB"] + exceptAmountOut.Amount[i + 1]);
                    after["ContractBalanceA"].ShouldBe(originList[i]["ContractBalanceA"]);
                    after["ContractBalanceB"].ShouldBe(originList[i]["ContractBalanceB"] - exceptAmountOut.Amount[i + 1]);
                }
                else
                {
                    after["UserBalanceA"].ShouldBe(originList[i]["UserBalanceA"]);
                    after["UserBalanceB"].ShouldBe(originList[i]["UserBalanceB"]);
                    after["ContractBalanceA"].ShouldBe(originList[i]["ContractBalanceA"]);
                    after["ContractBalanceB"].ShouldBe(originList[i]["ContractBalanceB"]);
                }
            }
        }

        [TestMethod]
        public void SwapTokenForExactToken()
        {
            var symbolIn = "USDT";
            var symbolOut = "ELF";
            var amountOut = 10_00000000;
            var account = InitAccount;
            var pairAddress = _awakenSwapContract.GetPairAddress(symbolIn, symbolOut);

            var exceptAmountIn = _awakenSwapContract.GetAmountIn(symbolIn, symbolOut, amountOut);
            Logger.Info(exceptAmountIn);
            Approve(account, exceptAmountIn, symbolIn);

            var origin = CheckPairData(symbolIn, symbolOut, account);
            var rate = CheckFeeRate(exceptAmountIn, origin["ReserveA"], origin["ReserveB"], symbolIn, symbolOut);
            Logger.Info(rate);
            // rate.Value.ShouldBe((10000 - FeeRate).ToString());
            var check = CheckAmountIn(amountOut, origin["ReserveA"], origin["ReserveB"], symbolIn, symbolOut);
            Logger.Info(check);
            // check.ShouldBeTrue();

            var path = new List<string> {symbolIn, symbolOut};
            _awakenSwapContract.SetAccount(account);
            var result =
                _awakenSwapContract.SwapTokensForExactTokens(out var output, account.ConvertAddress(), path, amountOut);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            output.Amount[0].ShouldBe(exceptAmountIn);
            output.Amount[1].ShouldBe(amountOut);

            var logs = ByteString.FromBase64(result.Logs.First(l => l.Name.Contains("Swap")).NonIndexed);
            var swap = Swap.Parser.ParseFrom(logs);
            Logger.Info(swap);
            swap.SymbolIn.ShouldBe(symbolIn);
            swap.SymbolOut.ShouldBe(symbolOut);
            swap.AmountOut.ShouldBe(amountOut);
            swap.TotalFee.ShouldBe(swap.AmountIn.Mul(FeeRate).Div(10000));
            swap.AmountIn.ShouldBe(exceptAmountIn);
            var amountIn = swap.AmountIn;
            var syncLogs = ByteString.FromBase64(result.Logs.First(l => l.Name.Contains("Sync")).NonIndexed);
            var sync = Sync.Parser.ParseFrom(syncLogs);
            CheckSyncEvent(sync, symbolIn, symbolOut, pairAddress, origin["ReserveA"], origin["ReserveB"], amountIn,
                amountOut, "swap");

            var after = CheckPairData(symbolIn, symbolOut, account);
            after["ReserveA"].ShouldBe(origin["ReserveA"] + amountIn);
            after["ReserveB"].ShouldBe(origin["ReserveB"] - amountOut);
            after["UserBalanceA"].ShouldBe(origin["UserBalanceA"] - amountIn);
            after["UserBalanceB"].ShouldBe(origin["UserBalanceB"] + amountOut);
        }

        // A - B - C
        [TestMethod]
        public void SwapTokensForExactTokens()
        {
            var symbolIn = "ABC";
            var midSymbol = "USDT";
            var symbolOut = "ELF";
            var amountOut = 1_00000000;
            var account = InitAccount;
            var path = new List<string> {symbolIn, midSymbol, symbolOut};

            var exceptAmountIn = _awakenSwapContract.GetAmountsIn(path, amountOut);
            Logger.Info($"except amount in: {exceptAmountIn.Amount}");
            exceptAmountIn.Amount.Last().ShouldBe(amountOut);
            Approve(account, exceptAmountIn.Amount.First(), symbolIn);
            var originList = new List<Dictionary<string, long>>();

            for (var i = 0; i < path.Count - 1; i++)
            {
                var inSymbol = path[i];
                var outSymbol = path[i + 1];
                var origin = CheckPairData(inSymbol, outSymbol, account);
                var rate = CheckFeeRate(exceptAmountIn.Amount[i], origin["ReserveA"], origin["ReserveB"], inSymbol,
                    outSymbol);
                rate.Add(1).ShouldBeGreaterThanOrEqualTo(new BigIntValue(10000 - FeeRate));
                var check = CheckAmountIn(amountOut, origin["ReserveA"], origin["ReserveB"], inSymbol, outSymbol);
                Logger.Info(check);
                // check.ShouldBeTrue();
                originList.Add(origin);
            }

            _awakenSwapContract.SetAccount(account);
            var result =
                _awakenSwapContract.SwapTokensForExactTokens(out var output, account.ConvertAddress(), path, amountOut);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            output.Amount[0].ShouldBe(exceptAmountIn.Amount[0]);
            output.Amount[1].ShouldBe(exceptAmountIn.Amount[1]);
            output.Amount[2].ShouldBe(amountOut);

            var swapLogs = result.Logs.Where(l => l.Name.Contains("Swap")).ToList();
            swapLogs.Count.ShouldBe(path.Count - 1);
            for (var i = 0; i < swapLogs.Count; i++)
            {
                var byteString = ByteString.FromBase64(swapLogs[i].NonIndexed);
                var swap = Swap.Parser.ParseFrom(byteString);
                var inSymbol = path[i];
                var outSymbol = path[i + 1];
                var pairAddress = _awakenSwapContract.GetPairAddress(inSymbol, outSymbol);
                swap.Pair.ShouldBe(pairAddress);
                swap.SymbolIn.ShouldBe(inSymbol);
                swap.SymbolOut.ShouldBe(outSymbol);
                swap.AmountIn.ShouldBe(exceptAmountIn.Amount[i]);
                swap.TotalFee.ShouldBe(swap.AmountIn.Mul(FeeRate).Div(10000));
                swap.AmountOut.ShouldBe(exceptAmountIn.Amount[i + 1]);
            }

            var syncLogs = result.Logs.Where(l => l.Name.Contains("Sync")).ToList();
            syncLogs.Count.ShouldBe(path.Count - 1);
            for (var i = 0; i < syncLogs.Count; i++)
            {
                var byteString = ByteString.FromBase64(syncLogs[i].NonIndexed);
                var sync = Sync.Parser.ParseFrom(byteString);
                var inSymbol = path[i];
                var outSymbol = path[i + 1];
                var pairAddress = _awakenSwapContract.GetPairAddress(inSymbol, outSymbol);

                CheckSyncEvent(sync, inSymbol, outSymbol, pairAddress, originList[i]["ReserveA"],
                    originList[i]["ReserveB"], exceptAmountIn.Amount[i], exceptAmountIn.Amount[i + 1], "swap");
            }

            for (var i = 0; i < path.Count - 1; i++)
            {
                var inSymbol = path[i];
                var outSymbol = path[i + 1];
                var after = CheckPairData(inSymbol, outSymbol, account);

                after["ReserveA"].ShouldBe(originList[i]["ReserveA"] + exceptAmountIn.Amount[i]);
                after["ReserveB"].ShouldBe(originList[i]["ReserveB"] - exceptAmountIn.Amount[i + 1]);
                if (i == 0)
                {
                    after["UserBalanceA"].ShouldBe(originList[i]["UserBalanceA"] - exceptAmountIn.Amount[i]);
                    after["UserBalanceB"].ShouldBe(originList[i]["UserBalanceB"]);
                }
                else if (i == path.Count - 2)
                {
                    after["UserBalanceA"].ShouldBe(originList[i]["UserBalanceA"]);
                    after["UserBalanceB"].ShouldBe(originList[i]["UserBalanceB"] + exceptAmountIn.Amount[i + 1]);
                }
                else
                {
                    after["UserBalanceA"].ShouldBe(originList[i]["UserBalanceA"]);
                    after["UserBalanceB"].ShouldBe(originList[i]["UserBalanceB"]);
                }
            }
        }
        
        //TokenA - TokenB
        [TestMethod]
        public void SwapExactTokenForTokensSupportingFeeOnTransferTokens()
        {
            var symbolIn = "TEST";
            var symbolOut = "ABC";
            var account = InitAccount;
            var amountIn = 2000000;
            var pairAddress = _awakenSwapContract.GetPairAddress(symbolIn, symbolOut);
            Approve(account, amountIn, symbolIn);
            CheckBalance(symbolIn, amountIn, account);

            var origin = CheckPairData(symbolIn, symbolOut, account);
            var exceptAmountOut = _awakenSwapContract.GetAmountOut(symbolIn, symbolOut, amountIn);
            Logger.Info($"except amount out: {exceptAmountOut}");
            var rate = CheckFeeRate(amountIn, origin["ReserveA"], origin["ReserveB"], symbolIn, symbolOut);
            rate.Add(1).ShouldBeGreaterThanOrEqualTo(new BigIntValue(10000 - FeeRate));
            var check = CheckAmountOut(amountIn, origin["ReserveA"], origin["ReserveB"], symbolIn, symbolOut);
            Logger.Info(check);
            check.ShouldBeTrue();

            var path = new List<string> {symbolIn, symbolOut};
            _awakenSwapContract.SetAccount(account);
            var result =
                _awakenSwapContract.SwapSupportingFeeOnTransferTokens(account.ConvertAddress(), path, amountIn);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var logs = ByteString.FromBase64(result.Logs.First(l => l.Name.Contains("Swap")).NonIndexed);
            var swap = Swap.Parser.ParseFrom(logs);
            Logger.Info(swap);
            swap.Pair.ShouldBe(pairAddress);
            swap.SymbolIn.ShouldBe(symbolIn);
            swap.SymbolOut.ShouldBe(symbolOut);
            swap.AmountIn.ShouldBe(amountIn);
            swap.TotalFee.ShouldBe(swap.AmountIn.Mul(FeeRate).Div(10000));
            var amountOut = swap.AmountOut;

            var syncLogs = ByteString.FromBase64(result.Logs.First(l => l.Name.Contains("Sync")).NonIndexed);
            var sync = Sync.Parser.ParseFrom(syncLogs);
            CheckSyncEvent(sync, symbolIn, symbolOut, pairAddress, origin["ReserveA"], origin["ReserveB"], amountIn,
                amountOut, "swap");

            var after = CheckPairData(symbolIn, symbolOut);
            after["ReserveA"].ShouldBe(origin["ReserveA"] + amountIn);
            after["ReserveB"].ShouldBe(origin["ReserveB"] - amountOut);
            after["UserBalanceA"].ShouldBe(origin["UserBalanceA"] - amountIn);
            after["UserBalanceB"].ShouldBe(origin["UserBalanceB"] + amountOut);
        }

        //TokenA - TokenB - TokenC
        [TestMethod]
        public void SwapExactTokensForTokensSupportingFeeOnTransferTokens()
        {
            var symbolIn = "ABC";
            var midSymbol = "USDT";
            var symbolOut = "ELF";
            var account = InitAccount;
            var amountIn = 100000000;
            var path = new List<string> {symbolIn, midSymbol, symbolOut};
            var originList = new List<Dictionary<string, long>>();
            Approve(account, amountIn, symbolIn);

            var exceptAmountOut = _awakenSwapContract.GetAmountsOut(path, amountIn);
            Logger.Info($"except amount out: {exceptAmountOut.Amount}");
            exceptAmountOut.Amount.First().ShouldBe(amountIn);

            for (var i = 0; i < exceptAmountOut.Amount.Count - 1; i++)
            {
                var inSymbol = path[i];
                var outSymbol = path[i + 1];
                var origin = CheckPairData(inSymbol, outSymbol, account);
                var rate = CheckFeeRate(exceptAmountOut.Amount[i], origin["ReserveA"], origin["ReserveB"], inSymbol,
                    outSymbol);
                // 精度损失。保留整数后可能会得到9969
                rate.Add(1).ShouldBeGreaterThanOrEqualTo(new BigIntValue(10000 - FeeRate));
                var check = CheckAmountOut(exceptAmountOut.Amount[i], origin["ReserveA"], origin["ReserveB"], inSymbol,
                    outSymbol);
                Logger.Info(check);
                // check.ShouldBeTrue();
                originList.Add(origin);
            }

            _awakenSwapContract.SetAccount(account);
            var result =
                _awakenSwapContract.SwapSupportingFeeOnTransferTokens(account.ConvertAddress(), path, amountIn);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var swapLogs = result.Logs.Where(l => l.Name.Contains("Swap")).ToList();
            swapLogs.Count.ShouldBe(path.Count - 1);
            for (var i = 0; i < swapLogs.Count; i++)
            {
                var byteString = ByteString.FromBase64(swapLogs[i].NonIndexed);
                var swap = Swap.Parser.ParseFrom(byteString);
                var inSymbol = path[i];
                var outSymbol = path[i + 1];
                var pairAddress = _awakenSwapContract.GetPairAddress(inSymbol, outSymbol);
                swap.Pair.ShouldBe(pairAddress);
                swap.SymbolIn.ShouldBe(inSymbol);
                swap.SymbolOut.ShouldBe(outSymbol);
                swap.AmountIn.ShouldBe(exceptAmountOut.Amount[i]);
                swap.TotalFee.ShouldBe(swap.AmountIn.Mul(FeeRate).Div(10000));
                swap.AmountOut.ShouldBe(exceptAmountOut.Amount[i + 1]);
            }

            var syncLogs = result.Logs.Where(l => l.Name.Contains("Sync")).ToList();
            syncLogs.Count.ShouldBe(path.Count - 1);
            for (var i = 0; i < syncLogs.Count; i++)
            {
                var byteString = ByteString.FromBase64(syncLogs[i].NonIndexed);
                var sync = Sync.Parser.ParseFrom(byteString);
                var inSymbol = path[i];
                var outSymbol = path[i + 1];
                var pairAddress = _awakenSwapContract.GetPairAddress(inSymbol, outSymbol);

                CheckSyncEvent(sync, inSymbol, outSymbol, pairAddress, originList[i]["ReserveA"],
                    originList[i]["ReserveB"], exceptAmountOut.Amount[i], exceptAmountOut.Amount[i + 1], "swap");
            }

            for (var i = 0; i < path.Count - 1; i++)
            {
                var inSymbol = path[i];
                var outSymbol = path[i + 1];
                var after = CheckPairData(inSymbol, outSymbol, account);

                after["ReserveA"].ShouldBe(originList[i]["ReserveA"] + exceptAmountOut.Amount[i]);
                after["ReserveB"].ShouldBe(originList[i]["ReserveB"] - exceptAmountOut.Amount[i + 1]);
                if (i == 0)
                {
                    after["UserBalanceA"].ShouldBe(originList[i]["UserBalanceA"] - exceptAmountOut.Amount[i]);
                    after["UserBalanceB"].ShouldBe(originList[i]["UserBalanceB"]);
                }
                else if (i == path.Count - 2)
                {
                    after["UserBalanceA"].ShouldBe(originList[i]["UserBalanceA"]);
                    after["UserBalanceB"].ShouldBe(originList[i]["UserBalanceB"] + exceptAmountOut.Amount[i + 1]);
                }
                else
                {
                    after["UserBalanceA"].ShouldBe(originList[i]["UserBalanceA"]);
                    after["UserBalanceB"].ShouldBe(originList[i]["UserBalanceB"]);
                }
            }
        }

        [TestMethod]
        public void Swap_Error()
        {
            var symbolIn = "USDT";
            var symbolOut = "ELF";
            var origin = CheckPairData(symbolIn, symbolOut);
            {
                var path = new List<string> {"ABC", "DEF"};
                var result1 =
                    _awakenSwapContract.SwapExactTokensForTokens(out var output1, InitAccount.ConvertAddress(), path,
                        1);
                result1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result1.Error.ShouldContain("Pair ABC-DEF does not exist.");
                var result2 =
                    _awakenSwapContract.SwapTokensForExactTokens(out var output2, InitAccount.ConvertAddress(), path,
                        1);
                result2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result2.Error.ShouldContain("Pair ABC-DEF does not exist.");
            }
            {
                var amountIn = 10000000;
                var path = new List<string> {symbolIn, symbolOut};
                var result1 = _awakenSwapContract.SwapExactTokensForTokens(out var output1,
                    InitAccount.ConvertAddress(), path, amountIn, 100000000000000000);
                result1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result1.Error.ShouldContain("Insufficient Output amount");

                var amountOut = 100000000;
                var getAmountIn = _awakenSwapContract.GetAmountIn(symbolIn, symbolOut, amountOut);
                var result2 = _awakenSwapContract.SwapTokensForExactTokens(out var output2,
                    InitAccount.ConvertAddress(), path, amountOut, getAmountIn / 2);
                result2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result2.Error.ShouldContain("Excessive Input amount");
            }
            {
                var amountOut = origin["ReserveA"];
                var path = new List<string> {symbolOut, symbolIn};
                // var getAmountIn = _awakenSwapContract.GetAmountIn(symbolIn, symbolOut, amountOut - 1);
                Approve(InitAccount, origin["ReserveB"].Mul(10), symbolOut);

                var result1 = _awakenSwapContract.SwapExactTokensForTokens(out var output1,
                    InitAccount.ConvertAddress(), path, origin["ReserveB"].Mul(10));
                result1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result1.Error.ShouldContain("Insufficient reserve");

                var result2 = _awakenSwapContract.SwapTokensForExactTokens(out var output2,
                    InitAccount.ConvertAddress(), path, amountOut);
                result2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result2.Error.ShouldContain("Insufficient reserve");
            }
        }

        [TestMethod]
        [DataRow("ELF","USDT", true)]
        public void TransferLiquidityTokens(string symbolA, string symbolB, bool isALl)
        {
            var pairSymbol = GetTokenPairSymbol(symbolA, symbolB);
            var pair = GetTokenPair(symbolA, symbolB);
            var account = InitAccount;
            var toAccount = NodeManager.NewAccount("12345678");

            _awakenSwapContract.SetAccount(toAccount);
            var beforeAccountAsset = _awakenSwapContract.GetAccountAssets();
            beforeAccountAsset.Value.ShouldNotContain(pair);

            var lpTokenAccountOriginBalance = _awakenTokenContract.GetBalance(pairSymbol, toAccount.ConvertAddress());
            var lpTokenBalance = _awakenTokenContract.GetBalance(pairSymbol, InitAccount.ConvertAddress());
            var amount = isALl ? lpTokenBalance.Amount : lpTokenBalance.Amount / 4;
            Logger.Info($"before:\n" +
                        $"{account} LP token balance: {lpTokenBalance.Amount},\n" +
                        $"{toAccount} LP token balance: {lpTokenAccountOriginBalance.Amount},\n" +
                        $"Transfer amount: {amount}");

            var approveResult =
                _awakenTokenContract.ApproveLPToken(_awakenSwapContract.ContractAddress, account, amount, pairSymbol);
            approveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            _awakenSwapContract.SetAccount(account);
            var result = _awakenSwapContract.TransferLiquidityTokens(pair, toAccount, amount);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var lpTokenAfterBalance = _awakenTokenContract.GetBalance(pairSymbol, InitAccount.ConvertAddress());
            lpTokenAfterBalance.Amount.ShouldBe(lpTokenBalance.Amount - amount);

            var lpTokenAccountBalance = _awakenTokenContract.GetBalance(pairSymbol, toAccount.ConvertAddress());
            lpTokenAccountBalance.Amount.ShouldBe(lpTokenAccountOriginBalance.Amount + amount);
            Logger.Info($"after:\n" +
                        $"{account} LP token balance: {lpTokenAfterBalance.Amount},\n" +
                        $"{toAccount} LP token balance: {lpTokenAccountBalance.Amount},\n" +
                        $"Transfer amount: {amount}");

            _awakenSwapContract.SetAccount(toAccount);
            var toAccountAsset = _awakenSwapContract.GetAccountAssets();
            toAccountAsset.Value.ShouldContain(pair);

            _awakenSwapContract.SetAccount(account);
            var accountAsset = _awakenSwapContract.GetAccountAssets();
            if (isALl)
                accountAsset.Value.ShouldNotContain(pair);
            else
                accountAsset.Value.ShouldContain(pair);

        }

        [TestMethod]
        public void TransferLiquidityTokens_Error()
        {
            var symbolA = "USDT";
            var symbolB = "ELF";
            var pairSymbol = GetTokenPairSymbol(symbolA, symbolB);
            var pair = GetTokenPair(symbolA, symbolB);
            var account = InitAccount;
            var toAccount = NodeManager.NewAccount("12345678");

            var lpTokenAccountOriginBalance = _awakenTokenContract.GetBalance(pairSymbol, toAccount.ConvertAddress());
            var accountBalance = lpTokenAccountOriginBalance.Amount;
            var lpTokenBalance = _awakenTokenContract.GetBalance(pairSymbol, account.ConvertAddress());
            var balanceResults = lpTokenBalance.Amount;
            {
                var amount = balanceResults.Mul(4);
                _awakenSwapContract.SetAccount(InitAccount);
                var result = _awakenSwapContract.TransferLiquidityTokens(pair, toAccount, amount);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain($"Insufficient balance of {pairSymbol}.");

                var lpTokenAfterBalance = _awakenTokenContract.GetBalance(pairSymbol, account.ConvertAddress());
                lpTokenAfterBalance.Amount.ShouldBe(balanceResults);
                var lpTokenAccountBalance = _awakenTokenContract.GetBalance(pairSymbol, toAccount.ConvertAddress());
                lpTokenAccountBalance.Amount.ShouldBe(accountBalance);
            }
            {
                var notExistsPair = "ABC-ABC";
                _awakenSwapContract.SetAccount(InitAccount);
                var result = _awakenSwapContract.TransferLiquidityTokens(notExistsPair, toAccount, 1);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain($"Pair {notExistsPair} does not exist.");
            }
            {
                _awakenSwapContract.SetAccount(InitAccount);
                var result = _awakenSwapContract.TransferLiquidityTokens(pair, toAccount, 0);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain(" Invalid amount.");
            }
        }

        [TestMethod]
        public void Take()
        {
            var vault = "NUddzDNy8PBMUgPCAcFW7jkaGmofDTEmr5DUoddXDpdR6E85X";
            var takeSymbol = "TEST";
            var setVault = _awakenSwapContract.SetVault(vault.ConvertAddress());
            setVault.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            _awakenSwapContract.SetAccount(vault);
            var balance = _tokenContract.GetUserBalance(_awakenSwapContract.ContractAddress, takeSymbol);
            var takeAmount = balance.Div(4);
            var beforeVaultBalance = _tokenContract.GetUserBalance(vault, takeSymbol);
            Logger.Info($"before contract balance: {balance}\n" +
                        $"before vault balance: {beforeVaultBalance}\n" +
                        $"take amount: {takeAmount}");
            
            var takeResult = _awakenSwapContract.Take(takeSymbol, takeAmount);
            takeResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var afterBalance = _tokenContract.GetUserBalance(_awakenSwapContract.ContractAddress, takeSymbol);
            var afterVaultBalance =  _tokenContract.GetUserBalance(vault, takeSymbol);
            
            Logger.Info($"after contract balance: {afterBalance}\n" +
                        $"after vault balance: {afterVaultBalance}");
        }

        [TestMethod]
        public void CheckInfo()
        {
            var admin = _awakenSwapContract.GetAdmin();
            admin.ShouldBe(InitAccount.ConvertAddress());

            var getFeeTo = _awakenSwapContract.GetFeeTo();
            getFeeTo.ShouldBe(FeeToAccount.ConvertAddress());

            var getFeeRate = _awakenSwapContract.GetFeeRate();
            getFeeRate.ShouldBe(FeeRate);

            Logger.Info($"admin: {admin}\n" +
                        $"feeTo: {getFeeTo}\n" +
                        $"feeRate: {getFeeRate}");
        }

        [TestMethod]
        public void CheckReserve()
        {
            var symbolA = "ELF";
            var symbolB = "USDT";

            CheckPairData(symbolA, symbolB);
        }

        [TestMethod]
        public void GetPairList()
        {
            var pairList = _awakenSwapContract.GetPairs();
            Logger.Info(pairList.Value);
        }

        [TestMethod]
        public void GetPairAddress()
        {
            var symbolA = "ELF";
            var symbolB = "USDT";
            var pairAddress = _awakenSwapContract.GetPairAddress(symbolA, symbolB);
            Logger.Info(pairAddress);
        }

        [TestMethod]
        public void GetTokenInfo()
        {
            var token = "ETH";
            var info = _tokenContract.GetTokenInfo(token);
            Logger.Info(info);
        }

        #region Private method

        private void CreateToken(string symbol, int d)
        {
            long totalSupply = 100000000;
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

        private bool CheckToken(string symbol)
        {
            var tokenInfo = _tokenContract.GetTokenInfo(symbol);
            return tokenInfo.Equals(new AElf.Contracts.MultiToken.TokenInfo());
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

        private Dictionary<string, long> CheckPairData(string symbolA, string symbolB, string sender = "")
        {
            var account = sender == "" ? InitAccount : sender;
            var resultsList = new Dictionary<string, long>();
            _awakenSwapContract.SetAccount(account);
            var pair = $"{symbolA}-{symbolB}";
            var pairSymbol = GetTokenPairSymbol(symbolA, symbolB);
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
            var contractBalanceA = _tokenContract.GetUserBalance(_awakenSwapContract.ContractAddress, symbolList.First());
            var contractBalanceB = _tokenContract.GetUserBalance(_awakenSwapContract.ContractAddress, symbolList.Last());

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

        private Dictionary<string, string> CheckKLast(Address pairAddress)
        {
            var KLastInfo = new Dictionary<string, string>();
            var KLast = _awakenSwapContract.GetKLast(pairAddress);
            Logger.Info($"KLast: {KLast.Value}");
            KLastInfo["KLast"] = KLast.Value;
            return KLastInfo;
        }

        private bool CheckAmountIn(long amountOut, long reserveIn, long reserveOut, string symbolIn, string symbolOut)
        {
            var k = new BigIntValue(reserveIn).Mul(reserveOut);
            Logger.Info(k.Value);
            var exceptAmountIn = _awakenSwapContract.GetAmountIn(symbolIn, symbolOut, amountOut);
            //var exceptAmountOut = _awakenSwapContract.GetAmountOut(symbolIn, symbolOut, exceptAmountIn);

            var feeRate = _awakenSwapContract.GetFeeRate();
            var p = new BigIntValue(10000 - feeRate);
            var x = new BigIntValue(reserveIn).Add(p.Mul(new BigIntValue(exceptAmountIn)).Div(10000));
            var y = new BigIntValue(reserveOut).Sub(amountOut);
            //精度损失过大
            var k1 = x.Add(1).Mul(y);
            var k11 = x.Mul(y.Sub(1));
            Logger.Info($"{k1.Value}");
            Logger.Info($"{k11.Value}");
            var check = k >= k11 && k <= k1;
            return check;
        }

        private bool CheckAmountOut(long amountIn, long reserveIn, long reserveOut, string symbolIn, string symbolOut)
        {
            var k = new BigIntValue(reserveIn).Mul(reserveOut);
            Logger.Info(k.Value);
            var exceptAmountOut = _awakenSwapContract.GetAmountOut(symbolIn, symbolOut, amountIn);
            var feeRate = _awakenSwapContract.GetFeeRate();
            var p = new BigIntValue(10000 - feeRate);
            var x = new BigIntValue(reserveIn).Add(p.Mul(new BigIntValue(amountIn)).Div(10000));
            var y = new BigIntValue(reserveOut).Sub(exceptAmountOut);
            //精度损失
            var k1 = x.Mul(y);
            var k11 = x.Mul(y.Sub(1));
            Logger.Info($"{k1.Value}");
            Logger.Info($"{k11.Value}");
            var check = k >= k11 && k <= k1;
            return check;
        }

        private BigIntValue CheckFeeRate(long amountIn, long reserveIn, long reserveOut, string symbolIn,
            string symbolOut)
        {
            var exceptAmountOut = _awakenSwapContract.GetAmountOut(symbolIn, symbolOut, amountIn);
            Logger.Info($"{amountIn},{reserveIn},{reserveOut},{symbolIn},{symbolOut}");
            var noFeeAmountOut =
                new BigIntValue(amountIn).Mul(reserveOut).Div(new BigIntValue(reserveIn).Add(amountIn));
            Logger.Info($"No Fee Amount Out: {noFeeAmountOut}");
            var rate = new BigIntValue(exceptAmountOut).Mul(10000).Div(noFeeAmountOut);
            Logger.Info(rate.Value);
            return rate;
        }

        private long CheckFee(long totalFee, long reserveIn, long reserveOut, long supply)
        {
            //收取的费用为和amountIn totalFee等值的 lpToken 所以要除以2
            //除以5是因为feeTo地址只占五分之一
            var amountB = new BigIntValue(totalFee).Mul(reserveOut).Div(reserveIn);
            var fee = Math.Min(Convert.ToInt64(new BigIntValue(totalFee).Mul(supply).Div(reserveIn).Value),
                Convert.ToInt64(new BigIntValue(amountB).Mul(supply).Div(reserveOut).Value)).Div(2).Div(5);
            Logger.Info($"check fee: {fee}\n");
            return fee;
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

        private string GetTokenPair(string tokenA, string tokenB)
        {
            var symbols = SortSymbols(tokenA, tokenB);
            return $"{symbols[0]}-{symbols[1]}";
        }

        private string GetTokenPairSymbol(string tokenA, string tokenB)
        {
            var symbols = SortSymbols(tokenA, tokenB);
            return $"ALP {symbols[0]}-{symbols[1]}";
        }

        private string[] SortSymbols(params string[] symbols)
        {
            return symbols.OrderBy(s => s).ToArray();
        }

        #endregion
    }
}