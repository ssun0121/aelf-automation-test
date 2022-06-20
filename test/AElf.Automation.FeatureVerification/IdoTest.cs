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
using Nethereum.Hex.HexConvertors.Extensions;
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
        private const int ProportionMax = 100;
        private static readonly string[] ProjectCurrency = {"PEOPLE","LUNA","LUNC"};
        private static readonly string[] AcceptedCurrency = {"USDTT","ETHH","BTCC"};
        private const long LiquidatedDamageProportion = 10;


        //线下部署 第一套
        private string awakenTokenAddress = "vqRuJR3LDDMHbrgqaLmsLAhKSQgrbH1r5xrs4aVDx9EezViGF";
        private string awakenSwapAddress = "62j1oMP2D8y4f6YHHL9WyhdtcFiLhMtrs7tBqXkMwJudz9AY5";
        private string idoAddress = "b4qRwAME9XoEanYP7xyvie2SBBXgeeEHjdR2Tab7KrFqSzAhW";
        private string whitelistAddress = "288zpvtQg1Qwz4m3hydPJ6hGDsXUzvQ7tWz41ZYwmKDpYwJeCM";
        private string InitAccount { get; } = "nn659b9X1BLhnu5RWmEUbuuV7J9QKVVSN54j9UmeCbF3Dve5D";
        private string UserA { get; } = "YUW9zH5GhRboT5JK4vXp5BLAfCDv28rRmTQwo418FuaJmkSg8";
        private string UserB { get; } = "FHdcx45K5kovWsAKSb3rrdyNPFus8eoJ1XTQE7aXFHTgfpgzN";
        private string UserC { get; } = "2NKnGrarMPTXFNMRDiYH4hqfSoZw72NLxZHzgHD1Q3xmNoqdmR";
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

            //init
            CreateTokensAndIssue();
            CreatePairs();
        }
        
        [TestMethod]
        public void RegisterWithNullWhitelistId(string acceptedToken, string projectToken, out Hash projectId, bool enableWhitelist = true)
        {
            CreatePair(acceptedToken, projectToken);

            var registerInput = CreateRegisterInput(acceptedToken, projectToken, enableWhitelist);
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
        public void RegisterMultiPeriodProject()
        {
            var acceptedToken = AcceptedCurrency[1];
            var projectToken = ProjectCurrency[1];
            var registerInput = CreateRegisterInput(acceptedToken, projectToken, totalPeriod:3, firstDistributeProportion:50,restDistributeProportion:10);
            var registerResult = _idoContract.ExecuteMethodWithResult(IdoMethod.Register, registerInput);
            registerResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //exception test
            var errorCase = CreateRegisterInput(acceptedToken, projectToken, totalPeriod:3, firstDistributeProportion:50,restDistributeProportion:50);
            var errorResult = _idoContract.ExecuteMethodWithResult(IdoMethod.Register, errorCase);
            errorResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
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
            RegisterWithNullWhitelistId("ELF", "ABC", out var projectId,true);
            
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
        public void UpdateAdditionalInfo()
        {
            RegisterWithNullWhitelistId("ELF", "ABC", out var projectId);
            
            var data = new Dictionary<string, string>();
            data["logo网址"] = "http://www.project.fake";
            data["首页网址"] = "http://www.project.fake";
            var addtionalInfo = new AdditionalInfo
            {
                Data = {data}
            };
            var updateAddtionalInfo = _idoContract.UpdateAdditionalInfo(projectId, addtionalInfo);
            updateAddtionalInfo.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var projectAdditionInfo = _idoContract.GetProjectInfo(projectId).AdditionalInfo.Data;
            Logger.Info(projectAdditionInfo);
            Logger.Info(projectAdditionInfo.Keys);
            projectAdditionInfo.ContainsKey("logo网址").ShouldBe(true);
            projectAdditionInfo.ContainsKey("首页网址").ShouldBe(true);
            projectAdditionInfo["logo网址"].ShouldBe("http://www.project.fake");
            projectAdditionInfo["首页网址"].ShouldBe("http://www.project.fake");
            
        }

        [TestMethod]
        public void InvestWithInvalidOrBoundaryValue()
        {
            RegisterWithNullWhitelistId(AcceptedCurrency[0],ProjectCurrency[0], out var projectId,enableWhitelist:false);

            var projectInfo = _idoContract.GetProjectInfo(projectId);
            //projectInfo.
            {
                //The currency is invalid
                var currency = AcceptedCurrency[2];
                var approveToken = _tokenContract.ApproveToken(UserA, _idoContract.ContractAddress, 2_00000000, currency);
                approveToken.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                var invest = _idoContract.Invest(UserA, AcceptedCurrency[2], 2_00000000, projectId);
                invest.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                invest.Error.ShouldContain("The currency is invalid");

            }
            {
                //investAmount = 0 
                var invest = _idoContract.Invest(UserA, projectInfo.AcceptedCurrency, 0, projectId);
                invest.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                invest.Error.ShouldContain("Invest amount should be positive");
            }
            {
                //investAmount > max
                var investAmount = projectInfo.MaxSubscription.Add(1_00000000);
                var approveToken = _tokenContract.ApproveToken(UserA, _idoContract.ContractAddress, investAmount, projectInfo.AcceptedCurrency);
                approveToken.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                var invest = _idoContract.Invest(UserA, projectInfo.AcceptedCurrency, investAmount, projectId);
                invest.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                invest.Error.ShouldContain("Invest amount should be in the range of subscription");
            }
            {
                //investAmount < min
                var investAmount = projectInfo.MinSubscription.Sub(10000000);
                var approveToken = _tokenContract.ApproveToken(UserA, _idoContract.ContractAddress, investAmount, projectInfo.AcceptedCurrency);
                approveToken.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                var invest = _idoContract.Invest(UserA, projectInfo.AcceptedCurrency, investAmount, projectId);
                invest.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                invest.Error.ShouldContain("Invest amount should be in the range of subscription");
            }
            {
                //investAmount = min
                var investAmount = projectInfo.MinSubscription;
                var approveToken = _tokenContract.ApproveToken(UserA, _idoContract.ContractAddress, projectInfo.MaxSubscription, projectInfo.AcceptedCurrency);
                approveToken.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                var invest = _idoContract.Invest(UserA, projectInfo.AcceptedCurrency, investAmount, projectId);
                invest.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                
                //investAmount = max
                var investAgain = _idoContract.Invest(UserA, projectInfo.AcceptedCurrency, projectInfo.MaxSubscription.Sub(investAmount), projectId);
                investAgain.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }
            {
                //total investAmount > CrowdFundingIssueAmount/preSaleAmount
                var currentProjectInfo = _idoContract.GetProjectInfo(projectId);
                var investAmount = currentProjectInfo.CrowdFundingIssueAmount.Div(currentProjectInfo.PreSalePrice).Mul(1_00000000).Sub(currentProjectInfo.CurrentRaisedAmount).Add(1_00000000);
                var approveToken = _tokenContract.ApproveToken(UserB, _idoContract.ContractAddress, investAmount, projectInfo.AcceptedCurrency);
                approveToken.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                Logger.Info($"current raised amount({currentProjectInfo.CurrentRaisedAmount}) ({investAmount}) ({currentProjectInfo.ToRaisedAmount})");
                var invest = _idoContract.Invest(UserB, projectInfo.AcceptedCurrency, investAmount, projectId);
                Logger.Info($"userA amount:({_idoContract.GetInvestDetail(projectId,UserB)})");
                invest.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                invest.Error.ShouldContain("The investment quota is already full");
            }
            {
                //currenttime > endtime
                Thread.Sleep(120 * 1000);
                var invest = _idoContract.Invest(UserC, projectInfo.AcceptedCurrency, 10_00000000, projectId);
                invest.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                invest.Error.ShouldContain("Can't invest right now");
            }
            
        }

        [TestMethod]
        public void UnInvestTwice()
        {
            RegisterWithNullWhitelistId(AcceptedCurrency[0],ProjectCurrency[1],out var projectId, enableWhitelist:false);
            var projectInfo = _idoContract.GetProjectInfo(projectId);
            Invest(UserA, projectId,projectInfo.AcceptedCurrency, 2_00000000);
            
            UnInvest(UserA,projectId);
            Thread.Sleep(60 * 1000);
            var unInvest = _idoContract.UnInvest(UserA, projectId);
            unInvest.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);

        }

        [TestMethod]
        public void UnInvestAtInvalidTime()
        {
            RegisterWithNullWhitelistId(AcceptedCurrency[0],ProjectCurrency[0],out var projectId, enableWhitelist:false);
            var projectInfo = _idoContract.GetProjectInfo(projectId);

            {
                var unInvest = _idoContract.UnInvest(UserA, projectId);
                unInvest.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            }

            {
                Invest(UserA,projectId,projectInfo.AcceptedCurrency,2_00000000);
                Thread.Sleep(120 * 1000);
                var unInvest = _idoContract.UnInvest(UserA, projectId);
                unInvest.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                
            }
            
        }

        [TestMethod]
        public void UpdateClaimPeriodAtInvalidTime()
        {
            RegisterWithNullWhitelistId(AcceptedCurrency[0],ProjectCurrency[0],out var projectId, enableWhitelist:false);
            var projectInfo = _idoContract.GetProjectInfo(projectId);

            var updatePeriod = _idoContract.NextPeriod(projectId);
            updatePeriod.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
        }
            

        [TestMethod]
        // 未售出清算 销毁或返还
        // 除锁定流动性以外的众筹币转给项目方
        // 违约金转给项目方
        public void Withdraw()
        {
            //register
            RegisterWithNullWhitelistId(AcceptedCurrency[0], ProjectCurrency[0], out var projectId);
            var projectInfo = _idoContract.GetProjectInfo(projectId);
            var projectListInfo = _idoContract.GetProjectListInfo(projectId);
            
            //calculate projectcurrency amount need to transafer to idocontract 
            //（1+流动性锁定比例）* 发行量
            var balance =
                projectInfo.CrowdFundingIssueAmount.Mul(projectListInfo.LiquidityLockProportion.Add(ProportionMax)).Div(ProportionMax);
            Logger.Info($"balance of project token need to transfer to idocontratc({balance}({projectListInfo.LiquidityLockProportion.Div(100)}))");

            //var approve = _tokenContract.ApproveToken(InitAccount, _idoContract.ContractAddress, balance,
            //    projectInfo.ProjectCurrency);
            //var transfer = _tokenContract.TransferBalance(InitAccount, _idoContract.ContractAddress, balance,
            //    projectInfo.ProjectCurrency);
            //transfer.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //invest
            AddWhitelist(projectId, UserA);
            AddWhitelist(projectId, UserB);
            Invest(UserA, projectId, projectInfo.AcceptedCurrency, 10_00000000);
            Invest(UserB, projectId, projectInfo.AcceptedCurrency, 10_00000000);
            var userbAmount = _idoContract.GetInvestDetail(projectId, UserB).Amount;
            var liquidatedDamageAmount = userbAmount.Mul(LiquidatedDamageProportion).Div(ProportionMax);
            var unInvestAmount = userbAmount.Sub(liquidatedDamageAmount);
            Logger.Info($"userbAmount({userbAmount})");
            Logger.Info($"liquidatedDamageAmount({liquidatedDamageAmount})");
            Logger.Info($"unInvestAmount({unInvestAmount})");

            //uninvest
            UnInvest(UserB,projectId);

            var balanceBeforeWithdraw =
                _tokenContract.GetUserBalance(InitAccount, projectInfo.AcceptedCurrency);
            Logger.Info($"contract balance before transtfer({balanceBeforeWithdraw})");
            //withdraw
            Thread.Sleep(120 * 1000);
            var withdraw = _idoContract.Withdraw(InitAccount, projectId);
            withdraw.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var balanceAfterWithdraw =
                _tokenContract.GetUserBalance(InitAccount, projectInfo.AcceptedCurrency);
            Logger.Info($"contract balance before transtfer({balanceAfterWithdraw})");

            projectInfo = _idoContract.GetProjectInfo(projectId);
            
            //验证未售出清算 burn 
            //1. burn事件
            var burnLogStr = GetEventStr(withdraw, "Burned");
            var burnLogs = Burned.Parser.ParseFrom(ByteString.FromBase64(burnLogStr));
            var actualBurn =
                projectInfo.CrowdFundingIssueAmount.Sub(projectInfo.PreSalePrice.Mul(projectInfo.CurrentRaisedAmount).Div(100000000));
            Logger.Info($"burn event({burnLogs})");
            Logger.Info($"burn actual({actualBurn})");
            Logger.Info($"PreSalePrice({projectInfo.PreSalePrice})");
            Logger.Info($"CurrentRaisedAmount({projectInfo.CurrentRaisedAmount})");
            Logger.Info($"projectInfo({projectInfo})");
            
            //burnLogs.Burner.ShouldBe(_idoContract.Contract);
            //burnLogs.Symbol.ShouldBe(projectInfo.ProjectCurrency);
            burnLogs.Amount.ShouldBe(actualBurn);

            //2. ido合约中项目币减少burn的数量
            var expectBurn = projectInfo.CrowdFundingIssueAmount.Sub(projectInfo.PreSalePrice.Mul(10_00000000).Div(100000000));
            Logger.Info($"expected burn ({expectBurn})");
            expectBurn.ShouldBe(actualBurn);
            
            //验证项目方收到的违约金和剩余众筹币 bug
            var liquidatedDamage =LiquidatedDamageProportion.Mul(10_00000000).Div(ProportionMax);
            var expectedTotalTransfer = liquidatedDamage.Add(10_00000000);
            var actualTotalTransfer = balanceAfterWithdraw.Sub(balanceBeforeWithdraw);
            actualTotalTransfer.ShouldBe(expectedTotalTransfer);


            var projectListInfoAfterWithdraw = _idoContract.GetProjectListInfo(projectId);
            projectListInfoAfterWithdraw.IsWithdraw.ShouldBe(true);
            
            Thread.Sleep(120 * 1000);
            //exception test: withdraw twice
            var withdrawTwice = _idoContract.Withdraw(InitAccount, projectId);
            withdrawTwice.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);


        }

        [TestMethod]
        public void CancelRegisteredProject()
        {
            RegisterWithNullWhitelistId(AcceptedCurrency[0],ProjectCurrency[1],out var projectId);
            var cancelResult = _idoContract.Cancel(InitAccount,projectId);
            cancelResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var projectInfo = _idoContract.GetProjectInfo(projectId);
            projectInfo.Enabled.ShouldBe(false);
        }
        
        [TestMethod]
        public void CancellationTimeExpired()
        {
            RegisterWithNullWhitelistId(AcceptedCurrency[0],ProjectCurrency[1],out var projectId);
            Thread.Sleep(120 * 1000);
            var cancelResult = _idoContract.Cancel(InitAccount,projectId);
            cancelResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);

            var projectInfo = _idoContract.GetProjectInfo(projectId);
            projectInfo.Enabled.ShouldBe(true);
        }

        [TestMethod]
        public void Refund()
        {
            RegisterWithNullWhitelistId(AcceptedCurrency[0],ProjectCurrency[1],out var projectId);
            var projectInfo = _idoContract.GetProjectInfo(projectId);
            //invest
            AddWhitelist(projectId,UserA);
            AddWhitelist(projectId,UserB);
            Invest(UserA, projectId, projectInfo.AcceptedCurrency, 10_00000000);
            Invest(UserB, projectId, projectInfo.AcceptedCurrency, 10_00000000);
            
            //uninvest
            UnInvest(UserB,projectId);
            
            //cancel project
            var cancelResult = _idoContract.Cancel(InitAccount,projectId);
            cancelResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _idoContract.GetProjectInfo(projectId).Enabled.ShouldBe(false);
            
            Logger.Info($"projectinfo ({projectId})({_idoContract.GetProjectInfo(projectId).Enabled})");
            //invest fail after cancel
            var approveToken = _tokenContract.ApproveToken(UserB, _idoContract.ContractAddress, 10_00000000, projectInfo.AcceptedCurrency);
            approveToken.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var invest = _idoContract.Invest(UserB, projectInfo.AcceptedCurrency, 5_00000000, projectId);
            invest.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            
            //refund
            var useraRefundResult = _idoContract.ReFund(UserA, projectId);
            useraRefundResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var useraRefundLogStr = GetEventStr(useraRefundResult, "ReFunded");
            var useraRefundLogs = ReFunded.Parser.ParseFrom(ByteString.FromBase64(useraRefundLogStr));
            useraRefundLogs.User.ShouldBe(UserA.ConvertAddress());
            useraRefundLogs.Amount.ShouldBe(10_00000000);
            useraRefundLogs.InvestSymbol.ShouldBe(projectInfo.AcceptedCurrency);
            useraRefundLogs.ProjectId.ShouldBe(projectId);
            
            var userbRefundResult = _idoContract.ReFund(UserB, projectId);
            userbRefundResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed); 
            
            //claim damage
            var userbClaimDamaged = _idoContract.ClaimLiquidatedDamage(UserB, projectId);
            userbClaimDamaged.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var userbClaimDamagedLogStr = GetEventStr(userbClaimDamaged, "LiquidatedDamageClaimed");
            var userbCliamDamagedLogs =
                LiquidatedDamageClaimed.Parser.ParseFrom(ByteString.FromBase64(userbClaimDamagedLogStr));
            var amount = LiquidatedDamageProportion.Mul(10_00000000).Div(ProportionMax);
            userbCliamDamagedLogs.Amount.ShouldBe(amount);
            userbCliamDamagedLogs.User.ShouldBe(UserB.ConvertAddress());
            userbCliamDamagedLogs.InvestSymbol.ShouldBe(projectInfo.AcceptedCurrency);
            userbCliamDamagedLogs.ProjectId.ShouldBe(projectId);

            var claimDetail = _idoContract.GetLiquidatedDamageDetails(projectId);
            claimDetail.Details.First(x => x.User == UserB.ConvertAddress()).Claimed.ShouldBe(true);
            Thread.Sleep(60 * 1000);
            //exception test: claim twice bug
            var userbClaimDamagedTwice = _idoContract.ClaimLiquidatedDamage(UserB, projectId);
            userbClaimDamagedTwice.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);

        }
        
        
        
        private void UnInvest(string user, Hash projectId)
        {
            var unInvest = _idoContract.UnInvest(user, projectId);
            unInvest.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        private void Invest(string user, Hash projectId, string currency, long investAmount)
        {
            var approveToken = _tokenContract.ApproveToken(user, _idoContract.ContractAddress, investAmount, currency);
            approveToken.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var invest = _idoContract.Invest(user, currency, investAmount, projectId);
            invest.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        private void AddWhitelist(Hash projectId, string user)
        {
            var addWhitelistsInput = new AddWhitelistsInput
            {
                ProjectId = projectId
            };
            addWhitelistsInput.Users.Add(user.ConvertAddress());
            var addResult = _idoContract.AddWhitelist(InitAccount, addWhitelistsInput);
            addResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
        
        [TestMethod]
        public void InvestWithRegister()
        {
            //create project
            RegisterWithNullWhitelistId("ELF", "ABC", out var projectId);
            //add whitelist
            var addWhitelistsInput = new AddWhitelistsInput
            {
                ProjectId = projectId
            };
            addWhitelistsInput.Users.Add(UserA.ConvertAddress());
            var addResult = _idoContract.AddWhitelist(InitAccount, addWhitelistsInput);
            addResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //invest
            _idoContract.SetAccount(UserA);
            var currency = _idoContract.GetProjectInfo(projectId).AcceptedCurrency;
            var investAmount = 10_00000000;
            
            //approve
            var approveToken = _tokenContract.ApproveToken(UserA, _idoContract.ContractAddress, investAmount, currency);
            approveToken.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var invest = _idoContract.Invest(UserA, currency, investAmount, projectId);
            invest.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var investDetail = _idoContract.GetInvestDetail(projectId, UserA);
            investDetail.Amount.ShouldBe(investAmount);
            investDetail.InvestSymbol.ShouldBe(currency);
            investDetail.IsUnInvest.ShouldBe(false);

            Thread.Sleep(120 * 1000);
            LockLiquidity(projectId);
        }

        [TestMethod]
        public void AddOrRemoveWhitelistToProject()
        {
            //create project
            RegisterWithNullWhitelistId("ELF", "ABC", out var projectId);

            //add user to whitelist
            var addWhitelistsInput = new AddWhitelistsInput
            {
                ProjectId = projectId
            };
            addWhitelistsInput.Users.Add(UserA.ConvertAddress());
            var addResult = _idoContract.AddWhitelist(InitAccount, addWhitelistsInput);
            addResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var whitelistId = _idoContract.GetWhitelistId(projectId);
            var addressInWhitelist = _whitelistContract.GetWhitelistDetail(whitelistId).Value[0].AddressList.Value;
            addressInWhitelist.ShouldContain(UserA.ConvertAddress());
            //remove user from whitelist
            var removeWhitelistsInput = new RemoveWhitelistsInput
            {
                ProjectId = projectId
            };
            removeWhitelistsInput.Users.Add(UserA.ConvertAddress());
            var removeResult = _idoContract.RemoveWhitelist(removeWhitelistsInput);
            removeResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var addressInWhitelistAfter = _whitelistContract.GetWhitelistDetail(whitelistId).Value[0].AddressList.Value;
            addressInWhitelistAfter.ShouldNotContain(UserA.ConvertAddress());
        }

        [TestMethod]
        public void InvestToWhiteListDisabledProject()
        {
            var acceptedToken = AcceptedCurrency[0];
            var projectToken = ProjectCurrency[2];
            RegisterWithNullWhitelistId(acceptedToken, projectToken, out var projectId, false);
            
            //invest
            Invest(UserA, projectId, acceptedToken, 10_00000000);

        }
        
        [TestMethod]
        [DataRow("d8a98f64f21fe4dcebcea883cca3f0e76ed50a47245a98be6b6bbe7d4937ed10")]
        public void Claim(string projectHex)
        {
            var projectId = Hash.LoadFromHex(projectHex);
            var projectInfo = _idoContract.GetProjectInfo(projectId);
            var investAmount = _idoContract.GetInvestDetail(projectId, UserA).Amount;
            var preSalePrice = projectInfo.PreSalePrice;
            
            //pre-check
            var userBalance = _tokenContract.GetUserBalance(UserA, projectInfo.ProjectCurrency);
            var profitDetail = _idoContract.GetProfitDetail(projectId, UserA);
            Logger.Info($"profitDetail before({profitDetail})");
            Logger.Info($"project token balance before({userBalance})");
            
            //NextPeriod
            var nextPeriod = _idoContract.NextPeriod(projectId);
            nextPeriod.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //claim
            var claim = _idoContract.Claim(projectId, UserA);
            claim.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            
            //post-check
            var userBalanceAfterClaim = _tokenContract.GetUserBalance(UserA, projectInfo.ProjectCurrency);
            var profitDetailAfterClaim = _idoContract.GetProfitDetail(projectId, UserA);
            Logger.Info($"profitDetail after({profitDetailAfterClaim})");
            Logger.Info($"project token balance after({userBalanceAfterClaim})");

            //nextperiod event check
            var nextPeriodLogStr = GetEventStr(nextPeriod, "PeriodUpdated");
            var nextPeriodLogs = PeriodUpdated.Parser.ParseFrom(ByteString.FromBase64(nextPeriodLogStr));
            nextPeriodLogs.NewPeriod.ShouldBe(1);
            nextPeriodLogs.ProjectId.ShouldBe(projectId);
            
            //claim event check
            var claimLogStr = GetEventStr(claim, "Claimed");
            var claimLogs = Claimed.Parser.ParseFrom(ByteString.FromBase64(claimLogStr));
            Logger.Info($"actual transfer balance ({userBalanceAfterClaim.Sub(userBalance)})");
            Logger.Info($"profit calculate ({investAmount.Mul(preSalePrice).Div(100000000)})");
            Logger.Info($"claimLogs({claimLogs.Amount})({claimLogs.TotalClaimedAmount})({claimLogs.LatestPeriod})({claimLogs.TotalPeriod})");
            
            claimLogs.Amount.ShouldBe(investAmount.Mul(preSalePrice).Div(100000000));
            claimLogs.LatestPeriod.ShouldBe(1);
            claimLogs.TotalPeriod.ShouldBe(1);
            claimLogs.TotalClaimedAmount.ShouldBe(investAmount.Mul(preSalePrice).Div(100000000));
            claimLogs.ProjectCurrency.ShouldBe(projectInfo.ProjectCurrency);
        }

        [TestMethod]
        public void ClaimWhileUnInvest()
        {
            var acceptToken = AcceptedCurrency[1];
            var projectToken = ProjectCurrency[0];
            RegisterWithNullWhitelistId(acceptToken, projectToken, out var projectId, false);
            
            //invest
            Invest(UserA, projectId, acceptToken, 10_00000000);
            
            //uninvest
            UnInvest(UserA, projectId);
            
            //wait till end
            Thread.Sleep(120 * 1000);
            
            //do claim
            var claim = _idoContract.Claim(projectId, UserA);
            claim.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
        }

        private void RegisterMultiPeriodProjectWithNullWhitelistId(string accepted, string project, out Hash projectId)
        {
            var registerInput = CreateRegisterInput(accepted, project, false, totalPeriod:3, firstDistributeProportion:50,restDistributeProportion:25, periodDuration:30);
            var registerResult = _idoContract.ExecuteMethodWithResult(IdoMethod.Register, registerInput);
            registerResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //get Register event
            var registerLogs =
                ProjectRegistered.Parser.ParseFrom(
                    ByteString.FromBase64(GetEventStr(registerResult, "ProjectRegistered")));
            projectId = registerLogs.ProjectId;
        }
        
        [TestMethod]
        public void SingleUserInvestMultiProjectScenario()
        {
            //PM
            //create 4 multi period project
            RegisterMultiPeriodProjectWithNullWhitelistId(AcceptedCurrency[0], ProjectCurrency[0], out var projectOneId);
            RegisterMultiPeriodProjectWithNullWhitelistId(AcceptedCurrency[0], ProjectCurrency[1], out var projectTwoId);
            RegisterMultiPeriodProjectWithNullWhitelistId(AcceptedCurrency[0], ProjectCurrency[2], out var projectThreeId);
            RegisterMultiPeriodProjectWithNullWhitelistId(AcceptedCurrency[1], ProjectCurrency[2], out var projectFourId);

            //transfer projectcurrency to idocontract
            TransferBalance(InitAccount, _idoContract.ContractAddress, ProjectCurrency[0], 1000_00000000);
            TransferBalance(InitAccount, _idoContract.ContractAddress, ProjectCurrency[1], 1000_00000000);
            TransferBalance(InitAccount, _idoContract.ContractAddress, ProjectCurrency[2], 1000_00000000);
            var projectOneInfo = _idoContract.GetProjectInfo(projectOneId);
            var projectTwoInfo = _idoContract.GetProjectInfo(projectTwoId);
            var projectThreeInfo = _idoContract.GetProjectInfo(projectThreeId);
            var projectFourInfo = _idoContract.GetProjectInfo(projectFourId);

            var projectOneListInfo = _idoContract.GetProjectListInfo(projectOneId);
            var projectTwoListInfo = _idoContract.GetProjectListInfo(projectTwoId);
            var projectThreeListInfo = _idoContract.GetProjectListInfo(projectThreeId);
            var projectFourListInfo = _idoContract.GetProjectListInfo(projectFourId);
            //investor
            //var investAmount = 10_00000000;
            //invest
            Invest(UserA, projectOneId, projectOneInfo.AcceptedCurrency, 10_00000000);
            Invest(UserA, projectTwoId, projectTwoInfo.AcceptedCurrency, 20_00000000);
            Invest(UserA, projectThreeId, projectThreeInfo.AcceptedCurrency, 5_00000000);
            Invest(UserA, projectFourId, projectFourInfo.AcceptedCurrency, 5_00000000);

            //balance pre-check
            var balanceAcceptedCurrency0 = _tokenContract.GetUserBalance(UserA, AcceptedCurrency[0]);
            var balanceAcceptedCurrency1 = _tokenContract.GetUserBalance(UserA, AcceptedCurrency[1]);
            
            //uninvest in project 3
            UnInvest(UserA, projectThreeId);
            
            //balance pre-check
            var balanceProjectCurrency0 = _tokenContract.GetUserBalance(UserA, ProjectCurrency[0]);
            var balanceProjectCurrency1 = _tokenContract.GetUserBalance(UserA, ProjectCurrency[1]);
            var balanceProjectCurrency2 = _tokenContract.GetUserBalance(UserA, ProjectCurrency[2]);

            //canceled project 4
            var cancel = _idoContract.Cancel(InitAccount, projectFourId);
            cancel.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //refund in project 4
            //var refund = _idoContract.ReFund(UserA, projectFourId);
            //refund.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //wait till the end
            Thread.Sleep(120 * 1000);
            
            //update period
            for (int i = 0; i < 3; i++)
            {
                _idoContract.NextPeriod(projectOneId);
                _idoContract.NextPeriod(projectTwoId);
                _idoContract.NextPeriod(projectTwoId);
                _idoContract.NextPeriod(projectTwoId);
                if (i != 2)
                    Thread.Sleep(60 * 1000);
            }
            
            //claim at the /middle end of p1,p2
            var claimProjectOne = _idoContract.Claim(projectOneId, UserA);
            claimProjectOne.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var claimProjectTwo = _idoContract.Claim(projectTwoId, UserA);
            claimProjectTwo.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var claimProjectThree = _idoContract.Claim(projectThreeId, UserA);
            claimProjectThree.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            claimProjectThree.Error.ShouldContain("no invest record");
            var claimProjectFour = _idoContract.Claim(projectFourId, UserA);
            claimProjectThree.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            claimProjectThree.Error.ShouldContain("project is not enabled");


            //post check balance
            var balanceProjectCurrency0After = _tokenContract.GetUserBalance(UserA, ProjectCurrency[0]);
            var balanceProjectCurrency1After = _tokenContract.GetUserBalance(UserA, ProjectCurrency[1]);
            var balanceProjectCurrency2After = _tokenContract.GetUserBalance(UserA, ProjectCurrency[2]);
            
            balanceProjectCurrency0After.Sub(balanceProjectCurrency0).ShouldBe(100_00000000);
            balanceProjectCurrency1After.Sub(balanceProjectCurrency1).ShouldBe(200_00000000);
            balanceProjectCurrency2After.Sub(balanceProjectCurrency2).ShouldBe(0);
            
        }

        [TestMethod]
        public void MultipleUserInvestSingleProjectScenario()
        {
            //PM
            //create a multi period project
            RegisterMultiPeriodProjectWithNullWhitelistId(AcceptedCurrency[0], ProjectCurrency[0], out var projectOneId);
            
            //transfer projectcurrency to idocontract
            TransferBalance(InitAccount, _idoContract.ContractAddress, ProjectCurrency[0], 1000_00000000);

            var projectOneInfo = _idoContract.GetProjectInfo(projectOneId);
            var acceptedBalance = GetUserBalance(InitAccount, AcceptedCurrency[0]);
            
            //invest
            Invest(UserA, projectOneId, projectOneInfo.AcceptedCurrency, 10_00000000);
            Invest(UserB, projectOneId, projectOneInfo.AcceptedCurrency, 20_00000000);
            Invest(UserC, projectOneId, projectOneInfo.AcceptedCurrency, 20_00000000);
            
            var userabalanceBeforeClaim = _tokenContract.GetUserBalance(UserA, ProjectCurrency[0]);
            var userbbalanceBeforeClaim = _tokenContract.GetUserBalance(UserB, ProjectCurrency[0]);
            var usercbalanceBeforeClaim = _tokenContract.GetUserBalance(UserC, ProjectCurrency[0]);
            var userbbalanceAcceptedBalance = _tokenContract.GetUserBalance(UserB, AcceptedCurrency[0]);

            //userb uninvest
            UnInvest(UserB, projectOneId);
            GetUserBalance(UserB,AcceptedCurrency[0]).Sub(userbbalanceAcceptedBalance).ShouldBe(18_00000000);

            //wait till the end
            Thread.Sleep(120 * 1000);
            
            //pm withdraw
            var withdraw = _idoContract.Withdraw(InitAccount, projectOneId);
            withdraw.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            GetUserBalance(InitAccount, AcceptedCurrency[0]).Sub(acceptedBalance).ShouldBe(30_00000000.Add(2_00000000));

            //unlock claim period 0
            _idoContract.NextPeriod(projectOneId);
            
            //usera claim in period 0
            var useraClaimPeriodOne = _idoContract.Claim(projectOneId, UserA);
            useraClaimPeriodOne.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            GetUserBalance(UserA,ProjectCurrency[0]).Sub(userabalanceBeforeClaim).ShouldBe(50_00000000);

            //update period
            for (int i = 0; i < 2; i++)
            {
                Thread.Sleep(60 * 1000);
                _idoContract.NextPeriod(projectOneId);
            }
            
            //usera claim after period 2
            var useraClaimPeriodThree = _idoContract.Claim(projectOneId, UserA);
            useraClaimPeriodThree.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            GetUserBalance(UserA,ProjectCurrency[0]).Sub(userabalanceBeforeClaim).ShouldBe(100_00000000);

            //userb cannot calim
            var userbClaimPeriodThree = _idoContract.Claim(projectOneId, UserB);
            userbClaimPeriodThree.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            userbClaimPeriodThree.Error.ShouldContain("no invest record");
            
            //userc claim after period 2
            var usercClaimPeriodThree = _idoContract.Claim(projectOneId, UserC);
            usercClaimPeriodThree.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            GetUserBalance(UserC,ProjectCurrency[0]).Sub(usercbalanceBeforeClaim).ShouldBe(200_00000000);
            
        }

        private long GetUserBalance(string user, string symbol)
        {
             return _tokenContract.GetUserBalance(user, symbol);
        }

        private void TransferBalance(string user, string to, string symbol, long balance)
        {
            var approve = _tokenContract.ApproveToken(user, to, balance,
                symbol);
            var transfer = _tokenContract.TransferBalance(user, to, balance,
                symbol);
            transfer.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
        private void LockLiquidity(Hash projectId)
        {
            _idoContract.SetAccount(InitAccount);
            var projectInfo = _idoContract.GetProjectInfo(projectId);
            var projectListInfo = _idoContract.GetProjectListInfo(projectId);
            var acceptedCurrency = projectInfo.AcceptedCurrency;
            var projectCurrency = projectInfo.ProjectCurrency;
            var acceptedTokenToLock = projectInfo.CurrentRaisedAmount.Mul(projectListInfo.LiquidityLockProportion).Div(ProportionMax);
            var projectTokenToLock = acceptedTokenToLock.Mul(projectListInfo.PublicSalePrice).Div(100000000);
            Logger.Info($"acceptedTokenToLock({acceptedTokenToLock})");
            Logger.Info($"projectTokenToLock({projectTokenToLock})");
            var lockLiq = _idoContract.LockLiquidity(projectId);
            lockLiq.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var addLiqStr = GetEventStr(lockLiq, "LiquidityAdded");
            var addLiqLog = LiquidityAdded.Parser.ParseFrom(ByteString.FromBase64(addLiqStr));
            if (addLiqLog.SymbolA.Equals(acceptedCurrency))
            {
                addLiqLog.SymbolB.ShouldBe(projectCurrency);
                addLiqLog.AmountA.ShouldBe(acceptedTokenToLock);
                addLiqLog.AmountB.ShouldBe(projectTokenToLock);
            }
            else
            {
                addLiqLog.SymbolB.ShouldBe(acceptedCurrency);
                addLiqLog.AmountA.ShouldBe(projectTokenToLock);
                addLiqLog.AmountB.ShouldBe(acceptedTokenToLock);
            }

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

        private RegisterInput CreateRegisterInput(string acceptedToken, string projectToken, bool enableWhitelist = true, Hash whitelistId = null, int totalPeriod = 1, int firstDistributeProportion = 0, int restDistributeProportion = 0, int periodDuration = 0)
        {
        
            //var test = input.ToRaisedAmount.Mul(input.PreSalePrice).Div(Mantissa);
            //var test = input.FirstDistributeProportion.Add(input.TotalPeriod.Sub(1)
              //  .Mul(input.RestDistributeProportion));
            var registerInput = new RegisterInput()
            {
                AcceptedCurrency = acceptedToken,
                ProjectCurrency = projectToken,
                AdditionalInfo = new AdditionalInfo(),
                CrowdFundingIssueAmount = 100_00000000, //发行量
                CrowdFundingType = "标价销售",//众筹类型
                StartTime = Timestamp.FromDateTime(DateTime.UtcNow.Add(new TimeSpan(0, 0, 3))),
                EndTime = Timestamp.FromDateTime(DateTime.UtcNow.Add(new TimeSpan(0, 0,100))),
                FirstDistributeProportion = firstDistributeProportion,//首次发放比例
                PreSalePrice = 2_00000000, //众筹价格 1众筹币换多少个项目币
                PublicSalePrice = 9_00000000, //公售价格 高于众筹价格5%
                MinSubscription = 1_00000000, //最低认购数量
                MaxSubscription = 27_00000000, //最高认购数量
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
                UnlockTime = Timestamp.FromDateTime(DateTime.UtcNow.Add(new TimeSpan(0, 0, 30000))), //众筹结束后保持流动性多久，流动性锁定时间
                IsEnableWhitelist = enableWhitelist,//是否启用白名单
                WhitelistId = whitelistId,
                IsBurnRestToken = true,//未出售代币清算方式 true为销毁 false为返还项目方
                TotalPeriod = totalPeriod,//是否分期发放 取1时不分期
                //ToRaisedAmount = 200_00000000,
                RestDistributeProportion = restDistributeProportion, //每期发放比例
                PeriodDuration = periodDuration,//发放周期
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
        
        private void CreateToken(string symbol, int decimals, Address issuer, long totalSupply)
        {
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
        
        public string GetTokenPair(string tokenA, string tokenB)
        {
            var symbols = SortSymbols(tokenA, tokenB);
            return $"{symbols[0]}-{symbols[1]}";
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
        
        private void IssueBalance(string symbol, long amount, Address toAddress)
        {
            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Issue, new IssueInput
            {
                Symbol = symbol,
                Amount = amount,
                To = toAddress,
            });
            
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"Successfully issue amount {amount} to {toAddress}");
        }

        [TestMethod]
        public void CreateTokensAndIssue()
        {
            for (int i = 0; i < AcceptedCurrency.Length; i++)
            {
                var depositTokenResult = _tokenContract.GetTokenInfo(AcceptedCurrency[i]);
                if (depositTokenResult.Equals(new TokenInfo()))
                {
                    CreateToken(AcceptedCurrency[i], 8, InitAccount.ConvertAddress(), 10000000000000000);
                    IssueBalance(AcceptedCurrency[i], 1000000000000, UserA.ConvertAddress());
                    IssueBalance(AcceptedCurrency[i], 1000000000000, UserB.ConvertAddress());
                }
                if (_tokenContract.GetUserBalance(UserA,AcceptedCurrency[i]) < 10000000000)
                {
                    IssueBalance(AcceptedCurrency[i], 1000000000000, UserA.ConvertAddress());
                }
                if (_tokenContract.GetUserBalance(UserB,AcceptedCurrency[i]) < 10000000000)
                {
                    IssueBalance(AcceptedCurrency[i], 1000000000000, UserB.ConvertAddress());
                }
                if (_tokenContract.GetUserBalance(UserC,AcceptedCurrency[i]) < 10000000000)
                {
                    IssueBalance(AcceptedCurrency[i], 1000000000000, UserC.ConvertAddress());
                }
            }
            for (int i = 0; i < ProjectCurrency.Length; i++)
            {
                var depositTokenResult = _tokenContract.GetTokenInfo(ProjectCurrency[i]);
                if (depositTokenResult.Equals(new TokenInfo()))
                {
                    CreateToken(ProjectCurrency[i], 8, InitAccount.ConvertAddress(), 10000000000000000);
                    IssueBalance(ProjectCurrency[i], 1000000000000, InitAccount.ConvertAddress());
                }
                if (_tokenContract.GetUserBalance(InitAccount,AcceptedCurrency[i]) <= 1000000000000)
                {
                    IssueBalance(ProjectCurrency[i], 1000000000000, InitAccount.ConvertAddress());
                }
            }

        }
        
        private void CreatePairs()
        {
            foreach (var acceptSymbol in AcceptedCurrency)
            {
                foreach (var projectSymbol in ProjectCurrency)
                {
                    var createPair = _awakenSwapContract.CreatePair(GetTokenPair(acceptSymbol, projectSymbol), out var pairAddress);
                    createPair.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                    var createPairStr = GetEventStr(createPair, "PairCreated");
                    var createPairLogs = PairCreated.Parser.ParseFrom(ByteString.FromBase64(createPairStr));
                    Logger.Info($"pair created({createPairLogs.Pair})");
                }
            }
        }
        
        
        [TestMethod]
        public void TestSomething()
        {
            //IssueBalance("ABC", 100_00000000, _idoContract.Contract);
            //Logger.Info(_tokenContract.GetUserBalance(_idoContract.ContractAddress, "ABC"));
            //Logger.Info(_tokenContract.GetUserBalance(_idoContract.ContractAddress, "ELF"));
            Logger.Info(_tokenContract.GetTokenInfo("ABC"));
            Logger.Info(_awakenTokenContract.GetTokenInfo(GetTokenPairSymbol("ABC","ELF")));
            var addLiqStr =
                "CiIKIMgCU+tuWHMDrzHED9q0HbSlvBE9F8GM2r8hMkaECupCEgNFTEYaA0FCQyCAjI2eAiiA7PWOFDIiCiBn8XAsfEek6lopmDNh6SbKk7k3sOO6hkJLYrPRASHHqzoiCiDPdo8KUVJiyRMkDMapc/eWRscNPB5kecIyBa+iI4ZRukD/o6faBg==";
            var addLiqLog = LiquidityAdded.Parser.ParseFrom(ByteString.FromBase64(addLiqStr));
            Logger.Info($"addLiqLog.AmountA {addLiqLog.AmountA}");
            Logger.Info($"addLiqLog.AmountB {addLiqLog.AmountB}");
            Logger.Info($"addLiqLog.symbolA {addLiqLog.SymbolA}");
            Logger.Info($"addLiqLog.symbolB {addLiqLog.SymbolB}");
            var investStr =
                "CiIKINipj2TyH+Tc686og8yj8Odu1QpHJFqYvmtrvn1JN+0QEiIKIEd3SsyLE1vlJpi2wuppcZ22if+E+ParyUrw4hZe5YnOGgNFTEYggJTr3AMogJTr3AMyA0FCQziAyK+gJQ==";
            var investLog = Invested.Parser.ParseFrom(ByteString.FromBase64(investStr));
            Logger.Info($"projectId({investLog.ProjectId})");

            foreach (var i in AcceptedCurrency)
            {
                Logger.Info(_tokenContract.GetTokenInfo(i));
            }

            var projectHex = "28b660f8a87402ec80d7950ea0b57b5ee83e34abda64f176ffa87e284dbd6485";
            Logger.Info(_idoContract.GetProjectInfo(Hash.LoadFromHex(projectHex)));
            
            Logger.Info(_tokenContract.GetUserBalance(_idoContract.ContractAddress, ProjectCurrency[1]));
        }

    }
}