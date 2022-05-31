using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using AElf.Client.Dto;
using AElf.Client.MultiToken;
using AElf.Contracts.Genesis;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Awaken.Contracts.Swap;
using Awaken.Contracts.SwapExchangeContract;
using Gandalf.Contracts.DividendPoolContract;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using log4net;
using log4net.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.Asn1.X509;
using Shouldly;
using Shouldly.Configuration;
using ApproveInput = AElf.Contracts.MultiToken.ApproveInput;
using CreateInput = AElf.Contracts.MultiToken.CreateInput;
using GetBalanceOutput = AElf.Contracts.MultiToken.GetBalanceOutput;
using IssueInput = AElf.Contracts.MultiToken.IssueInput;
using TransferInput = AElf.Contracts.MultiToken.TransferInput;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class SwapExchangeTest
    {
        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;
        private SwapExchangeContract _swapExchangeContract;
        private AwakenSwapContract _awakenSwapContract;
        private AwakenTokenContract _awakenTokenContract;
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }

        private string awakenSwapAddress = "";
        private string awakenTokenAddress = "";
        private string swapExchangeAddress = "";
        private string InitAccount { get; } = "nn659b9X1BLhnu5RWmEUbuuV7J9QKVVSN54j9UmeCbF3Dve5D";
        private string UserA { get; } = "YUW9zH5GhRboT5JK4vXp5BLAfCDv28rRmTQwo418FuaJmkSg8";
        private string Receiver { get; } = "AviSYTKSFpNZwHwuAKGWQFtBQ4oG6babJJU7WtZexx8bNNAn5";
        private static string RpcUrl { get; } = "172.25.127.105:8000";
        private static readonly string TARGETTOKEN = "USDTE";
        private const string SymbolUsdt = "USDTE";
        private const string SymbolElff = "ELFF";
        private const string SymbolAave = "AAVE";
        private const string SymbolLink = "LINK";
        private const long TotalSupply = 10000000_00000000;
        private const string ExpansionCoefficient = "1000000000000000000";


        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("SwapExchangeTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env2-main");

            NodeManager = new NodeManager(RpcUrl);
            AuthorityManager = new AuthorityManager(NodeManager, InitAccount);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
            _awakenTokenContract = awakenTokenAddress == ""
                ? new AwakenTokenContract(NodeManager, InitAccount)
                : new AwakenTokenContract(NodeManager, InitAccount, awakenTokenAddress);
            _awakenSwapContract = awakenSwapAddress == ""
                ? new AwakenSwapContract(NodeManager, InitAccount)
                : new AwakenSwapContract(NodeManager, InitAccount, awakenSwapAddress);
            _swapExchangeContract = swapExchangeAddress == ""
                ? new SwapExchangeContract(NodeManager, InitAccount)
                : new SwapExchangeContract(NodeManager, InitAccount, swapExchangeAddress);
        }

        [TestMethod]
        public void InitializeTest()
        {
            
            var initSwap = _awakenSwapContract.ExecuteMethodWithResult(SwapMethod.Initialize, new Awaken.Contracts.Swap.InitializeInput
            {
                Admin = InitAccount.ConvertAddress(),
                AwakenTokenContractAddress = _awakenTokenContract.Contract
            });
            initSwap.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            _swapExchangeContract.Initialize(TARGETTOKEN, _awakenSwapContract.ContractAddress,
                _awakenTokenContract.ContractAddress, Receiver);
            var receiver = _swapExchangeContract.CallViewMethod<Address>(SwapExchangeMethod.Receivor, new Empty());
            receiver.ShouldBe(Receiver.ConvertAddress());
            var targetToken =
                _swapExchangeContract.CallViewMethod<StringValue>(SwapExchangeMethod.TargetToken, new Empty()).Value;
            targetToken.ShouldBe(TARGETTOKEN);

        }

        [TestMethod]
        public void InitInfra()
        {
            InitCommonTokens();
            CreatePairs();
            AddLiquidity();
        }
        
        [TestMethod]
        public void SetReceiverTest()
        {
            var setResult =
                _swapExchangeContract.ExecuteMethodWithResult(SwapExchangeMethod.SetReceivor,
                    Receiver.ConvertAddress());
            setResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void SetTargetToken()
        {
            var setResult = _swapExchangeContract.ExecuteMethodWithResult(SwapExchangeMethod.SetTargetToken,
                new StringValue
                {
                    Value = TARGETTOKEN
                });
            setResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void SetSwapToTargetTokenThresholdTest()
        {
            var lpTokenThreshold = 100;
            var commonTokenThreshold = 100;
            var setResult = _swapExchangeContract.ExecuteMethodWithResult(
                SwapExchangeMethod.SetSwapToTargetTokenThreshold, new Thresholdinput
                {
                    LpTokenThreshold = lpTokenThreshold,
                    CommonTokenThreshold = commonTokenThreshold
                });
            var result =
                _swapExchangeContract.CallViewMethod<ThresholdOutput>(SwapExchangeMethod.Threshold, new Empty());
            result.CommonTokenThreshold.ShouldBe(lpTokenThreshold);
            result.LpTokenThreshold.ShouldBe(commonTokenThreshold);
        }

        [TestMethod]
        public void SatisfiedOrNotWithCommonTokenThresholdTest()
        {
            var setResult = _swapExchangeContract.ExecuteMethodWithResult(
                SwapExchangeMethod.SetSwapToTargetTokenThreshold, new Thresholdinput
                {
                    CommonTokenThreshold = 10000000
                });
            setResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var path = new Dictionary<string, Path>();
            var tokenList = new Awaken.Contracts.SwapExchangeContract.TokenList();
            long totalUsdtOut = 0;

            {
                var amountsOutResult = _awakenSwapContract.GetAmountsOut(new List<string> {SymbolElff, SymbolLink, SymbolUsdt},
                    40_00000000);
                var usdtOut = amountsOutResult.Amount[2];
                totalUsdtOut = totalUsdtOut.Add(usdtOut);
                var expectedPrice = new BigIntValue(ExpansionCoefficient).Mul(usdtOut).Div(40_00000000);
                Logger.Info($"{SymbolElff} usdtout({usdtOut}) expectedPrice({expectedPrice})");
                path[SymbolElff] = new Path
                {
                    Value = {SymbolElff, SymbolLink, SymbolUsdt},
                    ExpectPrice = expectedPrice.Value,
                    SlipPoint = 1
                };
            }
            
            
            {
                var amountsOutResult = _awakenSwapContract.GetAmountsOut(new List<string> {SymbolAave, SymbolLink, SymbolUsdt},
                    1);
                var usdtOut = amountsOutResult.Amount[2];
                var expectedPrice = new BigIntValue(ExpansionCoefficient).Mul(usdtOut).Div(10_00000000);
                Logger.Info($"{SymbolAave} usdtout({usdtOut}) expectedPrice({expectedPrice})");
                path[SymbolAave] = new Path
                {
                    Value = {SymbolAave, SymbolLink, SymbolUsdt},
                    ExpectPrice = expectedPrice.Value,
                    SlipPoint = 1
                };
            }
            
            tokenList.TokensInfo.Add(new Awaken.Contracts.SwapExchangeContract.Token
            {
                Amount = 1,
                TokenSymbol = SymbolAave
            });
            _tokenContract.ExecuteMethodWithResult(TokenMethod.Approve, new ApproveInput
            {
                Amount = 1,
                Spender = _swapExchangeContract.ContractAddress.ConvertAddress(),
                Symbol = SymbolAave
            });
            
            tokenList.TokensInfo.Add(new Awaken.Contracts.SwapExchangeContract.Token
            {
                Amount = 40_00000000,
                TokenSymbol = SymbolElff
            });
            _tokenContract.ExecuteMethodWithResult(TokenMethod.Approve, new ApproveInput
            {
                Amount = 40_00000000,
                Spender = _swapExchangeContract.ContractAddress.ConvertAddress(),
                Symbol = SymbolElff
            });
            
            var receiverBalanceBefore = _tokenContract.GetUserBalance(Receiver, SymbolUsdt);
            var ownerSymbolAaveBalanceBefore = _tokenContract.GetUserBalance(InitAccount, SymbolAave);
            var ownerSymbolElffBalanceBefore = _tokenContract.GetUserBalance(InitAccount, SymbolElff);
            var swapResult = _swapExchangeContract.SwapCommonTokens(path, tokenList);
            swapResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var receiverBalanceAfter = _tokenContract.GetUserBalance(Receiver, SymbolUsdt);

            var swapEventLogStr = swapResult.Logs;
            foreach (var log in swapEventLogStr)
            {
                bool isSwapEvent = log.Name.Equals("SwapResultEvent");
                if (isSwapEvent)
                {
                    var eventLogs = SwapResultEvent.Parser.ParseFrom(ByteString.FromBase64(log.NonIndexed));
                    if (eventLogs.Symbol.Equals(SymbolElff))
                    {
                        eventLogs.Result.ShouldBe(true);
                    }
                    else if (eventLogs.Symbol.Equals(SymbolAave))
                    {
                        eventLogs.Result.ShouldBe(false);
                    }
                    Logger.Info($"log({eventLogs.Amount},{eventLogs.Result},{eventLogs.Symbol})");
                }
            }
            receiverBalanceAfter.Sub(receiverBalanceBefore).ShouldBe(totalUsdtOut);
            _tokenContract.GetUserBalance(InitAccount,SymbolAave).ShouldBe(ownerSymbolAaveBalanceBefore);
            _tokenContract.GetUserBalance(InitAccount,SymbolElff).ShouldBe(ownerSymbolElffBalanceBefore.Sub(40_00000000));
        }

        [TestMethod]
        public void SatisfiedOrNotWithCommonTokensSlipLimitTest()
        {
            var setResult = _swapExchangeContract.ExecuteMethodWithResult(
                SwapExchangeMethod.SetSwapToTargetTokenThreshold, new Thresholdinput
                {
                    CommonTokenThreshold = 500
                });
            setResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var path = new Dictionary<string, Path>();
            var tokenList = new Awaken.Contracts.SwapExchangeContract.TokenList();
            long totalUsdtOut = 0;

            {
                var amountsOutResult = _awakenSwapContract.GetAmountsOut(new List<string> {SymbolAave, SymbolLink, SymbolUsdt},
                    10_00000000);
                var usdtOut = amountsOutResult.Amount[2];
                totalUsdtOut = totalUsdtOut.Add(usdtOut);
                var expectedPrice = new BigIntValue(ExpansionCoefficient).Mul(usdtOut).Div(2).Div(10_00000000);
                Logger.Info($"{SymbolAave} usdtout({usdtOut}) expectedPrice({expectedPrice})");
                path[SymbolAave] = new Path
                {
                    Value = {SymbolAave, SymbolLink, SymbolUsdt},
                    ExpectPrice = expectedPrice.Value,
                    SlipPoint = 1
                };
            }
            {
                var amountsOutResult = _awakenSwapContract.GetAmountsOut(new List<string> {SymbolElff, SymbolLink, SymbolUsdt},
                    40_00000000);
                var usdtOut = amountsOutResult.Amount[2];
                var expectedPrice = new BigIntValue(ExpansionCoefficient).Mul(usdtOut).Div(40_00000000);
                Logger.Info($"{SymbolElff} usdtout({usdtOut}) expectedPrice({expectedPrice})");
                path[SymbolElff] = new Path
                {
                    Value = {SymbolElff, SymbolLink, SymbolUsdt},
                    ExpectPrice = expectedPrice.Value,
                    SlipPoint = 1
                };
            }
            {
                var amountsOutResult = _awakenSwapContract.GetAmountsOut(new List<string> {SymbolLink, SymbolUsdt},
                    20_00000000);
                var usdtOut = amountsOutResult.Amount.Last();
                var expectedPrice = new BigIntValue(ExpansionCoefficient).Mul(usdtOut).Div(20_00000000);
                Logger.Info($"{SymbolLink} usdtout({usdtOut}) expectedPrice({expectedPrice})");
                path[SymbolLink] = new Path
                {
                    Value = {SymbolLink, SymbolUsdt},
                    ExpectPrice = expectedPrice.Value,
                    SlipPoint = 1
                };
            }

            tokenList.TokensInfo.Add(new Awaken.Contracts.SwapExchangeContract.Token
            {
                Amount = 10_00000000,
                TokenSymbol = SymbolAave
            });
            _tokenContract.ExecuteMethodWithResult(TokenMethod.Approve, new ApproveInput
            {
                Amount = 10_00000000,
                Spender = _swapExchangeContract.ContractAddress.ConvertAddress(),
                Symbol = SymbolAave
            });
            
            tokenList.TokensInfo.Add(new Awaken.Contracts.SwapExchangeContract.Token
            {
                Amount = 40_00000000,
                TokenSymbol = SymbolElff
            });
            _tokenContract.ExecuteMethodWithResult(TokenMethod.Approve, new ApproveInput
            {
                Amount = 40_00000000,
                Spender = _swapExchangeContract.ContractAddress.ConvertAddress(),
                Symbol = SymbolElff
            });
            
            tokenList.TokensInfo.Add(new Awaken.Contracts.SwapExchangeContract.Token
            {
                Amount = 20_00000000,
                TokenSymbol = SymbolLink
            });
            _tokenContract.ExecuteMethodWithResult(TokenMethod.Approve, new ApproveInput
            {
                Amount = 20_00000000,
                Spender = _swapExchangeContract.ContractAddress.ConvertAddress(),
                Symbol = SymbolLink
            });
            
            // pre-check balance
            var receiverUsdtBalanceBefore = _tokenContract.GetUserBalance(Receiver, SymbolUsdt);
            var ownerSymbolAaveBalanceBefore = _tokenContract.GetUserBalance(InitAccount, SymbolAave);
            var ownerSymbolElffBalanceBefore = _tokenContract.GetUserBalance(InitAccount, SymbolElff);
            var ownerSymbolLinkBalanceBefore = _tokenContract.GetUserBalance(InitAccount, SymbolLink);

            //swap common token
            var swapResult = _swapExchangeContract.ExecuteMethodWithResult(SwapExchangeMethod.SwapCommonTokens, new SwapTokensInput
            {
                PathMap = { path },
                SwapTokenList = tokenList
            });
            swapResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //verify event
            var swapEventLogStr = swapResult.Logs;
            foreach (var log in swapEventLogStr)
            {
                bool isSwapEvent = log.Name.Equals("SwapResultEvent");
                if (isSwapEvent)
                {
                    var eventLogs = SwapResultEvent.Parser.ParseFrom(ByteString.FromBase64(log.NonIndexed));
                    Logger.Info($"log({eventLogs.Amount},{eventLogs.Result},{eventLogs.Symbol})");
                    if (eventLogs.Symbol.Equals(SymbolAave))
                    {
                        eventLogs.Result.ShouldBe(true);
                        eventLogs.Amount.ShouldBe(10_00000000);
                        eventLogs.IsLptoken.ShouldBe(false);
                        //eventLogs.AmountOut.ShouldBe(totalUsdtOut);
                    }
                    else if(eventLogs.Symbol.Equals(SymbolElff))
                    {
                        eventLogs.Result.ShouldBe(false);
                        eventLogs.Amount.ShouldBe(40_00000000);
                        eventLogs.IsLptoken.ShouldBe(false);
                    }
                    else
                    {
                        eventLogs.Symbol.ShouldBe(SymbolLink);
                        eventLogs.Result.ShouldBe(false);
                    }
                }
            }

            //verify balance
            var receiverUsdtBalanceAfter = _tokenContract.GetUserBalance(Receiver, SymbolUsdt);
            receiverUsdtBalanceAfter.Sub(receiverUsdtBalanceBefore).ShouldBe(totalUsdtOut);
            _tokenContract.GetUserBalance(InitAccount,SymbolAave).ShouldBe(ownerSymbolAaveBalanceBefore.Sub(10_00000000));
            _tokenContract.GetUserBalance(InitAccount, SymbolElff).ShouldBe(ownerSymbolElffBalanceBefore);
            _tokenContract.GetUserBalance(InitAccount,SymbolLink).ShouldBe(ownerSymbolLinkBalanceBefore);

        }
        

        [TestMethod]
        public void SwapCommonTokenWithInvalidArgsTest()
        {
            //pre-check balance
            var ownerSymbolAaveBalanceBefore = _tokenContract.GetUserBalance(InitAccount, SymbolAave);
            var ownerSymbolElffBalanceBefore = _tokenContract.GetUserBalance(InitAccount, SymbolElff);
            var ownerSymbolLinkBalanceBefore = _tokenContract.GetUserBalance(InitAccount, SymbolLink);
            var receiverUsdtBalanceBefore = _tokenContract.GetUserBalance(Receiver, SymbolUsdt);
            
            // path is null
            {
                var path = new Dictionary<string, Path>();
                var tokenList = new Awaken.Contracts.SwapExchangeContract.TokenList();

                tokenList.TokensInfo.Add(new Awaken.Contracts.SwapExchangeContract.Token
                {
                    Amount = 10_00000000,
                    TokenSymbol = SymbolAave
                });
                _tokenContract.ExecuteMethodWithResult(TokenMethod.Approve, new ApproveInput
                {
                    Amount = 10_00000000,
                    Spender = _swapExchangeContract.ContractAddress.ConvertAddress(),
                    Symbol = SymbolAave
                });
            
                tokenList.TokensInfo.Add(new Awaken.Contracts.SwapExchangeContract.Token
                {
                    Amount = 40_00000000,
                    TokenSymbol = SymbolElff
                });
                _tokenContract.ExecuteMethodWithResult(TokenMethod.Approve, new ApproveInput
                {
                    Amount = 40_00000000,
                    Spender = _swapExchangeContract.ContractAddress.ConvertAddress(),
                    Symbol = SymbolElff
                });
            
                var swapResult = _swapExchangeContract.SwapCommonTokens(path, tokenList);
                swapResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            }

            //tokenlist is null
            {
                var path = new Dictionary<string, Path>();
                var tokenList = new Awaken.Contracts.SwapExchangeContract.TokenList();

                path[SymbolAave] = new Path
                {
                    Value = {SymbolAave, SymbolLink, SymbolUsdt},
                    ExpectPrice = "0",
                    SlipPoint = 1
                };

                var swapResult = _swapExchangeContract.SwapCommonTokens(path, tokenList);
                swapResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);

            }

            // path contain non exsit token pair
            {
                var path = new Dictionary<string, Path>();
                var tokenList = new Awaken.Contracts.SwapExchangeContract.TokenList();
                var amountsOut =
                    _awakenSwapContract.GetAmountsOut(new List<string> {SymbolAave, SymbolLink, SymbolUsdt},
                        10_00000000).Amount;
                var usdtOut = amountsOut[amountsOut.Count - 1];
                var expectedPrice = new BigIntValue(ExpansionCoefficient).Mul(usdtOut).Div(10_00000000);

                path[SymbolAave] = new Path
                {
                    Value = {SymbolAave, SymbolLink, SymbolUsdt},
                    ExpectPrice = expectedPrice.Value,
                    SlipPoint = 1
                };
                
                path[SymbolElff] = new Path
                {
                    Value = {SymbolElff, SymbolAave,SymbolLink, SymbolUsdt},
                    ExpectPrice = expectedPrice.Value,
                    SlipPoint = 1
                };
                
                tokenList.TokensInfo.Add(new Awaken.Contracts.SwapExchangeContract.Token
                {
                    Amount = 10_00000000,
                    TokenSymbol = SymbolAave
                });
                _tokenContract.ExecuteMethodWithResult(TokenMethod.Approve, new ApproveInput
                {
                    Amount = 10_00000000,
                    Spender = _swapExchangeContract.ContractAddress.ConvertAddress(),
                    Symbol = SymbolAave
                });
                tokenList.TokensInfo.Add(new Awaken.Contracts.SwapExchangeContract.Token
                {
                    Amount = 40_00000000,
                    TokenSymbol = SymbolElff
                });
                _tokenContract.ExecuteMethodWithResult(TokenMethod.Approve, new ApproveInput
                {
                    Amount = 40_00000000,
                    Spender = _swapExchangeContract.ContractAddress.ConvertAddress(),
                    Symbol = SymbolElff
                });
                var swapResult = _swapExchangeContract.SwapCommonTokens(path, tokenList);
                swapResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                
                //verift balance
                _tokenContract.GetUserBalance(Receiver,SymbolUsdt).ShouldBe(receiverUsdtBalanceBefore);
                _tokenContract.GetUserBalance(InitAccount, SymbolElff).ShouldBe(ownerSymbolElffBalanceBefore);
                _tokenContract.GetUserBalance(InitAccount, SymbolAave).ShouldBe(ownerSymbolAaveBalanceBefore);
            }


        }

        [TestMethod]
        public void NotSatisfiedWithLpTokenThreshold()
        {
            var minUsdtOut = GetLpTokenPairMinOut(10_00000000, GetTokenPair(SymbolElff,SymbolLink), new List<string>
                {SymbolElff, SymbolLink, SymbolUsdt}, new List<string> {SymbolLink, SymbolUsdt});
            
            var setResult = _swapExchangeContract.ExecuteMethodWithResult(
                SwapExchangeMethod.SetSwapToTargetTokenThreshold, new Thresholdinput
                {
                    LpTokenThreshold = minUsdtOut.Add(1)
                });
            setResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //swap
            var tokenList = new Awaken.Contracts.SwapExchangeContract.TokenList();
            var path = new Dictionary<string, Path>();
        
            path[SymbolElff] = new Path
            {
                Value = {SymbolElff, SymbolLink, SymbolUsdt},
                ExpectPrice = "1",
                SlipPoint = 1
            };
            path[SymbolLink] = new Path
            {
                Value = {SymbolLink, SymbolUsdt},
                ExpectPrice = "1",
                SlipPoint = 1
            };
            tokenList.TokensInfo.Add( new Awaken.Contracts.SwapExchangeContract.Token
            {
                Amount = 10_00000000,
                TokenSymbol = GetTokenPairSymbol(SymbolElff,SymbolLink)
            });
            _awakenTokenContract.ExecuteMethodWithResult(AwakenTokenMethod.Approve, new ApproveInput
            {
                Amount = 10_00000000,
                Spender = _swapExchangeContract.ContractAddress.ConvertAddress(),
                Symbol = GetTokenPairSymbol(SymbolElff,SymbolLink)
            });
            
            //pre-check
            var receiverUsdtBalanceBefore = _tokenContract.GetUserBalance(Receiver, SymbolUsdt);
            var ownerLpTokenBalanceBefore = _awakenTokenContract.GetBalance(GetTokenPairSymbol(SymbolElff, SymbolAave),
                InitAccount.ConvertAddress());
            
            //swap
            var swapResult = _swapExchangeContract.SwapLpTokens(path, tokenList);
            swapResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //verify event
            var swapEventLogStr = swapResult.Logs;
            foreach (var log in swapEventLogStr)
            {
                bool isSwapEvent = log.Name.Equals("SwapResultEvent");
                if (isSwapEvent)
                {
                    var eventLogs = SwapResultEvent.Parser.ParseFrom(ByteString.FromBase64(log.NonIndexed));
                    eventLogs.Symbol.ShouldBe(GetTokenPairSymbol(SymbolElff, SymbolLink));
                    eventLogs.Result.ShouldBe(false);
                    eventLogs.IsLptoken.ShouldBe(true);
                    eventLogs.Amount.ShouldBe(10_00000000);
                    Logger.Info($"log({eventLogs.Amount},{eventLogs.Result},{eventLogs.Symbol})");
                }
            }
            
            //verify balance
            _tokenContract.GetUserBalance(Receiver,SymbolUsdt).ShouldBe(receiverUsdtBalanceBefore);
            _awakenTokenContract.GetBalance(GetTokenPairSymbol(SymbolElff, SymbolAave), InitAccount.ConvertAddress())
                .ShouldBe(ownerLpTokenBalanceBefore);

        }
        
        [TestMethod]
        public void SatisfiedOrNotWithLpTokensSlipLimitTest()
        {
            var setResult = _swapExchangeContract.ExecuteMethodWithResult(
                SwapExchangeMethod.SetSwapToTargetTokenThreshold, new Thresholdinput
                {
                    LpTokenThreshold = 500
                });
            setResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var path = new Dictionary<string, Path>();
            var tokenList = new Awaken.Contracts.SwapExchangeContract.TokenList();
            long totalUsdtOut = 0;

            var lp1 = GetTokenPairSymbol(SymbolLink, SymbolElff);
            var lp2 = GetTokenPairSymbol(SymbolLink, SymbolUsdt);
            var lp3 = GetTokenPairSymbol(SymbolAave, SymbolLink);

            path[SymbolAave] = new Path
            {
                Value = {$"ALP {SymbolAave}-{SymbolLink}", $"ALP {SymbolLink}-{SymbolUsdt}"},
                ExpectPrice = "1",
                SlipPoint = 100
            };
            
            path[SymbolElff] = new Path
            {
                Value = {$"{SymbolElff}-{SymbolLink}", $"{SymbolLink}-{SymbolUsdt}"},
                ExpectPrice = new BigIntValue(100000000_00000000).Mul(ExpansionCoefficient).Value,
                SlipPoint = 1
            };

            path[SymbolLink] = new Path
            {
                Value = {$"{SymbolLink}-{SymbolUsdt}"},
                ExpectPrice = new BigIntValue(100000000_00000000).Mul(ExpansionCoefficient).Value,
                SlipPoint = 100
            };

            tokenList.TokensInfo.Add(new Awaken.Contracts.SwapExchangeContract.Token
            {
                Amount = 1_00000000,
                TokenSymbol = lp1
            });
            _awakenTokenContract.ExecuteMethodWithResult(AwakenTokenMethod.Approve, new ApproveInput
            {
                Amount = 1_00000000,
                Spender = _swapExchangeContract.ContractAddress.ConvertAddress(),
                Symbol = lp1
            });
            
            tokenList.TokensInfo.Add(new Awaken.Contracts.SwapExchangeContract.Token
            {
                Amount = 1_00000000,
                TokenSymbol = lp2
            });
            _awakenTokenContract.ExecuteMethodWithResult(AwakenTokenMethod.Approve, new ApproveInput
            {
                Amount = 1_00000000,
                Spender = _swapExchangeContract.ContractAddress.ConvertAddress(),
                Symbol = lp2
            });
            
            tokenList.TokensInfo.Add(new Awaken.Contracts.SwapExchangeContract.Token
            {
                Amount = 1_00000000,
                TokenSymbol = lp3
            });
            _awakenTokenContract.ExecuteMethodWithResult(AwakenTokenMethod.Approve, new ApproveInput
            {
                Amount = 1_00000000,
                Spender = _swapExchangeContract.ContractAddress.ConvertAddress(),
                Symbol = lp3
            });

            
            //pre-check
            long[] ownerBalanceBefore = {_awakenTokenContract.GetBalance(lp1,InitAccount.ConvertAddress()).Amount
                ,_awakenTokenContract.GetBalance(lp2,InitAccount.ConvertAddress()).Amount
                ,_awakenTokenContract.GetBalance(lp3, InitAccount.ConvertAddress()).Amount
            };
            
            //swap
            var swapResult = _swapExchangeContract.ExecuteMethodWithResult(SwapExchangeMethod.SwapLpTokens, new SwapTokensInput
            {
                PathMap = { path },
                SwapTokenList = tokenList
            });
            swapResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //verify event
            long removedAmountSymbolAave = 0;
            long removedAmountSymbolUsdt = 0;
            long swapAmountSymbolAave = 0;
            long swapAmountSymbolUsdt = 0;
            long amountSymbolAaveUsdtOut = 0;
            long amountSymbolUsdtUsdtOut = 0;
            
            var swapEventLogStr = swapResult.Logs;
            foreach (var log in swapEventLogStr)
            {
                bool isSwapEvent = log.Name.Equals("SwapResultEvent");
                if (isSwapEvent)
                {
                    var eventLogs = SwapResultEvent.Parser.ParseFrom(ByteString.FromBase64(log.NonIndexed));
                    if (eventLogs.Symbol.Equals(SymbolAave))
                    {
                        Logger.Info($"log({eventLogs.Amount},{eventLogs.Result},{eventLogs.Symbol}");
                        eventLogs.Result.ShouldBe(true);
                        eventLogs.IsLptoken.ShouldBe(false);
                        swapAmountSymbolAave = eventLogs.Amount;
                    }
                    else if(eventLogs.Symbol.Equals(SymbolUsdt))
                    {
                        Logger.Info($"log({eventLogs.Amount},{eventLogs.Result},{eventLogs.Symbol})");
                        eventLogs.Result.ShouldBe(true);
                        eventLogs.IsLptoken.ShouldBe(false);
                        swapAmountSymbolUsdt = eventLogs.Amount;
                    }
                    else if (eventLogs.Symbol.Equals(SymbolLink))
                    {
                        eventLogs.Result.ShouldBe(true);
                    }
                    else
                    {
                        Logger.Info($"log({eventLogs.Amount},{eventLogs.Result},{eventLogs.Symbol})");
                        eventLogs.Result.ShouldBe(false);
                        eventLogs.IsLptoken.ShouldBe(false);
                    }
                }
                else if (log.Name.Equals("LiquidityRemoved"))
                {
                    var liquidityRemoved = LiquidityRemoved.Parser.ParseFrom(ByteString.FromBase64(log.NonIndexed));
                    if (liquidityRemoved.SymbolA.Equals(SymbolAave))
                    {
                        removedAmountSymbolAave = liquidityRemoved.AmountA;
                    }

                    if (liquidityRemoved.SymbolB.Equals(SymbolUsdt))
                    {
                        removedAmountSymbolUsdt = liquidityRemoved.AmountB;
                    }

                }
            }
            
            swapAmountSymbolAave.ShouldBe(removedAmountSymbolAave);
            Logger.Info($"swapAmountSymbolAave({swapAmountSymbolAave})");
            //swapAmountSymbolUsdt.ShouldBe(removedAmountSymbolUsdt);
            //_tokenContract.GetUserBalance(Receiver, SymbolUsdt).Sub(receiverUSDTBalanceBefore)
              //  .ShouldBe(amountSymbolAaveUsdtOut.Add(amountSymbolUsdtUsdtOut));

            _awakenTokenContract.GetBalance(lp1, InitAccount.ConvertAddress()).Amount
                .ShouldBe(ownerBalanceBefore[0].Sub(1_00000000));
            _awakenTokenContract.GetBalance(lp2, InitAccount.ConvertAddress()).Amount
                .ShouldBe(ownerBalanceBefore[1].Sub(1_00000000));
            _awakenTokenContract.GetBalance(lp3, InitAccount.ConvertAddress()).Amount
                .ShouldBe(ownerBalanceBefore[2].Sub(1_00000000));
            
        }

        [TestMethod]
        public void SwapLpTokenWithInvalidArgsTest()
        {
            var setResult = _swapExchangeContract.ExecuteMethodWithResult(
                SwapExchangeMethod.SetSwapToTargetTokenThreshold, new Thresholdinput
                {
                    LpTokenThreshold = 500
                });
            setResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            // tokenList is null
            {
                var tokenList = new Awaken.Contracts.SwapExchangeContract.TokenList();
                var pathMap = new Dictionary<string, Path>();

                pathMap[SymbolElff] = new Path
                {
                    Value = {SymbolElff, SymbolLink, SymbolUsdt},
                    ExpectPrice = "1",
                    SlipPoint = 1
                };

                var result = _swapExchangeContract.SwapLpTokens(pathMap, tokenList);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            }
            
            //pathMap is null
            {
                var tokenList = new Awaken.Contracts.SwapExchangeContract.TokenList();
                var pathMap = new Dictionary<string, Path>();
                
                tokenList.TokensInfo.Add(new Awaken.Contracts.SwapExchangeContract.Token
                {
                    TokenSymbol = GetTokenPairSymbol(SymbolElff,SymbolLink),
                    Amount = 10_00000000
                });
                _awakenTokenContract.ExecuteMethodWithResult(AwakenTokenMethod.Approve,
                    new Awaken.Contracts.Token.ApproveInput
                    {
                        Amount = 10_00000000,
                        Spender = _swapExchangeContract.ContractAddress.ConvertAddress(),
                        Symbol = GetTokenPairSymbol(SymbolElff, SymbolLink)
                    });

                var result = _swapExchangeContract.SwapLpTokens(pathMap, tokenList);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            }
            //pathMap contain non exist token pair in path
            {
                var tokenList = new Awaken.Contracts.SwapExchangeContract.TokenList();
                var pathMap = new Dictionary<string, Path>();

                pathMap[SymbolAave] = new Path
                {
                    Value = { SymbolAave,SymbolElff,SymbolLink,SymbolUsdt},
                    ExpectPrice = "1",
                    SlipPoint = 1
                };
                
                pathMap[SymbolLink] = new Path
                {
                    Value = { SymbolLink,SymbolUsdt},
                    ExpectPrice = "1",
                    SlipPoint = 1
                };
                
                tokenList.TokensInfo.Add(new Awaken.Contracts.SwapExchangeContract.Token
                {
                    Amount = 10_00000000,
                    TokenSymbol = GetTokenPairSymbol(SymbolAave,SymbolLink)
                });

                _awakenTokenContract.ExecuteMethodWithResult(AwakenTokenMethod.Approve,
                    new Awaken.Contracts.Token.ApproveInput
                    {
                        Amount = 10_00000000,
                        Spender = _swapExchangeContract.ContractAddress.ConvertAddress(),
                        Symbol = GetTokenPairSymbol(SymbolAave, SymbolLink)
                    });

                var result = _swapExchangeContract.SwapLpTokens(pathMap, tokenList);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            }
            
            // tokenlist contain non exist token pair
            {
                var tokenList = new Awaken.Contracts.SwapExchangeContract.TokenList();
                var pathMap = new Dictionary<string, Path>();

                pathMap[SymbolAave] = new Path
                {
                    Value = { SymbolAave,SymbolLink,SymbolUsdt},
                    ExpectPrice = "1",
                    SlipPoint = 1
                };
                
                tokenList.TokensInfo.Add(new Awaken.Contracts.SwapExchangeContract.Token
                {
                    Amount = 10_00000000,
                    TokenSymbol = GetTokenPairSymbol(SymbolAave,SymbolUsdt)
                });

                var result = _swapExchangeContract.SwapLpTokens(pathMap, tokenList);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            }

            
            
        }
        /*
        [TestMethod]
        public void HandlePathTest()
        {
            HandlePath("ZZZ",new RepeatedField<string>{"ZZZ", "ABC", "USDT"}, new RepeatedField<string>{"ZZZ","ABC","USDT"});
            //ABC-X USDT-X
            HandlePath("ABC",new RepeatedField<string>{"ALP ABC-ZZZ", "USDT-ZZZ"}, new RepeatedField<string>{"ABC","ZZZ","USDT"});
            HandlePath("ABC",new RepeatedField<string>{"ALP ABC-ZZZ","YYY-ZZZ","USDT-YYY"}, new RepeatedField<string>{"ABC","ZZZ","YYY","USDT"});
            HandlePath("ABC",new RepeatedField<string>{"ALP ABC-ABD","ABD-ZZZ","USDT-ZZZ"}, new RepeatedField<string>{"ABC","ABD","ZZZ","USDT"});
            //TOKENA-X X-USDT
            HandlePath("TokenA",new RepeatedField<string>{"ALP TokenA-TokenB","ALP TokenB-USDT"}, new RepeatedField<string>{"TokenA","TokenB","USDT"});
            HandlePath("TokenA",new RepeatedField<string>{"ALP TokenA-TokenB", "ALP TokenB-TokenC","ALP TokenC-USDT"}, new RepeatedField<string>{"TokenA","TokenB","TokenC","USDT"});
            HandlePath("TokenA",new RepeatedField<string>{"ALP TokenA-TokenC", "ALP TokenB-TokenC","ALP TokenB-USDT"}, new RepeatedField<string>{"TokenA","TokenC","TokenB","USDT"});
            //X-ZZZ, X-USDT        
            HandlePath("ZZZ",new RepeatedField<string>{"TEST-ZZZ","TEST-USDT"}, new RepeatedField<string>{"ZZZ","TEST","USDT"});
            HandlePath("ZZZ",new RepeatedField<string>{"TEST-ZZZ","SSS-TEST","SSS-USDT"}, new RepeatedField<string>{"ZZZ","TEST","SSS","USDT"});
            HandlePath("ZZZ",new RepeatedField<string>{"TEST-ZZZ","TEST-UAA","UAA-USDT"}, new RepeatedField<string>{"ZZZ","TEST","UAA","USDT"});
            //X-ZZZ,USDT-X
            HandlePath("ZZZ",new RepeatedField<string>{"YYY-ZZZ","USDT-YYY"}, new RepeatedField<string>{"ZZZ","YYY","USDT"});
            HandlePath("ZZZ",new RepeatedField<string>{"TEST-ZZZ","TEST-UUU","USDT-UUU"}, new RepeatedField<string>{"ZZZ","TEST","UUU","USDT"});
            HandlePath("ZZZ",new RepeatedField<string>{"ZAA-ZZZ","YYY-ZAA","USDT-YYY"}, new RepeatedField<string>{"ZZZ","ZAA","YYY","USDT"});

        }*/
        
        private long GetLpTokenPairMinOut(long lpAmount, string tokenPair, List<string> pathA, List<string> pathB)
        {
            //Get total supply.
            var totalSupplyObject = _awakenSwapContract.CallViewMethod<GetTotalSupplyOutput>(SwapMethod.GetTotalSupply,
                new StringList
                {
                    Value = {tokenPair}
                });
            
            var totalSupply = totalSupplyObject.Results.First().TotalSupply;
            tokenPair.ShouldBe(totalSupplyObject.Results.First().SymbolPair);
            // Get reserves.  
            var reserves = _awakenSwapContract.CallViewMethod<GetReservesOutput>(SwapMethod.GetReserves,
                new GetReservesInput
                {
                    SymbolPair = { tokenPair }
                });
            var tokenA = reserves.Results.First().SymbolA;
            long resultA;
            if (!tokenA.Equals(TARGETTOKEN))
            {
                var amountA = new BigIntValue(lpAmount).Mul(reserves.Results.First().ReserveA).Div(totalSupply);
                long amount = 0;
                long.TryParse(amountA.Value, out amount);
                var amountsOut = _awakenSwapContract.CallViewMethod<GetAmountsOutOutput>(SwapMethod.GetAmountsOut,
                    new GetAmountsOutInput
                    {
                        AmountIn = amount,
                        Path = {pathA}
                    });
                resultA = amountsOut.Amount[pathA.Count - 1];
            }
            else
            {
                resultA = reserves.Results.First().ReserveA;
            }
            
            var tokenB = reserves.Results.First().SymbolB;
            long resultB;
            if (!tokenB.Equals(TARGETTOKEN))
            {
                var amountB = new BigIntValue(lpAmount).Mul(reserves.Results.First().ReserveB).Div(totalSupply);
                long amount = 0;
                long.TryParse(amountB.Value, out amount);
                var amountsOut = _awakenSwapContract.CallViewMethod<GetAmountsOutOutput>(SwapMethod.GetAmountsOut,
                    new GetAmountsOutInput
                    {
                        AmountIn = amount,
                        Path = {pathB}
                    });
                resultB = amountsOut.Amount[pathB.Count - 1];
            }
            else
            {
                resultB = reserves.Results.First().ReserveB;
            }
            
            Logger.Info(resultA);
            Logger.Info(resultB);

            return resultA <= resultB
                ? resultA
                : resultB;
        }
        
        private void InitCommonTokens()
        {
            CreateToken(SymbolUsdt, InitAccount.ConvertAddress(), TotalSupply);
            CreateToken(SymbolElff, InitAccount.ConvertAddress(), TotalSupply);
            CreateToken(SymbolLink, InitAccount.ConvertAddress(), TotalSupply);
            CreateToken(SymbolAave, InitAccount.ConvertAddress(), TotalSupply);
        }
        
        private void CreateToken(string symbol, Address issuer, long totalSupply)
        {
            
            var createResult = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create,
                new CreateInput
                {
                    Symbol = symbol,
                    Decimals = 8,
                    Issuer = issuer,
                    TokenName = $"{symbol} token",
                    TotalSupply = totalSupply,
                    IsBurnable = true
                });
            createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var issueResult = _tokenContract.ExecuteMethodWithResult(TokenMethod.Issue, new IssueInput
            {
                Symbol = symbol,
                Amount = totalSupply,
                To = InitAccount.ConvertAddress(),
            });
            issueResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var balance = _tokenContract.GetUserBalance(InitAccount, symbol);
            balance.ShouldBe(totalSupply);

            DistributeToken(symbol, 1000_00000000, UserA.ConvertAddress());
        }
        
        private void CreatePairs()
        {
            _awakenSwapContract.ExecuteMethodWithResult(SwapMethod.CreatePair, new CreatePairInput
            {
                SymbolPair = $"{SymbolLink}-{SymbolElff}"
            });
            
            _awakenSwapContract.ExecuteMethodWithResult(SwapMethod.CreatePair, new CreatePairInput
            {
                SymbolPair = $"{SymbolLink}-{SymbolUsdt}"
            });
            
            _awakenSwapContract.ExecuteMethodWithResult(SwapMethod.CreatePair, new CreatePairInput
            {
                SymbolPair = $"{SymbolAave}-{SymbolLink}"
            });

            var pairList = _awakenSwapContract.GetPairs();
            pairList.Value.ShouldContain($"{SymbolElff}-{SymbolLink}");
            pairList.Value.ShouldContain($"{SymbolLink}-{SymbolUsdt}");
            pairList.Value.ShouldContain($"{SymbolAave}-{SymbolLink}");
        }
        
        private void DistributeToken(string symbol,long amount,Address to)
        {
            _tokenContract.ExecuteMethodWithResult(TokenMethod.Transfer, new AElf.Contracts.MultiToken.TransferInput
            {
                Amount = amount,
                Symbol = symbol,
                To = to
            });
            
            var balance = _tokenContract.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new AElf.Contracts.MultiToken.GetBalanceInput
            {
                Symbol = symbol,
                Owner = to
            }).Balance;
            balance.ShouldBe(amount);
        }
        private void AddLiquidity()
        {
            _awakenTokenContract.SetAccount(UserA);

            // approve first

            _tokenContract.ExecuteMethodWithResult(TokenMethod.Approve,
                new ApproveInput
                {
                    Amount = 50_00000000,
                    Symbol = SymbolLink,
                    Spender = _awakenSwapContract.ContractAddress.ConvertAddress()
                });

            _tokenContract.ExecuteMethodWithResult(TokenMethod.Approve,
                new ApproveInput
                {
                    Amount = 100_00000000,
                    Symbol = SymbolElff,
                    Spender = _awakenSwapContract.ContractAddress.ConvertAddress()
                });
            
            _awakenSwapContract.ExecuteMethodWithResult(SwapMethod.AddLiquidity, new AddLiquidityInput
            {
                AmountADesired = 50_00000000,
                AmountAMin = 1,
                AmountBDesired = 100_00000000,
                AmountBMin = 1,
                To = UserA.ConvertAddress(),
                Deadline = Timestamp.FromDateTime(DateTime.UtcNow.Add(new TimeSpan(0, 0, 3))),
                SymbolA = SymbolLink,
                SymbolB = SymbolElff
            });


            Logger.Info(_awakenTokenContract.GetBalance(GetTokenPairSymbol(SymbolLink, SymbolElff),
                UserA.ConvertAddress()));
            Logger.Info(_awakenSwapContract.GetAmountOut(SymbolLink, SymbolElff, 1));
            Logger.Info(_awakenSwapContract.GetAmountOut(SymbolElff, SymbolLink, 2));
            Logger.Info(_awakenSwapContract.GetReserves(GetTokenPair(SymbolLink, SymbolElff)));
            Logger.Info(_awakenSwapContract.GetTotalSupply(GetTokenPair(SymbolLink, SymbolElff)));
            Logger.Info(_awakenSwapContract.GetFeeRate());

            _awakenTokenContract.ExecuteMethodWithResult(AwakenTokenMethod.Transfer, new TransferInput
            {
                Amount = 50_00000000,
                Symbol = GetTokenPairSymbol(SymbolLink, SymbolElff),
                To = InitAccount.ConvertAddress()
            });

            _tokenContract.ExecuteMethodWithResult(TokenMethod.Approve, new ApproveInput
            {
                Amount = 100_00000000,
                Spender = _awakenSwapContract.ContractAddress.ConvertAddress(),
                Symbol = SymbolLink
            });
            _tokenContract.ExecuteMethodWithResult(TokenMethod.Approve, new ApproveInput
            {
                Amount = 50_00000000,
                Spender = _awakenSwapContract.ContractAddress.ConvertAddress(),
                Symbol = SymbolUsdt
            });

            _awakenSwapContract.ExecuteMethodWithResult(SwapMethod.AddLiquidity, new AddLiquidityInput
            {
                AmountADesired = 100_00000000,
                AmountBDesired = 50_00000000,
                AmountAMin = 1,
                AmountBMin = 1,
                SymbolA = SymbolLink,
                SymbolB = SymbolUsdt,
                Deadline = Timestamp.FromDateTime(DateTime.UtcNow.Add(new TimeSpan(0,0,3))),
                To = UserA.ConvertAddress()
            });

            _awakenTokenContract.GetBalance(GetTokenPairSymbol(SymbolLink,SymbolUsdt), UserA.ConvertAddress());
            Logger.Info(
                $"balance({_awakenTokenContract.GetBalance(GetTokenPairSymbol(SymbolLink, SymbolUsdt), UserA.ConvertAddress())})");
            Logger.Info($"reserves({_awakenSwapContract.GetReserves(GetTokenPair(SymbolLink, SymbolUsdt))})");
            Logger.Info($"totalsupply({_awakenSwapContract.GetTotalSupply(GetTokenPair(SymbolLink, SymbolUsdt))})");
            Logger.Info($"getamountout({_awakenSwapContract.GetAmountOut(SymbolLink,SymbolUsdt,10)})");
            Logger.Info($"getamountout({_awakenSwapContract.GetAmountOut(SymbolUsdt,SymbolLink,10)})");

            _awakenTokenContract.ExecuteMethodWithResult(AwakenTokenMethod.Transfer, new TransferInput
            {
                To = InitAccount.ConvertAddress(),
                Amount = 30_00000000,
                Symbol = GetTokenPairSymbol(SymbolLink,SymbolUsdt)
            });
            
            
            _awakenTokenContract.GetBalance(GetTokenPairSymbol(SymbolAave,SymbolLink), UserA.ConvertAddress());
            Logger.Info(
                $"balance({_awakenTokenContract.GetBalance(GetTokenPairSymbol(SymbolAave, SymbolLink), UserA.ConvertAddress())})");
            Logger.Info($"reserves({_awakenSwapContract.GetReserves(GetTokenPair(SymbolAave, SymbolLink))})");
            Logger.Info($"totalsupply({_awakenSwapContract.GetTotalSupply(GetTokenPair(SymbolAave, SymbolLink))})");
            
            // aave-link
            _tokenContract.ExecuteMethodWithResult(TokenMethod.Approve, new ApproveInput
            {
                Amount = 200_00000000,
                Spender = _awakenSwapContract.ContractAddress.ConvertAddress(),
                Symbol = SymbolAave
            });

            _tokenContract.ExecuteMethodWithResult(TokenMethod.Approve, new ApproveInput
            {
                Amount = 400_00000000,
                Spender = _awakenSwapContract.ContractAddress.ConvertAddress(),
                Symbol = SymbolLink 
            });

            _awakenSwapContract.ExecuteMethodWithResult(SwapMethod.AddLiquidity, new AddLiquidityInput
            {
                AmountADesired = 200_00000000,
                AmountBDesired = 400_00000000,
                AmountAMin = 1,
                AmountBMin = 1,
                SymbolA = SymbolAave,
                SymbolB = SymbolLink,
                Deadline = Timestamp.FromDateTime(DateTime.UtcNow.Add(new TimeSpan(0, 0, 3))),
                To = UserA.ConvertAddress()
            });
            
            _awakenTokenContract.GetBalance(GetTokenPairSymbol(SymbolAave,SymbolLink), UserA.ConvertAddress());
            Logger.Info(
                $"balance({_awakenTokenContract.GetBalance(GetTokenPairSymbol(SymbolAave, SymbolLink), UserA.ConvertAddress())})");
            Logger.Info($"reserves({_awakenSwapContract.GetReserves(GetTokenPair(SymbolAave, SymbolLink))})");
            Logger.Info($"totalsupply({_awakenSwapContract.GetTotalSupply(GetTokenPair(SymbolAave, SymbolLink))})");
            Logger.Info($"getamountout({_awakenSwapContract.GetAmountOut(SymbolAave,SymbolLink,10)})");
            Logger.Info($"getamountout({_awakenSwapContract.GetAmountOut(SymbolLink,SymbolAave,10)})");
            
            _awakenTokenContract.ExecuteMethodWithResult(AwakenTokenMethod.Transfer, new TransferInput
            {
                Amount = 200_00000000,
                Symbol = GetTokenPairSymbol(SymbolAave,SymbolLink),
                To = InitAccount.ConvertAddress() 
            });
            
            var reservesResult = _awakenSwapContract.CallViewMethod<GetReservesOutput>(SwapMethod.GetReserves, new GetReservesInput
            {
                SymbolPair = { $"{SymbolLink}-{SymbolElff}",$"{SymbolLink}-{SymbolUsdt}",$"{SymbolAave}-{SymbolLink}"}
            });
        }
        
        /*
        private void HandlePath(string symbol, RepeatedField<string> path, RepeatedField<string> expectedPath)
        {
            var result = _swapExchangeContract.GetHandlePath(symbol, new Path
            {
                Value = {path}
            });
            Logger.Info($"handlePath result({result})");
            result.ShouldBe(expectedPath);
        }*/
        
        private string GetTokenPairSymbol(string tokenA, string tokenB)
        {
            var symbols = SortSymbols(tokenA, tokenB);
            return $"ALP {symbols[0]}-{symbols[1]}";
        }
        
        public string GetTokenPair(string tokenA, string tokenB)
        {
            var symbols = SortSymbols(tokenA, tokenB);
            return $"{symbols[0]}-{symbols[1]}";
        }
        
        private string[] SortSymbols(params string[] symbols)
        {
            return symbols.OrderBy(s => s).ToArray();
        }
        
    }
}