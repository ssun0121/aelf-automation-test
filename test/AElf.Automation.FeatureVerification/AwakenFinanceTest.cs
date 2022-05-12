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
        private AwakenTokenContract _awakenTokenContract;
        private AwakenFinanceATokenContract _awakenATokenContract;
        private AwakenFinanceControllerContract _awakenFinanceControllerContract;
        private AwakenFinanceInterestRateModelContract _awakenFinanceInterestRateModelContract;
        private AwakenFinanceLendingLensContract _awakenFinanceLendingLensContract;
        private AwakenTestPriceContract _testPriceContract;

        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }
        
        private string tokenAddress = "2u6Dd139bHvZJdZ835XnNKL5y6cxqzV9PEWD5fZdQXdFZLgevc";
        private string aTokenAddress = "DHo2K7oUXXq3kJRs1JpuwqBJP56gqoaeSKFfuvr9x8svf3vEJ";
        private string controllerAddress = "2hqsqJndRAZGzk96fsEvyuVBTAvoBjcuwTjkuyJffBPueJFrLa";
        private string interestRateModelAddress = "SsSqZWLf7Dk9NWyWyvDwuuY5nzn5n99jiscKZgRPaajZP5p8y";
        private string lendingLensAddress = "GwsSp1MZPmkMvXdbfSCDydHhZtDpvqkFpmPvStYho288fb7QZ";
        private string testPriceAddress = "2M24EKAecggCnttZ9DUUMCXi4xC67rozA87kFgid9qEwRUMHTs";

        private string InitAccount { get; } = "nn659b9X1BLhnu5RWmEUbuuV7J9QKVVSN54j9UmeCbF3Dve5D";
        private string TestAccount { get; } = "Zz4iuCCCktGjZGmQ9vMcxh2JT9pDTFA4XSR7WDNrndYcEXtRx";
        private string TestAccount2 { get; } = "2asoy1ZbqcxguA2TBaMt5V1nFpfsE4bAdCUrG1YK9CCXnTCJKN";
        private string TestAccount3 { get; } = "1Fy4ar9CjDuwQTbde3syqGzY5Wzfvoj4uwcJPNtWMCNS6USYS";

        private string FeeToAccount { get; } = "Zz4iuCCCktGjZGmQ9vMcxh2JT9pDTFA4XSR7WDNrndYcEXtRx";
        private static string RpcUrl { get; } = "http://172.25.127.105:8000";

        private long _baseRatePerYear = 0;
        private long _multiplierPerYear = 57500000000000000;
        private long _jumpMultiplierPerYear = 3000000000000000000;
        private long _kink = 800000000000000000;
        private const long Mantissa = 1_000000000000000000;
        private string _platformTokenSymbol = "AWAKEN";

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("AwakenFinanceTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes");

            NodeManager = new NodeManager(RpcUrl);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
            _awakenTokenContract = tokenAddress == ""
                ? new AwakenTokenContract(NodeManager, InitAccount)
                : new AwakenTokenContract(NodeManager, InitAccount, tokenAddress);
            _awakenATokenContract = aTokenAddress == ""
                ? new AwakenFinanceATokenContract(NodeManager, InitAccount)
                : new AwakenFinanceATokenContract(NodeManager, aTokenAddress, InitAccount);
            _awakenFinanceControllerContract = controllerAddress == ""
                ? new AwakenFinanceControllerContract(NodeManager, InitAccount)
                : new AwakenFinanceControllerContract(NodeManager, controllerAddress, InitAccount);
            _awakenFinanceInterestRateModelContract = interestRateModelAddress == ""
                ? new AwakenFinanceInterestRateModelContract(NodeManager, InitAccount)
                : new AwakenFinanceInterestRateModelContract(NodeManager, interestRateModelAddress, InitAccount);
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
        [DataRow("LLL", 100)]
        [DataRow("MMM", 1)]
        [DataRow("TEST", 10)]
        [DataRow("USDT", 100)]
        [DataRow("ELF", 20)]
        public void SetPrice(string symbol, long price)
        {
            var setPrice = _testPriceContract.ExecuteMethodWithResult(
                PriceMethod.SetPrice, new SetPriceInput
                {
                    TokenSymbol = symbol,
                    Price = price
                });
            setPrice.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        #region User Action

        [TestMethod]
        public void EnterMarkets()
        {
            var listToken = new List<string> { "ELF" };
            var listAToken = new List<Address>();
            foreach (var aToken in listToken.Select(t => _awakenATokenContract.GetATokenAddress(t)))
            {
                Logger.Info(aToken);
                listAToken.Add(aToken);
            }

            var user = InitAccount;
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

            // foreach (var checkMarket in listAToken.Select(aToken => _awakenFinanceControllerContract.GetMarket(aToken)))
            //     checkMarket.AccountMembership[user].ShouldBeTrue();

            var checkAccountAssets = _awakenFinanceControllerContract.GetAssetsIn(user.ConvertAddress());
            checkAccountAssets.Assets.Count.ShouldBe(listToken.Count);
            checkAccountAssets.Assets.ShouldBe(listAToken);
        }

        [TestMethod]
        [DataRow("ELF", 1000000000000)]
        public void MintToken(string mintToken, long amount)
        {
            var user = TestAccount2;
            CheckBalance(mintToken, user, amount);
            var aToken = _awakenATokenContract.GetATokenAddress(mintToken);
            _awakenATokenContract.SetAccount(user);
            ApproveToken(mintToken, user, amount);
            var origin = Verify(user, mintToken, aToken);
            var exchangeRate = CalculateExchangeRate(aToken);
            var mintTokensStr = new BigIntValue(Mantissa).Mul(amount).Div(exchangeRate).Value;
            var aTokenAmount = long.Parse(mintTokensStr);
            Logger.Info($"\nExchangeRate: {exchangeRate}\nATokenAmount: {aTokenAmount}");
            var userMint = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.Mint, new MintInput
            {
                AToken = aToken,
                MintAmount = amount,
                Channel = "channel"
            });
            userMint.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var userMintFee = userMint.GetDefaultTransactionFee();
            var after = Verify(user, mintToken, aToken);
            origin["userBalance"].ShouldBe(mintToken.Equals("ELF")
                ? after["userBalance"].Add(amount).Add(userMintFee)
                : after["userBalance"].Add(amount));
            origin["contractBalance"].ShouldBe(after["contractBalance"].Sub(amount));
            origin["userATokenBalance"].ShouldBe(after["userATokenBalance"].Sub(aTokenAmount));
            // origin["totalReserves"].ShouldBe(after["totalReserves"].Sub(amount));
            origin["totalSupply"].ShouldBe(after["totalSupply"].Sub(aTokenAmount));
            origin["totalCash"].ShouldBe(after["totalCash"].Sub(amount));
            
            var logs = userMint.Logs.First(l => l.Name.Equals(nameof(Mint)));
            var mintLogs = Mint.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed));
            mintLogs.Sender.ShouldBe(user.ConvertAddress());
            mintLogs.UnderlyingAmount.ShouldBe(amount);
            mintLogs.AToken.ShouldBe(aToken);
            mintLogs.ATokenAmount.ShouldBe(aTokenAmount);
            mintLogs.Underlying.ShouldBe(mintToken);
            mintLogs.Channel.ShouldBe("channel");
            
            var interestLogs = AccrueInterest.Parser.ParseFrom(
                ByteString.FromBase64(userMint.Logs.First(l => l.Name.Equals(nameof(AccrueInterest))).NonIndexed));
            interestLogs.Cash.ShouldBe(origin["totalCash"]);
            interestLogs.AToken.ShouldBe(aToken);
            Logger.Info(interestLogs);
        }

        //按AToken还款
        [TestMethod]
        [DataRow("ELF", 100000000000)]
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
            var actualATokenAmount = long.Parse(new BigIntValue(actualRedeemAmount).Mul(Mantissa).Div(exchangeRate).Value) ;

            Logger.Info($"redeem amount: {actualRedeemAmount}");

            var userRedeem = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.RedeemUnderlying, new RedeemUnderlyingInput
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
            // origin["totalReserves"].ShouldBe(after["totalReserves"].Sub(amount));
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
            // origin["totalReserves"].ShouldBe(after["totalReserves"].Sub(amount));
            origin["totalSupply"].ShouldBe(after["totalSupply"].Add(redeemTokenAmount));
            origin["totalCash"].ShouldBe(after["totalCash"].Add(redeemAmount));

            var interestLogs = AccrueInterest.Parser.ParseFrom(
                ByteString.FromBase64(userRedeem.Logs.First(l => l.Name.Equals(nameof(AccrueInterest))).NonIndexed));
            interestLogs.Cash.ShouldBe(origin["totalCash"]);
            interestLogs.AToken.ShouldBe(aToken);
            Logger.Info(interestLogs);
        }

        [TestMethod]
        [DataRow("ELF", 10000000)]
        public void BorrowToken(string borrowToken, long amount)
        {
            var user = NodeManager.AccountManager.NewAccount("12345678");
            _tokenContract.TransferBalance(InitAccount, user, 10_00000000, "ELF");
            var aToken = _awakenATokenContract.GetATokenAddress(borrowToken);
            _awakenATokenContract.SetAccount(user);
            var origin = Verify(user, borrowToken, aToken);
            
            var userBorrow = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.Borrow, new BorrowInput
            {
                AToken = aToken, 
                Amount= amount,
                Channel = "channel"
            });
            userBorrow.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var logs = userBorrow.Logs.First(l => l.Name.Equals(nameof(Borrow)));
            var borrowLogs = Borrow.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed));
            borrowLogs.Borrower.ShouldBe(user.ConvertAddress());
            borrowLogs.Amount.ShouldBe(amount);
            borrowLogs.AToken.ShouldBe(aToken);
            // borrowLogs.BorrowBalance.ShouldBe();
            // borrowLogs.TotalBorrows.ShouldBe();
            Logger.Info(borrowLogs);
            
            var after = Verify(user, borrowToken, aToken);

            var interestLogs = AccrueInterest.Parser.ParseFrom(
                ByteString.FromBase64(userBorrow.Logs.First(l => l.Name.Equals(nameof(AccrueInterest))).NonIndexed));
            interestLogs.Cash.ShouldBe(origin["totalCash"]);
            interestLogs.AToken.ShouldBe(aToken);
            Logger.Info(interestLogs);
        }
        
        //需要计算还款时候的高度来判断剩余多少borrowAmount 
        [TestMethod]
        [DataRow("TEST", 1000000000)]
        public void RepayBorrowToken(string repayToken, long amount)
        {
            var user = TestAccount;
            var aToken = _awakenATokenContract.GetATokenAddress(repayToken);
            _awakenATokenContract.SetAccount(user);
            ApproveToken(repayToken, user, amount);
            var origin = Verify(user, repayToken, aToken);
            var userSnapshot = _awakenATokenContract.GetAccountSnapshot(user, aToken);
            var actualRepayAmount = amount > userSnapshot.BorrowBalance ? userSnapshot.BorrowBalance : amount;
            Logger.Info($"actualRepayAmount: {actualRepayAmount}");
            var userRepay = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.RepayBorrow, new RepayBorrowInput
            {
                AToken = aToken, 
                Amount= actualRepayAmount
            });
            userRepay.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var repayBlockNumber = userRepay.BlockNumber;
            
            var userRepayFee = userRepay.GetDefaultTransactionFee();
            var logs = userRepay.Logs.First(l => l.Name.Equals(nameof(RepayBorrow)));
            var repayLogs = RepayBorrow.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed));
            repayLogs.Borrower.ShouldBe(user.ConvertAddress());
            repayLogs.Payer.ShouldBe(user.ConvertAddress());
            repayLogs.Amount.ShouldBe(amount);
            repayLogs.AToken.ShouldBe(aToken);
            // repayLogs.BorrowBalance.ShouldBe(userSnapshot.BorrowBalance.Sub(actualRepayAmount));
            // repayLogs.TotalBorrows.ShouldBe(origin["totalBorrow"].Sub(actualRepayAmount));
            Logger.Info(repayLogs);
            
            var after = Verify(user, repayToken, aToken);
            origin["userBalance"].ShouldBe(repayToken.Equals("ELF")
                ? after["userBalance"].Add(actualRepayAmount).Add(userRepayFee)
                : after["userBalance"].Add(actualRepayAmount));
            origin["contractBalance"].ShouldBe(after["contractBalance"].Sub(actualRepayAmount));
            origin["userATokenBalance"].ShouldBe(after["userATokenBalance"]);
            origin["totalBorrow"].ShouldBe(after["totalBorrow"].Add(actualRepayAmount));
            
            var interestLogs = AccrueInterest.Parser.ParseFrom(
                ByteString.FromBase64(userRepay.Logs.First(l => l.Name.Equals(nameof(AccrueInterest))).NonIndexed));
            interestLogs.Cash.ShouldBe(origin["totalCash"]);
            interestLogs.AToken.ShouldBe(aToken);
            Logger.Info(interestLogs);
        }
        

        #endregion

        
        [TestMethod]
        [DataRow("LLL", 1_000000000000000000)]
        [DataRow("MMM", 1_0000000000000000)]
        [DataRow("TEST", 1_00000000000000000)]
        [DataRow("USDT", 1_000000000000000000)]
        [DataRow("ELF", 1_000000000000000000)]
        public void CreateAToken(string underlyingSymbol, long initialExchangeRate)
        {
            var result = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.Create, new CreateInput
            {
                InitialExchangeRate = initialExchangeRate,
                InterestRateModel = _awakenFinanceInterestRateModelContract.Contract,
                UnderlyingSymbol = underlyingSymbol
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var aToken = _awakenATokenContract.GetATokenAddress(underlyingSymbol);
            Logger.Info(aToken);
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
                        InterestRateModelType = true
                    });
            interestRateModeInitializeResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            GetInterestModeInfo();
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
        // [DataRow("LLL")]
        // [DataRow("MMM")]
        // [DataRow("TEST")]
        // [DataRow("USDT")]
        [DataRow("ELF")]
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
        [DataRow("LLL", 800000000000000000)]
        [DataRow("MMM", 750000000000000000)]
        [DataRow("TEST", 750000000000000000)]
        [DataRow("USDT", 700000000000000000)]
        [DataRow("ELF", 750000000000000000)]
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
            var changeCloseFactor = 70000000000000000;
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
            var changeLiquidationIncentive = 1000000000000000000;
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
            var changeMaxAssets = 5;
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
            var changePlatformTokenRate = 10000000;
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
            var listToken = new List<string> {"ELF"};
            var listAToken = new List<Address>();
            foreach (var aToken in listToken.Select(t => _awakenATokenContract.GetATokenAddress(t)))
            {
                Logger.Info(aToken);
                listAToken.Add(aToken);
            }

            // var aTokenAddress = _awakenATokenContract.GetATokenAddress("ELF");
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
        [TestMethod]
        public void SetReserveFactor(string underlyingSymbol)
        {
            var aToken = _awakenATokenContract.GetATokenAddress(underlyingSymbol);
            Logger.Info(aToken);
            var changeReserveFactor = 500000000000000000;
            var oldReserveFactor = _awakenATokenContract.GetReserveFactor(aToken);

            var result = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.SetReserveFactor,
                new SetReserveFactorInput { AToken = aToken, ReserveFactor = changeReserveFactor });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

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
        public void GetMarketInfo()
        {
            var user = InitAccount;
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
        public void GetPlatformTokenSpeeds(string token)
        {
            var aToken = _awakenATokenContract.GetATokenAddress(token);
            var speeds = _awakenFinanceControllerContract.GetPlatformTokenSpeeds(aToken);
            Logger.Info(speeds);
        }
        #endregion

        #region Controller Check

        [TestMethod]
        [DataRow(100000,"ELF")]
        public void BorrowAllowed(long borrowAmount, string underlyingToken)
        {
            var borrower = NodeManager.NewAccount("12345678");
            var aToken = _awakenATokenContract.GetATokenAddress(underlyingToken);
            
            var result =
                _awakenFinanceControllerContract.ExecuteMethodWithResult(ControllerMethod.BorrowAllowed,
                    new BorrowAllowedInput
                    {
                        BorrowAmount = borrowAmount,
                        Borrower = borrower.ConvertAddress(),
                        AToken = aToken
                    });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
        }


        #endregion

        private void GetInterestModeInfo()
        {
            var getBaseRate = _awakenFinanceInterestRateModelContract.GetBaseRatePerBlock();
            var getMultiplier = _awakenFinanceInterestRateModelContract.GetMultiplierPerBlock();
            var getKink = _awakenFinanceInterestRateModelContract.GetKink();
            var getJump = _awakenFinanceInterestRateModelContract.GetJumpMultiplierPerBlock();
            Logger.Info($"{getBaseRate}, {getMultiplier}, {getKink}, {getJump}");
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
                var issue = _tokenContract.IssueBalance(tokenInfo.Issuer.ToBase58(), user, mintAmount.Add(1000000000), symbol);
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

            var totalBorrow = _awakenATokenContract.GetTotalBorrows(aToken);
            var totalReserves = _awakenATokenContract.GetTotalReserves(aToken);
            var totalCash = _awakenATokenContract.GetCash(aToken);
            var totalSupply = _awakenATokenContract.GetTotalSupply(aToken);

            infoList.Add("userBalance", userBalance);
            infoList.Add("contractBalance", contractBalance);
            infoList.Add("userATokenBalance", userATokenBalance);
            infoList.Add("totalBorrow", totalBorrow);
            infoList.Add("totalReserves", totalReserves);
            infoList.Add("totalCash", totalCash);
            infoList.Add("totalSupply", totalSupply);

            Logger.Info($"\nuserBalance: {userBalance}\n" +
                        $"contractBalance: {contractBalance}\n" +
                        $"userATokenBalance: {userATokenBalance}\n" +
                        $"totalBorrow: {totalBorrow}\n" +
                        $"totalReserves: {totalReserves}\n" +
                        $"totalCash: {totalCash}\n" +
                        $"totalSupply: {totalSupply}\n");
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
            var exchangeRateStr = new BigIntValue(totalCash).Add(totalBorrow).Sub(totalReserves).Mul(Mantissa)
                .Div(totalSupply).Value;
            return long.Parse(exchangeRateStr);
        }

        // private long GetBorrowBalance(long borrowIndexHeight, long accrualBlockNumbers)
        // {
        //     var blockDelta = borrowIndexHeight.Sub(accrualBlockNumbers);
        //     var simpleInterestFactor = new BigIntValue(borrowRate).Mul(blockDelta);
        //     var interestAccumulated = simpleInterestFactor.Mul(borrowPrior).Div(Mantissa);
        //     
        //     var totalBorrowsNewStr = interestAccumulated.Add(borrowPrior).Value;
        //     var totalReservesNewStr =
        //         interestAccumulated.Mul(State.ReserveFactor[aToken]).Div(Mantissa).Add(reservesPrior).Value;
        //     var borrowIndexNewStr = simpleInterestFactor.Mul(borrowIndexPrior).Div(Mantissa).Add(borrowIndexPrior).Value;
        //     
        //     var borrowBalanceStr = new BigIntValue(borrowIndex).Mul(principal).Div(borrowSnapshot.InterestIndex).Value;
        //
        // }
        
        
        
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
            _tokenContract.IssueBalance(InitAccount, _awakenFinanceControllerContract.ContractAddress, 1000_00000000,
                _platformTokenSymbol);
        }

        [TestMethod]
        public void CheckElement()
        {
            Logger.Info(long.MaxValue);
        }
    }
}