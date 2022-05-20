using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Awaken.Contracts.AToken;
using Awaken.Contracts.Controller;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class AwakenFinanceOtherTest
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
        private string tokenAddress = "2n7M1JZTEWe8AduWTFrqfbfoGwbttUdD9uaQoytFxURxQwpMK4";
        private string aTokenAddress = "pL9HkouMwrHXYpzFYZsgQwP2u2LhLsdxNeWTV1Je4mmitpRbW";
        private string controllerAddress = "2NdoyR9rw5LHqTAoXqqPa9sdXiN2sK7wzWuLcYWokthjwVePWv";
        private string interestRateModelAddress = "2CRLguiYL1oGN5U4LuoAvHUMSFeeckQ1NKAKe1bNEL1rM1bzz";
        private string lendingLensAddress = "S69UaMkTgiyjWd3rtYFWYX26CVbAdooLjfaB3XotN2sBF7fvy";
        private string testPriceAddress = "kP3YgKgEiiUwKMb1F7XkAaBrEUbAn21JJeEqEErttakMSPec2";
        private string InitAccount { get; } = "nn659b9X1BLhnu5RWmEUbuuV7J9QKVVSN54j9UmeCbF3Dve5D";
        private string TestAccount2 { get; } = "2Pm6opkpmnQedgc5yDUoVmnPpTus5vSjFNDmSbckMzLw22W6Er";
        private string TestAccount3 { get; } = "2MdZqEwXJGtseRQFKC9yiDxNBYP45Q9JWePEF7fwQ2mffC9G3n";
        private static string RpcUrl { get; } = "http://172.25.127.105:8000";

        private const long Mantissa = 1_000000000000000000;

        private const long ExchangeMantissa = 1_00000000;

        // private const long Mantissa = 1_00000000;
        private string _platformTokenSymbol = "AWAKEN";

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("AwakenFinanceTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env1-main");

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
        [DataRow("AAA")]
        public void MintTokenErrorTest(string mintToken)
        {
            var user = TestAccount2;
            var aToken = _awakenATokenContract.GetATokenAddress(mintToken);
            var userBalance = _tokenContract.GetUserBalance(user, mintToken);
            var illegalMintAmount = userBalance.Add(1);

            _awakenATokenContract.SetAccount(user);
            ApproveToken(mintToken, user, illegalMintAmount);

            var mintPaused = _awakenFinanceControllerContract.ExecuteMethodWithResult(
                ControllerMethod.SetMintPaused,
                new SetPausedInput
                {
                    AToken = aToken,
                    State = false
                });
            mintPaused.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            {
                var userMint = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.Mint, new MintInput
                {
                    AToken = aToken,
                    MintAmount = illegalMintAmount,
                    Channel = "channel"
                });
                userMint.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                userMint.Error.ShouldContain("Insufficient balance");
            }

            {
                var userMint = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.Mint, new MintInput
                {
                    AToken = aToken,
                    MintAmount = 0,
                    Channel = "channel"
                });
                userMint.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                userMint.Error.ShouldContain("Invalid amount.");
            }

            {
                var userMint = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.Mint, new MintInput
                {
                    AToken = aToken,
                    MintAmount = -1,
                    Channel = "channel"
                });
                userMint.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                userMint.Error.ShouldContain("Invalid amount.");
            }

            {
                mintPaused = _awakenFinanceControllerContract.ExecuteMethodWithResult(
                    ControllerMethod.SetMintPaused,
                    new SetPausedInput
                    {
                        AToken = aToken,
                        State = true
                    });
                mintPaused.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                var userMint = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.Mint, new MintInput
                {
                    AToken = aToken,
                    MintAmount = userBalance,
                    Channel = "channel"
                });
                userMint.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                userMint.Error.ShouldContain("Mint is paused");
            }
        }

        [TestMethod]
        [DataRow("ELF")]
        public void RedeemErrorTest(string redeemToken)
        {
            var user = TestAccount2;
            var aToken = _awakenATokenContract.GetATokenAddress(redeemToken);
            _awakenATokenContract.SetAccount(user);
            var origin = Verify(user, redeemToken, aToken);

            //ATokenAmount
            var redeemTokenAmount = origin["userATokenBalance"];
            var illegalRedeemAmount = redeemTokenAmount.Add(1);

            {
                var userRedeem = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.Redeem, new RedeemInput
                {
                    AToken = aToken,
                    Amount = illegalRedeemAmount
                });

                userRedeem.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                userRedeem.Error.ShouldContain("Insufficient Token Cash");
            }

            {
                var userRedeem = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.Redeem, new RedeemInput
                {
                    AToken = aToken,
                    Amount = 0
                });

                userRedeem.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                userRedeem.Error.ShouldContain("Invalid amount.");
            }

            {
                var userRedeem = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.Redeem, new RedeemInput
                {
                    AToken = aToken,
                    Amount = -1
                });

                userRedeem.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                userRedeem.Error.ShouldContain("Invalid amount.");
            }
        }

        [TestMethod]
        [DataRow("AAA")]
        public void RedeemUnderlyingErrorTest(string redeemToken)
        {
            var user = TestAccount2;
            var aToken = _awakenATokenContract.GetATokenAddress(redeemToken);
            _awakenATokenContract.SetAccount(user);

            var origin = Verify(user, redeemToken, aToken);
            var exchangeRate = CalculateExchangeRate(aToken);
            //ATokenAmount
            var redeemTokenAmount = origin["userATokenBalance"];
            //UnderlyingTokenAmount
            var redeemAmountStr = new BigIntValue(exchangeRate).Mul(redeemTokenAmount).Div(ExchangeMantissa).Value;
            var calculateRedeemAmount = long.Parse(redeemAmountStr);
            var illegalRedeemAmount = calculateRedeemAmount.Add(1);

            {
                var userRedeem = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.RedeemUnderlying,
                    new RedeemUnderlyingInput
                    {
                        AToken = aToken,
                        Amount = illegalRedeemAmount
                    });
                userRedeem.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                userRedeem.Error.ShouldContain("Insufficient Token Cash");
            }

            {
                var userRedeem = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.RedeemUnderlying,
                    new RedeemUnderlyingInput
                    {
                        AToken = aToken,
                        Amount = 0
                    });
                userRedeem.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                userRedeem.Error.ShouldContain("Invalid amount.");
            }

            {
                var userRedeem = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.RedeemUnderlying,
                    new RedeemUnderlyingInput
                    {
                        AToken = aToken,
                        Amount = -1
                    });
                userRedeem.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                userRedeem.Error.ShouldContain("Invalid amount.");
            }
        }

        [TestMethod]
        [DataRow("AAA", 30_00000000)]
        public void BorrowOtherToken(string borrowToken, long amount)
        {
            // var user = NodeManager.AccountManager.NewAccount("wanghuan");
            // _tokenContract.TransferBalance(InitAccount, user, 10_00000000, "ELF");

            var user = TestAccount3;
            _tokenContract.TransferBalance(InitAccount, user, 10_00000000, "ELF");

            var token1 = _awakenATokenContract.GetATokenAddress("BBB");
            var token2 = _awakenATokenContract.GetATokenAddress("AAA");
            var balanceBefore1 = _awakenATokenContract.GetBalance(user, token1);
            var balanceBefore2 = _awakenATokenContract.GetBalance(user, token2);

            var aToken = _awakenATokenContract.GetATokenAddress(borrowToken);
            _awakenATokenContract.SetAccount(user);
            var origin = Verify(user, borrowToken, aToken);

            var userBorrow = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.Borrow, new BorrowInput
            {
                AToken = aToken,
                Amount = amount,
                Channel = "channel"
            });

            userBorrow.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var after = Verify(user, borrowToken, aToken);

            var balanceAfter1 = _awakenATokenContract.GetBalance(user, token1);
            var balanceAfter2 = _awakenATokenContract.GetBalance(user, token2);
            Logger.Info($"\n---------------------" +
                        $"\nbalanceBefore1:{balanceBefore1}" +
                        $"\nbalanceBefore2:{balanceBefore2}" +
                        $"\nbalanceAfter1:{balanceAfter1}" +
                        $"\nbalanceAfter2:{balanceAfter2}");
        }

        [TestMethod]
        [DataRow("AAA", 1_00000000)]
        public void BorrowErrorToken(string borrowToken, long amount)
        {
            var user = NodeManager.AccountManager.NewAccount("wanghuan");
            _tokenContract.TransferBalance(InitAccount, user, 10_00000000, "ELF");
            var aToken = _awakenATokenContract.GetATokenAddress(borrowToken);
            _awakenATokenContract.SetAccount(user);
            var origin = Verify(user, borrowToken, aToken);

            var setBorrowPaused = _awakenFinanceControllerContract.ExecuteMethodWithResult(
                ControllerMethod.SetBorrowPaused,
                new SetPausedInput
                {
                    AToken = aToken,
                    State = false
                });
            setBorrowPaused.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            {
                var userBorrow = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.Borrow, new BorrowInput
                {
                    AToken = aToken,
                    Amount = 0,
                    Channel = "channel"
                });
                userBorrow.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                userBorrow.Error.ShouldContain("Invalid amount.");
            }

            {
                var userBorrow = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.Borrow, new BorrowInput
                {
                    AToken = aToken,
                    Amount = -1,
                    Channel = "channel"
                });
                userBorrow.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                userBorrow.Error.ShouldContain("Invalid amount.");
            }

            {
                setBorrowPaused = _awakenFinanceControllerContract.ExecuteMethodWithResult(
                    ControllerMethod.SetBorrowPaused,
                    new SetPausedInput
                    {
                        AToken = aToken,
                        State = true
                    });
                setBorrowPaused.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                var userBorrow = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.Borrow, new BorrowInput
                {
                    AToken = aToken,
                    Amount = amount,
                    Channel = "channel"
                });
                userBorrow.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                userBorrow.Error.ShouldContain("Borrow is paused");
            }
        }

        [TestMethod]
        public void MintBorrowWithoutEnterMarketsTest()
        {
            var mintUnderlyingToken = "BBB";
            var borrowUnderlyingToken = "CCC";
            var mintAmount = 100_00000000;
            var borrowAmount = 100_00000000;

            var listToken = new List<string> {mintUnderlyingToken, borrowUnderlyingToken};
            var listAToken = new List<Address>();
            foreach (var aToken in listToken.Select(t => _awakenATokenContract.GetATokenAddress(t)))
            {
                Logger.Info(aToken);
                listAToken.Add(aToken);
            }

            var user = NodeManager.AccountManager.NewAccount("wanghuan");
            _tokenContract.TransferBalance(InitAccount, user, 10_00000000, "ELF");

            var checkAccountAssets = _awakenFinanceControllerContract.GetAssetsIn(user.ConvertAddress());
            checkAccountAssets.Assets.ShouldNotContain(listAToken[0]);

            // Mint
            CheckBalance(mintUnderlyingToken, user, mintAmount);
            ApproveToken(mintUnderlyingToken, user, mintAmount);
            _awakenATokenContract.SetAccount(user);
            var userMint = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.Mint, new MintInput
            {
                AToken = listAToken[0],
                MintAmount = mintAmount,
                Channel = "channel"
            });
            userMint.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            checkAccountAssets = _awakenFinanceControllerContract.GetAssetsIn(user.ConvertAddress());
            checkAccountAssets.Assets.ShouldNotContain(listAToken[0]);

            // Borrow
            _tokenContract.TransferBalance(InitAccount, user, 10_00000000, "ELF");
            var before = Verify(user, borrowUnderlyingToken, listAToken[1]);
            _awakenATokenContract.SetAccount(user);
            var userBorrow = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.Borrow, new BorrowInput
            {
                AToken = listAToken[1],
                Amount = borrowAmount,
                Channel = "channel"
            });
            userBorrow.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            userBorrow.Error.ShouldContain("Insufficient liquidity");

            // Enter markets
            _awakenFinanceControllerContract.SetAccount(user);
            var enterResult = _awakenFinanceControllerContract.ExecuteMethodWithResult(
                ControllerMethod.EnterMarkets, new ATokens
                {
                    AToken = {listAToken[0]}
                });
            enterResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            checkAccountAssets = _awakenFinanceControllerContract.GetAssetsIn(user.ConvertAddress());
            checkAccountAssets.Assets.ShouldContain(listAToken[0]);
            Logger.Info($"checkAccountAssets:{checkAccountAssets}");

            // Borrow for the second time
            _awakenATokenContract.SetAccount(user);
            userBorrow = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.Borrow, new BorrowInput
            {
                AToken = listAToken[1],
                Amount = borrowAmount,
                Channel = "channel"
            });
            userBorrow.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var after = Verify(user, borrowUnderlyingToken, listAToken[1]);
        }

        [TestMethod]
        public void ExitMarketWithoutBorrowBalanceTest()
        {
            var listToken = new List<string> {"CCC"};
            var listAToken = new List<Address>();
            foreach (var aToken in listToken.Select(t => _awakenATokenContract.GetATokenAddress(t)))
            {
                Logger.Info(aToken);
                listAToken.Add(aToken);
            }

            var user = InitAccount;

            // Enter markets
            _awakenFinanceControllerContract.SetAccount(user);
            var enterResult = _awakenFinanceControllerContract.ExecuteMethodWithResult(
                ControllerMethod.EnterMarkets, new ATokens
                {
                    AToken = {listAToken}
                });
            enterResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var checkAccountAssets = _awakenFinanceControllerContract.GetAssetsIn(user.ConvertAddress());
            checkAccountAssets.Assets.ShouldContain(listAToken[0]);
            Logger.Info($"checkAccountAssets:{checkAccountAssets}");

            // Exit market
            var exitMarketResult = _awakenFinanceControllerContract.ExecuteMethodWithResult(
                ControllerMethod.ExitMarket, listAToken[0]);
            exitMarketResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            checkAccountAssets = _awakenFinanceControllerContract.GetAssetsIn(user.ConvertAddress());
            checkAccountAssets.Assets.ShouldNotContain(listAToken[0]);
            Logger.Info($"checkAccountAssets:{checkAccountAssets}");
        }

        [TestMethod]
        public void ExitMarketErrorTest()
        {
            var mintUnderlyingToken = "CCC";
            var mintAmount = 100_00000000;
            var listToken = new List<string> {mintUnderlyingToken};
            var listAToken = new List<Address>();
            foreach (var aToken in listToken.Select(t => _awakenATokenContract.GetATokenAddress(t)))
            {
                Logger.Info(aToken);
                listAToken.Add(aToken);
            }

            var user = InitAccount;
            // Mint
            CheckBalance(mintUnderlyingToken, user, mintAmount);
            ApproveToken(mintUnderlyingToken, user, mintAmount);
            _awakenATokenContract.SetAccount(user);
            var userMint = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.Mint, new MintInput
            {
                AToken = listAToken[0],
                MintAmount = mintAmount,
                Channel = "channel"
            });
            userMint.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Enter markets
            _awakenFinanceControllerContract.SetAccount(user);
            var enterResult = _awakenFinanceControllerContract.ExecuteMethodWithResult(
                ControllerMethod.EnterMarkets, new ATokens
                {
                    AToken = {listAToken}
                });
            enterResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var checkAccountAssets = _awakenFinanceControllerContract.GetAssetsIn(user.ConvertAddress());
            checkAccountAssets.Assets.ShouldContain(listAToken[0]);
            Logger.Info($"checkAccountAssets:{checkAccountAssets}");

            // Borrow
            var borrowAmount = 10_00000000;
            _awakenATokenContract.SetAccount(user);
            var userBorrow = _awakenATokenContract.ExecuteMethodWithResult(ATokenMethod.Borrow, new BorrowInput
            {
                AToken = listAToken[0],
                Amount = borrowAmount,
                Channel = "channel"
            });
            userBorrow.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Exit market
            var exitMarketResult = _awakenFinanceControllerContract.ExecuteMethodWithResult(
                ControllerMethod.ExitMarket, listAToken[0]);
            exitMarketResult.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            exitMarketResult.Error.ShouldContain("Nonzero borrow balance");

            // Set mint paused
            var mintPaused = _awakenFinanceControllerContract.ExecuteMethodWithResult(
                ControllerMethod.SetMintPaused,
                new SetPausedInput
                {
                    AToken = listAToken[0],
                    State = false
                });
            // mintPaused.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);

            var mintGuardianPaused = _awakenFinanceControllerContract.GetMintGuardianPaused(listAToken[0]);
            Logger.Info($"mintGuardianPaused:{mintGuardianPaused}");
        }

        [TestMethod]
        [DataRow("CCC", false)]
        public void SetMintPaused(string underlyingToken, bool state)
        {
            var aToken = _awakenATokenContract.GetATokenAddress(underlyingToken);

            var mintPaused = _awakenFinanceControllerContract.ExecuteMethodWithResult(
                ControllerMethod.SetMintPaused,
                new SetPausedInput
                {
                    AToken = aToken,
                    State = state
                });
            mintPaused.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var mintGuardianPaused = _awakenFinanceControllerContract.GetMintGuardianPaused(aToken);
            mintGuardianPaused.ShouldBe(state);
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
    }
}