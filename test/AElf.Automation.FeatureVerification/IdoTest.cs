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
        private string idoAddress = "c9tMPCqjRDNV3Z4H5Gx4dMn5SbnrYjfA1dD1Bmc8E2ExXSbpH";
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
        public void Register()
        {
            var registerInput = CreateRegisterInput(AcceptedCurrency[0], ProjectCurrency[0], enableWhitelist:true,firstDistributeProportion:100_000000,hour:1);

            var registerResult = _idoContract.ExecuteMethodWithResult(IdoMethod.Register, registerInput);
            registerResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //check Register event
            var registerLogs =
                ProjectRegistered.Parser.ParseFrom(
                    ByteString.FromBase64(GetEventStr(registerResult, "ProjectRegistered")));
            var projectId = registerLogs.ProjectId;
            registerLogs.Creator.ShouldBe(InitAccount.ConvertAddress());

            CheckRegisterEventValues(registerLogs, registerInput);
            var projectInfo = _idoContract.GetProjectInfo(projectId);
            Logger.Info($"project info: {projectInfo}");
            var whiteListId = _idoContract.CallViewMethod<Hash>(IdoMethod.GetWhitelistId, projectId);
            Logger.Info($"white list: {whiteListId.ToHex()}");
            //check NewWhitelistIdSet event
            var whitelistSetLogs =
                NewWhitelistIdSet.Parser.ParseFrom(
                    ByteString.FromBase64(GetEventStr(registerResult, "NewWhitelistIdSet")));
            whitelistSetLogs.ProjectId.ShouldBe(projectId);
            whitelistSetLogs.WhitelistId.ShouldBe(whiteListId);
        }
        
        [TestMethod]
        public void RegisterMultiPeriodProject()
        {
            var acceptedToken = AcceptedCurrency[1];
            var projectToken = ProjectCurrency[1];
            var registerInput = CreateRegisterInput(acceptedToken, projectToken, totalPeriod:3, firstDistributeProportion:50_000000,restDistributeProportion:25_000000);
            var registerResult = _idoContract.ExecuteMethodWithResult(IdoMethod.Register, registerInput);
            registerResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //exception test
            var errorCase = CreateRegisterInput(acceptedToken, projectToken, totalPeriod:3, firstDistributeProportion:50_000000,restDistributeProportion:25_000001);
            var errorResult = _idoContract.ExecuteMethodWithResult(IdoMethod.Register, errorCase);
            errorResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
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
        public void UpdateAdditionalInfo()
        {
            RegisterWithNullWhitelistId("ELF", "ABC", out var projectId);
            
            var data = new Dictionary<string, string>();
            data["logo"] = "http://www.project.fake";
            data["website"] = "http://www.project.fake";
            var addtionalInfo = new AdditionalInfo
            {
                Data = {data}
            };
            var updateAddtionalInfo = _idoContract.UpdateAdditionalInfo(projectId, addtionalInfo);
            updateAddtionalInfo.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var projectAdditionInfo = _idoContract.GetProjectInfo(projectId).AdditionalInfo.Data;
            Logger.Info(projectAdditionInfo);
            Logger.Info(projectAdditionInfo.Keys);
            projectAdditionInfo.ContainsKey("logo").ShouldBe(true);
            projectAdditionInfo.ContainsKey("website").ShouldBe(true);
            projectAdditionInfo["logo"].ShouldBe("http://www.project.fake");
            projectAdditionInfo["website"].ShouldBe("http://www.project.fake");
            
            //check event
            var updateAddtionalInfoLogs =
                AdditionalInfoUpdated.Parser.ParseFrom(
                    ByteString.FromBase64(GetEventStr(updateAddtionalInfo, "AdditionalInfoUpdated")));
            updateAddtionalInfoLogs.AdditionalInfo.Data.ShouldBe(data);
            updateAddtionalInfoLogs.ProjectId.ShouldBe(projectId);

        }
        
        [TestMethod]
        public void CancelRegisteredProject()
        {
            RegisterWithNullWhitelistId(AcceptedCurrency[0],ProjectCurrency[1],out var projectId);
            var cancelResult = _idoContract.Cancel(InitAccount,projectId);
            cancelResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var projectInfo = _idoContract.GetProjectInfo(projectId);
            projectInfo.Enabled.ShouldBe(false);
            
            //check cancel event
            var cancelLogs =
                ProjectCanceled.Parser.ParseFrom(ByteString.FromBase64(GetEventStr(cancelResult, "ProjectCanceled")));
            cancelLogs.ProjectId.ShouldBe(projectId);
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
        public void ClaimMultiDamagedLiquidity()
        {
            //register
            //RegisterWithNullWhitelistId(AcceptedCurrency[0],ProjectCurrency[0],out var projectId, false, hour:1);
            var registerInput = CreateRegisterInput(AcceptedCurrency[0], ProjectCurrency[0], enableWhitelist:false,firstDistributeProportion:100_000000,hour:1);

            var registerResult = _idoContract.ExecuteMethodWithResult(IdoMethod.Register, registerInput);
            registerResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //check Register event
            var registerLogs =
                ProjectRegistered.Parser.ParseFrom(
                    ByteString.FromBase64(GetEventStr(registerResult, "ProjectRegistered")));
            var projectId = registerLogs.ProjectId;
            var projectInfo = _idoContract.GetProjectInfo(projectId);            
            Logger.Info($"RegisterInput:starttime({registerInput.StartTime})({registerInput.EndTime})");
            Logger.Info($"GetProjectInfo:starttime({projectInfo.StartTime})endtime({projectInfo.EndTime})");
            Logger.Info($"RegisterEvent:starttime({registerLogs.StartTime})endtime({registerLogs.EndTime})");

            var balance = _tokenContract.GetUserBalance(UserA, AcceptedCurrency[0]);
            //invest uninvest several times
            Invest(UserA, projectId, AcceptedCurrency[0], 10_00000000);
            UnInvest(UserA,projectId);
            Thread.Sleep(60 * 1000);
            Invest(UserA, projectId, AcceptedCurrency[0], 8_00000000);
            UnInvest(UserA,projectId);
            Thread.Sleep(60 * 1000);
            Invest(UserA, projectId, AcceptedCurrency[0], 9_00000000);
            UnInvest(UserA,projectId);

            //cancel
            var cancelResult = _idoContract.Cancel(InitAccount,projectId);
            cancelResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var claimDamaged = _idoContract.ClaimLiquidatedDamage(UserA, projectId);
            claimDamaged.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            _tokenContract.GetUserBalance(UserA,AcceptedCurrency[0]).ShouldBe(balance);

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
        public void Invest()
        {
            //create project
            RegisterWithNullWhitelistId(AcceptedCurrency[0], ProjectCurrency[0], out var projectId);
            var projectInfo = _idoContract.GetProjectInfo(projectId);
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
            
            var approveToken = _tokenContract.ApproveToken(UserA, _idoContract.ContractAddress, investAmount, currency);
            approveToken.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var invest = _idoContract.Invest(UserA, currency, investAmount, projectId);
            invest.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var investDetail = _idoContract.GetInvestDetail(projectId, UserA);
            investDetail.Amount.ShouldBe(investAmount);
            investDetail.InvestSymbol.ShouldBe(currency);
            investDetail.IsUnInvest.ShouldBe(false);
            
            //check invest event
            var investLogs = Invested.Parser.ParseFrom(ByteString.FromBase64(GetEventStr(invest, "Invested")));
            var totalProjectTokenAmountStr = new BigIntValue(investAmount).Mul(projectInfo.PreSalePrice).Div(100000000);
            var totalProjectTokenAmount = Parse(totalProjectTokenAmountStr.Value);
            investLogs.Amount.ShouldBe(investAmount);
            investLogs.User.ShouldBe(UserA.ConvertAddress());
            investLogs.InvestSymbol.ShouldBe(AcceptedCurrency[0]);
            investLogs.ProjectCurrency.ShouldBe(ProjectCurrency[0]);
            investLogs.ProjectId.ShouldBe(projectId);
            investLogs.TotalAmount.ShouldBe(investDetail.Amount);
            investLogs.ToClaimAmount.ShouldBe(totalProjectTokenAmount);
            
            Logger.Info($"projectId ({projectId.ToHex()})");
        }
        
        [TestMethod]
        [DataRow("752cb13cd84db9bd147e2ca21e7aa62e4d9a8e42061edda91d335f7746da6e47")]
        public void Claim(string projectHex)
        {
            var projectId = Hash.LoadFromHex(projectHex);
            var projectInfo = _idoContract.GetProjectInfo(projectId);
            var investAmount = _idoContract.GetInvestDetail(projectId, UserA).Amount;
            var preSalePrice = projectInfo.PreSalePrice;
            
            var balance = projectInfo.CrowdFundingIssueAmount;
            var approve = _tokenContract.ApproveToken(InitAccount, _idoContract.ContractAddress, balance,
                projectInfo.ProjectCurrency);
            var transfer = _tokenContract.TransferBalance(InitAccount, _idoContract.ContractAddress, balance,
                projectInfo.ProjectCurrency);
            transfer.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //pre-check
            var userBalance = _tokenContract.GetUserBalance(UserA, projectInfo.ProjectCurrency);
            var profitDetail = _idoContract.GetProfitDetail(projectId, UserA);
            Logger.Info($"profitDetail before({profitDetail})");
            Logger.Info($"project token balance before({userBalance})");

            var duration = projectInfo.EndTime.Seconds.Sub(projectInfo.StartTime.Seconds);
            Logger.Info(duration);
            Thread.Sleep((int)duration * 1000);
            
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
            Logger.Info($"claimLogs({claimLogs.Amount})({claimLogs.LatestPeriod})({claimLogs.TotalPeriod})");
            
            claimLogs.Amount.ShouldBe(investAmount.Mul(preSalePrice).Div(100000000));
            claimLogs.LatestPeriod.ShouldBe(1);
            claimLogs.TotalPeriod.ShouldBe(1);
            //claimLogs.TotalClaimedAmount.ShouldBe(investAmount.Mul(preSalePrice).Div(100000000));
            claimLogs.ProjectCurrency.ShouldBe(projectInfo.ProjectCurrency);
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
                Logger.Info($"");
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
        public void UnInvestWithInvalidValue()
        {
            RegisterWithNullWhitelistId(AcceptedCurrency[0],ProjectCurrency[0],out var projectId, enableWhitelist:false);
            var projectInfo = _idoContract.GetProjectInfo(projectId);

            {
                Thread.Sleep(5 * 1000);
                var unInvest = _idoContract.UnInvest(UserB, projectId);
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


        [TestMethod]
        public void UpdateClaimPeriodAtInvalidTime()
        {
            RegisterWithNullWhitelistId(AcceptedCurrency[0],ProjectCurrency[0],out var projectId, enableWhitelist:false);
            var projectInfo = _idoContract.GetProjectInfo(projectId);

            var updatePeriod = _idoContract.NextPeriod(projectId);
            updatePeriod.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            updatePeriod.Error.ShouldContain("Time is not ready");
        }
            

        [TestMethod]
        public void Withdraw()
        {
            //register
            RegisterWithNullWhitelistId(AcceptedCurrency[0], ProjectCurrency[0], out var projectId);
            var projectInfo = _idoContract.GetProjectInfo(projectId);
            var projectListInfo = _idoContract.GetProjectListInfo(projectId);
            
            //calculate projectcurrency amount need to transafer to idocontract 
            var balance = projectInfo.CrowdFundingIssueAmount;
            var approve = _tokenContract.ApproveToken(InitAccount, _idoContract.ContractAddress, balance,
                projectInfo.ProjectCurrency);
            var transfer = _tokenContract.TransferBalance(InitAccount, _idoContract.ContractAddress, balance,
                projectInfo.ProjectCurrency);
            transfer.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

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
            
            //verify burn amount
            var burnLogStr = GetEventStr(withdraw, "Burned");
            var burnLogs = Burned.Parser.ParseFrom(ByteString.FromBase64(burnLogStr));
            var actualBurn =
                projectInfo.CrowdFundingIssueAmount.Sub(projectInfo.PreSalePrice.Mul(projectInfo.CurrentRaisedAmount).Div(100000000));
            burnLogs.Amount.ShouldBe(actualBurn);
            
            var expectBurn = projectInfo.CrowdFundingIssueAmount.Sub(projectInfo.PreSalePrice.Mul(10_00000000).Div(100000000));
            Logger.Info($"expected burn ({expectBurn})");
            expectBurn.ShouldBe(actualBurn);
            
            //verify liquidateDamage and accepted balance transferred to pm
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

            //invest
            Invest(UserA, projectOneId, projectOneInfo.AcceptedCurrency, 10_00000000);
            Invest(UserA, projectTwoId, projectTwoInfo.AcceptedCurrency, 20_00000000);
            Invest(UserA, projectThreeId, projectThreeInfo.AcceptedCurrency, 5_00000000);
            Invest(UserA, projectFourId, projectFourInfo.AcceptedCurrency, 5_00000000);

            Logger.Info($"profit detail({_idoContract.GetProfitDetail(projectOneId,UserA)})");
            Logger.Info($"profit detail({_idoContract.GetProfitDetail(projectTwoId,UserA)})");
            Logger.Info($"profit detail({_idoContract.GetProfitDetail(projectThreeId,UserA)})");
            Logger.Info($"profit detail({_idoContract.GetProfitDetail(projectFourId,UserA)})");
            
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
            
            //wait till the end
            Thread.Sleep(120 * 1000);
            
            //update period
            for (int i = 0; i < 3; i++)
            {
                _idoContract.NextPeriod(projectOneId);
                _idoContract.NextPeriod(projectTwoId);
                _idoContract.NextPeriod(projectThreeId);
                _idoContract.NextPeriod(projectFourId);
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
            claimProjectFour.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            claimProjectFour.Error.ShouldContain("project is not enabled");


            //post check balance
            var balanceProjectCurrency0After = _tokenContract.GetUserBalance(UserA, ProjectCurrency[0]);
            var balanceProjectCurrency1After = _tokenContract.GetUserBalance(UserA, ProjectCurrency[1]);
            var balanceProjectCurrency2After = _tokenContract.GetUserBalance(UserA, ProjectCurrency[2]);
            
            balanceProjectCurrency0After.Sub(balanceProjectCurrency0).ShouldBe(200_00000000);
            balanceProjectCurrency1After.Sub(balanceProjectCurrency1).ShouldBe(400_00000000);
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
            GetUserBalance(UserA,ProjectCurrency[0]).Sub(userabalanceBeforeClaim).ShouldBe(100_00000000);

            //update period
            for (int i = 0; i < 2; i++)
            {
                Thread.Sleep(60 * 1000);
                _idoContract.NextPeriod(projectOneId);
            }
            
            //usera claim after period 2
            var useraClaimPeriodThree = _idoContract.Claim(projectOneId, UserA);
            useraClaimPeriodThree.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            GetUserBalance(UserA,ProjectCurrency[0]).Sub(userabalanceBeforeClaim).ShouldBe(200_00000000);

            //userb cannot calim
            var userbClaimPeriodThree = _idoContract.Claim(projectOneId, UserB);
            userbClaimPeriodThree.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            userbClaimPeriodThree.Error.ShouldContain("no invest record");
            
            //userc claim after period 2
            var usercClaimPeriodThree = _idoContract.Claim(projectOneId, UserC);
            usercClaimPeriodThree.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            GetUserBalance(UserC,ProjectCurrency[0]).Sub(usercbalanceBeforeClaim).ShouldBe(200_00000000);
            
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
        
        private void RegisterMultiPeriodProjectWithNullWhitelistId(string accepted, string project, out Hash projectId)
        {
            var registerInput = CreateRegisterInput(accepted, project, false, totalPeriod:3, firstDistributeProportion:50_000000,restDistributeProportion:25_000000, periodDuration:30);
            var registerResult = _idoContract.ExecuteMethodWithResult(IdoMethod.Register, registerInput);
            registerResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //get Register event
            var registerLogs =
                ProjectRegistered.Parser.ParseFrom(
                    ByteString.FromBase64(GetEventStr(registerResult, "ProjectRegistered")));
            projectId = registerLogs.ProjectId;
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
        
        private void RegisterWithNullWhitelistId(string acceptedToken, string projectToken, out Hash projectId, bool enableWhitelist = true, int hour = 1)
        {
            var registerInput = CreateRegisterInput(acceptedToken, projectToken, enableWhitelist);
            var registerResult = _idoContract.ExecuteMethodWithResult(IdoMethod.Register, registerInput);
            registerResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //get RegisterId
            var registerLogs =
                ProjectRegistered.Parser.ParseFrom(
                    ByteString.FromBase64(GetEventStr(registerResult, "ProjectRegistered")));
            projectId = registerLogs.ProjectId;
        }
        private RegisterInput CreateRegisterInput(string acceptedToken, string projectToken, bool enableWhitelist = true, Hash whitelistId = null, int totalPeriod = 1, int firstDistributeProportion = 100_000000, int restDistributeProportion = 0, int periodDuration = 0, int hour = 0)
        {
            var registerInput = new RegisterInput()
            {
                AcceptedCurrency = acceptedToken,
                ProjectCurrency = projectToken,
                AdditionalInfo = new AdditionalInfo(),
                CrowdFundingIssueAmount = 1000_00000000,
                CrowdFundingType = "Sale",
                StartTime = Timestamp.FromDateTime(DateTime.UtcNow.Add(new TimeSpan(0, 0, 4))),
                EndTime = Timestamp.FromDateTime(DateTime.UtcNow.Add(new TimeSpan(hour, 0,100))),
                FirstDistributeProportion = firstDistributeProportion,
                PreSalePrice = 20_00000000, 
                PublicSalePrice = 9_00000000, 
                MinSubscription = 1_00000000, 
                MaxSubscription = 27_00000000,
                IsEnableWhitelist = enableWhitelist,
                WhitelistId = whitelistId,
                IsBurnRestToken = true,
                TotalPeriod = totalPeriod,
                RestDistributeProportion = restDistributeProportion, 
                PeriodDuration = periodDuration,
            };

            return registerInput;
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

        private string[] SortSymbols(params string[] symbols)
        {
            return symbols.OrderBy(s => s).ToArray();
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
        
        private void CreateTokensAndIssue()
        {
            foreach (var acceptSymbol in AcceptedCurrency)
            {
                var depositTokenResult = _tokenContract.GetTokenInfo(acceptSymbol);
                if (depositTokenResult.Equals(new TokenInfo()))
                {
                    CreateToken(acceptSymbol, 8, InitAccount.ConvertAddress(), 10000000000000000);
                    IssueBalance(acceptSymbol, 1000000000000, UserA.ConvertAddress());
                    IssueBalance(acceptSymbol, 1000000000000, UserB.ConvertAddress());
                }
                if (GetUserBalance(UserA,acceptSymbol) < 10000000000)
                {
                    IssueBalance(acceptSymbol, 1000000000000, UserA.ConvertAddress());
                }
                if (GetUserBalance(UserB,acceptSymbol) < 10000000000)
                {
                    IssueBalance(acceptSymbol, 1000000000000, UserB.ConvertAddress());
                }
                if (GetUserBalance(UserC,acceptSymbol) < 10000000000)
                {
                    IssueBalance(acceptSymbol, 1000000000000, UserC.ConvertAddress());
                }
            }

            foreach (var projectSymbol in ProjectCurrency)
            {
                var depositTokenResult = _tokenContract.GetTokenInfo(projectSymbol);
                if (depositTokenResult.Equals(new TokenInfo()))
                {
                    CreateToken(projectSymbol, 8, InitAccount.ConvertAddress(), 10000000000000000);
                    IssueBalance(projectSymbol, 1000000000000, InitAccount.ConvertAddress());
                }
                if (GetUserBalance(InitAccount,projectSymbol) <= 1000000000000)
                {
                    IssueBalance(projectSymbol, 1000000000000, InitAccount.ConvertAddress());
                }
            }
        }
        
        private void CreatePairs()
        {
            foreach (var acceptSymbol in AcceptedCurrency)
            {
                foreach (var projectSymbol in ProjectCurrency)
                {
                    var tokens = SortSymbols(acceptSymbol, projectSymbol);
                    var pairListCurrent = _awakenSwapContract.GetPairs();
                    if (!pairListCurrent.Value.Contains($"{tokens[0]}-{tokens[1]}"))
                    {
                        var createPair = _awakenSwapContract.CreatePair(GetTokenPair(acceptSymbol, projectSymbol), out var pairAddress);
                        createPair.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                        var createPairStr = GetEventStr(createPair, "PairCreated");
                        var createPairLogs = PairCreated.Parser.ParseFrom(ByteString.FromBase64(createPairStr));
                        Logger.Info($"pair created({createPairLogs.Pair})");
                    }
                    
                }
            }
        }
        
        private void CheckRegisterEventValues(ProjectRegistered registered, RegisterInput expected)
        {
            registered.Creator.ShouldBe(InitAccount.ConvertAddress());
            registered.AcceptedCurrency.ShouldBe(expected.AcceptedCurrency);
            registered.AdditionalInfo.ShouldBe(expected.AdditionalInfo);
            registered.EndTime.ShouldBe(expected.EndTime);
            registered.MaxSubscription.ShouldBe(expected.MaxSubscription);
            registered.MinSubscription.ShouldBe(expected.MinSubscription);
            registered.PeriodDuration.ShouldBe(expected.PeriodDuration);
            registered.ProjectCurrency.ShouldBe(expected.ProjectCurrency);
            registered.StartTime.ShouldBe(expected.StartTime);
            registered.TotalPeriod.ShouldBe(expected.TotalPeriod);
            registered.UnlockTime.ShouldBe(expected.UnlockTime);
            registered.CrowdFundingType.ShouldBe(expected.CrowdFundingType);
            registered.FirstDistributeProportion.ShouldBe(expected.FirstDistributeProportion);
            registered.IsEnableWhitelist.ShouldBe(expected.IsEnableWhitelist);
            registered.PreSalePrice.ShouldBe(expected.PreSalePrice);
            registered.PublicSalePrice.ShouldBe(expected.PublicSalePrice);
            registered.RestDistributeProportion.ShouldBe(expected.RestDistributeProportion);
            registered.CrowdFundingIssueAmount.ShouldBe(expected.CrowdFundingIssueAmount);
            registered.IsBurnRestToken.ShouldBe(expected.IsBurnRestToken);

            var toRaisedAmount = Parse(new BigIntValue(expected.CrowdFundingIssueAmount).Mul(100000000).Div(expected.PreSalePrice).Value);
            registered.ToRaisedAmount.ShouldBe(toRaisedAmount);
        }

        private static long Parse(string input)
        {
            if (!long.TryParse(input, out var output))
            {
                throw new Exception($"Failed to parse {input}");
            }

            return output;
        }
        
        [TestMethod]
        public void TestSomething()
        {
            var registerStr =
                "CiIKINAe8Qed6GoL28wP/pRkyHvi8GJLCmdhTLysrs3HA6NmEgVVU0RUVBoGUEVPUExFIgzmoIfku7fplIDllK4ogNDbw/QCMICo1rkHOgsIm8DLlQYQqLjEWkILCPrAy5UGEMi+9lpIgMLXL1CA9rqHCliA0pOtA2IoCiYKIgogC2utqg6ajBBsijb2K27U+RKBWisvYLlXQUGGqbWlRDsQZGg8cgsIxqrNlQYQiIfHW4gBAZABAZoBAKABgOSX0BKqASIKIGfxcCx8R6TqWimYM2HpJsqTuTew47qGQktis9EBIcersAGAwtcv";
            var registerLog = ProjectRegistered.Parser.ParseFrom(ByteString.FromBase64(registerStr));
            Logger.Info($"start time ({registerLog.StartTime}) endtime ({registerLog.EndTime})");

            var invest =
                "CiIKIEuaQ2WWbPNRvz80SSWDsIJeoOaywVWD0Vn7PEgYrxOIEiIKIEd3SsyLE1vlJpi2wuppcZ22if+E+ParyUrw4hZe5YnOGgVVU0RUVCCAlOvcAyiAlOvcAzIGUEVPUExFOICQ38BK";
            var investLog1 = Invested.Parser.ParseFrom(ByteString.FromBase64(invest));
            Logger.Info($"first invest({investLog1})");

            var uninvest = "a44dbfe54e95ffa8d4c116ba83bfc832b289f3801e7f1fdad1da5a78e01910b6";

        }

    }
}