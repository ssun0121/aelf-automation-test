using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using AElf.Client.Dto;
using AElf.Contracts.Genesis;
using AElf.Contracts.Ido;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Whitelist;
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
using log4net.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.Bcpg;
using Shouldly;
using Shouldly.Configuration;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class IdoTest
    {
        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;
        private AwakenSwapContract _awakenSwapContract;
        private AwakenTokenContract _awakenTokenContract;
        private IdoContract _idoContract;
        private WhiteListContract _whitelistContract;
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }
        
        //线下部署 第一套
        private string awakenTokenAddress = "vqRuJR3LDDMHbrgqaLmsLAhKSQgrbH1r5xrs4aVDx9EezViGF";
        private string awakenSwapAddress = "62j1oMP2D8y4f6YHHL9WyhdtcFiLhMtrs7tBqXkMwJudz9AY5";
        private string idoAddress = "2X5xuMq5fmjQ4NgYS2kH97xUVxSCXgop4mJvvRonTCyxwMcjfu";
        private string whitelistAddress = "288zpvtQg1Qwz4m3hydPJ6hGDsXUzvQ7tWz41ZYwmKDpYwJeCM";

        
        private string InitAccount { get; } = "nn659b9X1BLhnu5RWmEUbuuV7J9QKVVSN54j9UmeCbF3Dve5D";
        private string UserA { get; } = "YUW9zH5GhRboT5JK4vXp5BLAfCDv28rRmTQwo418FuaJmkSg8";
        private string UserB { get; } = "FHdcx45K5kovWsAKSb3rrdyNPFus8eoJ1XTQE7aXFHTgfpgzN";
        private static string RpcUrl { get; } = "192.168.67.166:8000";

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("IdoContractTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env2-main.json");
            //NodeInfoHelper.SetConfig("nodes-online-stage-side1.json");

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
            _idoContract = idoAddress == ""
                ? new IdoContract(NodeManager, InitAccount)
                : new IdoContract(NodeManager, InitAccount, idoAddress);
            _whitelistContract = whitelistAddress == ""
                ? new WhiteListContract(NodeManager, InitAccount)
                : new WhiteListContract(NodeManager, InitAccount, whitelistAddress);
        }

        [TestMethod]
        public void InitializeTest()
        {
            var initIdoInput = new AElf.Contracts.Ido.InitializeInput
            {
                WhitelistContract = _whitelistContract.ContractAddress.ConvertAddress()
            };

            var initIdoResult = _idoContract.ExecuteMethodWithResult(IdoMethod.Initialize, initIdoInput);
            initIdoResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var initWhitelistResult =
                _whitelistContract.ExecuteMethodWithResult(WhiteListContractMethod.Initialize, new Empty());
            initWhitelistResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var initSwap = _awakenSwapContract.ExecuteMethodWithResult(SwapMethod.Initialize, new Awaken.Contracts.Swap.InitializeInput
            {
                Admin = InitAccount.ConvertAddress(),
                AwakenTokenContractAddress = _awakenTokenContract.ContractAddress.ConvertAddress()
            });
            initSwap.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //set fee rate
            var setFeeRate =
                _awakenSwapContract.ExecuteMethodWithResult(SwapMethod.SetFeeRate, new Int64Value {Value = 30});
            setFeeRate.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        }

        [TestMethod]
        public void RegisterWithNullWhitelistId(out Hash projectId)
        {
            var acceptedToken = "ELF";
            var projectToken = "ABC";
            CreatePair(acceptedToken, projectToken);

            var registerInput = CreateRegisterInput(acceptedToken, projectToken);
            var registerResult = _idoContract.ExecuteMethodWithResult(IdoMethod.Register, registerInput);
            registerResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //get Register event
            var registerLogs =
                ProjectRegistered.Parser.ParseFrom(
                    ByteString.FromBase64(GetEventStr(registerResult, "ProjectRegistered")));
            projectId = registerLogs.ProjectId;

            var projectInfo = _idoContract.GetProjectInfo(projectId);
            Logger.Info($"project info: {projectInfo}");
            var whiteListId = _idoContract.CallViewMethod<Hash>(IdoMethod.GetWhitelistId, projectId);
            Logger.Info($"white list: {whiteListId.ToHex()}");
            
            //registerLogs.WhitelistId.ShouldBe(whiteListId);
        }

        [TestMethod]
        public void RegisterWithExistWhitelistId()
        {
            var acceptedToken = "ELF";
            var projectToken = "ABC";
            CreatePair(acceptedToken, projectToken);
            var whiteList = CreateWhiteList();

            var registerInput = CreateRegisterInput(acceptedToken, projectToken, true, whiteList);
            var registerResult = _idoContract.ExecuteMethodWithResult(IdoMethod.Register, registerInput);
            registerResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //get Register event
            var registerLogs =
                ProjectRegistered.Parser.ParseFrom(
                    ByteString.FromBase64(GetEventStr(registerResult, "ProjectRegistered")));
            var projectId = registerLogs.ProjectId;
            registerLogs.WhitelistId.ShouldBe(whiteList);

            var projectInfo = _idoContract.GetProjectInfo(projectId);
            Logger.Info($"project info: {projectInfo}");
            var whiteListId = _idoContract.CallViewMethod<Hash>(IdoMethod.GetWhitelistId, projectId);
            Logger.Info($"white list: {whiteListId.ToHex()}");
            whiteListId.ShouldBe(whiteList);

        }
        
        [TestMethod]
        public Hash CreateWhiteList()
        {
            var list = new List<Address>{InitAccount.ConvertAddress()};
            var managerList = new AddressList {Value = {list}};
            var projectId = HashHelper.ComputeFrom("Test");
            var result = _whitelistContract.ExecuteMethodWithResult(WhiteListContractMethod.CreateWhitelist, new CreateWhitelistInput()
            {
                Creator = _idoContract.Contract,
                ProjectId = HashHelper.ComputeFrom("Test"),
                ManagerList = managerList,
                ExtraInfoList = new ExtraInfoList()
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var eventStr = GetEventStr(result, "WhitelistCreated");
            var whitelistInfo = WhitelistCreated.Parser.ParseFrom(ByteString.FromBase64(eventStr));
            return whitelistInfo.WhitelistId;
        }

        [TestMethod]
        public void RegisterError()
        {
            //sender is not token issuer
            _idoContract.SetAccount(UserA);
            
            var acceptedToken = "ELF";
            var projectToken = "ABC";
            var registerInput = CreateRegisterInput(acceptedToken, projectToken);
            var registerResult = _idoContract.ExecuteMethodWithResult(IdoMethod.Register, registerInput);
            registerResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
        }

        [TestMethod]
        public void ChangeWhitelistState()
        {
            RegisterWithNullWhitelistId(out var projectId);
            
            var whitelistId = _idoContract.CallViewMethod<Hash>(IdoMethod.GetWhitelistId, projectId);
            {
                var disableResult = _idoContract.DisableWhitelist(projectId);
                disableResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                var isEnableWhitelist = _idoContract.GetProjectInfo(projectId).IsEnableWhitelist;
                isEnableWhitelist.ShouldBe(false);
            }
            {
                var enableResult = _idoContract.EnableWhitelist(projectId);
                enableResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
                var isEnableWhitelist = _idoContract.GetProjectInfo(projectId).IsEnableWhitelist;
                isEnableWhitelist.ShouldBe(true);
            }

        }

        [TestMethod]
        public void Invest()
        {
            //create project
            RegisterWithNullWhitelistId(out var projectId);
            //add whitelist
            var addWhitelistsInput = new AddWhitelistsInput();
            addWhitelistsInput.ProjectId = projectId;
            addWhitelistsInput.Users.Add(UserA.ConvertAddress());
            var addResult = _idoContract.AddWhitelist(addWhitelistsInput);
            addResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //invest
            _idoContract.SetAccount(UserA);
            var currency = _idoContract.GetProjectInfo(projectId).AcceptedCurrency;
            var investAmount = 10_00000000;
            
            //approve
            var approveToken = _tokenContract.ApproveToken(UserA, _idoContract.ContractAddress, investAmount, currency);
            approveToken.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var invest = _idoContract.Invest(currency, investAmount, projectId);
            invest.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var investDetail = _idoContract.GetInvestDetail(projectId, UserA);
            investDetail.Amount.ShouldBe(investAmount);
            investDetail.InvestSymbol.ShouldBe(currency);
            investDetail.IsUnInvest.ShouldBe(false);
        }

        private string GetTokenPairSymbol(string tokenA, string tokenB)
        {
            var symbols = SortSymbols(tokenA, tokenB);
            return $"ALP {symbols[0]}-{symbols[1]}";
        }

        private string GetEventStr(TransactionResultDto resultDto, string eventName)
        {
            var registerLogStr = resultDto.Logs.First(l => l.Name.Equals(eventName)).NonIndexed;
            return registerLogStr;
        }

        private RegisterInput CreateRegisterInput(string acceptedToken, string projectToken, bool enableWhitelist = true, Hash whitelistId = null)
        {
            var registerInput = new RegisterInput()
            {
                AcceptedCurrency = acceptedToken,
                ProjectCurrency = projectToken,
                AdditionalInfo = new AdditionalInfo(),
                CrowdFundingIssueAmount = 1000_00000000, //ABC issue amount
                CrowdFundingType = "标价销售",
                StartTime = Timestamp.FromDateTime(DateTime.UtcNow.Add(new TimeSpan(0, 0, 3))),
                EndTime = Timestamp.FromDateTime(DateTime.UtcNow.Add(new TimeSpan(0, 0, 100))),
                FirstDistributeProportion = 100,
                PreSalePrice = 10_00000000, //众筹价格 1众筹币换多少个项目币
                PublicSalePrice = 9_00000000, //公售价格 高于众筹价格5%
                MinSubscription = 1_00000000, //最低认购数量
                MaxSubscription = 100_00000000, //最高认购数量
                ListMarketInfo = new ListMarketInfo()
                {
                    Data =
                    {
                        new ListMarket()
                        {
                            Market = awakenSwapAddress.ConvertAddress(),
                            Weight = 100
                        }
                    }
                }, //上市信息
                LiquidityLockProportion = 60, //众筹代币的流动性锁定比例
                UnlockTime = Timestamp.FromDateTime(DateTime.UtcNow.Add(new TimeSpan(0, 0, 30000))), //众筹结束后保持流动性多久
                IsEnableWhitelist = enableWhitelist,
                WhitelistId = whitelistId,
                IsBurnRestToken = false,
                TotalPeriod = 1,
                ToRaisedAmount = 200_00000000,
                RestDistributeProportion = 0,
                PeriodDuration = 0,
            };

            return registerInput;
        }
        private void CreatePair(string symbolA, string symbolB)
        {
            var tokens = SortSymbols(symbolA, symbolB);
            var pairListCurrent = _awakenSwapContract.GetPairs();
            if (pairListCurrent.Value.Contains($"{tokens[0]}-{tokens[1]}"))
                return;
            
            _awakenSwapContract.ExecuteMethodWithResult(SwapMethod.CreatePair, new CreatePairInput
            {
                SymbolPair = $"{tokens[0]}-{tokens[1]}"
            });

            var pairList = _awakenSwapContract.GetPairs();
            pairList.Value.ShouldContain($"{tokens[0]}-{tokens[1]}");
        }
        
        private string[] SortSymbols(params string[] symbols)
        {
            return symbols.OrderBy(s => s).ToArray();
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




    }
}