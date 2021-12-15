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
using Gandalf.Contracts.Swap;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class GandalfSwapContractTest
    {
        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;
        private GandalfSwapContract _gandalfSwapContract;
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }
        private string swapAddress = "2WHXRoLRjbUTDQsuqR5CntygVfnDb125qdJkudev4kVNbLhTdG";
        private string InitAccount { get; } = "J6zgLjGwd1bxTBpULLXrGVeV74tnS2n74FFJJz7KNdjTYkDF6";
        private string FeeToAccount { get; } = "Zz4iuCCCktGjZGmQ9vMcxh2JT9pDTFA4XSR7WDNrndYcEXtRx";
        private static string RpcUrl { get; } = "192.168.66.9:8000";
        private long FeeRate { get; } = 30;


        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("GandalfSwapContractTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env2-main");

            NodeManager = new NodeManager(RpcUrl);
            AuthorityManager = new AuthorityManager(NodeManager, InitAccount);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
            _gandalfSwapContract = swapAddress == ""
                ? new GandalfSwapContract(NodeManager, InitAccount)
                : new GandalfSwapContract(NodeManager, InitAccount, swapAddress);
        }

        [TestMethod]
        public void InitializeTest()
        {
            var result = _gandalfSwapContract.ExecuteMethodWithResult(SwapMethod.Initialize, new Empty());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var admin = _gandalfSwapContract.GetAdmin();
            admin.ShouldBe(InitAccount.ConvertAddress());

            var setFeeTo =
                _gandalfSwapContract.ExecuteMethodWithResult(SwapMethod.SetFeeTo, FeeToAccount.ConvertAddress());
            setFeeTo.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var getFeeTo = _gandalfSwapContract.GetFeeTo();
            getFeeTo.ShouldBe(FeeToAccount.ConvertAddress());

            var setFeeRate =
                _gandalfSwapContract.ExecuteMethodWithResult(SwapMethod.SetFeeRate, new Int64Value {Value = FeeRate});
            setFeeRate.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var getFeeRate = _gandalfSwapContract.GetFeeRate();
            getFeeRate.ShouldBe(FeeRate);
        }

        [TestMethod]
        public void InitializeTest_ERROR()
        {
            var result = _gandalfSwapContract.ExecuteMethodWithResult(SwapMethod.Initialize, new Empty());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("Already initialized.");

            _gandalfSwapContract.SetAccount(FeeToAccount);
            var setFeeTo =
                _gandalfSwapContract.ExecuteMethodWithResult(SwapMethod.SetFeeTo, FeeToAccount.ConvertAddress());
            setFeeTo.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            setFeeTo.Error.ShouldContain("No permission");

            var setFeeRate =
                _gandalfSwapContract.ExecuteMethodWithResult(SwapMethod.SetFeeRate, new Int64Value {Value = FeeRate});
            setFeeRate.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            setFeeRate.Error.ShouldContain("No permission");
        }

        //ETH-ELF: SN13go1NtgJnTuJKeMvgAWCYPjXLwmCG8jhmyzAcbmhQ5JSqm
        //USDT-ELF：2EGFWrTSsam3aSwZMMKV4GvS4QDXE8UwGuceffU7qff4CoB3rq
        [TestMethod]
        public void CreatePair()
        {
            var symbolA = "USDT";
            var symbolB = "ELF";
            if (CheckToken(symbolA))
                CreateToken(symbolA, 6);
            if (CheckToken(symbolB))
                CreateToken(symbolB, 8);
            var pair = $"{symbolA}-{symbolB}";
            var create = _gandalfSwapContract.CreatePair(pair, out var pairAddress);
            create.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logs = ByteString.FromBase64(create.Logs.First(l => l.Name.Contains("PairCreated")).NonIndexed);
            var pairCreated = PairCreated.Parser.ParseFrom(logs);
            pairCreated.SymbolA.ShouldBe(symbolA);
            pairCreated.SymbolB.ShouldBe(symbolB);
            pairCreated.Pair.ShouldBe(pairAddress);
            Logger.Info(pairAddress);

            var pairList = _gandalfSwapContract.GetPairs();
            pairList.SymbolPair.ShouldContain(pair);
            var reserves = _gandalfSwapContract.GetReserves(pair);
            var reserveResult = reserves.Results.First(r => r.SymbolPair.Equals(pair));
            reserveResult.ReserveA.ShouldBe(0);
            reserveResult.ReserveB.ShouldBe(0);
            var totalSupply = _gandalfSwapContract.GetTotalSupply(pair);
            var totalSupplyResult = totalSupply.Results.First(t => t.SymbolPair.Equals(pair));
            totalSupplyResult.TotalSupply.ShouldBe(0);
            var getPairAddress = _gandalfSwapContract.GetPairAddress(symbolA, symbolB);
            getPairAddress.ShouldBe(pairAddress);
        }

        [TestMethod]
        public void CreatePair_ERROR()
        {
            {
                var symbolA = "ELF";
                var symbolB = "USDT";
                var pair = $"{symbolA}-{symbolB}";
                var create = _gandalfSwapContract.CreatePair(pair, out var pairAddress);
                create.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                create.Error.ShouldContain("Pair Existed");
            }
            {
                var symbolA = "ELF";
                var symbolB = "ABC";
                var pair = $"{symbolA}-{symbolB}";
                var create = _gandalfSwapContract.CreatePair(pair, out var pairAddress);
                create.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                create.Error.ShouldContain("Invalid Tokens");
            }
            {
                var symbolA = "ABC";
                var symbolB = "ELF";
                var pair = $"{symbolA}-{symbolB}";
                var create = _gandalfSwapContract.CreatePair(pair, out var pairAddress);
                create.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                create.Error.ShouldContain("Invalid Tokens");
            }
            {
                var symbolA = "ELF";
                var symbolB = "ELF";
                var pair = $"{symbolA}-{symbolB}";
                var create = _gandalfSwapContract.CreatePair(pair, out var pairAddress);
                create.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                create.Error.ShouldContain("Identical Tokens");
            }
            {
                var symbolA = "ELF";
                var symbolB = "ETH";
                var pair = $"{symbolA}{symbolB}";
                var create = _gandalfSwapContract.CreatePair(pair, out var pairAddress);
                create.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                create.Error.ShouldContain("Invalid TokenPair");
            }
        }

        [TestMethod]
        public void AddLiquidity()
        {
            var symbolA = "ETH";
            var symbolB = "ELF";
            long amountA = 100_00000000;
            long amountB = 1000_00000000;
            var pair = $"{symbolA}-{symbolB}";
            var pairList = new List<string> {pair};
            var origin = CheckPairData(pair);
            var originFeeToLpTokenBalance = _gandalfSwapContract.GetLiquidityTokenBalance(pairList, FeeToAccount);
            var originFeeToBalanceResults = originFeeToLpTokenBalance.Results.First(r => r.SymbolPair.Equals(pair));
            Logger.Info($"origin FeeTo LPTokenBalance: {originFeeToBalanceResults.Balance}");
            var pairAddress = _gandalfSwapContract.GetPairAddress(symbolA, symbolB);

            BigIntValue KLast = 0;
            if (origin["totalSupply"] != 0)
            {
                amountB = _gandalfSwapContract.Quote(symbolA, symbolB, amountA);
                Logger.Info($"new amountB is {amountB}");
                KLast = _gandalfSwapContract.GetKLast(pairAddress);
                Logger.Info(KLast);
            }

            CheckBalance(symbolA, amountA, InitAccount);
            CheckBalance(symbolB, amountB, InitAccount);
            Approve(InitAccount, amountA, symbolA);
            Approve(InitAccount, amountB, symbolB);

            origin = CheckPairData(pair);

            _gandalfSwapContract.SetAccount(InitAccount);
            var result = _gandalfSwapContract.AddLiquidity(out var output, symbolA, symbolB, amountA, amountB);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var txFee = result.GetResourceTokenFee();
            var logs = ByteString.FromBase64(result.Logs.First(l => l.Name.Contains("LiquidityAdded")).NonIndexed);
            var liquidityAdded = LiquidityAdded.Parser.ParseFrom(logs);
            liquidityAdded.SymbolA.ShouldBe(symbolA);
            liquidityAdded.SymbolB.ShouldBe(symbolB);
            liquidityAdded.AmountA.ShouldBe(amountA);
            liquidityAdded.AmountB.ShouldBe(amountB);

            Logger.Info(liquidityAdded.Pair);
            Logger.Info(liquidityAdded.LiquidityToken);
            var after = CheckPairData(pair);
            after["lpTokenBalance"].ShouldBe(origin["lpTokenBalance"] + liquidityAdded.LiquidityToken);
            after["ReserveA"].ShouldBe(origin["ReserveA"] + liquidityAdded.AmountA);
            after["ReserveB"].ShouldBe(origin["ReserveB"] + liquidityAdded.AmountB);

            if (origin["totalSupply"] == 0)
            {
                after["totalSupply"].ShouldBe(origin["totalSupply"] + liquidityAdded.LiquidityToken + 1);
                var feeToLpTokenBalance = _gandalfSwapContract.GetLiquidityTokenBalance(pairList, FeeToAccount);
                var feeToBalanceResults = feeToLpTokenBalance.Results.First(r => r.SymbolPair.Equals(pair));
                feeToBalanceResults.Balance.ShouldBe(0);
            }
            else
            {
                var K = _gandalfSwapContract.GetKLast(pairAddress);
                var feeToLpTokenBalance = _gandalfSwapContract.GetLiquidityTokenBalance(pairList, FeeToAccount);
                var feeToBalanceResults = feeToLpTokenBalance.Results.First(r => r.SymbolPair.Equals(pair));
                Logger.Info($"after add liquidity {feeToBalanceResults.Balance}");
                var fee = feeToBalanceResults.Balance - originFeeToBalanceResults.Balance;
                Logger.Info(fee);
                after["totalSupply"].ShouldBe(origin["totalSupply"] + liquidityAdded.LiquidityToken +
                    feeToBalanceResults.Balance - originFeeToBalanceResults.Balance);
                Logger.Info(K);
                if (fee != 0)
                {
                    var totalFee = 30000;
                    var checkFee = CheckFee(totalFee, symbolA, origin["ReserveA"], origin["ReserveB"],
                        origin["totalSupply"]);
                    checkFee.Value.ShouldBeGreaterThan(fee.ToString());
                    checkFee.Value.ShouldBeLessThan(fee.Add(10).ToString());
                    Logger.Info($"check fee: {checkFee.Value}\n");
                }
            }

            long addATxFee = 0;
            long addBTxFee = 0;
            if (txFee.Keys.Contains(symbolA))
                addATxFee = txFee.First(t => t.Key.Equals(symbolA)).Value;
            else if (txFee.Keys.Contains(symbolB))
                addBTxFee = txFee.First(t => t.Key.Equals(symbolB)).Value;

            after["UserBalanceA"].ShouldBe(origin["UserBalanceA"] - amountA - addATxFee);
            after["UserBalanceB"].ShouldBe(origin["UserBalanceB"] - amountB - addBTxFee);
            after["PairBalanceA"].ShouldBe(origin["PairBalanceA"] + amountA);
            after["PairBalanceB"].ShouldBe(origin["PairBalanceB"] + amountB);
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
                    _gandalfSwapContract.AddLiquidity(out var output, symbolA, symbolB, amountA, amountB, -1, -1);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Invalid Input");
            }
            {
                var symbolA = "ABC";
                var symbolB = "ETH";
                var amountA = 10_000000;
                var amountB = 300_00000000;
                var result = _gandalfSwapContract.AddLiquidity(out var output, symbolA, symbolB, amountA, amountB);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Pair not exists");
            }
            {
                var symbolA = "USDT";
                var symbolB = "ELF";
                var amountA = 10_000000;
                var amountB = 300_00000000;
                var timestamp = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-1));
                var result = _gandalfSwapContract.AddLiquidity(out var output, symbolA, symbolB, amountA, amountB, 1, 1,
                    "", timestamp);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Expired");
            }
            {
                var symbolA = "ETH";
                var symbolB = "ELF";
                var amountA = 10_000000000;
                var amountBOptimal = _gandalfSwapContract.Quote(symbolA, symbolB, amountA);
                var amountBMin = amountBOptimal + 100000000000;
                var result = _gandalfSwapContract.AddLiquidity(out var output, symbolA, symbolB, amountA,
                    amountBOptimal,
                    1, amountBMin);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Insufficient amount of tokenB");
            }
            {
                var symbolA = "ETH";
                var symbolB = "ELF";
                var amountA = 10000;
                var amountBOptimal = _gandalfSwapContract.Quote(symbolA, symbolB, amountA);
                var amountAMin = amountA.Add(1000000);
                var result = _gandalfSwapContract.AddLiquidity(out var output, symbolA, symbolB, amountAMin,
                    amountBOptimal,
                    amountAMin);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Insufficient amount of tokenA");
            }
        }

        [TestMethod]
        public void RemoveLiquidity()
        {
            var symbolA = "ETH";
            var symbolB = "ELF";
            var pair = $"{symbolA}-{symbolB}";
            var pairList = new List<string> {pair};
            var account = "2ahBgPQf5wgzabwie3MjGDnPsJKEW8EFmHFrisUmKFybcHFKxs";

            var origin = CheckPairData(pair,account);
            var originFeeToLpTokenBalance = _gandalfSwapContract.GetLiquidityTokenBalance(pairList, FeeToAccount);
            var originFeeToBalanceResults = originFeeToLpTokenBalance.Results.First(r => r.SymbolPair.Equals(pair));
            Logger.Info($"origin FeeTo LPTokenBalance: {originFeeToBalanceResults.Balance}");
            var lpTokenAmount = origin["lpTokenBalance"] / 2;
            var pairAddress = _gandalfSwapContract.GetPairAddress(symbolA, symbolB);

            BigIntValue KLast = 0;
            KLast = _gandalfSwapContract.GetKLast(pairAddress);
            Logger.Info(KLast);

            _gandalfSwapContract.SetAccount(account);
            var result = _gandalfSwapContract.RemoveLiquidity(out var output, symbolA, symbolB, lpTokenAmount);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logs = ByteString.FromBase64(result.Logs.First(l => l.Name.Contains("LiquidityRemoved")).NonIndexed);
            var liquidityRemoved = LiquidityRemoved.Parser.ParseFrom(logs);
            liquidityRemoved.SymbolA.ShouldBe(symbolA);
            liquidityRemoved.SymbolB.ShouldBe(symbolB);
            liquidityRemoved.LiquidityToken.ShouldBe(lpTokenAmount);
            output.AmountA.ShouldBe(liquidityRemoved.AmountA);
            output.AmountB.ShouldBe(liquidityRemoved.AmountB);
            Logger.Info(output);

            Logger.Info(liquidityRemoved.Pair);
            Logger.Info(liquidityRemoved.LiquidityToken);
            var after = CheckPairData(pair, account);
            after["lpTokenBalance"].ShouldBe(origin["lpTokenBalance"] - liquidityRemoved.LiquidityToken);
            after["ReserveA"].ShouldBe(origin["ReserveA"] - liquidityRemoved.AmountA);
            after["ReserveB"].ShouldBe(origin["ReserveB"] - liquidityRemoved.AmountB);

            var K = _gandalfSwapContract.GetKLast(pairAddress);
            var feeToLpTokenBalance = _gandalfSwapContract.GetLiquidityTokenBalance(pairList, FeeToAccount);
            var feeToBalanceResults = feeToLpTokenBalance.Results.First(r => r.SymbolPair.Equals(pair));
            Logger.Info($"after add liquidity {feeToBalanceResults.Balance}");
            var fee = feeToBalanceResults.Balance - originFeeToBalanceResults.Balance;
            Logger.Info(fee);

            after["totalSupply"].ShouldBe(origin["totalSupply"] - liquidityRemoved.LiquidityToken +
                feeToBalanceResults.Balance - originFeeToBalanceResults.Balance);
            Logger.Info(K);
            if (fee != 0)
            {
                var totalFee = 30000;
                var checkFee = CheckFee(totalFee, symbolA, origin["ReserveA"], origin["ReserveB"],
                    origin["totalSupply"]);
                checkFee.Value.ShouldBeGreaterThan(fee.ToString());
                checkFee.Value.ShouldBeLessThan(fee.Add(10).ToString());
                Logger.Info($"check fee: {checkFee.Value}\n");
            }

            after["UserBalanceA"].ShouldBe(origin["UserBalanceA"] + liquidityRemoved.AmountA);
            after["UserBalanceB"].ShouldBe(origin["UserBalanceB"] + liquidityRemoved.AmountB);
            after["PairBalanceA"].ShouldBe(origin["PairBalanceA"] - liquidityRemoved.AmountA);
            after["PairBalanceB"].ShouldBe(origin["PairBalanceB"] - liquidityRemoved.AmountB);
        }

        [TestMethod]
        public void SwapExactTokenForToken()
        {
            var symbolA = "ETH";
            var symbolB = "ELF";
            var pair = $"{symbolA}-{symbolB}";
            var amountIn = 10000000;
            Approve(InitAccount, amountIn, symbolA);

            var origin = CheckPairData(pair);
            var exceptAmountOut = _gandalfSwapContract.GetAmountOut(symbolA, symbolB, amountIn);
            var rate = CheckFeeRate(amountIn, origin["ReserveA"], origin["ReserveB"], symbolA, symbolB);
            rate.Value.ShouldBe((10000 - FeeRate).ToString());
            var check = CheckAmountOut(amountIn, origin["ReserveA"], origin["ReserveB"], symbolA, symbolB);
            check.ShouldBeTrue();

            var result = _gandalfSwapContract.SwapExactTokenForToken(out var output, symbolA, symbolB, amountIn);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info(output);

            var logs = ByteString.FromBase64(result.Logs.First(l => l.Name.Contains("Swap")).NonIndexed);
            var swap = Swap.Parser.ParseFrom(logs);
            Logger.Info(swap);
            swap.SymbolIn.ShouldBe(symbolA);
            swap.SymbolOut.ShouldBe(symbolB);
            swap.AmountIn.ShouldBe(amountIn);
            swap.TotalFee.ShouldBe(swap.AmountIn.Mul(FeeRate).Div(10000));
            var amountOut = swap.AmountOut;
            var after = CheckPairData(pair);
            after["ReserveA"].ShouldBe(origin["ReserveA"] + amountIn);
            after["ReserveB"].ShouldBe(origin["ReserveB"] - amountOut);
            after["UserBalanceA"].ShouldBe(origin["UserBalanceA"] - amountIn);
            after["UserBalanceB"].ShouldBe(origin["UserBalanceB"] + amountOut);
            after["PairBalanceA"].ShouldBe(origin["PairBalanceA"] + amountIn);
            after["PairBalanceB"].ShouldBe(origin["PairBalanceB"] - amountOut);
        }

        [TestMethod]
        public void SwapTokenForExactToken()
        {
            var symbolA = "ETH";
            var symbolB = "ELF";
            var pair = $"{symbolA}-{symbolB}";
            var amountOut = 10_00000000;
            var exceptAmountIn = _gandalfSwapContract.GetAmountIn(symbolA, symbolB, amountOut);
            Approve(InitAccount, exceptAmountIn, symbolA);

            var origin = CheckPairData(pair);
            var rate = CheckFeeRate(exceptAmountIn, origin["ReserveA"], origin["ReserveB"], symbolA, symbolB);
            rate.Value.ShouldBe((10000 - FeeRate).ToString());
            var check = CheckAmountIn(amountOut, origin["ReserveA"], origin["ReserveB"], symbolA, symbolB);
//            check.ShouldBeTrue();

            var result = _gandalfSwapContract.SwapTokenForExactToken(out var output, symbolA, symbolB, amountOut);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            output.AmountOut.ShouldBe(amountOut);

            var logs = ByteString.FromBase64(result.Logs.First(l => l.Name.Contains("Swap")).NonIndexed);
            var swap = Swap.Parser.ParseFrom(logs);
            Logger.Info(swap);
            swap.SymbolIn.ShouldBe(symbolA);
            swap.SymbolOut.ShouldBe(symbolB);
            swap.AmountOut.ShouldBe(amountOut);
            swap.TotalFee.ShouldBe(swap.AmountIn.Mul(FeeRate).Div(10000));

            var amountIn = swap.AmountIn;
            var after = CheckPairData(pair);
            after["ReserveA"].ShouldBe(origin["ReserveA"] + amountIn);
            after["ReserveB"].ShouldBe(origin["ReserveB"] - amountOut);
            after["UserBalanceA"].ShouldBe(origin["UserBalanceA"] - amountIn);
            after["UserBalanceB"].ShouldBe(origin["UserBalanceB"] + amountOut);
            after["PairBalanceA"].ShouldBe(origin["PairBalanceA"] + amountIn);
            after["PairBalanceB"].ShouldBe(origin["PairBalanceB"] - amountOut);
        }

        [TestMethod]
        public void Swap_Error()
        {
            var symbolA = "ETH";
            var symbolB = "ELF";
            var pair = $"{symbolA}-{symbolB}";
            var pairList = new List<string> {pair};
            var origin = CheckPairData(pair);
            {
                var result1 = _gandalfSwapContract.SwapExactTokenForToken(out var output1, "ABC", "DEF", 1);
                result1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result1.Error.ShouldContain("Pair not exists");
                var result2 = _gandalfSwapContract.SwapTokenForExactToken(out var output2, "ABC", "DEF", 1);
                result2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result2.Error.ShouldContain("Pair not exists");
            }
            {
                var amountIn = 10000000;
                var result1 = _gandalfSwapContract.SwapExactTokenForToken(out var output1, symbolA, symbolB, amountIn, 100000000000000000);
                result1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result1.Error.ShouldContain("Insufficient Output amount");

                var amountOut = 100000000;
                var getAmountIn = _gandalfSwapContract.GetAmountIn(symbolA, symbolB, amountOut);
                var result2 = _gandalfSwapContract.SwapTokenForExactToken(out var output2, symbolA, symbolB, amountOut, getAmountIn/2);
                result2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result2.Error.ShouldContain("Insufficient Input amount");
            }
            {
                var amountOut = origin["ReserveB"];
                // var getAmountIn = _gandalfSwapContract.GetAmountIn(symbolA, symbolB, amountOut - 1);
                var result1 = _gandalfSwapContract.SwapExactTokenForToken(out var output1, symbolA, symbolB,  origin["ReserveA"]);
                result1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result1.Error.ShouldContain("Insufficient reserve");

                var result2 = _gandalfSwapContract.SwapTokenForExactToken(out var output2, symbolA, symbolB, amountOut);
                result2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result2.Error.ShouldContain("Insufficient reserve");
            }

        }
        
        [TestMethod]
        public void TransferLiquidityTokens()
        {
            var symbolA = "ETH";
            var symbolB = "ELF";
            var pair = $"{symbolA}-{symbolB}";
            var pairList = new List<string> {pair};
            var account = NodeManager.NewAccount("12345678");
            
            var lpTokenAccountOriginBalance = _gandalfSwapContract.GetLiquidityTokenBalance(pairList, account);
            var accountBalance = lpTokenAccountOriginBalance.Results.First(r => r.SymbolPair.Equals(pair));
            
            var lpTokenBalance = _gandalfSwapContract.GetLiquidityTokenBalance(pairList, InitAccount);
            var balanceResults = lpTokenBalance.Results.First(r => r.SymbolPair.Equals(pair));
            var amount = balanceResults.Balance / 4;
            Logger.Info($"before: {accountBalance.Balance}, {balanceResults.Balance}, {amount}");

            _gandalfSwapContract.SetAccount(InitAccount);
            var result = _gandalfSwapContract.TransferLiquidityTokens(pair, account, amount);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var lpTokenAfterBalance = _gandalfSwapContract.GetLiquidityTokenBalance(pairList, InitAccount);
            var balanceAfterResults = lpTokenAfterBalance.Results.First(r => r.SymbolPair.Equals(pair));
            balanceAfterResults.Balance.ShouldBe(balanceResults.Balance - amount);

            var lpTokenAccountBalance = _gandalfSwapContract.GetLiquidityTokenBalance(pairList, account);
            var afterBalance = lpTokenAccountBalance.Results.First(r => r.SymbolPair.Equals(pair));
            afterBalance.Balance.ShouldBe(accountBalance.Balance + amount);
            Logger.Info($"after: {afterBalance.Balance}, {balanceAfterResults.Balance}, {amount}");
        }
        
        [TestMethod]
        public void TransferLiquidityTokens_Error()
        {
            var symbolA = "ETH";
            var symbolB = "ELF";
            var pair = $"{symbolA}-{symbolB}";
            var pairList = new List<string> {pair};
            var account = NodeManager.NewAccount("12345678");
            
            var lpTokenAccountOriginBalance = _gandalfSwapContract.GetLiquidityTokenBalance(pairList, account);
            var accountBalance = lpTokenAccountOriginBalance.Results.First(r => r.SymbolPair.Equals(pair));
            
            var lpTokenBalance = _gandalfSwapContract.GetLiquidityTokenBalance(pairList, InitAccount);
            var balanceResults = lpTokenBalance.Results.First(r => r.SymbolPair.Equals(pair));
            {
                var amount = balanceResults.Balance.Mul(4);
                _gandalfSwapContract.SetAccount(InitAccount);
                var result = _gandalfSwapContract.TransferLiquidityTokens(pair, account, amount);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Insufficient LiquidityToken");
            
                var lpTokenAfterBalance = _gandalfSwapContract.GetLiquidityTokenBalance(pairList, InitAccount);
                var balanceAfterResults = lpTokenAfterBalance.Results.First(r => r.SymbolPair.Equals(pair));
                balanceAfterResults.Balance.ShouldBe(balanceResults.Balance);

                var lpTokenAccountBalance = _gandalfSwapContract.GetLiquidityTokenBalance(pairList, account);
                var afterBalance = lpTokenAccountBalance.Results.First(r => r.SymbolPair.Equals(pair));
                afterBalance.Balance.ShouldBe(accountBalance.Balance);
                Logger.Info($"after: {afterBalance.Balance}, {balanceAfterResults.Balance}, {amount}");
            }
            {
                var notExistsPair = "ABC-ABC";
                _gandalfSwapContract.SetAccount(InitAccount);
                var result = _gandalfSwapContract.TransferLiquidityTokens(notExistsPair, account, 1);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Pair not exists");
            }
            {
                _gandalfSwapContract.SetAccount(InitAccount);
                var result = _gandalfSwapContract.TransferLiquidityTokens(pair, account, 0);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Invalid Input");
            }
        }


        [TestMethod]
        public void GetPairList()
        {
            var pairList = _gandalfSwapContract.GetPairs();
            Logger.Info(pairList.SymbolPair);
        }

        [TestMethod]
        public void GetTokenInfo()
        {
            var token = "ETH";
            var info = _tokenContract.GetTokenInfo(token);
            Logger.Info(info);
        }

        private void CreateToken(string symbol, int d)
        {
            long totalSupply = 100000000;
            var t = totalSupply.Mul(Int64.Parse(new BigIntValue(10).Pow(d).Value));
            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
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
            return tokenInfo.Equals(new TokenInfo());
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
            var approve = _tokenContract.ApproveToken(sender, _gandalfSwapContract.ContractAddress, amount, symbol);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        private Dictionary<string, long> CheckPairData(string pair, string sender = "")
        {
            var account = sender == "" ? InitAccount : sender;
            var resultsList = new Dictionary<string, long>();
            var pairList = new List<string> {pair};
            _gandalfSwapContract.SetAccount(account);
            var originTotalSupply = _gandalfSwapContract.GetTotalSupply(pair);
            var originResult = originTotalSupply.Results.First(r => r.SymbolPair.Equals(pair));
            resultsList["totalSupply"] = originResult.TotalSupply;
            var lpTokenOriginBalance = _gandalfSwapContract.GetLiquidityTokenBalance(pairList, account);
            var originBalanceResults = lpTokenOriginBalance.Results.First(r => r.SymbolPair.Equals(pair));
            resultsList["lpTokenBalance"] = originBalanceResults.Balance;
            var reserves = _gandalfSwapContract.GetReserves(pair);
            var reservesResult = reserves.Results.First(r => r.SymbolPair.Equals(pair));
            resultsList["ReserveA"] = reservesResult.ReserveA;
            resultsList["ReserveB"] = reservesResult.ReserveB;
            var symbolList = pair.Split("-").ToList();
            var pairAddress = _gandalfSwapContract.GetPairAddress(symbolList.First(), symbolList.Last());

            var userBalanceA = _tokenContract.GetUserBalance(account, symbolList.First());
            var userBalanceB = _tokenContract.GetUserBalance(account, symbolList.Last());
            var pairBalanceA = _tokenContract.GetUserBalance(pairAddress.ToBase58(), symbolList.First());
            var pairBalanceB = _tokenContract.GetUserBalance(pairAddress.ToBase58(), symbolList.Last());
            resultsList["UserBalanceA"] = userBalanceA;
            resultsList["UserBalanceB"] = userBalanceB;
            resultsList["PairBalanceA"] = pairBalanceA;
            resultsList["PairBalanceB"] = pairBalanceB;

            Logger.Info($"\ntotalSupply: {originResult.TotalSupply}\n" +
                        $"UserLpBalance: {originBalanceResults.Balance}\n" +
                        $"ReserveA: {reservesResult.ReserveA}\n" +
                        $"ReserveB: {reservesResult.ReserveB}\n" +
                        $"Pair tokenA balance: {pairBalanceA}\n" +
                        $"Pair tokenB balance: {pairBalanceB}\n" +
                        $"UserBalanceA: {userBalanceA}\n" +
                        $"UserBalanceB: {userBalanceB}");
            return resultsList;
        }

        private bool CheckAmountIn(long amountOut, long reserveIn, long reserveOut, string symbolIn, string symbolOut)
        {
            var k = new BigIntValue(reserveIn).Mul(reserveOut);
            Logger.Info(k.Value);
            var exceptAmountIn = _gandalfSwapContract.GetAmountIn(symbolIn, symbolOut, amountOut);
            //var exceptAmountOut = _gandalfSwapContract.GetAmountOut(symbolIn, symbolOut, exceptAmountIn);

            var feeRate = _gandalfSwapContract.GetFeeRate();
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
            var exceptAmountOut = _gandalfSwapContract.GetAmountOut(symbolIn, symbolOut, amountIn);
            var feeRate = _gandalfSwapContract.GetFeeRate();
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
            var exceptAmountOut = _gandalfSwapContract.GetAmountOut(symbolIn, symbolOut, amountIn);
            var noFeeAmountOut =
                new BigIntValue(amountIn).Mul(reserveOut).Div(new BigIntValue(reserveIn).Add(amountIn));
            var rate = new BigIntValue(exceptAmountOut).Mul(10000).Div(noFeeAmountOut);
            Logger.Info(rate.Value);
            return rate;
        }

        private BigIntValue CheckFee(long totalFee, string symbolA, long reserveA, long reserveB, long totalSupply)
        {
            //收取的费用为和amountIn totalFee等值的 lpToken 所以要除以2
            var amountB = new BigIntValue(totalFee).Mul(reserveB).Div(reserveA);
            var fee = CommonHelper.Sqrt(amountB.Div(2).Mul(totalFee.Div(2))).Div(5);
            return fee;
        }
    }
}