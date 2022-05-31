using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Price;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Awaken.Contracts.AToken;
using Awaken.Contracts.Controller;
using Awaken.Contracts.InterestRateModel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using CloseFactorChanged = Awaken.Contracts.Controller.CloseFactorChanged;
using CollateralFactorChanged = Awaken.Contracts.Controller.CollateralFactorChanged;
using InitializeInput = Awaken.Contracts.AToken.InitializeInput;
using LiquidationIncentiveChanged = Awaken.Contracts.Controller.LiquidationIncentiveChanged;
using MarketListed = Awaken.Contracts.Controller.MarketListed;
using MaxAssetsChanged = Awaken.Contracts.Controller.MaxAssetsChanged;
using TokenInfo = AElf.Contracts.MultiToken.TokenInfo;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class AwakenFinanceTest
    {
        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;
        private AwakenFinanceATokenContract _awakenATokenContract;
        private AwakenFinanceControllerContract _awakenFinanceControllerContract;
        private AwakenFinanceInterestRateModelContract _awakenFinanceInterestRateModelContract;
        private AwakenFinanceInterestRateModelContract _awakenFinanceWhiteInterestRateModelContract;
        private AwakenFinanceLendingLensContract _awakenFinanceLendingLensContract;
        private AwakenTestPriceContract _testPriceContract;

        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }

        // private string tokenAddress = "2gCotL1p6Q4DveEPzpVMuFwfjj3nmitT4d1widWPms6GDzHEse";
        private string aTokenAddress = "";
        private string controllerAddress = "";
        private string jumpInterestRateModelAddress = "";
        private string whiteInterestRateModelAddress = "";
        private string lendingLensAddress = "";
        private string testPriceAddress = "";

        private string InitAccount { get; } = "nn659b9X1BLhnu5RWmEUbuuV7J9QKVVSN54j9UmeCbF3Dve5D";
        private string TestAccount { get; } = "Zz4iuCCCktGjZGmQ9vMcxh2JT9pDTFA4XSR7WDNrndYcEXtRx";
        private string TestAccount2 { get; } = "2asoy1ZbqcxguA2TBaMt5V1nFpfsE4bAdCUrG1YK9CCXnTCJKN";
        private string TestAccount3 { get; } = "1Fy4ar9CjDuwQTbde3syqGzY5Wzfvoj4uwcJPNtWMCNS6USYS";

        private string FeeToAccount { get; } = "Zz4iuCCCktGjZGmQ9vMcxh2JT9pDTFA4XSR7WDNrndYcEXtRx";
        private static string RpcUrl { get; } = "http://192.168.66.9:8000";

        private long _baseRatePerYear = 0;
        private long _multiplierPerYear = 57500000000000000;
        
        private long _baseRatePerYearWhite = 23000000000000000; // 0.023
        private long _whiteMultiplierPerYear = 283750000000000000; // 283750000000000000 115000000000000000
        private long _jumpMultiplierPerYear = 3000000000000000000;
        private long _kink = 800000000000000000; //0.8
        private const long Mantissa = 1_000000000000000000;
        private const long ExchangeMantissa = 1_00000000;
        private const long PriceMantissa = 1000000000000000000;
        private string _platformTokenSymbol = "AWKN";

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("AwakenFinanceTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes");

            NodeManager = new NodeManager(RpcUrl);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
            _awakenATokenContract = aTokenAddress == ""
                ? new AwakenFinanceATokenContract(NodeManager, InitAccount)
                : new AwakenFinanceATokenContract(NodeManager, aTokenAddress, InitAccount);
            _awakenFinanceControllerContract = controllerAddress == ""
                ? new AwakenFinanceControllerContract(NodeManager, InitAccount)
                : new AwakenFinanceControllerContract(NodeManager, controllerAddress, InitAccount);
            _awakenFinanceInterestRateModelContract = jumpInterestRateModelAddress == ""
                ? new AwakenFinanceInterestRateModelContract(NodeManager, InitAccount)
                : new AwakenFinanceInterestRateModelContract(NodeManager, jumpInterestRateModelAddress, InitAccount);
            _awakenFinanceWhiteInterestRateModelContract = whiteInterestRateModelAddress == ""
                ? new AwakenFinanceInterestRateModelContract(NodeManager, InitAccount)
                : new AwakenFinanceInterestRateModelContract(NodeManager, whiteInterestRateModelAddress, InitAccount);
            _awakenFinanceLendingLensContract = lendingLensAddress == ""
                ? new AwakenFinanceLendingLensContract(NodeManager, InitAccount)
                : new AwakenFinanceLendingLensContract(NodeManager, lendingLensAddress, InitAccount);
            _testPriceContract = testPriceAddress == ""
                ? new AwakenTestPriceContract(NodeManager, InitAccount)
                : new AwakenTestPriceContract(NodeManager, testPriceAddress, InitAccount);
        }

        [TestMethod]
        public void InitializeTest()
        {
            CreateToken(_platformTokenSymbol, 8, InitAccount);
            InitializeAToken();
            InitializeLendingLens();
            InitializeInterestRateMode();
            InitializeWhiteInterestRateMode();
            InitializeController();
            SetPriceOracle();
            // CreateTestToken();
        }

        [TestMethod]
        public void CreateTestToken()
        {
            CreateToken("MMM", 8, InitAccount);
            CreateToken("USDT", 6, InitAccount);
            CreateToken("TEST", 8, InitAccount);
            CreateToken("LLL", 10, InitAccount);
        }

        [TestMethod]
        [DataRow("SIDETOKEN", 1_0000000000000000)] // 0.01
        [DataRow("USDTE", 5_0000000000000000)] // 0.05
        [DataRow("ETHTE", 2_000000000000000000)] // 2
        [DataRow("BTETE", 1_000000000000000000)] // 1
        [DataRow("ELF", 1_00000000000000000)] // 0.1
        public void SetPrice(string symbol, long price)
        {
            var setPrice = _testPriceContract.ExecuteMethodWithResult(
                PriceMethod.SetPrice, new SetPriceInput
                {
                    TokenSymbol = symbol,
                    Price = price
                });
            setPrice.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var getPrice = _testPriceContract.GetExchangeTokenPriceInfo(symbol);
            Logger.Info(getPrice);
        }

        #region User Action

        [TestMethod]
        public void EnterMarkets()
        {
            var listToken = new List<string> { "ELF", "USDTE", "BTETE", "ETHTE" };
            var listAToken = new List<Address>();
            foreach (var aToken in listToken.Select(t => _awakenATokenContract.GetATokenAddress(t)))
            {
                Logger.Info(aToken);
                listAToken.Add(aToken);
            }

            var user = TestAccount;
            _awakenFinanceControllerContract.SetAccount(user);
            var enterResult =
                _awakenFinanceControllerContract.ExecuteMethodWithResult(
                    ControllerMethod.EnterMarkets, new ATokens
                    {
                        AToken = { listAToken }
                    });
            enterResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logs = enterResult.Logs.Where(l => l.Name.Equals(nameof(MarketEntered))).ToList();
            foreach (var marketEntered in logs.Select(l =>
                         MarketEntered.Parser.ParseFrom(ByteString.FromBase64(l.NonIndexed))))
            {
                marketEntered.Account.ShouldBe(user.ConvertAddress());
                listAToken.ShouldContain(marketEntered.AToken);
            }

            var checkAccountAssets = _awakenFinanceControllerContract.GetAssetsIn(user.ConvertAddress());
            foreach (var a in listAToken)
                checkAccountAssets.Assets.ShouldContain(a);

            foreach (var checkMarket in listAToken.Select(aToken => _awakenFinanceControllerContract.GetMarket(aToken)))
                Logger.Info(checkMarket);
        }

        [TestMethod]
        [DataRow("USDT")]
        public void ExitMarkets(string symbol)
        {
            var aToken = _awakenATokenContract.GetATokenAddress(symbol);
            var user = TestAccount;
            _awakenFinanceControllerContract.SetAccount(user);
            var exitResult =
                _awakenFinanceControllerContract.ExecuteMethodWithResult(
                    ControllerMethod.ExitMarket, aToken);
            exitResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var logs = exitResult.Logs.IsNullOrEmpty()
                ? null
                : exitResult.Logs.First(l => l.Name.Equals(nameof(MarketExited))).NonIndexed;
            if (logs != null)
            {
                var marketExited = MarketExited.Parser.ParseFrom(ByteString.FromBase64(logs));
                marketExited.Account.ShouldBe(user.ConvertAddress());
                marketExited.AToken.ShouldBe(aToken);
            }

            var checkAccountAssets = _awakenFinanceControllerContract.GetAssetsIn(user.ConvertAddress());
            checkAccountAssets.Assets.ShouldNotContain(aToken);
            var checkMarket = _awakenFinanceControllerContract.GetMarket(aToken);
            checkMarket.AccountMembership.Keys.ShouldNotContain(user);
            Logger.Info(checkAccountAssets);
            Logger.Info(checkMarket);
        }

        [TestMethod]
        [DataRow("ELF", 100_00000000)]
        public void MintToken(string mintToken, long amount)
        {
            var user = TestAccount;
            CheckBalance(mintToken, user, amount);
            var aToken = _awakenATokenContract.GetATokenAddress(mintToken);
            _awakenATokenContract.SetAccount(user);
            ApproveToken(mintToken, user, amount);
            var origin = Verify(user, mintToken, aToken);
            var exchangeRate = CalculateExchangeRate(aToken);
            var getCurrentExchangeRate = _awakenATokenContract.GetCurrentExchangeRate(aToken);
            // getCurrentExchangeRate.ShouldBe(exchangeRate);
            var accrualBlockNumbers = _awakenATokenContract.GetAccrualBlockNumber(aToken);
            var borrowIndex = _awakenATokenContract.GetBorrowIndex(aToken).Value;
            
            Logger.Info($"\nExchangeRate: {exchangeRate}\n" +
                        $"CurrentExchangeRate: {getCurrentExchangeRate}");
            var userMint = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.Mint, new MintInput
            {
                AToken = aToken,
                MintAmount = amount,
                Channel = "channel"
            });
            userMint.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var userMintFee = userMint.GetDefaultTransactionFee();
            var logs = userMint.Logs.First(l => l.Name.Equals(nameof(Mint)));
            var mintLogs = Mint.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed));
            var interestLogs = AccrueInterest.Parser.ParseFrom(
                ByteString.FromBase64(userMint.Logs.First(l => l.Name.Equals(nameof(AccrueInterest))).NonIndexed));
            Logger.Info(mintLogs);
            Logger.Info(interestLogs);
            long userGetATokenAmount = 0;
            if (accrualBlockNumbers.Equals(0))
            {
                var mintTokensStr = new BigIntValue(ExchangeMantissa).Mul(amount).Div(exchangeRate).Value;
                userGetATokenAmount = long.Parse(mintTokensStr);
            }
            else
            {
                var amountInfo = CalculateInterest(aToken, userMint.BlockNumber, accrualBlockNumbers, origin, borrowIndex);
                var exchangeRateUpdate = CalculateExchangeRateUpdate(amountInfo["totalBorrows"],
                    amountInfo["totalReserves"], origin["totalCash"], origin["totalSupply"]);
                userGetATokenAmount = CalculateMintAmount(amount, exchangeRateUpdate);
                interestLogs.Cash.ShouldBe(origin["totalCash"]);
                interestLogs.AToken.ShouldBe(aToken);
                interestLogs.BorrowIndex.ShouldBe(amountInfo["newBorrowIndex"]);
                interestLogs.InterestAccumulated.ShouldBe(amountInfo["interestAccumulated"]);
                interestLogs.TotalBorrows.ShouldBe(amountInfo["totalBorrows"]);
                interestLogs.BorrowRatePerBlock.ShouldBe(amountInfo["borrowRatePerBlock"]);
                interestLogs.SupplyRatePerBlock.ShouldBe(amountInfo["supplyRatePerBlock"]);
            }
            
            var after = Verify(user, mintToken, aToken);
            origin["userBalance"].ShouldBe(mintToken.Equals("ELF")
                ? after["userBalance"].Add(amount).Add(userMintFee)
                : after["userBalance"].Add(amount));
            origin["contractBalance"].ShouldBe(after["contractBalance"].Sub(amount));
            origin["userATokenBalance"].ShouldBe(after["userATokenBalance"].Sub(userGetATokenAmount));
            origin["totalSupply"].ShouldBe(after["totalSupply"].Sub(userGetATokenAmount));
            origin["totalCash"].ShouldBe(after["totalCash"].Sub(amount));

            mintLogs.Sender.ShouldBe(user.ConvertAddress());
            mintLogs.UnderlyingAmount.ShouldBe(amount);
            mintLogs.AToken.ShouldBe(aToken);
            mintLogs.ATokenAmount.ShouldBe(userGetATokenAmount);
            mintLogs.Underlying.ShouldBe(mintToken);
            mintLogs.Channel.ShouldBe("channel");
        }

        //按AToken还款
        [TestMethod]
        [DataRow("ELF", 1000000000)]
        public void RedeemToken(string redeemToken, long redeemAmount)
        {
            var user = InitAccount;
            var aToken = _awakenATokenContract.GetATokenAddress(redeemToken);
            _awakenATokenContract.SetAccount(user);
            var origin = Verify(user, redeemToken, aToken);
            var exchangeRate = CalculateExchangeRate(aToken);
            //ATokenAmount
            var redeemTokenAmount = origin["userATokenBalance"];
            //UnderlyingTokenAmount
            var redeemAmountStr = new BigIntValue(exchangeRate).Mul(redeemTokenAmount).Div(Mantissa).Value;
            var calculateRedeemAmount = long.Parse(redeemAmountStr);
            var actualRedeemAmount = redeemAmount > calculateRedeemAmount ? calculateRedeemAmount : redeemAmount;
            var actualATokenAmount =
                long.Parse(new BigIntValue(actualRedeemAmount).Mul(Mantissa).Div(exchangeRate).Value);
            Logger.Info($"redeem amount: {actualRedeemAmount}");
            var accrualBlockNumbers = _awakenATokenContract.GetAccrualBlockNumber(aToken);
            var borrowIndex = _awakenATokenContract.GetBorrowIndex(aToken).Value;
            
            var userRedeem = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.RedeemUnderlying,
                new RedeemUnderlyingInput
                {
                    AToken = aToken,
                    Amount = actualRedeemAmount
                });
            userRedeem.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var userRedeemFee = userRedeem.GetDefaultTransactionFee();

            var logs = userRedeem.Logs.First(l => l.Name.Equals(nameof(Redeem)));
            var redeemLogs = Redeem.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed));
            redeemLogs.Sender.ShouldBe(user.ConvertAddress());
            redeemLogs.UnderlyingAmount.ShouldBe(actualRedeemAmount);
            redeemLogs.Underlying.ShouldBe(redeemToken);
            redeemLogs.AToken.ShouldBe(aToken);
            redeemLogs.ATokenAmount.ShouldBe(actualATokenAmount);

            var after = Verify(user, redeemToken, aToken);
            origin["userBalance"].ShouldBe(redeemToken.Equals("ELF")
                ? after["userBalance"].Sub(actualRedeemAmount).Add(userRedeemFee)
                : after["userBalance"].Sub(actualRedeemAmount));
            origin["contractBalance"].ShouldBe(after["contractBalance"].Add(actualRedeemAmount));
            origin["userATokenBalance"].ShouldBe(after["userATokenBalance"].Add(actualATokenAmount));
            origin["totalSupply"].ShouldBe(after["totalSupply"].Add(actualATokenAmount));
            origin["totalCash"].ShouldBe(after["totalCash"].Add(actualRedeemAmount));

            var interestLogs = AccrueInterest.Parser.ParseFrom(
                ByteString.FromBase64(userRedeem.Logs.First(l => l.Name.Equals(nameof(AccrueInterest))).NonIndexed));
            interestLogs.Cash.ShouldBe(origin["totalCash"]);
            interestLogs.AToken.ShouldBe(aToken);
            Logger.Info(interestLogs);
        }

        //按UnderlyingToken还款
        [TestMethod]
        [DataRow("ELF")]
        public void RedeemUnderlying(string redeemToken)
        {
            var user = TestAccount;
            var aToken = _awakenATokenContract.GetATokenAddress(redeemToken);
            _awakenATokenContract.SetAccount(user);
            var origin = Verify(user, redeemToken, aToken);
            var exchangeRate = CalculateExchangeRate(aToken);
            //ATokenAmount
            var redeemTokenAmount = origin["userATokenBalance"];
            //UnderlyingTokenAmount
            var redeemAmountStr = new BigIntValue(exchangeRate).Mul(redeemTokenAmount).Div(Mantissa).Value;
            var redeemAmount = long.Parse(redeemAmountStr);

            var userRedeem = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.Redeem, new RedeemInput
            {
                AToken = aToken,
                Amount = redeemTokenAmount
            });
            userRedeem.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var userRedeemFee = userRedeem.GetDefaultTransactionFee();

            var logs = userRedeem.Logs.First(l => l.Name.Equals(nameof(Redeem)));
            var redeemLogs = Redeem.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed));
            redeemLogs.Sender.ShouldBe(user.ConvertAddress());
            redeemLogs.UnderlyingAmount.ShouldBe(redeemAmount);
            redeemLogs.Underlying.ShouldBe(redeemToken);
            redeemLogs.AToken.ShouldBe(aToken);
            redeemLogs.ATokenAmount.ShouldBe(redeemTokenAmount);

            var after = Verify(user, redeemToken, aToken);
            origin["userBalance"].ShouldBe(redeemToken.Equals("ELF")
                ? after["userBalance"].Sub(redeemAmount).Add(userRedeemFee)
                : after["userBalance"].Sub(redeemAmount));
            origin["contractBalance"].ShouldBe(after["contractBalance"].Add(redeemAmount));
            origin["userATokenBalance"].ShouldBe(after["userATokenBalance"].Add(redeemTokenAmount));
            origin["totalSupply"].ShouldBe(after["totalSupply"].Add(redeemTokenAmount));
            origin["totalCash"].ShouldBe(after["totalCash"].Add(redeemAmount));

            var interestLogs = AccrueInterest.Parser.ParseFrom(
                ByteString.FromBase64(userRedeem.Logs.First(l => l.Name.Equals(nameof(AccrueInterest))).NonIndexed));
            interestLogs.Cash.ShouldBe(origin["totalCash"]);
            interestLogs.AToken.ShouldBe(aToken);
            Logger.Info(interestLogs);
        }

        [TestMethod]
        [DataRow("ELF", 1000000)]
        public void BorrowToken(string borrowToken, long amount)
        {
            var user = TestAccount2;
            _tokenContract.TransferBalance(InitAccount, user, 10_00000000, "ELF");
            var aToken = _awakenATokenContract.GetATokenAddress(borrowToken);
            _awakenATokenContract.SetAccount(user);
            var origin = Verify(user, borrowToken, aToken);
            var currentBorrowBalance = _awakenATokenContract.GetCurrentBorrowBalance(user, aToken);
            var accrualBlockNumbers = _awakenATokenContract.GetAccrualBlockNumber(aToken);
            var borrowIndex = _awakenATokenContract.GetBorrowIndex(aToken).Value;
            
            var userBorrow = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.Borrow, new BorrowInput
            {
                AToken = aToken,
                Amount = amount,
                Channel = "channel"
            });
            userBorrow.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var fee = userBorrow.GetDefaultTransactionFee();
            var logs = userBorrow.Logs.First(l => l.Name.Equals(nameof(Borrow)));
            var borrowLogs = Borrow.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed));

            var interestLogs = AccrueInterest.Parser.ParseFrom(
                ByteString.FromBase64(userBorrow.Logs.First(l => l.Name.Equals(nameof(AccrueInterest))).NonIndexed));
            Logger.Info(interestLogs);
            Logger.Info(borrowLogs);

            var amountInfo = CalculateInterest(aToken, userBorrow.BlockNumber, accrualBlockNumbers, origin, borrowIndex);
            var borrowBalance = CalculateBorrowBalance(currentBorrowBalance, amountInfo["newBorrowIndex"], long.Parse(borrowIndex));
            
            var after = Verify(user, borrowToken, aToken);
            origin["userBalance"].ShouldBe(borrowToken.Equals("ELF")
                ? after["userBalance"].Sub(amount).Add(fee)
                : after["userBalance"].Sub(amount));
            origin["contractBalance"].ShouldBe(after["contractBalance"].Add(amount));
            origin["userATokenBalance"].ShouldBe(after["userATokenBalance"]);
            origin["totalSupply"].ShouldBe(after["totalSupply"]);
            origin["totalCash"].ShouldBe(after["totalCash"].Add(amount));
            after["totalBorrow"].ShouldBe(amountInfo["totalBorrow"].Add(amount));
            
            interestLogs.Cash.ShouldBe(origin["totalCash"]);
            interestLogs.AToken.ShouldBe(aToken);
            interestLogs.BorrowIndex.ShouldBe(amountInfo["newBorrowIndex"]);
            interestLogs.InterestAccumulated.ShouldBe(amountInfo["interestAccumulated"]);
            interestLogs.TotalBorrows.ShouldBe(amountInfo["totalBorrows"]);
            interestLogs.BorrowRatePerBlock.ShouldBe(amountInfo["borrowRatePerBlock"]);
            interestLogs.SupplyRatePerBlock.ShouldBe(amountInfo["supplyRatePerBlock"]);
            
            borrowLogs.Borrower.ShouldBe(user.ConvertAddress());
            borrowLogs.Amount.ShouldBe(amount);
            borrowLogs.AToken.ShouldBe(aToken);
            borrowLogs.BorrowBalance.ShouldBe(borrowBalance.Add(amount));
            borrowLogs.TotalBorrows.ShouldBe(amountInfo["totalBorrow"].Add(amount));
        }

        [TestMethod]
        [DataRow("ELF", 10000_00000000)]
        public void RepayBorrowToken(string repayToken, long amount, bool isRepayAll)
        {
            var user = TestAccount;
            var aToken = _awakenATokenContract.GetATokenAddress(repayToken);
            _awakenATokenContract.SetAccount(user);
            ApproveToken(repayToken, user, amount);
            var origin = Verify(user, repayToken, aToken);
            var userSnapshot = _awakenATokenContract.GetAccountSnapshot(user, aToken);
            var actualRepayAmount = amount > userSnapshot.BorrowBalance ? userSnapshot.BorrowBalance : amount;
            Logger.Info($"actualRepayAmount: {actualRepayAmount}");
            var accrualBlockNumbers = _awakenATokenContract.GetAccrualBlockNumber(aToken);
            var borrowIndex = _awakenATokenContract.GetBorrowIndex(aToken).Value;
            
            var userRepay = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.RepayBorrow, new RepayBorrowInput
            {
                AToken = aToken,
                Amount = actualRepayAmount,
                IsRepayAll = isRepayAll
            });
            userRepay.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var repayBlockNumber = userRepay.BlockNumber;
            
            var userRepayFee = userRepay.GetDefaultTransactionFee();
            var logs = userRepay.Logs.First(l => l.Name.Equals(nameof(RepayBorrow)));
            var repayLogs = RepayBorrow.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed));
            Logger.Info(repayLogs);
            var interestLogs = AccrueInterest.Parser.ParseFrom(
                ByteString.FromBase64(userRepay.Logs.First(l => l.Name.Equals(nameof(AccrueInterest))).NonIndexed));
            Logger.Info(interestLogs);
            var afterUserSnapshot = _awakenATokenContract.GetAccountSnapshot(user, aToken);

            afterUserSnapshot.BorrowBalance.ShouldBe(isRepayAll
                ? 0
                : userSnapshot.BorrowBalance.Sub(actualRepayAmount));

            var amountInfo = CalculateInterest(aToken, repayBlockNumber, accrualBlockNumbers, origin, borrowIndex);
            var borrowBalance = CalculateBorrowBalance(userSnapshot.BorrowBalance, amountInfo["newBorrowIndex"], long.Parse(borrowIndex));
            repayLogs.Borrower.ShouldBe(user.ConvertAddress());
            repayLogs.Payer.ShouldBe(user.ConvertAddress());
            repayLogs.Amount.ShouldBe(isRepayAll ? borrowBalance : actualRepayAmount);
            repayLogs.AToken.ShouldBe(aToken);
            repayLogs.BorrowBalance.ShouldBe(isRepayAll
                ? borrowBalance
                : borrowBalance.Sub(actualRepayAmount));
            repayLogs.TotalBorrows.ShouldBe(isRepayAll 
                ? amountInfo["totalBorrows"].Sub(borrowBalance) 
                : amountInfo["totalBorrows"].Sub(actualRepayAmount));
            
            interestLogs.Cash.ShouldBe(origin["totalCash"]);
            interestLogs.AToken.ShouldBe(aToken);
            interestLogs.BorrowIndex.ShouldBe(amountInfo["newBorrowIndex"]);
            interestLogs.InterestAccumulated.ShouldBe(amountInfo["interestAccumulated"]);
            interestLogs.TotalBorrows.ShouldBe(amountInfo["totalBorrows"]);
            interestLogs.BorrowRatePerBlock.ShouldBe(amountInfo["borrowRatePerBlock"]);
            interestLogs.SupplyRatePerBlock.ShouldBe(amountInfo["supplyRatePerBlock"]);
            
            
            var after = Verify(user, repayToken, aToken);
            origin["userBalance"].ShouldBe(repayToken.Equals("ELF")
                ? after["userBalance"].Add(isRepayAll ? borrowBalance : actualRepayAmount).Add(userRepayFee)
                : after["userBalance"].Add(isRepayAll ? borrowBalance : actualRepayAmount));
            origin["contractBalance"].ShouldBe(after["contractBalance"].Sub(isRepayAll ? borrowBalance : actualRepayAmount));
            origin["userATokenBalance"].ShouldBe(after["userATokenBalance"]);
            origin["totalBorrow"].ShouldBe(after["totalBorrow"].Add(isRepayAll ? borrowBalance : actualRepayAmount));
        }

        [TestMethod]
        public void AddReserves(string addReserveToken, long amount)
        {
            var user = TestAccount;
            var aToken = _awakenATokenContract.GetATokenAddress(addReserveToken);
            var originExchangeRate = _awakenATokenContract.GetCurrentExchangeRate(aToken);
            Logger.Info($"Origin exchange rate: {originExchangeRate}");

            _awakenATokenContract.SetAccount(user);
            ApproveToken(addReserveToken, user, amount);
            var origin = Verify(user, addReserveToken, aToken);

            var result =
                _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.AddReserves, new AddReservesInput
                {
                    AToken = aToken,
                    Amount = amount
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var fee = result.GetDefaultTransactionFee();
            var logs = result.Logs.First(l => l.Name.Equals(nameof(ReservesAdded))).NonIndexed;
            var reservesAdded = ReservesAdded.Parser.ParseFrom(ByteString.FromBase64(logs));
            reservesAdded.Underlying.ShouldBe(addReserveToken);
            reservesAdded.AddAmount.ShouldBe(amount);
            reservesAdded.AToken.ShouldBe(aToken);
            reservesAdded.TotalReserves.ShouldBe(origin["totalReserves"].Add(amount));

            var after = Verify(user, addReserveToken, aToken);
            origin["userBalance"].ShouldBe(addReserveToken.Equals("ELF")
                ? after["userBalance"].Add(amount).Add(fee)
                : after["userBalance"].Add(amount));
            origin["contractBalance"].ShouldBe(after["contractBalance"].Sub(amount));

            var exchangeRate = _awakenATokenContract.GetCurrentExchangeRate(aToken);
            Logger.Info($"New exchange rate: {exchangeRate}");
            exchangeRate.ShouldBeLessThan(originExchangeRate);
        }

        [TestMethod]
        public void ReduceReserves(string reduceToken, long amount)
        {
            var owner = InitAccount;
            var aToken = _awakenATokenContract.GetATokenAddress(reduceToken);
            var originExchangeRate = _awakenATokenContract.GetCurrentExchangeRate(aToken);
            Logger.Info($"Origin exchange rate: {originExchangeRate}");

            _awakenATokenContract.SetAccount(owner);
            var origin = Verify(InitAccount, reduceToken, aToken);

            var result =
                _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.ReduceReserves, new ReduceReservesInput
                {
                    AToken = aToken,
                    Amount = amount
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var fee = result.GetDefaultTransactionFee();
            var logs = result.Logs.First(l => l.Name.Equals(nameof(ReservesReduced))).NonIndexed;
            var reservesReduced = ReservesReduced.Parser.ParseFrom(ByteString.FromBase64(logs));
            reservesReduced.Underlying.ShouldBe(reduceToken);
            reservesReduced.ReduceAmount.ShouldBe(amount);
            reservesReduced.AToken.ShouldBe(aToken);
            reservesReduced.TotalReserves.ShouldBe(origin["totalReserves"].Sub(amount));

            var after = Verify(owner, reduceToken, aToken);
            origin["userBalance"].ShouldBe(reduceToken.Equals("ELF")
                ? after["userBalance"].Sub(amount).Add(fee)
                : after["userBalance"].Sub(amount));
            origin["contractBalance"].ShouldBe(after["contractBalance"].Add(amount));

            var exchangeRate = _awakenATokenContract.GetCurrentExchangeRate(aToken);
            Logger.Info($"New exchange rate: {exchangeRate}");
            exchangeRate.ShouldBeGreaterThan(originExchangeRate);
        }

        #endregion
        
        [TestMethod]
        // [DataRow("LLL", 1_00000000, false)]
        // [DataRow("TEST", 1_00000000, false)]
        // [DataRow("USDT", 1_000000, true)]
        [DataRow("SIDETOKEN", 1_00000000, true)]
        public void CreateAToken(string underlyingSymbol, long initialExchangeRate, bool isJump)
        {
            var interestRateModel = isJump
            ? _awakenFinanceInterestRateModelContract.Contract
            : _awakenFinanceWhiteInterestRateModelContract.Contract;
                var result = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.Create, new CreateInput
            {
                InitialExchangeRate = initialExchangeRate,
                InterestRateModel = interestRateModel,
                UnderlyingSymbol = underlyingSymbol
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var aToken = _awakenATokenContract.GetATokenAddress(underlyingSymbol);
            Logger.Info(aToken);
            var logs = result.Logs.First(l => l.Name.Equals(nameof(TokenCreated))).NonIndexed;
            var createdLogs = TokenCreated.Parser.ParseFrom(ByteString.FromBase64(logs));
            createdLogs.Controller.ShouldBe(_awakenFinanceControllerContract.Contract);
            createdLogs.Decimals.ShouldBe(8);
            createdLogs.Symbol.ShouldBe(GetATokenSymbol(underlyingSymbol));
            createdLogs.Underlying.ShouldBe(underlyingSymbol);
            createdLogs.AToken.ShouldBe(aToken);
            createdLogs.TokenName.ShouldBe(GetATokenSymbol(underlyingSymbol));
        }

        #region Initialize

        private void InitializeAToken()
        {
            var aTokenInitializeResult =
                _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.Initialize, new InitializeInput
                {
                    Controller = _awakenFinanceControllerContract.Contract
                });
            aTokenInitializeResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var getAdmin = _awakenATokenContract.GetAdmin();
            var getComptroller = _awakenATokenContract.GetComptroller();
            getAdmin.ShouldBe(InitAccount.ConvertAddress());
            getComptroller.ShouldBe(_awakenFinanceControllerContract.Contract);
        }

        private void InitializeLendingLens()
        {
            var lendingLensInitializeResult =
                _awakenFinanceLendingLensContract.ExecuteMethodWithResult(LendingLensMethod.Initialize,
                    new Awaken.Contracts.AwakenLendingLens.InitializeInput
                    {
                        ATokenContract = _awakenATokenContract.Contract,
                        ComtrollerContract = _awakenFinanceControllerContract.Contract
                    });
            lendingLensInitializeResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        private void InitializeInterestRateMode()
        {
            var interestRateModeInitializeResult =
                _awakenFinanceInterestRateModelContract.ExecuteMethodWithResult(InterestRateModelMethod.Initialize,
                    new Awaken.Contracts.InterestRateModel.InitializeInput()
                    {
                        BaseRatePerYear = _baseRatePerYear,
                        MultiplierPerYear = _multiplierPerYear,
                        JumpMultiplierPerYear = _jumpMultiplierPerYear,
                        Kink = _kink,
                        InterestRateModelType = false
                    });
            interestRateModeInitializeResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            GetInterestModeInfo(_awakenFinanceInterestRateModelContract);
        }
        
        private void InitializeWhiteInterestRateMode()
        {
            var interestRateModeInitializeResult =
                _awakenFinanceWhiteInterestRateModelContract.ExecuteMethodWithResult(InterestRateModelMethod.Initialize,
                    new Awaken.Contracts.InterestRateModel.InitializeInput()
                    {
                        BaseRatePerYear = _baseRatePerYearWhite,
                        MultiplierPerYear = _whiteMultiplierPerYear,
                        InterestRateModelType = true
                    });
            interestRateModeInitializeResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            GetInterestModeInfo(_awakenFinanceWhiteInterestRateModelContract);
        }

        private void InitializeController()
        {
            var controllerInitializeResult =
                _awakenFinanceControllerContract.ExecuteMethodWithResult(ControllerMethod.Initialize,
                    new Awaken.Contracts.Controller.InitializeInput
                    {
                        ATokenContract = _awakenATokenContract.Contract,
                        PlatformTokenSymbol = _platformTokenSymbol
                    });
            controllerInitializeResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var getAdmin = _awakenFinanceControllerContract.GetAdmin();
            getAdmin.ShouldBe(InitAccount.ConvertAddress());
        }

        private void CreateToken(string tokenSymbol, int d, string issuer)
        {
            var tokenInfo = _tokenContract.GetTokenInfo(tokenSymbol);
            if (!tokenInfo.Equals(new TokenInfo())) return;
            var createResult = _tokenContract.CreateToken(tokenSymbol, d, issuer);
            createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        #endregion

        #region Controller SetInfo

        //SupportMarket
        [TestMethod]
        [DataRow("ELF")]
        [DataRow("ETHTE")]
        [DataRow("BTETE")]
        [DataRow("USDTE")]
        // [DataRow("SIDETOKEN")]
        public void SupportMarket(string underlyingSymbol)
        {
            var aToken = _awakenATokenContract.GetATokenAddress(underlyingSymbol);
            Logger.Info(aToken);
            var support =
                _awakenFinanceControllerContract.ExecuteMethodWithResult(ControllerMethod.SupportMarket, aToken);
            support.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logs = support.Logs.First(l => l.Name.Equals(nameof(MarketListed)));
            var marketList = MarketListed.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed));
            marketList.AToken.ShouldBe(aToken);
            var allMarketList = _awakenFinanceControllerContract.GetAllMarkets();
            allMarketList.AToken.ShouldContain(aToken);
            var marketInfo = _awakenFinanceControllerContract.GetMarket(aToken);
            marketInfo.IsListed.ShouldBeTrue();
        }

        //SetCollateralFactor
        //MaxCollateralFactor = 900000000000000000; // 0.9 scaled by 1e18
        [TestMethod]
        // [DataRow("USDTE", 550000000000000000)] // 0.8
        [DataRow("ETHTE", 750000000000000000)] // 0.75
        [DataRow("ELF", 550000000000000000)] // 0.75
        // [DataRow("BTETE", 750000000000000000)] //0.55
        // [DataRow("SIDETOKEN", 750000000000000000)] //0.75
        public void SetCollateralFactor(string underlyingSymbol, long changeCollateral)
        {
            var aToken = _awakenATokenContract.GetATokenAddress(underlyingSymbol);
            Logger.Info(aToken);
            var oldCollateral = _awakenFinanceControllerContract.GetCollateralFactor(aToken);

            var result = _awakenFinanceControllerContract.ExecuteMethodWithResult(ControllerMethod.SetCollateralFactor,
                new SetCollateralFactorInput
                {
                    AToken = aToken,
                    NewCollateralFactor = changeCollateral
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logs = result.Logs.First(l => l.Name.Equals(nameof(CollateralFactorChanged)));
            var changed = CollateralFactorChanged.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed));
            var newCollateral = _awakenFinanceControllerContract.GetCollateralFactor(aToken);

            changed.AToken.ShouldBe(aToken);
            changed.OldCollateralFactor.ShouldBe(oldCollateral);
            changed.NewCollateralFactor.ShouldBe(newCollateral);
            newCollateral.ShouldBe(changeCollateral);

            var market = _awakenFinanceControllerContract.GetMarket(aToken);
            market.CollateralFactor.ShouldBe(changeCollateral);
        }

        //SetCloseFactor
        //MinCloseFactor = 50000000000000000;  0.05  scaled by 1e18
        //MaxCloseFactor = 900000000000000000; 0.9 scaled by 1e18
        [TestMethod]
        public void SetCloseFactor()
        {
            var changeCloseFactor = 500000000000000000;
            var oldCloseFactor = _awakenFinanceControllerContract.GetCloseFactor();

            var result = _awakenFinanceControllerContract.ExecuteMethodWithResult(ControllerMethod.SetCloseFactor,
                new Int64Value { Value = changeCloseFactor });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logs = result.Logs.First(l => l.Name.Equals(nameof(CloseFactorChanged)));
            var changed = CloseFactorChanged.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed));
            var newCloseFactor = _awakenFinanceControllerContract.GetCloseFactor();

            changed.NewCloseFactor.ShouldBe(newCloseFactor);
            changed.OldCloseFactor.ShouldBe(oldCloseFactor);
            newCloseFactor.ShouldBe(changeCloseFactor);
        }

        //SetLiquidationIncentive
        //MinLiquidationIncentive = 1000000000000000000; // 1.0 scaled by 1e18
        //MaxLiquidationIncentive = 1500000000000000000; // 1.5 scaled by 1e18
        [TestMethod]
        public void SetLiquidationIncentive()
        {
            var changeLiquidationIncentive = 1080000000000000000;
            var oldLiquidationIncentive = _awakenFinanceControllerContract.GetLiquidationIncentive();

            var result = _awakenFinanceControllerContract.ExecuteMethodWithResult(
                ControllerMethod.SetLiquidationIncentive,
                new Int64Value { Value = changeLiquidationIncentive });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logs = result.Logs.First(l => l.Name.Equals(nameof(LiquidationIncentiveChanged)));
            var changed = LiquidationIncentiveChanged.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed));
            var newLiquidationIncentive = _awakenFinanceControllerContract.GetLiquidationIncentive();

            changed.NewLiquidationIncentive.ShouldBe(newLiquidationIncentive);
            changed.OldLiquidationIncentive.ShouldBe(oldLiquidationIncentive);
            newLiquidationIncentive.ShouldBe(changeLiquidationIncentive);
        }

        //MaxAssets
        [TestMethod]
        public void SetMaxAssets()
        {
            var changeMaxAssets = 20;
            var oldMaxAssets = _awakenFinanceControllerContract.GetMaxAssets();

            var result = _awakenFinanceControllerContract.ExecuteMethodWithResult(ControllerMethod.SetMaxAssets,
                new Int32Value { Value = changeMaxAssets });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logs = result.Logs.First(l => l.Name.Equals(nameof(MaxAssetsChanged)));
            var changed = MaxAssetsChanged.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed));
            var newMaxAssets = _awakenFinanceControllerContract.GetMaxAssets();

            changed.NewMaxAssets.ShouldBe(newMaxAssets);
            changed.OldMaxAssets.ShouldBe(oldMaxAssets);
            newMaxAssets.ShouldBe(changeMaxAssets);
        }

        //PlatformTokenRate
        [TestMethod]
        public void SetPlatformTokenRate()
        {
            var changePlatformTokenRate = 10000000; //0.1
            var oldPlatformTokenRate = _awakenFinanceControllerContract.GetPlatformTokenRate();

            var result = _awakenFinanceControllerContract.ExecuteMethodWithResult(ControllerMethod.SetPlatformTokenRate,
                new Int64Value { Value = changePlatformTokenRate });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logs = result.Logs.First(l => l.Name.Equals(nameof(PlatformTokenRateChanged)));
            var changed = PlatformTokenRateChanged.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed));
            var newPlatformTokenRate = _awakenFinanceControllerContract.GetPlatformTokenRate();
            var updatedLogs = result.Logs.Where(l => l.Name.Equals(nameof(PlatformTokenSpeedUpdated))).ToList();
            if (!updatedLogs.IsNullOrEmpty())
            {
                foreach (var updated in updatedLogs.Select(log =>
                             PlatformTokenSpeedUpdated.Parser.ParseFrom(ByteString.FromBase64(log.NonIndexed))))
                {
                    Logger.Info(updated);
                }
            }

            changed.NewPlatformTokenRate.ShouldBe(newPlatformTokenRate);
            changed.OldPlatformTokenRate.ShouldBe(oldPlatformTokenRate);
            newPlatformTokenRate.ShouldBe(changePlatformTokenRate);
        }

        [TestMethod]
        public void AddPlatformTokenMarkets()
        {
            var listToken = new List<string> { "ELF", "USDTE", "ETHTE" };
            var listAToken = new List<Address>();
            foreach (var aToken in listToken.Select(t => _awakenATokenContract.GetATokenAddress(t)))
            {
                Logger.Info(aToken);
                listAToken.Add(aToken);
            }

            var addPlatformTokenMarkets =
                _awakenFinanceControllerContract.ExecuteMethodWithResult(ControllerMethod.AddPlatformTokenMarkets,
                    new ATokens
                    {
                        AToken = { listAToken }
                    });
            addPlatformTokenMarkets.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logs = addPlatformTokenMarkets.Logs.Where(l => l.Name.Equals(nameof(MarketPlatformTokened))).ToList();
            foreach (var log in logs)
            {
                var platformTokenMarkets =
                    MarketPlatformTokened.Parser.ParseFrom(ByteString.FromBase64(log.NonIndexed));
                platformTokenMarkets.IsPlatformTokened.ShouldBeTrue();
                listAToken.ShouldContain(platformTokenMarkets.AToken);
            }

            foreach (var aToken in listAToken)
            {
                var marketInfo = _awakenFinanceControllerContract.GetMarket(aToken);
                marketInfo.IsPlatformTokened.ShouldBeTrue();
            }
        }

        [TestMethod]
        public void RefreshPlatformTokenSpeeds()
        {
            var result =
                _awakenFinanceControllerContract.ExecuteMethodWithResult(ControllerMethod.RefreshPlatformTokenSpeeds,
                    new Empty());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        #endregion

        #region AToken

        //SetReserveFactor
        //MaxReserveFactor = 1_000000000000000000;
        [TestMethod]
        [DataRow("ETHTE", 200000000000000000)]
        [DataRow("BTETE", 200000000000000000)]
        [DataRow("USDTE", 75000000000000000)]
        [DataRow("ELF", 200000000000000000)]
        public void SetReserveFactor(string underlyingSymbol, long changeReserveFactor)
        {
            var aToken = _awakenATokenContract.GetATokenAddress(underlyingSymbol);
            Logger.Info(aToken);
            var oldReserveFactor = _awakenATokenContract.GetReserveFactor(aToken);

            var result = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.SetReserveFactor,
                new SetReserveFactorInput { AToken = aToken, ReserveFactor = changeReserveFactor });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var reserveFactorLogs = result.Logs.First(l => l.Name.Equals(nameof(ReserveFactorChanged))).NonIndexed;
            var reserveFactorChanged = ReserveFactorChanged.Parser.ParseFrom(ByteString.FromBase64(reserveFactorLogs));
            reserveFactorChanged.AToken.ShouldBe(aToken);
            reserveFactorChanged.NewReserveFactor.ShouldBe(changeReserveFactor);
            reserveFactorChanged.OldReserveFactor.ShouldBe(oldReserveFactor);

            var newReserveFactor = _awakenATokenContract.GetReserveFactor(aToken);
            newReserveFactor.ShouldBe(changeReserveFactor);
        }

        [TestMethod]
        public void SetPriceOracle()
        {
            var setPriceOracle =
                _awakenFinanceControllerContract.ExecuteMethodWithResult(ControllerMethod.SetPriceOracle,
                    _testPriceContract.Contract);
            setPriceOracle.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logs = setPriceOracle.Logs.First(l => l.Name.Equals(nameof(PriceOracleChanged)));
            var changed = PriceOracleChanged.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed));
            changed.NewPriceOracle.ShouldBe(_testPriceContract.Contract);
        }

        #endregion


        #region View

        [TestMethod]
        public void GetATokenAddress()
        {
            var listToken = new List<string> { "LLL", "TEST", "MMM", "USDT", "ELF" };
            foreach (var token in listToken)
            {
                var aToken = _awakenATokenContract.GetATokenAddress(token);
                Logger.Info($"{token}: {aToken.ToBase58()}");
            }
        }

        [TestMethod]
        public void GetMarketInfo()
        {
            var user = "Shh7sPA3HyNwzU7LysXHioxjBz89faX1Wb7FtKnScR9ZPbFwW";
            var underlyingSymbol = "ELF";
            var aToken = _awakenATokenContract.GetATokenAddress(underlyingSymbol);
            Logger.Info(aToken);

            var getMarket = _awakenFinanceControllerContract.GetMarket(aToken);
            getMarket.IsListed.ShouldBeTrue();
            Logger.Info(getMarket);

            getMarket.AccountMembership.Keys.ShouldContain(user);
            var checkAccountAssets = _awakenFinanceControllerContract.GetAssetsIn(user.ConvertAddress());
            Logger.Info(checkAccountAssets);
        }

        [TestMethod]
        [DataRow("ELF")]
        [DataRow("USDT")]
        [DataRow("TEST")]
        [DataRow("LLL")]
        [DataRow("MMM")]
        public void GetPlatformTokenSpeeds(string token)
        {
            var aToken = _awakenATokenContract.GetATokenAddress(token);
            var speeds = _awakenFinanceControllerContract.GetPlatformTokenSpeeds(aToken);
            Logger.Info(speeds);
        }

        [TestMethod]
        [DataRow("ELF")]
        [DataRow("USDT")]
        [DataRow("TEST")]
        [DataRow("LLL")]
        [DataRow("MMM")]
        public void GetPlatformTokenAccrued(string underlying)
        {
            var aToken = _awakenATokenContract.GetATokenAddress(underlying);
            var result = _awakenFinanceControllerContract.GetPlatformTokenAccrued(aToken);
            Logger.Info(result);
        }

        [TestMethod]
        public void GetAccountLiquidity()
        {
            var user = "Shh7sPA3HyNwzU7LysXHioxjBz89faX1Wb7FtKnScR9ZPbFwW";
            var actionToken = "USDTE";
            long amount = 8_979698;
            var aTokenModify = _awakenATokenContract.GetATokenAddress(actionToken);
            var info = Verify(user, actionToken, aTokenModify);

            var result = _awakenFinanceControllerContract.GetAccountLiquidity(user.ConvertAddress());
            Logger.Info(result);
            var redeemTokens = amount.Mul(info["exchangeRate"]);
            var r = _awakenFinanceControllerContract.GetHypotheticalAccountLiquidity(user.ConvertAddress(),
                aTokenModify, 0, redeemTokens);
            Logger.Info(r);
        }
        

        [TestMethod]
        public void VerifyUser()
        {
            var user = "Shh7sPA3HyNwzU7LysXHioxjBz89faX1Wb7FtKnScR9ZPbFwW";
            var actionToken = "USDTE";
            long amount = 1_0000000;
            var target = "redeem";
            
            var aTokenModify = _awakenATokenContract.GetATokenAddress(actionToken);
            var asset = _awakenFinanceControllerContract.GetAssetsIn(user.ConvertAddress());
            if (!asset.Assets.Contains(aTokenModify))
            {
                asset.Assets.Add(aTokenModify);
            }
            var sumCollateral = new BigIntValue(){Value = "0"};
            var sumBorrowPlusEffects = new BigIntValue(){Value = "0"};
            for (int i = 0; i < asset.Assets.Count; i++)
            {
                var aToken = asset.Assets[i];
                var token = _awakenATokenContract.GetUnderlying(aToken);
                var info = Verify(user, token, aToken);
                var calculateExchangeRate = CalculateExchangeRate(aToken);
                Logger.Info(calculateExchangeRate);
                var price = _testPriceContract.GetExchangeTokenPriceInfo(token).Value;
     
                var collateralFactor = _awakenFinanceControllerContract.GetCollateralFactor(aToken);
                var tokensToDenom = new BigIntValue(info["exchangeRate"]).Mul(price).Mul(collateralFactor).Div(Mantissa).Div(PriceMantissa);
                var tokenCollateral = new BigIntValue(info["userATokenBalance"]).Mul(tokensToDenom).Div(ExchangeMantissa); 
                var tokenBorrowPlusEffects = new BigIntValue(info["userBorrowBalance"]).Mul(price).Div(PriceMantissa);
                Logger.Info($"{token}: {tokenCollateral} {tokenBorrowPlusEffects}");
                sumCollateral = sumCollateral.Add(tokenCollateral);
                sumBorrowPlusEffects = sumBorrowPlusEffects.Add(tokenBorrowPlusEffects);

                if (aTokenModify != aToken) continue;
                // redeem effect
                // sumBorrowPlusEffects += tokensToDenom * redeemTokens
                if (target == "borrow")
                {
                    // borrow effect
                    // sumBorrowPlusEffects += oraclePrice * borrowAmount
                    sumBorrowPlusEffects = sumBorrowPlusEffects.Add(new BigIntValue(price).Mul(amount).Div(PriceMantissa));
                    sumBorrowPlusEffects  = sumBorrowPlusEffects.Add(new BigIntValue(tokensToDenom).Mul(0).Div(ExchangeMantissa));
                }
                else
                { 
                    sumBorrowPlusEffects = sumBorrowPlusEffects.Add(new BigIntValue(price).Mul(0).Div(PriceMantissa));
                    var redeemTokens = amount.Mul(info["exchangeRate"]);
                    sumBorrowPlusEffects  = sumBorrowPlusEffects.Add(new BigIntValue(tokensToDenom).Mul(redeemTokens).Div(ExchangeMantissa));
                }
            }
            var liquidityStr = sumBorrowPlusEffects.Sub(sumCollateral).Value;
            Logger.Info(long.Parse(liquidityStr));
            Logger.Info(sumCollateral);
            Logger.Info(sumBorrowPlusEffects);
        }

        [TestMethod]
        public void GetCurrentBorrowBalance(string underlying)
        {
            var user = TestAccount;
            var aToken = _awakenATokenContract.GetATokenAddress(underlying);

            var balance = _awakenATokenContract.GetCurrentBorrowBalance(user, aToken);
            Logger.Info(balance);
        }


        [TestMethod]
        [DataRow("ELF")]
        [DataRow("USDT")]
        [DataRow("TEST")]
        [DataRow("MMM")]
        [DataRow("LLL")]
        public void GetAccountSnapshot(string underlyingToken)
        {
            var account = TestAccount;
            var aToken = _awakenATokenContract.GetATokenAddress(underlyingToken);
            var result = _awakenATokenContract.GetAccountSnapshot(account, aToken);
            Logger.Info(result);
        }

        #endregion

        //EhoPemr3rb2nz2X365F58pWk8tybw6vALdd8zwjdhzKYMfyKr
        //EU79trbPddWVRzZHUCXWNJBU7rr5VVWRGLjUcDxQPgWXRtbYp
        [TestMethod]
        [DataRow("EhoPemr3rb2nz2X365F58pWk8tybw6vALdd8zwjdhzKYMfyKr", 115000000000000000)]
        [DataRow("EU79trbPddWVRzZHUCXWNJBU7rr5VVWRGLjUcDxQPgWXRtbYp", 283750000000000000)]
        public void UpdateJumpRateModel(string whiteModel, long multiplier)
        {
            var whiteModelContract = new AwakenFinanceInterestRateModelContract(NodeManager, whiteModel, InitAccount);
            var update = whiteModelContract.ExecuteMethodWithResult(InterestRateModelMethod.UpdateRateModel,
                new UpdateRateModelInput
                {
                    BaseRatePerYear = 23000000000000000,
                    MultiplierPerYear = multiplier
                });
            update.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            GetInterestModeInfo(whiteModelContract);
        }

        private Dictionary<string, long> GetInterestModeInfo(AwakenFinanceInterestRateModelContract contract)
        {
            var info = new Dictionary<string, long>();
            var getBaseRate = contract.GetBaseRatePerBlock();
            var getMultiplier = contract.GetMultiplierPerBlock();
            var getKink = contract.GetKink();
            var getJump = contract.GetJumpMultiplierPerBlock();
            info.Add("baseRatePerBlock", getBaseRate);
            info.Add("multiplierPerBlock", getMultiplier);
            info.Add("kink", getKink);
            info.Add("jumpMultiplierPerBlock", getJump);

            Logger.Info($"{getBaseRate}, {getMultiplier}, {getKink}, {getJump}");
            return info;
        }

        private string GetATokenSymbol(string symbol)
        {
            return $"A-{symbol}";
        }

        private void CheckBalance(string symbol, string user, long mintAmount)
        {
            var userBalance = _tokenContract.GetUserBalance(user, symbol);
            if (userBalance > mintAmount) return;
            if (symbol != "ELF")
            {
                var tokenInfo = _tokenContract.GetTokenInfo(symbol);
                var issue = _tokenContract.IssueBalance(tokenInfo.Issuer.ToBase58(), user, mintAmount.Add(1000000000),
                    symbol);
                issue.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }
            else
            {
                var transfer = _tokenContract.TransferBalance(InitAccount, user, mintAmount, symbol);
                transfer.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }
        }

        private Dictionary<string, long> Verify(string user, string token, Address aToken)
        {
            var infoList = new Dictionary<string, long>();
            var userBalance = _tokenContract.GetUserBalance(user, token);
            var contractBalance = _tokenContract.GetUserBalance(_awakenATokenContract.ContractAddress, token);
            var userATokenBalance = _awakenATokenContract.GetBalance(user, aToken);
            var userPlatformBalance = _tokenContract.GetUserBalance(user, _platformTokenSymbol);
            var contractPlatformBalance =
                _tokenContract.GetUserBalance(_awakenFinanceControllerContract.ContractAddress, _platformTokenSymbol);
            var totalBorrow = _awakenATokenContract.GetTotalBorrows(aToken);
            var totalReserves = _awakenATokenContract.GetTotalReserves(aToken);
            var totalCash = _awakenATokenContract.GetCash(aToken);
            var totalSupply = _awakenATokenContract.GetTotalSupply(aToken);
            var userSnapshot = _awakenATokenContract.GetAccountSnapshot(user, aToken);
            userSnapshot.ATokenBalance.ShouldBe(userATokenBalance);

            infoList.Add("userBalance", userBalance);
            infoList.Add("contractBalance", contractBalance);
            infoList.Add("userATokenBalance", userATokenBalance);
            infoList.Add("totalBorrow", totalBorrow);
            infoList.Add("totalReserves", totalReserves);
            infoList.Add("totalCash", totalCash);
            infoList.Add("totalSupply", totalSupply);
            infoList.Add("userBorrowBalance", userSnapshot.BorrowBalance);
            infoList.Add("userPlatformTokenBalance", userPlatformBalance);
            infoList.Add("contractPlatformTokenBalance", contractPlatformBalance);
            infoList.Add("exchangeRate", userSnapshot.ExchangeRate);

            Logger.Info($"\nuserBalance: {userBalance}\n" +
                        $"contractBalance: {contractBalance}\n" +
                        $"userATokenBalance: {userATokenBalance}\n" +
                        $"userBorrowBalance: {userSnapshot.BorrowBalance}\n" +
                        $"totalBorrow: {totalBorrow}\n" +
                        $"totalReserves: {totalReserves}\n" +
                        $"totalCash: {totalCash}\n" +
                        $"totalSupply: {totalSupply}\n" +
                        $"userPlatformTokenBalance: {userPlatformBalance}\n" +
                        $"contractPlatformTokenBalance: {contractPlatformBalance}\n" +
                        $"exchangeRate: {userSnapshot.ExchangeRate}");
            return infoList;
        }

        private void ApproveToken(string symbol, string user, long mintAmount)
        {
            var approve = _tokenContract.ApproveToken(user, _awakenATokenContract.ContractAddress, mintAmount, symbol);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        private long CalculateExchangeRate(Address aToken)
        {
            var totalBorrow = _awakenATokenContract.GetTotalBorrows(aToken);
            var totalReserves = _awakenATokenContract.GetTotalReserves(aToken);
            var totalCash = _awakenATokenContract.GetCash(aToken);
            var totalSupply = _awakenATokenContract.GetTotalSupply(aToken);

            if (totalSupply == 0)
                return _awakenATokenContract.GetInitialExchangeRate(aToken);
            var exchangeRateStr = new BigIntValue(totalCash).Add(totalBorrow).Sub(totalReserves).Mul(ExchangeMantissa)
                .Div(totalSupply).Value;
            return long.Parse(exchangeRateStr);
        }
        
        private long CalculateExchangeRateUpdate(long totalBorrow, long totalReserves, long totalCash, long totalSupply)
        {
            var exchangeRateStr = new BigIntValue(totalCash).Add(totalBorrow).Sub(totalReserves).Mul(ExchangeMantissa)
                .Div(totalSupply).Value;
            return long.Parse(exchangeRateStr);
        }

        /* InterestModel 
         * utilizationRate = borrows / (cash + borrows - reserves)
         * white: utilizationRate.mul(multiplierPerBlock).div(1e18).add(baseRatePerBlock);
         * jump:
         * util <= kink
             utilizationRate.mul(multiplierPerBlock).div(1e18).add(baseRatePerBlock); 
         * util > kink
             uint normalRate = kink.mul(multiplierPerBlock).div(1e18).add(baseRatePerBlock);
             uint excessUtil = utilizationRate.sub(kink);
             return excessUtil.mul(jumpMultiplierPerBlock).div(1e18).add(normalRate);
         }
         */
        private Dictionary<string, long> CalculateInterest(Address aToken, long txHeight, long accrualBlockNumbers,
            IReadOnlyDictionary<string, long> origin, string borrowIndex)
        {
            var info = new Dictionary<string, long>();
            var model = _awakenATokenContract.GetInterestRateModel(aToken);
            var modelContract = new AwakenFinanceInterestRateModelContract(NodeManager, model.ToBase58(), InitAccount);
            var modelInfo =
                GetInterestModeInfo(modelContract);
            var currentHeight = txHeight;
            var blockDelta = currentHeight.Sub(accrualBlockNumbers);
            var reserveFactor = _awakenATokenContract.GetReserveFactor(aToken);
            var cashPrior = origin["totalCash"];
            var borrowPrior = origin["totalBorrow"];
            var reservesPrior = origin["totalReserves"];

            var utilizationRateStr = new BigIntValue(borrowPrior).Mul(Mantissa)
                .Div(cashPrior.Add(borrowPrior).Sub(reservesPrior)).Value;
            string borrowRateStr;

            if (modelInfo["kink"].Equals(0) && modelInfo["jumpMultiplierPerBlock"].Equals(0)) // white 
            {
                borrowRateStr = new BigIntValue(utilizationRateStr).Mul(modelInfo["multiplierPerBlock"]).Div(Mantissa)
                    .Add(modelInfo["baseRatePerBlock"]).Value;
            }
            else //jump
            {
                if (long.Parse(utilizationRateStr) <= modelInfo["kink"])
                {
                    borrowRateStr = new BigIntValue(utilizationRateStr).Mul(modelInfo["multiplierPerBlock"])
                        .Div(Mantissa)
                        .Add(modelInfo["baseRatePerBlock"]).Value;
                }
                else
                {
                    var normalRate = new BigIntValue(modelInfo["kink"]).Mul(modelInfo["multiplierPerBlock"])
                        .Div(Mantissa)
                        .Add(modelInfo["baseRatePerBlock"]);
                    var excessUtil = new BigIntValue(long.Parse(utilizationRateStr).Sub(modelInfo["kink"]));
                    borrowRateStr = excessUtil.Mul(modelInfo["jumpMultiplierPerBlock"]).Div(Mantissa)
                        .Add(normalRate).Value;
                }
            }

            var rateToPool = new BigIntValue(borrowRateStr).Mul(Mantissa.Sub(reserveFactor)).Div(Mantissa);
            var supplyRateStr = new BigIntValue(utilizationRateStr).Mul(rateToPool).Div(Mantissa).Value;

            var simpleInterestFactor = new BigIntValue(borrowRateStr).Mul(blockDelta); // 当前区块差 * 借贷利率 = 这一段时间的借贷利率
            var interestAccumulated = simpleInterestFactor.Mul(borrowPrior).Div(Mantissa); //累计的利息 = 这一段时间的借贷利率 * 借款总额 
            var totalBorrowsNewStr = interestAccumulated.Add(borrowPrior).Value;
            var totalReservesNewStr =
                interestAccumulated.Mul(reserveFactor).Div(Mantissa).Add(reservesPrior).Value;
            var borrowIndexNew = simpleInterestFactor.Mul(borrowIndex).Div(Mantissa).Add(borrowIndex);

            var interestAccumulatedParse = long.Parse(interestAccumulated.Value);
            var newBorrowIndex = long.Parse(borrowIndexNew.Value);
            var totalBorrows = long.Parse(totalBorrowsNewStr);
            var borrowRatePerBlock = long.Parse(borrowRateStr);
            var supplyRatePerBlock = long.Parse(supplyRateStr);
            var totalReserves = long.Parse(totalReservesNewStr);

            info.Add("interestAccumulated", interestAccumulatedParse);
            info.Add("newBorrowIndex", newBorrowIndex);
            info.Add("totalBorrows", totalBorrows);
            info.Add("borrowRatePerBlock", borrowRatePerBlock);
            info.Add("supplyRatePerBlock", supplyRatePerBlock);
            info.Add("totalReserves", totalReserves);

            return info;
        }

        private long CalculateBorrowBalance( long originBorrowBalance, long borrowIndex, long originBorrowIndex)
        {
            var borrowBalanceStr = 
                new BigIntValue(borrowIndex).Mul(originBorrowBalance).Div(originBorrowIndex).Value;
            return long.Parse(borrowBalanceStr);
        }

        private long CalculateMintAmount(long underlyingTokenAmount, long exchangeRate)
        {
            var mintTokensStr =  new BigIntValue(ExchangeMantissa).Mul(underlyingTokenAmount).Div(exchangeRate).Value;
            return long.Parse(mintTokensStr);
        }


        [TestMethod]
        [DataRow("LLL", 1000_0000000000)]
        [DataRow("MMM", 10000_00000000)]
        [DataRow("TEST", 10000_00000000)]
        [DataRow("USDT", 1000_000000)]
        [DataRow("ELF", 1000_00000000)]
        public void TransferTokenForTest(string symbol, long amount)
        {
            var account = TestAccount;
            CheckBalance(symbol, account, amount);
        }

        [TestMethod]
        public void IssueToken()
        {
            _tokenContract.IssueBalance(InitAccount, _awakenFinanceControllerContract.ContractAddress, 100000_00000000,
                _platformTokenSymbol);
        }

        [TestMethod]
        public void GetBalances()
        {
            var contractBalance = _tokenContract.GetUserBalance(_awakenATokenContract.ContractAddress, "ELF");
            Logger.Info(contractBalance);
        }
    }
}