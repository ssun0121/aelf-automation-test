using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.FinanceContract;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Account = AElf.Contracts.FinanceContract.Account;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class FinanceContractTests
    {
        public const string DefaultCollateralFactor = "0.75";
        public const string MaxBorrowRate = "0.00000016";
        public const string MaxReserveFactor = "1.00";

        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private INodeManager SideNodeManager { get; set; }

        private TokenContract _tokenContract;
        private TokenContract _sideTokenContract;

        private GenesisContract _genesisContract;
        private GenesisContract _sideGenesisContract;

        private FinanceContract _financeContract;
        private FinanceContractContainer.FinanceContractStub _financeContractStub;
        private FinanceContractContainer.FinanceContractStub _adminfinanceContractStub;

        private string InitAccount { get; } = "27hpKF3ND8SMn4YbzKpmw6U5CAYPzbG1o7e3TZ3w51vtiqw9j2";
//        private string InitAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        private string Password { get; } = "12345678";
//        private string Password { get; } = "123";
        private string TestAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        private string PageAccount { get; } = "h6CRCFAhyozJPwdFRd7i8A5zVAqy171AVty3uMQUQp1MB9AKa";

        private List<string> Tester = new List<string>
        {
            "2oSMWm1tjRqVdfmrdL8dgrRvhWu1FP8wcZidjS6wPbuoVtxhEz",
            "WRy3ADLZ4bEQTn86ENi5GXi5J1YyHp9e99pPso84v2NJkfn5k",
            "2frDVeV6VxUozNqcFbgoxruyqCRAuSyXyfCaov6bYWc7Gkxkh2",
            "2ZYyxEH6j8zAyJjef6Spa99Jx2zf5GbFktyAQEBPWLCvuSAn8D",
            "eFU9Quc8BsztYpEHKzbNtUpu9hGKgwGD2tyL13MqtFkbnAoCZ",
            "2V2UjHQGH8WT4TWnzebxnzo9uVboo67ZFbLjzJNTLrervAxnws",
            "EKRtNn3WGvFSTDewFH81S7TisUzs9wPyP4gCwTww32waYWtLB"
        };

        private static string RpcUrl { get; } = "192.168.199.109:8002";
        private static string SideRpcUrl { get; } = "192.168.199.109:8008";
        private string Symbol { get; } = "FINA";

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("FinanceContract");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env205-main");
            NodeManager = new NodeManager(RpcUrl);
            SideNodeManager = new NodeManager(SideRpcUrl);

            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount,Password);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount,Password);

            _sideGenesisContract = GenesisContract.GetGenesisContract(SideNodeManager, InitAccount,Password);
            _sideTokenContract = _sideGenesisContract.GetTokenContract(InitAccount,Password);
//            _financeContract = new FinanceContract(SideNodeManager, InitAccount);
//            Logger.Info($"Finance contract : {_financeContract}");
//            if (!_tokenContract.GetTokenInfo(Symbol).Symbol.Equals(Symbol))
//                CreateTokenAndIssue();

//            buePNjhmHckfZn9D8GTL1wq6JgA8K24SeTWnjCNcrz6Sf1FDh --109:8010
//            RXcxgSXuagn8RrvhQAV81Z652EEYSwR6JLnqHYJ5UVpEptW8Y -- 51:8001
//            2wRDbyVF28VBQoSPgdSEFaL4x7CaXz8TCBujYhgWc9qTMxBE3n
            _financeContract = new FinanceContract(SideNodeManager, InitAccount,
                "buePNjhmHckfZn9D8GTL1wq6JgA8K24SeTWnjCNcrz6Sf1FDh",Password);

//            InitializeContract();

            _adminfinanceContractStub =
                _financeContract.GetTestStub<FinanceContractContainer.FinanceContractStub>(InitAccount,Password);
            _financeContractStub =
                _financeContract.GetTestStub<FinanceContractContainer.FinanceContractStub>(TestAccount,Password);
            Logger.Info(_financeContract.ApiClient.BaseUrl);

//            foreach (var tester in Tester)
//            {
//                var balance = _sideTokenContract.GetUserBalance(tester, Symbol);
//                var elfBalance = _sideTokenContract.GetUserBalance(tester, "ELF");
//                if (balance < 100_00000)
//                    _sideTokenContract.TransferBalance(InitAccount, tester, 1000_00000, Symbol);
//                if (elfBalance < 100_00000000)
//                    _sideTokenContract.TransferBalance(InitAccount, tester, 1000_00000000, "ELF");
//            }
        }

        //MaxBorrowRate = "0.00000016";
        // TEST: 1 TEST -> 50 cTEST Price: 0.5ELF
        // ReserveFactor: 0.05 BaseRatePerBlock:00000000005, MultiplierPerBlock:0000000008
        //FINA: 1 FINA -> 5 cFINA Price: 0.2ELF
        //ReserveFactor: 0.05 BaseRatePerBlock:0.000000001, MultiplierPerBlock:0.000000005
//        var baseRatePerBlock = "0.0000001";
//        var multiplierPerBlock = "0.00000002";

        //FULL: 1 FULL -> 100 cFULL Price: 0.01ELF
        //ReserveFactor: 0.01 BaseRatePerBlock:0.000000000001, MultiplierPerBlock:0.00000000001
        //        var baseRatePerBlock = "0.00000000001";
        //        var multiplierPerBlock = "0.00000000005";

        //AEUSD: 1 AEUSD -> 10 cAEUSD Price 10ELF
        //ReserveFactor: 0.8 BaseRatePerBlock:0.000000001, MultiplierPerBlock:0.0000000005
        //CPU: 1 CPU -> 1 cCPU Price 2ELF
        //ReserveFactor: 0.2 BaseRatePerBlock:0.00000000001, MultiplierPerBlock:0.000000000001
        //ELF: 1 ELF -> 1 cELF Price 1ELF
        //ReserveFactor: 0.5 BaseRatePerBlock:0.0000000001, MultiplierPerBlock:0.0000000005
        [TestMethod]
        public void SupportMarket()
        {
            var symbol = "ELF";
            var reserveFactor = "0.5";
            var initialExchangeRate = "1";
            var baseRatePerBlock = "0.0000000001";
            var multiplierPerBlock = "0.0000000005";
            _financeContract.SetAccount(InitAccount,Password);
            var result =
                _financeContract.ExecuteMethodWithResult(FinanceMethod.SupportMarket, new SupportMarketInput
                {
                    Symbol = symbol,
                    ReserveFactor = reserveFactor,
                    InitialExchangeRate = initialExchangeRate,
                    BaseRatePerBlock = baseRatePerBlock,
                    MultiplierPerBlock = multiplierPerBlock
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var info = MarketListed.Parser.ParseFrom(
                ByteString.FromBase64(result.Logs.First(n => n.Name.Contains(nameof(MarketListed))).NonIndexed));
            info.Symbol.ShouldBe(symbol);
            info.BaseRatePerBlock.ShouldBe(baseRatePerBlock);
            info.ReserveFactor.ShouldBe(reserveFactor);
            info.MultiplierPerBlock.ShouldBe(multiplierPerBlock);
        }

        [TestMethod]
        public async Task GetMarket()
        {
            var allList = await _adminfinanceContractStub.GetAllMarkets.CallAsync(new Empty());
//            allList.Symbols.ShouldContain(Symbol);

            foreach (var s in allList.Symbols)
            {
                var rate = await _adminfinanceContractStub.GetCurrentExchangeRate.CallAsync(new StringValue
                    {Value = s});
                var reserveFactor =
                    await _adminfinanceContractStub.GetReserveFactor.CallAsync(new StringValue {Value = s});
                var interestRate = _adminfinanceContractStub.GetInterestRate.CallAsync(new StringValue {Value = s});
                Logger.Info(
                    $"{s}: exchange rate: {rate}; reserve factor: {reserveFactor}, " +
                    $"multiplier: {interestRate.Result.MultiplierPerBlock}, base rate per block: {interestRate.Result.BaseRatePerBlock}");
            }
        }

        [TestMethod]
        public void SupportMarket_ErrorTest()
        {
//            _sideTokenContract.TransferBalance(InitAccount, TestAccount, 1000_00000000);
            {
                _financeContract.SetAccount(TestAccount);
                var errorResult =
                    _financeContract.ExecuteMethodWithResult(FinanceMethod.SupportMarket, new SupportMarketInput
                    {
                        Symbol = Symbol,
                        ReserveFactor = "0.5",
                        InitialExchangeRate = "0.02",
                        BaseRatePerBlock = "0",
                        MultiplierPerBlock = "0"
                    });
                errorResult.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                errorResult.Error.ShouldContain("Unauthorized");
            }
            {
                _financeContract.SetAccount(InitAccount);
                var errorResult =
                    _financeContract.ExecuteMethodWithResult(FinanceMethod.SupportMarket, new SupportMarketInput
                    {
                        Symbol = "ABC",
                        ReserveFactor = "0.9",
                        InitialExchangeRate = "0.02",
                        BaseRatePerBlock = "0",
                        MultiplierPerBlock = "0"
                    });
                errorResult.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                errorResult.Error.ShouldContain("Support market exists");
            }
            {
                _financeContract.SetAccount(InitAccount);
                var errorResult =
                    _financeContract.ExecuteMethodWithResult(FinanceMethod.SupportMarket, new SupportMarketInput
                    {
                        Symbol = "ABCD",
                        ReserveFactor = "0.9",
                        InitialExchangeRate = "0.02",
                        BaseRatePerBlock = "0",
                        MultiplierPerBlock = "0"
                    });
                errorResult.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                errorResult.Error.ShouldContain("Invalid Symbol");
            }
            {
                _financeContract.SetAccount(InitAccount);
                var errorResult =
                    _financeContract.ExecuteMethodWithResult(FinanceMethod.SupportMarket, new SupportMarketInput
                    {
                        Symbol = "FINA",
                        ReserveFactor = "1.5",
                        InitialExchangeRate = "0.02",
                        BaseRatePerBlock = "0",
                        MultiplierPerBlock = "0"
                    });
                errorResult.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                errorResult.Error.ShouldContain("Invalid ReserveFactor");
            }
            {
                _financeContract.SetAccount(InitAccount);
                var errorResult =
                    _financeContract.ExecuteMethodWithResult(FinanceMethod.SupportMarket, new SupportMarketInput
                    {
                        Symbol = "FINA",
                        ReserveFactor = "0.1",
                        InitialExchangeRate = "0",
                        BaseRatePerBlock = "0",
                        MultiplierPerBlock = "0"
                    });
                errorResult.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                errorResult.Error.ShouldContain("Invalid InitialExchangeRate");
            }
            {
                _financeContract.SetAccount(InitAccount);
                var errorResult =
                    _financeContract.ExecuteMethodWithResult(FinanceMethod.SupportMarket, new SupportMarketInput
                    {
                        Symbol = "FINA",
                        ReserveFactor = "0.1",
                        InitialExchangeRate = "0.1",
                        BaseRatePerBlock = "-1",
                        MultiplierPerBlock = "0"
                    });
                errorResult.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                errorResult.Error.ShouldContain("Invalid BaseRatePerBlock");
            }
            {
                _financeContract.SetAccount(InitAccount);
                var errorResult =
                    _financeContract.ExecuteMethodWithResult(FinanceMethod.SupportMarket, new SupportMarketInput
                    {
                        Symbol = "FINA",
                        ReserveFactor = "0.1",
                        InitialExchangeRate = "0.1",
                        BaseRatePerBlock = "0",
                        MultiplierPerBlock = "-1"
                    });
                errorResult.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                errorResult.Error.ShouldContain("Invalid MultiplierPerBlock");
            }
            {
                _financeContract.SetAccount(InitAccount);
                var errorResult =
                    _financeContract.ExecuteMethodWithResult(FinanceMethod.SupportMarket, new SupportMarketInput
                    {
                        Symbol = "FINA",
                        ReserveFactor = "0.1",
                        InitialExchangeRate = "0.1",
                        BaseRatePerBlock = "0.1",
                        MultiplierPerBlock = "0.1"
                    });
                errorResult.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                errorResult.Error.ShouldContain("Invalid interestRate model");
            }
        }

        //Tester[0] 10000_00000000 "TEST" borrow limit: 10000*0.001*0.8 = 8ELF， 10_00000000 ELF = 8ELF
        //Tester[1] 10000_00000 "FULL" borrow limit: 10000*0.01*0.75 = 7500ELF
        //Tester[2] 10000_000000000 "FINA" borrow limit: 10000*0.2*0.75 = 1.5ELF
        //Tester[3] 10000_000 "AEUSD" borrow limit: 10000*10*0.75 = 0.75ELF
        //Tester[4] 10000_00000000 "CPU" borrow limit: 10000*2*0.75 = 15000ELF；
        //Tester[5] 10000_00000000 "ELF" borrow limit: 10000*1*0.75 = 7500ELF
        
        [TestMethod]
        public void MintTest()
        {
            var amount = 1000_00000000;
            var symbol = "TEST";
            var tester = Tester[5];

//            _sideTokenContract.TransferBalance(InitAccount, tester, 100_00000000, "ELF");
            _sideTokenContract.TransferBalance(InitAccount, tester, amount, symbol);

            var accountInfo =
                _financeContract.CallViewMethod<GetAccountSnapshotOutput>(FinanceMethod.GetAccountSnapshot,
                    new Account {Address = tester.ConvertAddress(), Symbol = symbol});
            var contractMintTokenBalance =
                _financeContract.CallViewMethod<Int64Value>(FinanceMethod.GetCash,
                    new StringValue {Value = symbol});
            var userCTokenBalance = _financeContract.CallViewMethod<Int64Value>(FinanceMethod.GetBalance,
                new Account {Address = tester.ConvertAddress(), Symbol = symbol});
            var userBalance = _sideTokenContract.GetUserBalance(tester, symbol);

            _financeContract.SetAccount(tester);
            _sideTokenContract.ApproveToken(tester, _financeContract.ContractAddress, amount, symbol);

            var result = _financeContract.ExecuteMethodWithResult(FinanceMethod.Mint, new MintInput
            {
                Symbol = symbol,
                Amount = amount
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var cTokenBalance = Mint.Parser
                .ParseFrom(ByteString.FromBase64(result.Logs.First(n => n.Name.Contains(nameof(Mint))).NonIndexed))
                .CTokenAmount;
            var accrueInterest = AccrueInterest.Parser.ParseFrom(
                ByteString.FromBase64(result.Logs.First(n => n.Name.Contains(nameof(AccrueInterest))).NonIndexed));
            accrueInterest.Symbol.ShouldBe(symbol);
            Logger.Info($"{symbol} AccrueInterest: {accrueInterest}");

            var rate = _financeContract.CallViewMethod<StringValue>(FinanceMethod.GetCurrentExchangeRate,
                new StringValue {Value = symbol});
            Logger.Info($"{symbol} exchange rate is {rate}; {amount} {symbol} exchange {cTokenBalance} cToken");
            var afterMintAccountInfo =
                _financeContract.CallViewMethod<GetAccountSnapshotOutput>(FinanceMethod.GetAccountSnapshot,
                    new Account {Address = tester.ConvertAddress(), Symbol = symbol});
            afterMintAccountInfo.CTokenBalance.ShouldBe(accountInfo.CTokenBalance + cTokenBalance);
            Logger.Info($"Account {tester}, {symbol} cToken balance is {afterMintAccountInfo.CTokenBalance}");

            var contractMintTokenBalanceAfter =
                _financeContract.CallViewMethod<Int64Value>(FinanceMethod.GetCash,
                    new StringValue {Value = symbol});
            contractMintTokenBalanceAfter.Value.ShouldBe(contractMintTokenBalance.Value + amount);
            var userCTokenBalanceAfter = _financeContract.CallViewMethod<Int64Value>(FinanceMethod.GetBalance,
                new Account {Address = tester.ConvertAddress(), Symbol = symbol});
            userCTokenBalance.Value.ShouldBe(userCTokenBalanceAfter.Value - cTokenBalance);
            var userBalanceAfter = _sideTokenContract.GetUserBalance(tester, symbol);
            userBalance.ShouldBe(userBalanceAfter + amount);
            
            cTokenBalance.ShouldBe(decimal.ToInt64(amount / decimal.Parse(rate.Value)));
        }

        [TestMethod]
        public void MinTest_Error()
        {
            {
                var amount = 500_00000000;
                var symbol = "ABCD";
                var tester = Tester[0];
                _financeContract.SetAccount(tester);
                _sideTokenContract.ApproveToken(tester, _financeContract.ContractAddress, amount, symbol);

                var result = _financeContract.ExecuteMethodWithResult(FinanceMethod.Mint, new MintInput
                {
                    Symbol = symbol,
                    Amount = amount
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Market is not listed");
            }
        }

        [TestMethod]
        public void EnterMarketsTest()
        {
            var symbol = "AEUSD";
            var tester = Tester[4];
//            _financeContract.SetAccount(tester);
//            var result = _financeContract.ExecuteMethodWithResult(FinanceMethod.EnterMarket, new EnterMarketInput
//            {
//                Symbol = symbol
//            });
//            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
//
//            var marketEntered = MarketEntered.Parser.ParseFrom(
//                ByteString.FromBase64(result.Logs.First(n => n.Name.Contains(nameof(MarketEntered))).NonIndexed));
//            marketEntered.Address.ShouldBe(tester.ConvertAddress());
//            marketEntered.Symbol.ShouldBe(symbol);
//
            var assetsIn =
                _financeContract.CallViewMethod<AssetList>(FinanceMethod.GetAssetsIn, tester.ConvertAddress());
//            assetsIn.Assets.ShouldContain(symbol);
            Logger.Info($"{assetsIn}");

            var check = _financeContract.CallViewMethod<BoolValue>(FinanceMethod.CheckMembership, new Account
            {
                Address = tester.ConvertAddress(),
                Symbol = symbol
            });
            check.Value.ShouldBeTrue();
        }

        [TestMethod]
        public void EnterMarkets_ErrorTest()
        {
            {
                _financeContract.SetAccount(TestAccount);
                var result = _financeContract.ExecuteMethodWithResult(FinanceMethod.EnterMarket, new EnterMarketInput
                {
                    Symbol = "ABCD"
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Market is not listed");
            
//                var check = _financeContract.CallViewMethod<BoolValue>(FinanceMethod.CheckMembership, new Account
//                {
//                    Address = TestAccount.ConvertAddress(),
//                    Symbol = "ABCD"
//                });
//                check.Value.ShouldBeFalse();
            }
            {
                _financeContract.SetAccount(Tester[0]);
                var result = _financeContract.ExecuteMethodWithResult(FinanceMethod.EnterMarket, new EnterMarketInput
                {
                    Symbol = "AEUSD"
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Too Many Assets");
            
                var check = _financeContract.CallViewMethod<BoolValue>(FinanceMethod.CheckMembership, new Account
                {
                    Address = TestAccount.ConvertAddress(),
                    Symbol = "AEUSD"
                });
                check.Value.ShouldBeFalse();
            }
        }

        [TestMethod]
        public void ExitMarket_Test()
        {
            var tester = Tester[0];
            var symbol = "FINA";
            _financeContract.SetAccount(tester);
            var result =
                _financeContract.ExecuteMethodWithResult(FinanceMethod.ExitMarket, new StringValue {Value = symbol});
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var info = MarketExited.Parser.ParseFrom(
                ByteString.FromBase64(result.Logs.First(n => n.Name.Contains(nameof(MarketExited))).NonIndexed));
            Logger.Info(info);
            var assetsIn =
                _financeContract.CallViewMethod<AssetList>(FinanceMethod.GetAssetsIn, tester.ConvertAddress());
            Logger.Info($"{assetsIn}");
            assetsIn.Assets.ShouldNotContain(symbol);
            var check = _financeContract.CallViewMethod<BoolValue>(FinanceMethod.CheckMembership, new Account
            {
                Address = tester.ConvertAddress(),
                Symbol = symbol
            });
            check.Value.ShouldBeFalse();
        }

        //Tester[0] -> Mint TEST borrow limit 16ELF borrow: 10_000000000FINA = 1ELF
        //Tester[1] -> Mint FULL borrow limit 1ELF borrow: 10_000000000FINA 
        //Tester[2] -> Mint FINA borrow limit 1_50000000ELF borrow: 1_00000000TEST(0_50000000ELF)
        //        Tester[2] -> Mint FINA borrow limit 1_50000000ELF borrow: 2000_000AEUSD (0_20000000ELF)
        //        Tester[2] -> Mint FINA borrow limit 1_50000000ELF borrow: 4000_000AEUSD (0_80000000ELF)
        //Tester[3] -> Mint AEUSD borrow limit 0_7500000ELF borrow 2_00000 FINA (0_00040000ELF)
        //Tester[4] -> Mint CPU borrow limit 15000_00000000ELF borrow 1000_0000000000 FULL (1000ELF)，
        //    Tester[4] -> Mint ELF borrow limit 7500_00000000ELF borrow  1000_000AEUSD (0_10000000ELF)
        //Tester[5] -> Mint ELF borrow limit 7500_00000000ELF borrow  1000_000AEUSD (0_10000000ELF)
        //  Tester[5] -> Mint ELF borrow limit 7500_00000000ELF borrow  100_00000000ELF (100ELF)
        [TestMethod]
        public void BorrowTest()
        {
            var amount = 100_00000000;
            var tester = Tester[5];
            var symbol = "TEST";
            _financeContract.SetAccount(tester);
            var cashBefore = _financeContract.CallViewMethod<Int64Value>(FinanceMethod.GetCash,
                new StringValue {Value = symbol});
            var balanceBeforeBorrow = _sideTokenContract.GetUserBalance(tester, symbol);
            var result = _financeContract.ExecuteMethodWithResult(FinanceMethod.Borrow, new BorrowInput
            {
                Symbol = symbol,
                Amount = amount
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var accrueInterestInfo = AccrueInterest.Parser.ParseFrom(
                ByteString.FromBase64(result.Logs.First(n => n.Name.Contains(nameof(AccrueInterest))).NonIndexed));
            Logger.Info(accrueInterestInfo);

            var borrowInfo =
                Borrow.Parser.ParseFrom(
                    ByteString.FromBase64(result.Logs.First(n => n.Name.Contains(nameof(Borrow))).NonIndexed));
            Logger.Info(borrowInfo);

            var balance = _sideTokenContract.GetUserBalance(tester, symbol);
            balance.ShouldBe(balanceBeforeBorrow + amount);
            Logger.Info($"{tester},before borrow symbol{symbol}: {balanceBeforeBorrow}; after:{balance}");

            var cash = _financeContract.CallViewMethod<Int64Value>(FinanceMethod.GetCash,
                new StringValue {Value = symbol});
            cash.Value.ShouldBe(cashBefore.Value - amount);
        }

        [TestMethod]
        public void Borrow_ErrorTest()
        {
            var amount = 100000000;
            var tester = Tester[0];
            var symbol = "ABCD";
            _financeContract.SetAccount(tester);

            var balanceBeforeBorrow = _sideTokenContract.GetUserBalance(tester, symbol);
            var result = _financeContract.ExecuteMethodWithResult(FinanceMethod.Borrow, new BorrowInput
            {
                Symbol = symbol,
                Amount = amount
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var accrueInterestInfo = AccrueInterest.Parser.ParseFrom(
                ByteString.FromBase64(result.Logs.First(n => n.Name.Contains(nameof(AccrueInterest))).NonIndexed));
            Logger.Info(accrueInterestInfo);

            var borrowInfo =
                Borrow.Parser.ParseFrom(
                    ByteString.FromBase64(result.Logs.First(n => n.Name.Contains(nameof(Borrow))).NonIndexed));
            Logger.Info(borrowInfo);

            var balance = _sideTokenContract.GetUserBalance(tester, symbol);
            balance.ShouldBe(balanceBeforeBorrow + amount);
            Logger.Info($"{tester},before borrow symbol{symbol}: {balanceBeforeBorrow}; after:{balance}");
        }

        [TestMethod]
        public void RepayBorrow_Test()
        {
            var tester = Tester[1];
            var symbol = "FINA";
            var amount = -1;
            var replyAmount = CheckAccountInfo(tester, symbol);
            CheckCash(symbol);
//            var amount = 1000000;
            _sideTokenContract.TransferBalance(InitAccount, tester, 1_000000000,symbol );
            _financeContract.SetAccount(tester);
            _sideTokenContract.ApproveToken(tester, _financeContract.ContractAddress, replyAmount, symbol);
            var result = _financeContract.ExecuteMethodWithResult(FinanceMethod.RepayBorrow, new RepayBorrowInput
            {
                Amount = amount,
                Symbol = symbol
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var repayInfo = RepayBorrow.Parser.ParseFrom(
                ByteString.FromBase64(result.Logs.First(n => n.Name.Contains(nameof(RepayBorrow))).NonIndexed));
            repayInfo.Amount.ShouldBe(replyAmount);
            repayInfo.Payer.ShouldBe(tester.ConvertAddress());
            Logger.Info($"{repayInfo}");

            CheckAccountInfo(tester, symbol);
            CheckCash(symbol);
        }

        [TestMethod]
        public void RepayBorrowBehalf()
        {
            var tester = InitAccount;
            var borrower = Tester[3];
            var symbol = "FINA";
            var amount = -1;
            CheckAccountInfo(borrower, symbol);
            CheckCash(symbol);
            _financeContract.SetAccount(tester);
            _sideTokenContract.ApproveToken(tester, _financeContract.ContractAddress, 1000_00000000, symbol);
            _financeContract.SetAccount(tester);
            var result = _financeContract.ExecuteMethodWithResult(FinanceMethod.RepayBorrowBehalf,
                new RepayBorrowBehalfInput
                {
                    Amount = amount,
                    Symbol = symbol,
                    Borrower = borrower.ConvertAddress()
                });

            var repayInfo = RepayBorrow.Parser.ParseFrom(
                ByteString.FromBase64(result.Logs.First(n => n.Name.Contains(nameof(RepayBorrow))).NonIndexed));
//            repayInfo.Amount.ShouldBe(amount);
            repayInfo.Payer.ShouldBe(tester.ConvertAddress());
            Logger.Info($"{repayInfo}");

            CheckAccountInfo(borrower, symbol);
            CheckCash(symbol);
        }

        // input amount = cToken amount 
        [TestMethod]
        public void Redeem_Test()
        {
            var tester = TestAccount;
//            var tester = "YF8o6ytMB7n5VF9d1RDioDXqyQ9EQjkFK3AwLPCH2b9LxdTEq";
            var symbol = "ELF";
            var amount = 629;
            CheckCash(symbol);
            CheckAccountInfo(tester, symbol);

            _financeContract.SetAccount(tester);
            var result = _financeContract.ExecuteMethodWithResult(FinanceMethod.Redeem, new RedeemInput
            {
                Amount = amount,
                Symbol = symbol
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var info = Redeem.Parser.ParseFrom(
                ByteString.FromBase64(result.Logs.First(n => n.Name.Contains(nameof(Redeem))).NonIndexed));
            Logger.Info(info);

            CheckCash(symbol);
            CheckAccountInfo(tester, symbol);
        }

        //input amount: token amount
        [TestMethod]
        public void RedeemUnderlying()
        {
//            var tester = "YF8o6ytMB7n5VF9d1RDioDXqyQ9EQjkFK3AwLPCH2b9LxdTEq";
            var tester = Tester[0];
            var symbol = "TEST";
            var cTokenBalance =  _financeContract.CallViewMethod<Int64Value>(FinanceMethod.GetBalance, new Account
            {
                Symbol = symbol,
                Address = tester.ConvertAddress()
            });
            var exchangeRate = _financeContract.CallViewMethod<StringValue>(FinanceMethod.GetCurrentExchangeRate,
                new StringValue {Value = symbol});
            var amount = decimal.ToInt64(cTokenBalance.Value * decimal.Parse(exchangeRate.Value));
            Logger.Info(amount);
            
            CheckCash(symbol);
            CheckAccountInfo(tester, symbol);

            _financeContract.SetAccount(tester);
            var result = _financeContract.ExecuteMethodWithResult(FinanceMethod.RedeemUnderlying,
                new RedeemUnderlyingInput()
                {
                    Amount = amount,
                    Symbol = symbol
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var info = Redeem.Parser.ParseFrom(
                ByteString.FromBase64(result.Logs.First(n => n.Name.Contains(nameof(Redeem))).NonIndexed));
            Logger.Info(info);

            CheckCash(symbol);
            CheckAccountInfo(tester, symbol);
        }

        //change FINA price 0.2 ->0.05
        [TestMethod]
        public void LiquidateBorrow_Test()
        {
            var symbol = "ELF";
            var borrower = Tester[0];
            var collatera = "TEST";
            var amount = 1_000000;
            var tester = TestAccount;

            var maxClose = decimal.Parse("0.1") * amount;
            CheckCash(symbol);
            CheckCash(collatera);

            CheckAccountInfo(borrower, symbol);
            CheckAccountInfo(tester, symbol);
            CheckAccountInfo(tester, collatera);

//            GetHypotheticalAccountLiquidityInternal();
            _financeContract.SetAccount(tester);
//            _sideTokenContract.TransferBalance(InitAccount, tester, 10000_000000000, symbol);
//            _sideTokenContract.TransferBalance(InitAccount, tester, 10000_000000000, "ELF");

            _sideTokenContract.ApproveToken(tester, _financeContract.ContractAddress, 10000_000000000, symbol);
            var result =
                _financeContract.ExecuteMethodWithResult(FinanceMethod.LiquidateBorrow, new LiquidateBorrowInput
                {
                    Borrower = borrower.ConvertAddress(),
                    BorrowSymbol = symbol,
                    CollateralSymbol = collatera,
                    RepayAmount = amount
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var info = LiquidateBorrow.Parser.ParseFrom(
                ByteString.FromBase64(result.Logs.First(n => n.Name.Contains(nameof(LiquidateBorrow))).NonIndexed));
            Logger.Info(info);

            CheckCash(symbol);
            CheckCash(collatera);

            CheckAccountInfo(borrower, symbol);
            CheckAccountInfo(tester, symbol);
            CheckAccountInfo(tester, collatera);
        }

        [TestMethod]
        public void AccrueInterestTest()
        {
            var symbol = "FINA";
            _financeContract.SetAccount(InitAccount);
            var result =
                _financeContract.ExecuteMethodWithResult(FinanceMethod.AccrueInterest,
                    new StringValue {Value = symbol});
            var info = AccrueInterest.Parser.ParseFrom(
                ByteString.FromBase64(result.Logs.First(n => n.Name.Contains(nameof(AccrueInterest))).NonIndexed));
            Logger.Info($"{info}");

            var reservesFactor =
                _financeContract.CallViewMethod<StringValue>(FinanceMethod.GetReserveFactor,
                    new StringValue {Value = symbol});
            var reservesPrior =
                _financeContract.CallViewMethod<Int64Value>(FinanceMethod.GetTotalReserves,
                    new StringValue {Value = symbol});
            var totalReserves = decimal.Parse(reservesFactor.Value) * info.InterestAccumulated + reservesPrior.Value;
            Logger.Info($"{totalReserves}");
        }

        //Admin Authorized 

        [TestMethod]
        public void SetPauseGuardian()
        {
            var result = _financeContract.ExecuteMethodWithResult(FinanceMethod.SetPauseGuardian,TestAccount.ConvertAddress());
            var guardian = PauseGuardianChanged.Parser.ParseFrom(
                ByteString.FromBase64(result.Logs.First(n => n.Name.Contains(nameof(PauseGuardianChanged)))
                    .NonIndexed));
            guardian.NewPauseGuardian.ShouldBe(TestAccount.ConvertAddress());
            var guardianInfo = _financeContract.CallViewMethod<Address>(FinanceMethod.GetPauseGuardian,new Empty());
            guardianInfo.ShouldBe(TestAccount.ConvertAddress());
        }

        [TestMethod]
        public void SetMintPaused()
        {
            var symbol = "DDD";
            _financeContract.SetAccount(InitAccount);
            var result = _financeContract.ExecuteMethodWithResult(FinanceMethod.SetMintPaused, new SetPausedInput{Symbol = symbol,State = true});
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var returnValue = BoolValue.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            returnValue.Value.ShouldBeTrue();
            
            var amount = 10000_00000000;
            var tester = Tester[1];
            _sideTokenContract.TransferBalance(InitAccount, tester, amount, symbol);
            _financeContract.SetAccount(tester);
            _sideTokenContract.ApproveToken(tester, _financeContract.ContractAddress, amount, symbol);

            var mint = _financeContract.ExecuteMethodWithResult(FinanceMethod.Mint, new MintInput
            {
                Symbol = symbol,
                Amount = amount
            });
            mint.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            mint.Error.ShouldContain("Mint is paused");
        }
        
        [TestMethod]
        public void SetMintPaused_Error()
        {
            var symbol = "TEST";
            _financeContract.SetAccount(TestAccount);
            var result = _financeContract.ExecuteMethodWithResult(FinanceMethod.SetMintPaused, new SetPausedInput{Symbol = symbol,State = false});
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
        }
        
        [TestMethod]
        public void SetMintPaused_False()
        {
            var symbol = "TEST";
            _financeContract.SetAccount(InitAccount);
            var result = _financeContract.ExecuteMethodWithResult(FinanceMethod.SetMintPaused, new SetPausedInput{Symbol = symbol,State = false});
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var amount = 10000_00000000;
            var tester = Tester[1];
            _sideTokenContract.TransferBalance(InitAccount, tester, amount, symbol);
            _financeContract.SetAccount(tester);
            _sideTokenContract.ApproveToken(tester, _financeContract.ContractAddress, amount, symbol);

            var mint = _financeContract.ExecuteMethodWithResult(FinanceMethod.Mint, new MintInput
            {
                Symbol = symbol,
                Amount = amount
            });
            mint.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
        
        [TestMethod]
        public void SetBorrowPaused()
        {
            var symbol = "FULL";
            _financeContract.SetAccount(TestAccount);
            var result = _financeContract.ExecuteMethodWithResult(FinanceMethod.SetBorrowPaused, new SetPausedInput{Symbol = symbol,State = true});
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var returnValue = BoolValue.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            returnValue.Value.ShouldBeTrue();
            
            var amount = 100_00000000;
            var tester = Tester[0];
            _financeContract.SetAccount(tester);
            var borrow = _financeContract.ExecuteMethodWithResult(FinanceMethod.Borrow, new MintInput
            {
                Symbol = symbol,
                Amount = amount
            });
            borrow.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            borrow.Error.ShouldContain("borrow is paused");
        }
        
        [TestMethod]
        public void SetSeizePaused()
        {
            var symbol = "TEST";
            _financeContract.SetAccount(TestAccount);
            var result = _financeContract.ExecuteMethodWithResult(FinanceMethod.SetSeizePaused, new SetPausedInput{Symbol = symbol,State = true});
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var returnValue = BoolValue.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            returnValue.Value.ShouldBeTrue();
            
            var borrower = Tester[3];
            var collatera = "TEST";
            var amount = 1_00000;
            var tester = TestAccount;
            
            _sideTokenContract.ApproveToken(tester, _financeContract.ContractAddress, 10000_000000000, symbol);
            var liquidateBorrow =
                _financeContract.ExecuteMethodWithResult(FinanceMethod.LiquidateBorrow, new LiquidateBorrowInput
                {
                    Borrower = borrower.ConvertAddress(),
                    BorrowSymbol = symbol,
                    CollateralSymbol = collatera,
                    RepayAmount = amount
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            liquidateBorrow.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            liquidateBorrow.Error.ShouldContain("liquidateBorrow is paused");
        }

        //Tester[0] 10000_00000000 "TEST" borrow limit: 10000*0.5*0.75 = 3750ELF
        //Tester[1] 10000_00000 "FULL" borrow limit: 10000*0.01*0.75 = 7500ELF
        //Tester[2] 10000_000000000 "FINA" borrow limit: 10000*0.2*0.75 = 1.5ELF
        //Tester[3] 10000_000 "AEUSD" borrow limit: 10000*10*0.75 = 0.75ELF
        //Tester[4] 10000_00000000 "CPU" borrow limit: 10000*2*0.75 = 15000ELF；
        //Tester[5] 10000_00000000 "ELF" borrow limit: 10000*1*0.75 = 7500ELF
        [TestMethod]
        [DataRow("ELF","1")]
//        [DataRow("CPU","2")]
//        [DataRow("AEUSD","1000000")]
        [DataRow("FINA","0.01")]
//        [DataRow("FULL","200")]
        [DataRow("TEST","1.5")]
//        [DataRow("ABCB","0.001")]
        public void SetUnderlyingPriceTest(string symbol,string price)
        {
            _financeContract.SetAccount(InitAccount,Password);
            var result =
                _financeContract.ExecuteMethodWithResult(FinanceMethod.SetUnderlyingPrice, new SetUnderlyingPriceInput
                {
                    Symbol = symbol,
                    Price = price
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var priceInfo = PricePosted.Parser.ParseFrom(
                ByteString.FromBase64(result.Logs.First(n => n.Name.Contains(nameof(PricePosted))).NonIndexed));
            priceInfo.Symbol.ShouldBe(symbol);
//            var tokenInfo = _sideTokenContract.GetTokenInfo(symbol);
//            var actualPrice = decimal.Parse(price)*100000000/Pow(10,(uint)tokenInfo.Decimals);
            priceInfo.NewPrice.ShouldBe(price.ToString(CultureInfo.InvariantCulture)); 
            var getPrice =
                _financeContract.CallViewMethod<StringValue>(FinanceMethod.GetUnderlyingPrice,
                    new StringValue {Value = symbol});
            getPrice.Value.ShouldBe(price);
            Logger.Info($"{symbol} input price {price}");
        }

        [TestMethod]
        public void Check()
        {
            var logs = "CgRGSU5BEggxMDAwMDAwMBoDMC4x";
            var price = PricePosted.Parser.ParseFrom(
                ByteString.FromBase64(logs));
            Logger.Info($"{price.Symbol} {price.NewPrice} {price.PreviousPrice}");

        }

        [TestMethod]
        public void GetUnderlyingPrice()
        {
            var symbols = new List<string> {"ELF", "CPU", "TEST", "FINA", "AEUSD", "FULL"};
//            var symbols = new List<string> {"ELF", "TEST"};

            foreach (var symbol in symbols)
            {
                var getPrice =
                    _financeContract.CallViewMethod<StringValue>(FinanceMethod.GetUnderlyingPrice,
                        new StringValue {Value = symbol});
                Logger.Info($"{symbol}: {getPrice.Value}");
            }
        }

        [TestMethod]
        public void AddReservesTest()
        {
            var amount = 100_000000000;
            var symbol = "FINA";
            var tester = TestAccount;
            var cashBefore =
                _financeContract.CallViewMethod<Int64Value>(FinanceMethod.GetCash, new StringValue {Value = symbol});

            _sideTokenContract.ApproveToken(tester, _financeContract.ContractAddress, amount, symbol);
            _financeContract.SetAccount(tester);
            var result = _financeContract.ExecuteMethodWithResult(FinanceMethod.AddReserves, new AddReservesInput
            {
                Symbol = symbol,
                Amount = amount
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var cash = _financeContract.CallViewMethod<Int64Value>(FinanceMethod.GetCash,
                new StringValue {Value = symbol});
            cash.Value.ShouldBe(cashBefore.Value + amount);
            Logger.Info($"{symbol} {cash.Value}");
        }
        
        [TestMethod]
        public void ReduceReservesTest()
        {
            var amount = 100_000000000;
            var symbol = "FINA";
            var tester = InitAccount;
            var cashBefore =
                _financeContract.CallViewMethod<Int64Value>(FinanceMethod.GetCash, new StringValue {Value = symbol});

            _sideTokenContract.ApproveToken(tester, _financeContract.ContractAddress, amount, symbol);
            _financeContract.SetAccount(tester);
            var result = _financeContract.ExecuteMethodWithResult(FinanceMethod.ReduceReserves, new AddReservesInput
            {
                Symbol = symbol,
                Amount = amount
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var cash = _financeContract.CallViewMethod<Int64Value>(FinanceMethod.GetCash,
                new StringValue {Value = symbol});
            cash.Value.ShouldBe(cashBefore.Value - amount);
            Logger.Info($"{symbol} {cash.Value}");
        }

//BaseRatePerBlock:0.0000000000000000001, MultiplierPerBlock:0.0000000000000000002
//ReserveFactor: 0.05 BaseRatePerBlock:0.00000000000001, MultiplierPerBlock:0.000000000000002
   // TEST: 1 TEST -> 1000 cTEST Price: 0.5ELF
        // ReserveFactor: 0.05 BaseRatePerBlock:00000000005, MultiplierPerBlock:0000000008
        
        //FINA: 1 FINA -> 5 cFINA Price: 0.2ELF
        //ReserveFactor: 0.05 BaseRatePerBlock:0.000000001, MultiplierPerBlock:0.000000005
        [TestMethod]
        public void SetInterestRate()
        {
            var symbol = "FINA";
            var baseRatePerBlock = "0.000000001";
            var multiplierPerBlock = "0.000000005";
            var interestRate = _financeContract.CallViewMethod<GetInterestRateOutput>(FinanceMethod.GetInterestRate,
                new StringValue {Value = symbol});
            var result =
                _financeContract.ExecuteMethodWithResult(FinanceMethod.SetInterestRate, new SetInterestRateInput
                {
                    BaseRatePerBlock = baseRatePerBlock,
                    MultiplierPerBlock = multiplierPerBlock,
                    Symbol = symbol
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var info = InterestRateChanged.Parser.ParseFrom(
                ByteString.FromBase64(result.Logs.First(n => n.Name.Contains(nameof(InterestRateChanged))).NonIndexed));
            info.Symbol.ShouldBe(symbol);
            info.MultiplierPerBlock.ShouldBe(multiplierPerBlock);
            info.BaseRatePerBlock.ShouldBe(baseRatePerBlock);
            interestRate.MultiplierPerBlock.ShouldNotBe(multiplierPerBlock);
        }

        [TestMethod]
        public void SetReserveFactor()
        {
            var symbol = "TEST";
            var reserve = "0.05";
            var reserveFactor = _financeContract.CallViewMethod<StringValue>(FinanceMethod.GetReserveFactor,
                new StringValue {Value = symbol});
            var result =
                _financeContract.ExecuteMethodWithResult(FinanceMethod.SetReserveFactor, new SetReserveFactorInput
                {
                    Symbol = symbol,
                    ReserveFactor = reserve
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var info = ReserveFactorChanged.Parser.ParseFrom(
                ByteString.FromBase64(result.Logs.First(n => n.Name.Contains(nameof(ReserveFactorChanged)))
                    .NonIndexed));
            info.Symbol.ShouldBe(symbol);
            info.NewReserveFactor.ShouldBe(reserve);
            info.OldReserveFactor.ShouldBe(reserveFactor.Value);
        }
        
        [TestMethod]
        public void SetCollateralFactor()
        {
            var symbol = "CPU";
            var collateral = "0.8";
            var collateralFactor = _financeContract.CallViewMethod<StringValue>(FinanceMethod.GetCollateralFactor,
                new StringValue {Value = symbol});
            var result =
                _financeContract.ExecuteMethodWithResult(FinanceMethod.SetCollateralFactor, new SetCollateralFactorInput
                {
                    Symbol = symbol,
                    CollateralFactor = collateral
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var info = CollateralFactorChanged.Parser.ParseFrom(
                ByteString.FromBase64(result.Logs.First(n => n.Name.Contains(nameof(CollateralFactorChanged)))
                    .NonIndexed));
            info.Symbol.ShouldBe(symbol);
            info.NewCollateralFactor.ShouldBe(collateral);
            info.OldCollateralFactor.ShouldBe(collateralFactor.Value);
        }
        
        [TestMethod]
        public void SetLiquidationIncentive()
        {
            var liquidationIncentive = "1.2";
            var liquidationIncentiveOrigin =
                _financeContract.CallViewMethod<StringValue>(FinanceMethod.GetLiquidationIncentive, new Empty());
            
            var result =
                _financeContract.ExecuteMethodWithResult(FinanceMethod.SetLiquidationIncentive, new StringValue
                {
                    Value = liquidationIncentive
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var info = LiquidationIncentiveChanged.Parser.ParseFrom(
                ByteString.FromBase64(result.Logs.First(n => n.Name.Contains(nameof(LiquidationIncentiveChanged)))
                    .NonIndexed));
            Logger.Info(info);
            info.OldLiquidationIncentive.ShouldBe(liquidationIncentiveOrigin.Value);
            info.NewLiquidationIncentive.ShouldBe(liquidationIncentive);
            var liquidationIncentiveAfter =
                _financeContract.CallViewMethod<StringValue>(FinanceMethod.GetLiquidationIncentive, new Empty());
            liquidationIncentiveAfter.Value.ShouldBe(liquidationIncentive);
        }
        
        [TestMethod]
        public void SetMaxAssets()
        {
            var newValue = 4;
            var maxAssets = _financeContract.CallViewMethod<Int32Value>(FinanceMethod.GetMaxAssets, new Empty());
            var result =
                _financeContract.ExecuteMethodWithResult(FinanceMethod.SetMaxAssets, new Int32Value {Value = newValue});
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var assetsChanged = MaxAssetsChanged.Parser.ParseFrom(
                ByteString.FromBase64(result.Logs.First(n => n.Name.Contains(nameof(MaxAssetsChanged))).NonIndexed));
            assetsChanged.OldMaxAssets.ShouldBe(maxAssets.Value);
            assetsChanged.NewMaxAssets.ShouldBe(newValue);
        }

        [TestMethod]
        public async Task ChangeAdmin()
        {
//            AcceptAdmin
//            SetPendingAdmin
            var result = await _adminfinanceContractStub.SetPendingAdmin.SendAsync(TestAccount.ConvertAddress());
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var info = PendingAdminChanged.Parser
                .ParseFrom(result.TransactionResult.Logs.First(n => n.Name.Contains(nameof(PendingAdminChanged)))
                    .NonIndexed);
            var pendingAdmin = await _adminfinanceContractStub.GetPendingAdmin.CallAsync(new Empty());
            pendingAdmin.ShouldBe(InitAccount.ConvertAddress());
            pendingAdmin.ShouldBe(info.NewPendingAdmin);

            var acceptAdmin = await _financeContractStub.AcceptAdmin.SendAsync(new Empty());
            acceptAdmin.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var adminChanged = AdminChanged.Parser
                .ParseFrom(acceptAdmin.TransactionResult.Logs.First(n => n.Name.Contains(nameof(AdminChanged)))
                    .NonIndexed);
            adminChanged.NewAdmin.ShouldBe(TestAccount.ConvertAddress());
            adminChanged.OldAdmin.ShouldBe(InitAccount.ConvertAddress());
            var pendingAdminChanged = PendingAdminChanged.Parser
                .ParseFrom(acceptAdmin.TransactionResult.Logs.First(n => n.Name.Contains(nameof(PendingAdminChanged)))
                    .NonIndexed);
            pendingAdminChanged.OldPendingAdmin.ShouldBe(pendingAdmin);
            pendingAdminChanged.NewPendingAdmin.ShouldBe(new Address());

            var admin = await _adminfinanceContractStub.GetAdmin.CallAsync(new Empty());
            admin.ShouldBe(InitAccount.ConvertAddress());
            var pendingAdminAfter = await _financeContractStub.GetPendingAdmin.CallAsync(new Empty());
            pendingAdminAfter.ShouldBe(new Address());
        }

        [TestMethod]
        public async Task SetPendingAdminTest()
        {
            var result = await _adminfinanceContractStub.SetPendingAdmin.SendAsync(TestAccount.ConvertAddress());
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var info = PendingAdminChanged.Parser
                .ParseFrom(result.TransactionResult.Logs.First(n => n.Name.Contains(nameof(PendingAdminChanged)))
                    .NonIndexed);
            var pendingAdmin = await _adminfinanceContractStub.GetPendingAdmin.CallAsync(new Empty());
            pendingAdmin.ShouldBe(TestAccount.ConvertAddress());
            pendingAdmin.ShouldBe(info.NewPendingAdmin);
        }

        [TestMethod]
        public void GetAssetsInTest()
        {
            var tester = Tester[3];
            _financeContract.SetAccount(tester);
            var assetsIn =
                _financeContract.CallViewMethod<AssetList>(FinanceMethod.GetAssetsIn, tester.ConvertAddress());
            Logger.Info($"{assetsIn}");
        }

        [TestMethod]
        public void GetBorrowRatePerBlock()
        {
            var tester = TestAccount;
            var symbol = "FINA";
            _financeContract.SetAccount(tester);
            var result =
                _financeContract.CallViewMethod<StringValue>(FinanceMethod.GetBorrowRatePerBlock,
                    new StringValue {Value = symbol});
            Logger.Info($"{symbol} borrow rate :{result.Value}");

            var depositsAPY = decimal.Parse(result.Value) * 2 * 60 * 60 * 24 * 365;
            Logger.Info($"{symbol} borrow rate APY :{depositsAPY.ToString()}");
        }

        [TestMethod]
        public void GetSupplyRatePerBlock()
        {
            var tester = TestAccount;
            var symbol = "FINA";
            _financeContract.SetAccount(tester);
            var result =
                _financeContract.CallViewMethod<StringValue>(FinanceMethod.GetSupplyRatePerBlock,
                    new StringValue {Value = symbol});
            Logger.Info($"{symbol} mint rate :{result.Value}");
        }

        [TestMethod]
        public void InitializeContract()
        {
            _financeContract.SetAccount(InitAccount);
            var result = _financeContract.ExecuteMethodWithResult(FinanceMethod.Initialize, new InitializeInput
            {
                LiquidationIncentive = "1.2",
                CloseFactor = "0.1",
                MaxAssets = 10
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void InitializeContract_ErrorTest()
        {
            {
                var result = _financeContract.ExecuteMethodWithResult(FinanceMethod.Initialize, new InitializeInput
                {
                    LiquidationIncentive = "1.5",
                    CloseFactor = "0.15",
                    MaxAssets = 5
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Invalid LiquidationIncentive");
            }
            {
                var result = _financeContract.ExecuteMethodWithResult(FinanceMethod.Initialize, new InitializeInput
                {
                    LiquidationIncentive = "1.3",
                    CloseFactor = "1",
                    MaxAssets = 5
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Invalid CloseFactor");
            }
            {
                var result = _financeContract.ExecuteMethodWithResult(FinanceMethod.Initialize, new InitializeInput
                {
                    LiquidationIncentive = "1.3",
                    CloseFactor = "0.89",
                    MaxAssets = 0
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("MaxAssets must greater than 0");
            }
            {
                _financeContract.SetAccount(TestAccount);
                var result = _financeContract.ExecuteMethodWithResult(FinanceMethod.Initialize, new InitializeInput
                {
                    LiquidationIncentive = "1.49",
                    CloseFactor = "0.89",
                    MaxAssets = 1
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("only admin may initialize the market");
            }
        }

        [TestMethod]
        public void GetInitializeContractInfo()
        {
            var closeFactor = _financeContract.CallViewMethod<StringValue>(FinanceMethod.GetCloseFactor, new Empty());
            var maxAssets = _financeContract.CallViewMethod<Int32Value>(FinanceMethod.GetMaxAssets, new Empty());
            var liquidationIncentive =
                _financeContract.CallViewMethod<StringValue>(FinanceMethod.GetLiquidationIncentive, new Empty());
            Logger.Info(
                $"CloseFactor: {closeFactor}, MaxAssets: {maxAssets}, LiquidationIncentive: {liquidationIncentive}");
        }

        
//        sumCollateral ：cTokenBalance * exchangeRate * collateralFactor * price // 用户所有的抵押token的数量乘以对应的价格然后加起来
//        sumBorrow：borrowBalance * price // 单个用户所有借贷的token乘以对应的价格然后加起来
        [TestMethod]
        public void GetHypotheticalAccountLiquidityInternal()
        {
            var tester = Tester[2].ConvertAddress();
//            var account = "YF8o6ytMB7n5VF9d1RDioDXqyQ9EQjkFK3AwLPCH2b9LxdTEq";
//            var tester = account.ConvertAddress();
            var cToken = "CPU";
            var redeemTokens = 1000_00000000;
            var borrowAmount = 0;
//            private long GetHypotheticalAccountLiquidityInternal(Address address, string cToken, long redeemTokens,
//                long borrowAmount)
            {
                var assets = _financeContract.CallViewMethod<AssetList>(FinanceMethod.GetAssetsIn, tester);
                decimal sumCollateral = 0;
                decimal sumBorrowPlusEffects = 0;
                for (int i = 0; i < assets.Assets.Count; i++)
                {
                    var symbol = assets.Assets[i];
                    var tokenInfo = _sideTokenContract.GetTokenInfo(symbol);
                    // Read the balances and exchange rate from the cToken
                    var accountSnapshot = _financeContract.CallViewMethod<GetAccountSnapshotOutput>(
                        FinanceMethod.GetAccountSnapshot,
                        new Account {Address = tester, Symbol = symbol});
                    var cTokenBalance = accountSnapshot.CTokenBalance;
                    var exchangeRate = decimal.Parse(accountSnapshot.ExchangeRate);
                    var priceInfo = _financeContract.CallViewMethod<StringValue>(FinanceMethod.GetUnderlyingPrice,
                        new StringValue {Value = symbol});
                    
                    var price = decimal.Parse(priceInfo.Value)*100000000/Pow(10,(uint)tokenInfo.Decimals);
                    var collateralFactorInfo = _financeContract.CallViewMethod<StringValue>(
                        FinanceMethod.GetCollateralFactor,
                        new StringValue {Value = symbol});
                    var collateralFactor = decimal.Parse(collateralFactorInfo.Value);
                    var tokensToDenom = exchangeRate * price * collateralFactor;
                    sumCollateral += cTokenBalance * tokensToDenom;
                    sumBorrowPlusEffects += accountSnapshot.BorrowBalance * price;
                    if (symbol == cToken)
                    {
                        // redeem effect
                        // sumBorrowPlusEffects += tokensToDenom * redeemTokens
                        sumBorrowPlusEffects += tokensToDenom * redeemTokens;
                        // borrow effect
                        // sumBorrowPlusEffects += oraclePrice * borrowAmount
                        sumBorrowPlusEffects += price * borrowAmount;
                    }
                }

                var result = decimal.ToInt64(sumBorrowPlusEffects - sumCollateral);
                Logger.Info($"{result}: {sumBorrowPlusEffects} - {sumCollateral}");
            }
        }

        [TestMethod]
        public void LiquidateCalculateSeizeTokens()
        {
            var borrowSymbol = "TEST";
            var collateralSymbol = "FINA";
            var repayAmount = 10000000;

            var priceBorrowInfo = _financeContract.CallViewMethod<StringValue>(FinanceMethod.GetUnderlyingPrice,
                new StringValue {Value = borrowSymbol});
            var priceBorrow = decimal.Parse(priceBorrowInfo.Value);
            var priceCollateralInfo = _financeContract.CallViewMethod<StringValue>(FinanceMethod.GetUnderlyingPrice,
                new StringValue {Value = collateralSymbol});
            var priceCollateral = decimal.Parse(priceCollateralInfo.Value);

            var exchangeRateInfo = _financeContract.CallViewMethod<StringValue>(FinanceMethod.GetCurrentExchangeRate,
                new StringValue {Value = collateralSymbol});
            var exchangeRate = decimal.Parse(exchangeRateInfo.Value);
            //Get the exchange rate and calculate the number of collateral tokens to seize:
            // *  seizeAmount = actualRepayAmount * liquidationIncentive * priceBorrowed / priceCollateral
            //    seizeTokens = seizeAmount / exchangeRate
            //   = actualRepayAmount * (liquidationIncentive * priceBorrowed) / (priceCollateral * exchangeRate)
            var liquidationIncentiveInfo =
                _financeContract.CallViewMethod<StringValue>(FinanceMethod.GetLiquidationIncentive, new Empty());

            var seizeAmount = repayAmount * decimal.Parse(liquidationIncentiveInfo.Value) *
                              priceBorrow / priceCollateral;
            var seizeTokens = decimal.ToInt64(seizeAmount / exchangeRate);
            Logger.Info(seizeTokens);
        }

        [TestMethod]
        public void GetRate()
        {
            var symbol = "FINA";
            var totalCash = _financeContract.CallViewMethod<Int64Value>(FinanceMethod.GetCash,
                new StringValue {Value = symbol});
            var totalBorrow = _financeContract.CallViewMethod<Int64Value>(FinanceMethod.GetTotalBorrows,
                new StringValue {Value = symbol});
            var totalReserves = _financeContract.CallViewMethod<Int64Value>(FinanceMethod.GetTotalReserves,
                new StringValue {Value = symbol});
            var denominator = totalCash.Value.Add(totalBorrow.Value).Sub(totalReserves.Value);
            if (denominator == 0) return;
            // utilizationRate = totalBorrows/(totalCash + totalBorrows - totalReserves)
            var utilizationRate = Convert.ToDecimal(totalBorrow.Value) / denominator;
            Logger.Info($"{symbol} UtilizationRate: {utilizationRate}");

            var interestRate = _financeContract.CallViewMethod<GetInterestRateOutput>(FinanceMethod.GetInterestRate,
                new StringValue {Value = symbol});
            var multiplierPerBlock = decimal.Parse(interestRate.MultiplierPerBlock);
            var baseRatePerBlock = decimal.Parse(interestRate.BaseRatePerBlock);
            var borrowRate = utilizationRate * multiplierPerBlock + baseRatePerBlock;
            Logger.Info($"{symbol} BorrowRate: {borrowRate}");
            
            var borrowRatePerBlock =
                _financeContract.CallViewMethod<StringValue>(FinanceMethod.GetBorrowRatePerBlock,
                    new StringValue {Value = symbol});
            Logger.Info($"{symbol} borrow rate :{borrowRatePerBlock.Value}");

            var depositsAPY = decimal.Parse(borrowRatePerBlock.Value) * 2 * 60 *60* 24 * 365;
            Logger.Info($"{symbol} borrow rate APY :{depositsAPY.ToString(CultureInfo.InvariantCulture)}");

            var reserveFactor =
                _financeContract.CallViewMethod<StringValue>(FinanceMethod.GetReserveFactor,
                    new StringValue {Value = symbol});
            var rateToPool = borrowRate - borrowRate * decimal.Parse(reserveFactor.Value);
            var supplyRate = utilizationRate * rateToPool;
            Logger.Info($"{symbol} rate :{supplyRate}");
            
            var supplyRatePerBlock =
                _financeContract.CallViewMethod<StringValue>(FinanceMethod.GetSupplyRatePerBlock,
                    new StringValue {Value = symbol});
            Logger.Info($"{symbol} mint rate :{supplyRatePerBlock.Value}");
            var supplyRateAPY = decimal.Parse(supplyRatePerBlock.Value) * 2 * 60 *60* 24 * 365;
            Logger.Info($"{symbol} mint rate APY :{supplyRateAPY.ToString(CultureInfo.InvariantCulture)}");
        }

        [TestMethod]
        public void NetInterest()
        {
            //当前存款和借款余额的净利率
        }

        [TestMethod]
        public void CheckBalance()
        {
            var symbols = new List<string> {"ELF", "CPU", "TEST", "FINA", "AEUSD", "FULL"};
            foreach (var symbol in symbols)
            {
                foreach (var tester in Tester)
                {
                    var balance = _sideTokenContract.GetUserBalance(tester, symbol);
                    Logger.Info($"{tester},{symbol} balance:{balance}");
                    var cBalance = _financeContract.CallViewMethod<Int64Value>(FinanceMethod.GetBalance, new Account
                    {
                        Symbol = symbol,
                        Address = tester.ConvertAddress()
                    });
                    Logger.Info($"{tester},{symbol} cToken balance:{cBalance.Value}");
                }

                var initBalance = _sideTokenContract.GetUserBalance(InitAccount, symbol);
                Logger.Info($"{InitAccount},{symbol} balance:{initBalance}");
                var initCBalance = _financeContract.CallViewMethod<Int64Value>(FinanceMethod.GetBalance, new Account
                {
                    Symbol = symbol,
                    Address = InitAccount.ConvertAddress()
                });
                Logger.Info($"{InitAccount},{symbol} cToken balance:{initCBalance.Value}");

                var testBalance = _sideTokenContract.GetUserBalance(TestAccount, symbol);
                Logger.Info($"{TestAccount},{symbol} balance:{testBalance}");
                var testCBalance = _financeContract.CallViewMethod<Int64Value>(FinanceMethod.GetBalance, new Account
                {
                    Symbol = symbol,
                    Address = TestAccount.ConvertAddress()
                });
                Logger.Info($"{TestAccount},{symbol} cToken balance:{testCBalance.Value}");
            }
        }

        [TestMethod]
        public void CheckOneBalance()
        {
//            var tester = Tester[0];
            var tester = TestAccount;
//            var tester = "JVJBMs2qH6fShN7f1XPUu6NJzrhSsPvzQ47pHnQ9SAKipk1gU";

//            var symbols = new List<string> {"ELF", "CPU", "TEST", "FINA", "AEUSD", "FULL"};
            var symbols = new List<string> {"ELF", "TEST","FINA"};
//            _sideTokenContract.TransferBalance(TestAccount, tester, 1000_000000000, "TEST");
//            _sideTokenContract.TransferBalance(TestAccount, tester, 100_00000000, "ELF");

            foreach (var symbol in symbols)
            {
                var balance = _sideTokenContract.GetUserBalance(tester, symbol);
                Logger.Info($"{tester},{symbol} balance:{balance}");
                var cBalance = _financeContract.CallViewMethod<Int64Value>(FinanceMethod.GetBalance, new Account
                {
                    Symbol = symbol,
                    Address = tester.ConvertAddress()
                });
                Logger.Info($"{tester},{symbol} cToken balance:{cBalance.Value}");

                var borrowBalance = _financeContract.CallViewMethod<Int64Value>(FinanceMethod.GetCurrentBorrowBalance,
                    new Account
                    {
                        Symbol = symbol,
                        Address = tester.ConvertAddress()
                    });
                Logger.Info($"{tester},{symbol} borrow balance:{borrowBalance.Value}");

                var borrowBalanceStored = _financeContract.CallViewMethod<Int64Value>(
                    FinanceMethod.GetBorrowBalanceStored,
                    new Account
                    {
                        Symbol = symbol,
                        Address = tester.ConvertAddress()
                    });
                Logger.Info($"{tester},{symbol} borrow balance stored:{borrowBalanceStored.Value}");
                var accountInfo =
                    _financeContract.CallViewMethod<GetAccountSnapshotOutput>(FinanceMethod.GetAccountSnapshot,
                        new Account {Address = tester.ConvertAddress(), Symbol = symbol});
                Logger.Info($"{tester},{symbol} borrow balance:{accountInfo.BorrowBalance}");
            }
        }

        [TestMethod]
        public void GetCash()
        {
//            var symbols = new List<string> {"ELF", "CPU", "TEST", "FINA", "AEUSD", "FULL"};
            var symbols = new List<string> {"ELF","FINA","TEST"};

            foreach (var symbol in symbols)
            {
                var balance =
                    _financeContract.CallViewMethod<Int64Value>(FinanceMethod.GetCash,
                        new StringValue {Value = symbol});
                Logger.Info($"{symbol} cash:{balance.Value}");
                var borrowBalance = _financeContract.CallViewMethod<Int64Value>(FinanceMethod.GetTotalBorrows,
                    new StringValue {Value = symbol});
                Logger.Info($"{symbol} borrow balance: {borrowBalance.Value}");
                var reserves = _financeContract.CallViewMethod<Int64Value>(FinanceMethod.GetTotalReserves,
                    new StringValue {Value = symbol});
                Logger.Info($"{symbol} reserves balance: {reserves.Value}");
                var rate = _financeContract.CallViewMethod<StringValue>(FinanceMethod.GetCurrentExchangeRate,
                    new StringValue {Value = symbol});
                Logger.Info($"{symbol} exchange rate: {rate.Value}");
                var getPrice =
                    _financeContract.CallViewMethod<StringValue>(FinanceMethod.GetUnderlyingPrice,
                        new StringValue {Value = symbol});
                Logger.Info($"{symbol} price : {getPrice.Value}ELF");
                var collateralFactor =
                    _financeContract.CallViewMethod<StringValue>(FinanceMethod.GetCollateralFactor, new StringValue{Value = symbol});
                Logger.Info($"{symbol} collateral factor : {collateralFactor.Value}");

            }
        }

        private void CreateTokenAndIssue()
        {
            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
            {
                Symbol = Symbol,
                TotalSupply = long.MaxValue,
                Decimals = 5,
                Issuer = InitAccount.ConvertAddress(),
                IsBurnable = true,
                TokenName = $"{Symbol} Token"
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _tokenContract.IssueBalance(InitAccount, InitAccount, 500000000_00000, Symbol);
            _tokenContract.IssueBalance(InitAccount, TestAccount, 50000_00000, Symbol);
        }

        private long CheckAccountInfo(string tester, string symbol)
        {
            var balance = _sideTokenContract.GetUserBalance(tester, symbol);
            Logger.Info($"{tester},{symbol} balance:{balance}");
            var cBalance = _financeContract.CallViewMethod<Int64Value>(FinanceMethod.GetBalance, new Account
            {
                Symbol = symbol,
                Address = tester.ConvertAddress()
            });
            Logger.Info($"{tester},{symbol} cToken balance:{cBalance.Value}");

            var borrowBalance = _financeContract.CallViewMethod<Int64Value>(FinanceMethod.GetCurrentBorrowBalance,
                new Account
                {
                    Symbol = symbol,
                    Address = tester.ConvertAddress()
                });
            Logger.Info($"{tester},{symbol} borrow balance:{borrowBalance.Value}");

            var borrowBalanceStored = _financeContract.CallViewMethod<Int64Value>(FinanceMethod.GetBorrowBalanceStored,
                new Account
                {
                    Symbol = symbol,
                    Address = tester.ConvertAddress()
                });
            Logger.Info($"{tester},{symbol} borrow balance stored:{borrowBalanceStored.Value}");
            var accountInfo =
                _financeContract.CallViewMethod<GetAccountSnapshotOutput>(FinanceMethod.GetAccountSnapshot,
                    new Account {Address = tester.ConvertAddress(), Symbol = symbol});
            Logger.Info($"{tester},{symbol} borrow balance:{accountInfo.BorrowBalance}");

            return borrowBalance.Value;
        }

        private void CheckCash(string symbol)
        {
            var balance =
                _financeContract.CallViewMethod<Int64Value>(FinanceMethod.GetCash,
                    new StringValue {Value = symbol});
            Logger.Info($"{symbol} cash:{balance}");
            var borrowBalance = _financeContract.CallViewMethod<Int64Value>(FinanceMethod.GetTotalBorrows,
                new StringValue {Value = symbol});
            Logger.Info($"{symbol} borrow balance: {borrowBalance.Value}");
            var reserves = _financeContract.CallViewMethod<Int64Value>(FinanceMethod.GetTotalReserves,
                new StringValue {Value = symbol});
            Logger.Info($"{symbol} reserves balance: {reserves.Value}");
            var rate = _financeContract.CallViewMethod<StringValue>(FinanceMethod.GetCurrentExchangeRate,
                new StringValue {Value = symbol});
            Logger.Info($"{symbol} exchange rate: {rate.Value}");
        }
        
        private static long Pow(int x, uint y)
        {
            if (y == 1)
                return x;
            long a = 1;
            if (y == 0)
                return a;
            var e = new BitArray(y.ToBytes(false));
            var t = e.Count;
            for (var i = t - 1; i >= 0; --i)
            {
                a *= a;
                if (e[i])
                {
                    a *= x;
                }
            }

            return a;
        }
    }
}