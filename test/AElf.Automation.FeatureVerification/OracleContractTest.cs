using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Client.Dto;
using AElf.Contracts.MultiToken;
using EBridge.Contracts.Oracle;
using AElf.Contracts.OracleUser;
using EBridge.Contracts.Regiment;
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
using AddAdminsInput = EBridge.Contracts.Oracle.AddAdminsInput;
using AddRegimentMemberInput = EBridge.Contracts.Oracle.AddRegimentMemberInput;
using CallbackInfo = EBridge.Contracts.Oracle.CallbackInfo;
using CreateRegimentInput = EBridge.Contracts.Oracle.CreateRegimentInput;
using DeleteAdminsInput = EBridge.Contracts.Oracle.DeleteAdminsInput;
using DeleteRegimentMemberInput = EBridge.Contracts.Oracle.DeleteRegimentMemberInput;
using InitializeInput = EBridge.Contracts.Oracle.InitializeInput;
using JoinRegimentInput = EBridge.Contracts.Oracle.JoinRegimentInput;
using LeaveRegimentInput = EBridge.Contracts.Oracle.LeaveRegimentInput;
using TransferRegimentOwnershipInput = EBridge.Contracts.Oracle.TransferRegimentOwnershipInput;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class OracleContractTest
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private INodeManager MainNodeManager { get; set; }
        private GenesisContract _mainGenesisContract;
        private TokenContract _mainTokenContract;

        private AuthorityManager AuthorityManager { get; set; }

        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;
        private ParliamentContract _parliamentContract;

        private OracleContract _oracleContract;
        private OracleUserContract _oracleUserContract;
        private RegimentContract _regimentContract;
        private Address _integerAggregator;

        private string TestAccount { get; } = "2RehEQSpXeZ5DUzkjTyhAkr9csu7fWgE5DAuB2RaKQCpdhB8zC";
        private string InitAccount { get; } = "J6zgLjGwd1bxTBpULLXrGVeV74tnS2n74FFJJz7KNdjTYkDF6";
        private string OtherNode { get; } = "sjzNpr5bku3ZyvMqQrXeBkXGEvG2CTLA2cuNDfcDMaPTTAqEy";

        private readonly List<string> _associationMember = new List<string>
        {
            "bBEDoBnPK28bYFf1M28hYLFVuGnkPkNR6r59XxGNmYfr7aRff",
            "FHdcx45K5kovWsAKSb3rrdyNPFus8eoJ1XTQE7aXFHTgfpgzN",
            "Gazx8ei5CnzPRnaFGojZemak8VJsC5ETqzC1CGqNb76ZM3BMY",
            "Muca5ZVorWCV51BNATadyC6f72871aZm2WnHfsrkioUHwyP8j",
            "bP7RkGBN5vK1wDFjuUbWh49QVLMWAWMuccYK1RSh9hRrVcP7v"
        };

        private readonly List<string> _associationMemberNotEnough = new List<string>
        {
            "bBEDoBnPK28bYFf1M28hYLFVuGnkPkNR6r59XxGNmYfr7aRff"
        };

        private readonly List<string> _addAccountList = new List<string>
        {
            "sjzNpr5bku3ZyvMqQrXeBkXGEvG2CTLA2cuNDfcDMaPTTAqEy",
            "yy71DP4pMvGAqnohHPJ3rzmxoi1qxHk4uXg8kdRvgXjnYcE1G",
            "GsiwFtm9K2iRWrPUsRSCriqRAcfTqUurp5kmWokjQJcf5TcSG",
            "2sKRVAjvtMcdKLA21qr1i59M57GX69QjKKSbJ2LY2SAQeSdsgS"
        };

        //2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS
        //2WHXRoLRjbUTDQsuqR5CntygVfnDb125qdJkudev4kVNbLhTdG
        //2NxwCPAGJr4knVdmwhb1cK7CkZw5sMJkRDLnT7E2GoDP2dy5iZ
        //2RHf2fxsnEaM3wb6N1yGqPupNZbcCY98LgWbGSFWmWzgEs5Sjo
        private string _oracleContractAddress = "2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS";
        private string _regimentContractAddress = "sr4zX6E7yVVL7HevExVcWv2ru3HSZakhsJMXfzxzfpnXofnZw";
        private string _oracleUserContractAddress = "2nyC8hqq3pGnRu8gJzCsTaxXB6snfGxmL2viimKXgEfYWGtjEh";
        private string _integerAggregatorAddress = "2u6Dd139bHvZJdZ835XnNKL5y6cxqzV9PEWD5fZdQXdFZLgevc";

        //6Yu5KJprje1EKf78MicuoAL3VsK3DoNoGm1ah1dUR5Y7frPdE
        //2S2Fx7PuK9Us3h7PVUmnsLX7Q3PTsFpTXuW52qdKUBAgJLw5s5
        //2gin3EoK8YvfeNfPjWyuKRaa3GEjF1KY1ZpBQCgPF3mHnQmDAX
        private Address _defaultParliamentOrganization;

        private string Password { get; } = "12345678";

        // private static string RpcUrl { get; } = "18.229.184.199:8000";
        private static string RpcUrl { get; } = "192.168.66.9:8000";

        // private static string MainRpcUrl { get; } = "18.228.140.143:8000";
        private string Symbol { get; } = "PORT";
        private readonly bool isNeedInitialize = false;
        private string _regiment = "USRPhS38yEzHqgkerhsSQ49tGbDSkNfjnyRRhKVUpbEDfnw2z";
        private string _regimentTrue = "";
        private string _regimentNotEnough = "";

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("OracleContactTest");
            Logger = Log4NetHelper.GetLogger();
            NodeManager = new NodeManager(RpcUrl);
            // MainNodeManager = new NodeManager(MainRpcUrl);
            // Logger.Info(MainRpcUrl,RpcUrl);

            NodeInfoHelper.SetConfig("nodes-env2-main");
            // _mainGenesisContract = GenesisContract.GetGenesisContract(MainNodeManager, InitAccount, Password);
            // _mainTokenContract = _mainGenesisContract.GetTokenContract(InitAccount, Password);
            AuthorityManager = new AuthorityManager(NodeManager, InitAccount, Password);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount, Password);
            _parliamentContract = _genesisContract.GetParliamentContract(InitAccount, Password);
            _defaultParliamentOrganization = _parliamentContract.GetGenesisOwnerAddress();
            _tokenContract = _genesisContract.GetTokenContract(InitAccount, Password);
            CreateToken();

            _oracleContract = _oracleContractAddress == ""
                ? new OracleContract(NodeManager, InitAccount)
                : new OracleContract(NodeManager, InitAccount, _oracleContractAddress);
            _regimentContract = _regimentContractAddress == ""
                ? new RegimentContract(NodeManager, InitAccount)
                : new RegimentContract(NodeManager, InitAccount, _regimentContractAddress);
            _oracleUserContract = _oracleUserContractAddress == ""
                ? new OracleUserContract(NodeManager, InitAccount)
                : new OracleUserContract(NodeManager, InitAccount, _oracleUserContractAddress);

            _integerAggregator = _integerAggregatorAddress == ""
                ? AuthorityManager.DeployContractWithAuthority(InitAccount, "AElf.Contracts.IntegerAggregator")
                : Address.FromBase58(_integerAggregatorAddress);
            CheckMemberBalance();
            if (!isNeedInitialize) return;
            // InitializeOracleTest();
            InitializeTestContract();
        }

        [TestMethod]
        public void InitializeOracleTest()
        {
            var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.Initialize, new InitializeInput
            {
                MinimumOracleNodesCount = 3,
                DefaultAggregateThreshold = 2,
                DefaultRevealThreshold = 2,
                IsChargeFee = true,
                RegimentContractAddress = _regimentContract.Contract
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            // ChangeTokenIssuer();

            var oracleNodeThreshold =
                _oracleContract.CallViewMethod<OracleNodeThreshold>(OracleMethod.GetThreshold, new Empty());
            oracleNodeThreshold.MinimumOracleNodesCount.ShouldBe(3);
            oracleNodeThreshold.DefaultAggregateThreshold.ShouldBe(2);
            oracleNodeThreshold.DefaultRevealThreshold.ShouldBe(2);

            var controller = _oracleContract.CallViewMethod<Address>(OracleMethod.GetController, new Empty());
            controller.ShouldBe(InitAccount.ConvertAddress());
            var tokenSymbol =
                _oracleContract.CallViewMethod<StringValue>(OracleMethod.GetOracleTokenSymbol, new Empty());
            tokenSymbol.Value.ShouldBe(Symbol);

            var getController = _regimentContract.CallViewMethod<Address>(RegimentMethod.GetController, new Empty());
            getController.ShouldBe(_oracleContract.Contract);
            var getConfig =
                _regimentContract.CallViewMethod<RegimentContractConfig>(RegimentMethod.GetConfig, new Empty());
            getConfig.RegimentLimit.ShouldBe(1024);
            getConfig.MaximumAdminsCount.ShouldBe(3);
            getConfig.MemberJoinLimit.ShouldBe(256);
        }

        #region Regiment

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CreateRegiment(bool isApproveToJoin)
        {
            var list = new List<Address>();
            _associationMember.ForEach(l => { list.Add(l.ConvertAddress()); });
            var result =
                _oracleContract.ExecuteMethodWithResult(OracleMethod.CreateRegiment, new CreateRegimentInput
                {
                    IsApproveToJoin = isApproveToJoin,
                    InitialMemberList = {list}
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var logEventDto = result.Logs.First(l => l.Name.Contains(nameof(RegimentCreated))).NonIndexed;
            var regimentCreated = RegimentCreated.Parser.ParseFrom(ByteString.FromBase64(logEventDto));
            Logger.Info($"Manager: {regimentCreated.Manager}\n" +
                        $"Create Time: {regimentCreated.CreateTime}\n" +
                        $"Regiment Address: {regimentCreated.RegimentAddress}\n");
            regimentCreated.Manager.ShouldBe(_oracleContract.CallAccount);
        }

        [TestMethod]
        public void CreateRegiment_NotEnough()
        {
            var list = new List<Address>();
            _associationMemberNotEnough.ForEach(l => { list.Add(l.ConvertAddress()); });
            var result =
                _oracleContract.ExecuteMethodWithResult(OracleMethod.CreateRegiment, new CreateRegimentInput
                {
                    IsApproveToJoin = false,
                    Manager = InitAccount.ConvertAddress(),
                    InitialMemberList = {list}
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var logEventDto = result.Logs.First(l => l.Name.Contains(nameof(RegimentCreated))).NonIndexed;
            var regimentCreated = RegimentCreated.Parser.ParseFrom(ByteString.FromBase64(logEventDto));
            Logger.Info($"Manager: {regimentCreated.Manager}\n" +
                        $"Create Time: {regimentCreated.CreateTime}\n" +
                        $"Regiment Address: {regimentCreated.RegimentAddress}\n");
            regimentCreated.Manager.ShouldBe(_oracleContract.CallAccount);
        }

        [TestMethod]
        public void JoinRegiment()
        {
            var getOriginRegimentMemberList =
                _regimentContract.CallViewMethod<RegimentMemberList>(RegimentMethod.GetRegimentMemberList,
                    _regimentNotEnough.ConvertAddress());

            var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.JoinRegiment, new JoinRegimentInput
            {
                RegimentAddress = _regimentNotEnough.ConvertAddress(),
                NewMemberAddress = _associationMember[1].ConvertAddress()
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logEventDto = result.Logs.First(l => l.Name.Contains(nameof(NewMemberAdded))).NonIndexed;
            var newMemberAdded = NewMemberAdded.Parser.ParseFrom(ByteString.FromBase64(logEventDto));
            Logger.Info($"RegimentAddress: {newMemberAdded.RegimentAddress}\n" +
                        $"NewMemberAddress: {newMemberAdded.NewMemberAddress}\n");
            var getRegimentMemberList =
                _regimentContract.CallViewMethod<RegimentMemberList>(RegimentMethod.GetRegimentMemberList,
                    _regimentNotEnough.ConvertAddress());
            getRegimentMemberList.Value.Count.ShouldBe(getOriginRegimentMemberList.Value.Count + 1);
            getRegimentMemberList.Value.ShouldContain(_associationMember[1].ConvertAddress());
        }

        [TestMethod]
        public void LeaveRegiment()
        {
            var getRegimentMemberList =
                _regimentContract.CallViewMethod<RegimentMemberList>(RegimentMethod.GetRegimentMemberList,
                    _regimentNotEnough.ConvertAddress());
            var leaveAccount = getRegimentMemberList.Value.Last();
            _oracleContract.SetAccount(leaveAccount.ToBase58());
            var leaveResult = _oracleContract.ExecuteMethodWithResult(OracleMethod.LeaveRegiment, new LeaveRegimentInput
            {
                RegimentAddress = _regimentNotEnough.ConvertAddress(),
                LeaveMemberAddress = leaveAccount
            });
            leaveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var leaveLogEventDto = leaveResult.Logs.First(l => l.Name.Equals(nameof(RegimentMemberLeft))).NonIndexed;
            var regimentMemberLeft = RegimentMemberLeft.Parser.ParseFrom(ByteString.FromBase64(leaveLogEventDto));
            Logger.Info($"RegimentAddress: {regimentMemberLeft.RegimentAddress}\n" +
                        $"LeftMemberAddress:{regimentMemberLeft.LeftMemberAddress}");
            var getLeaveRegimentMemberList =
                _regimentContract.CallViewMethod<RegimentMemberList>(RegimentMethod.GetRegimentMemberList,
                    _regimentNotEnough.ConvertAddress());
            getLeaveRegimentMemberList.Value.ShouldNotContain(leaveAccount);
            var isRegimentMember =
                _regimentContract.CallViewMethod<BoolValue>(RegimentMethod.IsRegimentMember,
                    new IsRegimentMemberInput
                    {
                        RegimentAddress = _regimentNotEnough.ConvertAddress(),
                        Address = leaveAccount
                    });
            isRegimentMember.Value.ShouldBeFalse();
        }

        [TestMethod]
        public void LeaveRegiment_Failed()
        {
            var getOriginRegimentMemberList =
                _regimentContract.CallViewMethod<RegimentMemberList>(RegimentMethod.GetRegimentMemberList,
                    _regimentNotEnough.ConvertAddress());
            {
                var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.LeaveRegiment, new LeaveRegimentInput
                {
                    RegimentAddress = _regimentNotEnough.ConvertAddress(),
                    LeaveMemberAddress = getOriginRegimentMemberList.Value.Last()
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                result.Error.ShouldContain("No permission.");
            }
            {
                var leaveAccount = _addAccountList.Last();
                _oracleContract.SetAccount(leaveAccount);
                var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.LeaveRegiment, new LeaveRegimentInput
                {
                    RegimentAddress = _regimentNotEnough.ConvertAddress(),
                    LeaveMemberAddress = leaveAccount.ConvertAddress()
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                result.Error.ShouldContain("is not a member of this regiment.");
            }
        }

        [TestMethod]
        public void JoinRegiment_Failed()
        {
            {
                var getOriginRegimentMemberList =
                    _regimentContract.CallViewMethod<RegimentMemberList>(RegimentMethod.GetRegimentMemberList,
                        _regimentNotEnough.ConvertAddress());

                var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.JoinRegiment, new JoinRegimentInput
                {
                    RegimentAddress = _regimentNotEnough.ConvertAddress(),
                    NewMemberAddress = getOriginRegimentMemberList.Value.First()
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                result.Error.ShouldContain("already exist in regiment");
            }
            {
                var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.JoinRegiment, new JoinRegimentInput
                {
                    RegimentAddress = _regimentTrue.ConvertAddress(),
                    NewMemberAddress = TestAccount.ConvertAddress()
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var logEventDto = result.Logs.First(l => l.Name.Equals(nameof(NewMemberApplied))).NonIndexed;
                var newMemberApplied = NewMemberApplied.Parser.ParseFrom(ByteString.FromBase64(logEventDto));
                newMemberApplied.RegimentAddress.ShouldBe(_regimentTrue.ConvertAddress());
            }
        }

        [TestMethod]
        public void AddAdmin()
        {
            var getOriginRegimentInfo = _regimentContract.CallViewMethod<RegimentInfo>(RegimentMethod.GetRegimentInfo,
                _regimentTrue.ConvertAddress());

            var addAdmin = _oracleContract.ExecuteMethodWithResult(OracleMethod.AddAdmins, new AddAdminsInput
            {
                NewAdmins = {TestAccount.ConvertAddress(), OtherNode.ConvertAddress()},
                RegimentAddress = _regimentTrue.ConvertAddress()
            });
            addAdmin.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var getRegimentInfo = _regimentContract.CallViewMethod<RegimentInfo>(RegimentMethod.GetRegimentInfo,
                _regimentTrue.ConvertAddress());
            getRegimentInfo.Admins.Count.ShouldBe(getOriginRegimentInfo.Admins.Count + 2);
            getRegimentInfo.Admins.ShouldContain(TestAccount.ConvertAddress());
            getRegimentInfo.Admins.ShouldContain(OtherNode.ConvertAddress());
        }

        [TestMethod]
        public void DeleteAdmin()
        {
            var getOriginRegimentInfo = _regimentContract.CallViewMethod<RegimentInfo>(RegimentMethod.GetRegimentInfo,
                _regimentTrue.ConvertAddress());

            var deleteAdmin = _oracleContract.ExecuteMethodWithResult(OracleMethod.DeleteAdmins, new DeleteAdminsInput
            {
                DeleteAdmins = {TestAccount.ConvertAddress(), OtherNode.ConvertAddress()},
                RegimentAddress = _regimentTrue.ConvertAddress()
            });
            deleteAdmin.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var getRegimentInfo = _regimentContract.CallViewMethod<RegimentInfo>(RegimentMethod.GetRegimentInfo,
                _regimentTrue.ConvertAddress());
            getRegimentInfo.Admins.ShouldNotContain(TestAccount.ConvertAddress());
            getRegimentInfo.Admins.ShouldNotContain(OtherNode.ConvertAddress());
        }

        [TestMethod]
        public void Add_DeleteRegimentMember()
        {
            var testAddress = _addAccountList[1].ConvertAddress();
            var getOriginRegimentInfo = _regimentContract.CallViewMethod<RegimentInfo>(RegimentMethod.GetRegimentInfo,
                _regimentTrue.ConvertAddress());
            //Manager add 
            _oracleContract.SetAccount(getOriginRegimentInfo.Admins.First().ToBase58());
            var addRegiment =
                _oracleContract.ExecuteMethodWithResult(OracleMethod.AddRegimentMember, new AddRegimentMemberInput
                {
                    RegimentAddress = _regimentTrue.ConvertAddress(),
                    NewMemberAddress = testAddress
                });
            addRegiment.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logEventDto = addRegiment.Logs.First(l => l.Name.Contains(nameof(NewMemberAdded))).NonIndexed;
            var newMemberAdded = NewMemberAdded.Parser.ParseFrom(ByteString.FromBase64(logEventDto));
            Logger.Info($"RegimentAddress: {newMemberAdded.RegimentAddress}\n" +
                        $"NewMemberAddress: {newMemberAdded.NewMemberAddress}\n" +
                        $"OperatorAddress: {_oracleContract.Contract}");
            var getRegimentMemberList =
                _regimentContract.CallViewMethod<RegimentMemberList>(RegimentMethod.GetRegimentMemberList,
                    _regimentTrue.ConvertAddress());
            getRegimentMemberList.Value.ShouldContain(testAddress);

            //Manager delete
            _oracleContract.SetAccount(getOriginRegimentInfo.Admins.First().ToBase58());
            var deleteRegiment = _oracleContract.ExecuteMethodWithResult(OracleMethod.DeleteRegimentMember,
                new DeleteRegimentMemberInput
                {
                    RegimentAddress = _regimentTrue.ConvertAddress(),
                    DeleteMemberAddress = testAddress
                });
            var deleteLogEventDto =
                deleteRegiment.Logs.First(l => l.Name.Equals(nameof(RegimentMemberLeft))).NonIndexed;
            var regimentMemberLeft = RegimentMemberLeft.Parser.ParseFrom(ByteString.FromBase64(deleteLogEventDto));
            Logger.Info($"RegimentAddress: {regimentMemberLeft.RegimentAddress}\n" +
                        $"LeftMemberAddress:{regimentMemberLeft.LeftMemberAddress}");

            var afterDeleteGetRegimentMemberList =
                _regimentContract.CallViewMethod<RegimentMemberList>(RegimentMethod.GetRegimentMemberList,
                    _regimentTrue.ConvertAddress());
            afterDeleteGetRegimentMemberList.Value.ShouldNotContain(testAddress);
            var isRegimentMember =
                _regimentContract.CallViewMethod<BoolValue>(RegimentMethod.IsRegimentMember,
                    new IsRegimentMemberInput
                    {
                        RegimentAddress = _regimentTrue.ConvertAddress(),
                        Address = testAddress
                    });
            isRegimentMember.Value.ShouldBeFalse();
        }

        [TestMethod]
        public void TransferOwnerShip()
        {
            var newManager = TestAccount.ConvertAddress();
            var getOriginRegimentInfo = _regimentContract.CallViewMethod<RegimentInfo>(RegimentMethod.GetRegimentInfo,
                _regimentTrue.ConvertAddress());
            var oldManager = getOriginRegimentInfo.Manager;

            var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.TransferRegimentOwnership,
                new TransferRegimentOwnershipInput
                {
                    RegimentAddress = _regimentTrue.ConvertAddress(),
                    NewManagerAddress = newManager
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var getRegimentInfo = _regimentContract.CallViewMethod<RegimentInfo>(RegimentMethod.GetRegimentInfo,
                _regimentTrue.ConvertAddress());
            getRegimentInfo.Manager.ShouldBe(newManager);

            _oracleContract.SetAccount(newManager.ToBase58());
            var changeResult = _oracleContract.ExecuteMethodWithResult(OracleMethod.TransferRegimentOwnership,
                new TransferRegimentOwnershipInput
                {
                    RegimentAddress = _regimentTrue.ConvertAddress(),
                    NewManagerAddress = oldManager
                });
            changeResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var getAfterRegimentInfo = _regimentContract.CallViewMethod<RegimentInfo>(RegimentMethod.GetRegimentInfo,
                _regimentTrue.ConvertAddress());
            getAfterRegimentInfo.Manager.ShouldBe(oldManager);
        }

        [TestMethod]
        public void GetRegimentMemberList()
        {
            var member = _oracleContract.CallViewMethod<AddressList>(OracleMethod.GetRegimentMemberList,
                _regimentNotEnough.ConvertAddress());
            Logger.Info($"{_regimentNotEnough}");
            foreach (var mem in member.Value)
            {
                Logger.Info($"{mem.ToBase58()}");
            }
        }

        #endregion

        #region Query
        [TestMethod]
        public void QueryWithoutCallBack()
        {
            var payAmount = 0;
            var allowance = _tokenContract.GetAllowance(InitAccount, _oracleContract.ContractAddress, Symbol);
            Logger.Info(allowance);
            if (allowance < payAmount)
                _tokenContract.ApproveToken(InitAccount, _oracleContract.ContractAddress, payAmount, Symbol);
            _tokenContract.IssueBalance(InitAccount, InitAccount, payAmount, Symbol);
            var nodes = _regimentContract.CallViewMethod<RegimentMemberList>(RegimentMethod.GetRegimentMemberList,
                _regiment.ConvertAddress()).Value.Count;

            var balance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            var result = ExecutedQuery(payAmount, _integerAggregator, new QueryInfo
            {
                Title = "https://api.coincap.io/v2/assets/aelf",
                Options =
                {
                    "data/priceUsd"
                }
            }, new AddressList
            {
                Value = {_regiment.ConvertAddress()}
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var byteString = result.Logs.First(l => l.Name.Contains(nameof(QueryCreated))).NonIndexed;
            var query = QueryCreated.Parser.ParseFrom(ByteString.FromBase64(byteString));
            query.Payment.ShouldBe(payAmount);
            query.Token.ShouldBe("Test");
            query.QuerySender.ShouldBe(InitAccount.ConvertAddress());
            query.AggregatorContractAddress.ShouldBe(_integerAggregator);
            // query.AggregateThreshold.ShouldBe(Math.Max(nodes.Div(3).Add(1),1));
            Logger.Info(query.QueryId.ToHex());

            var getQuery = GetQueryRecord(query.QueryId.ToHex());
            getQuery.Payment.ShouldBe(query.Payment);
            getQuery.Token.ShouldBe(query.Token);
            getQuery.AggregateThreshold.ShouldBe(Math.Max(nodes.Div(3).Add(1), 1));
            getQuery.CallbackInfo.ShouldBe(query.CallbackInfo);
            getQuery.QueryInfo.ShouldBe(query.QueryInfo);
            getQuery.IsCancelled.ShouldBeFalse();
            getQuery.AggregatorContractAddress.ShouldBe(query.AggregatorContractAddress);
            getQuery.QuerySender.ShouldBe(query.QuerySender);
            getQuery.IsCommitStageFinished.ShouldBeFalse();
            getQuery.IsSufficientDataCollected.ShouldBeFalse();
            getQuery.IsSufficientCommitmentsCollected.ShouldBeFalse();
            getQuery.IsPaidToOracleContract.ShouldBeFalse();

            //wait node to push message
            Thread.Sleep(120000);
            var finalRecord = GetQueryRecord(query.QueryId.ToHex());
            finalRecord.IsCommitStageFinished.ShouldBeTrue();
            finalRecord.IsSufficientDataCollected.ShouldBeTrue();
            finalRecord.IsSufficientCommitmentsCollected.ShouldBeTrue();
            CheckRevel();
            CheckNodeBalance();
            var afterBalance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            afterBalance.ShouldBe(balance - payAmount);
        }

        [TestMethod]
        public void QueryWithCommitAndReveal()
        {
            var payAmount = 0;
            var allowance = _tokenContract.GetAllowance(InitAccount, _oracleContract.ContractAddress, Symbol);
            Logger.Info(allowance);
            if (allowance < payAmount)
                _tokenContract.ApproveToken(InitAccount, _oracleContract.ContractAddress, payAmount, Symbol);
            _tokenContract.IssueBalance(InitAccount, InitAccount, payAmount, Symbol);
            var nodes = _regimentContract.CallViewMethod<RegimentMemberList>(RegimentMethod.GetRegimentMemberList,
                _regiment.ConvertAddress()).Value.Count;

            var balance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            var result = ExecutedQuery(payAmount, _integerAggregator, new QueryInfo
            {
                Title = "https://api.coincap.io/v2/assets/aelf",
                Options =
                {
                    "data/priceUsd"
                }
            }, new AddressList
            {
                Value = {_regiment.ConvertAddress()}
            }, new CallbackInfo
            {
                ContractAddress = _oracleUserContract.Contract,
                MethodName = "RecordPrice"
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var byteString = result.Logs.First(l => l.Name.Contains(nameof(QueryCreated))).NonIndexed;
            var query = QueryCreated.Parser.ParseFrom(ByteString.FromBase64(byteString));
            query.Payment.ShouldBe(payAmount);
            query.Token.ShouldBe("Test");
            query.QuerySender.ShouldBe(InitAccount.ConvertAddress());
            query.AggregatorContractAddress.ShouldBe(_integerAggregator);
            // query.AggregateThreshold.ShouldBe(Math.Max(nodes.Div(3).Add(1),1));
            Logger.Info(query.QueryId.ToHex());

            var getQuery = GetQueryRecord(query.QueryId.ToHex());
            getQuery.Payment.ShouldBe(query.Payment);
            getQuery.Token.ShouldBe(query.Token);
            getQuery.AggregateThreshold.ShouldBe(Math.Max(nodes.Div(3).Add(1), 1));
            getQuery.CallbackInfo.ShouldBe(query.CallbackInfo);
            getQuery.QueryInfo.ShouldBe(query.QueryInfo);
            getQuery.IsCancelled.ShouldBeFalse();
            getQuery.AggregatorContractAddress.ShouldBe(query.AggregatorContractAddress);
            getQuery.QuerySender.ShouldBe(query.QuerySender);
            getQuery.IsCommitStageFinished.ShouldBeFalse();
            getQuery.IsSufficientDataCollected.ShouldBeFalse();
            getQuery.IsSufficientCommitmentsCollected.ShouldBeFalse();
            getQuery.IsPaidToOracleContract.ShouldBeFalse();

            //wait node to push message
            Thread.Sleep(120000);
            var finalRecord = GetQueryRecord(query.QueryId.ToHex());
            finalRecord.IsCommitStageFinished.ShouldBeTrue();
            finalRecord.IsSufficientDataCollected.ShouldBeTrue();
            finalRecord.IsSufficientCommitmentsCollected.ShouldBeTrue();
            CheckRevel();
            CheckNodeBalance();
            var afterBalance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            afterBalance.ShouldBe(balance - payAmount);
        }

        [TestMethod]
        public void QueryWithCommitAndReveal_PaidToOracleIsTrue()
        {
            var payAmount = 100000000;
            var allowance = _tokenContract.GetAllowance(TestAccount, _oracleContract.ContractAddress, Symbol);
            Logger.Info(allowance);
            _tokenContract.TransferBalance(InitAccount, TestAccount, 1000_00000000);
            if (allowance < payAmount)
                _tokenContract.ApproveToken(TestAccount, _oracleContract.ContractAddress, payAmount, Symbol);
            _tokenContract.IssueBalance(InitAccount, TestAccount, payAmount, Symbol);
            var nodes = _regimentContract.CallViewMethod<RegimentMemberList>(RegimentMethod.GetRegimentMemberList,
                _regiment.ConvertAddress()).Value.Count;

            var balance = _tokenContract.GetUserBalance(TestAccount, Symbol);
            var result = ExecutedQuery(payAmount, _integerAggregator, new QueryInfo
            {
                Title = "https://api.coincap.io/v2/assets/aelf",
                Options =
                {
                    "data/priceUsd"
                }
            }, new AddressList
            {
                Value = {_regiment.ConvertAddress()}
            }, new CallbackInfo
            {
                ContractAddress = _oracleUserContract.Contract,
                MethodName = "RecordPrice"
            }, TestAccount);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = _tokenContract.GetUserBalance(TestAccount, Symbol);
            afterBalance.ShouldBe(balance - payAmount);
            var byteString = result.Logs.First(l => l.Name.Contains(nameof(QueryCreated))).NonIndexed;
            var query = QueryCreated.Parser.ParseFrom(ByteString.FromBase64(byteString));
            query.Payment.ShouldBe(payAmount);
            query.Token.ShouldBe("Test");
            query.QuerySender.ShouldBe(TestAccount.ConvertAddress());
            query.AggregatorContractAddress.ShouldBe(_integerAggregator);
            // query.AggregateThreshold.ShouldBe(Math.Max(nodes.Div(3).Add(1),1));
            Logger.Info(query.QueryId.ToHex());

            var getQuery = GetQueryRecord(query.QueryId.ToHex());
            getQuery.Payment.ShouldBe(query.Payment);
            getQuery.Token.ShouldBe(query.Token);
            getQuery.AggregateThreshold.ShouldBe(Math.Max(nodes.Div(3).Add(1), 1));
            getQuery.CallbackInfo.ShouldBe(query.CallbackInfo);
            getQuery.QueryInfo.ShouldBe(query.QueryInfo);
            getQuery.IsCancelled.ShouldBeFalse();
            getQuery.AggregatorContractAddress.ShouldBe(query.AggregatorContractAddress);
            getQuery.QuerySender.ShouldBe(query.QuerySender);
            getQuery.IsCommitStageFinished.ShouldBeFalse();
            getQuery.IsSufficientDataCollected.ShouldBeFalse();
            getQuery.IsSufficientCommitmentsCollected.ShouldBeFalse();
            getQuery.IsPaidToOracleContract.ShouldBeTrue();
            //wait node to push message
            Thread.Sleep(120000);
            var finalRecord = GetQueryRecord(query.QueryId.ToHex());
            finalRecord.IsCommitStageFinished.ShouldBeTrue();
            finalRecord.IsSufficientDataCollected.ShouldBeTrue();
            finalRecord.IsSufficientCommitmentsCollected.ShouldBeTrue();
            CheckRevel();
            CheckNodeBalance();
        }

        [TestMethod]
        public void QueryWithCommitAndReveal_Parliament()
        {
            var payAmount = 0;
            var allowance = _tokenContract.GetAllowance(InitAccount, _oracleContract.ContractAddress, Symbol);
            Logger.Info(allowance);
            if (allowance < payAmount)
                _tokenContract.ApproveToken(InitAccount, _oracleContract.ContractAddress, payAmount, Symbol);
            // _tokenContract.IssueBalance(InitAccount, InitAccount, payAmount, Symbol);
            var nodes = AuthorityManager.GetCurrentMiners().Count;
            var balance = _tokenContract.GetUserBalance(InitAccount, Symbol);

            var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.Query, new QueryInput
            {
                Payment = payAmount,
                AggregateThreshold = 1,
                AggregatorContractAddress = _integerAggregator,
                QueryInfo = new QueryInfo
                {
                    Title = "https://api.coincap.io/v2/assets/aelf",
                    Options =
                    {
                        "data/priceUsd"
                    }
                },
                DesignatedNodeList = new AddressList
                {
                    Value = {_parliamentContract.Contract}
                },
                Token = "Test"
            });

            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            afterBalance.ShouldBe(balance - payAmount);
            var byteString = result.Logs.First(l => l.Name.Contains(nameof(QueryCreated))).NonIndexed;
            var query = QueryCreated.Parser.ParseFrom(ByteString.FromBase64(byteString));
            var oracleNodeThreshold =
                _oracleContract.CallViewMethod<OracleNodeThreshold>(OracleMethod.GetThreshold, new Empty());
            var setAggregateThreshold = oracleNodeThreshold.DefaultAggregateThreshold;
            query.Payment.ShouldBe(payAmount);
            // query.Token.ShouldBe("Test");
            query.QuerySender.ShouldBe(InitAccount.ConvertAddress());
            query.AggregatorContractAddress.ShouldBe(_integerAggregator);
            query.AggregateThreshold.ShouldBe(Math.Max(Math.Max(nodes.Div(3).Add(1), 1), setAggregateThreshold));
            Logger.Info(query.QueryId.ToHex());

            var getQuery = GetQueryRecord(query.QueryId.ToHex());
            getQuery.Payment.ShouldBe(query.Payment);
            getQuery.Token.ShouldBe(query.Token);
            getQuery.AggregateThreshold.ShouldBe(Math.Max(Math.Max(nodes.Div(3).Add(1), 1), setAggregateThreshold));
            getQuery.CallbackInfo.ShouldBe(query.CallbackInfo);
            getQuery.QueryInfo.ShouldBe(query.QueryInfo);
            getQuery.IsCancelled.ShouldBeFalse();
            getQuery.AggregatorContractAddress.ShouldBe(query.AggregatorContractAddress);
            getQuery.QuerySender.ShouldBe(query.QuerySender);
            getQuery.IsCommitStageFinished.ShouldBeFalse();
            getQuery.IsSufficientDataCollected.ShouldBeFalse();
            getQuery.IsSufficientCommitmentsCollected.ShouldBeFalse();
            //wait node to push message
            Thread.Sleep(30000);
            var finalRecord = GetQueryRecord(query.QueryId.ToHex());
            finalRecord.IsCommitStageFinished.ShouldBeTrue();
            finalRecord.IsSufficientDataCollected.ShouldBeTrue();
            finalRecord.IsSufficientCommitmentsCollected.ShouldBeTrue();
            var price = finalRecord.FinalResult;
            Logger.Info($"{price}");

            // CheckRevel();
            // CheckNodeBalance();
        }

        [TestMethod]
        public void QueryWithCommitAndReveal_OtherRegiment()
        {
            var payAmount = 100000000;
            var allowance = _tokenContract.GetAllowance(InitAccount, _oracleContract.ContractAddress, Symbol);
            Logger.Info(allowance);
            if (allowance < payAmount)
                _tokenContract.ApproveToken(InitAccount, _oracleContract.ContractAddress, payAmount, Symbol);
            _tokenContract.IssueBalance(InitAccount, InitAccount, payAmount, Symbol);

            var balance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            var result = ExecutedQuery(payAmount, _integerAggregator, new QueryInfo
            {
                Title = "https://api.coincap.io/v2/assets/aelf",
                Options =
                {
                    "data/priceUsd"
                }
            }, new AddressList
            {
                Value = {_regimentTrue.ConvertAddress() }
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            afterBalance.ShouldBe(balance - payAmount);
            var byteString = result.Logs.First(l => l.Name.Contains(nameof(QueryCreated))).NonIndexed;
            var query = QueryCreated.Parser.ParseFrom(ByteString.FromBase64(byteString));
            query.Payment.ShouldBe(payAmount);
            query.Token.ShouldBe("Test");
            query.QuerySender.ShouldBe(InitAccount.ConvertAddress());
            query.AggregatorContractAddress.ShouldBe(_integerAggregator);
            // query.AggregateThreshold.ShouldBe(Math.Max(nodes.Div(3).Add(1),1));
            Logger.Info(query.QueryId.ToHex());

            var getQuery = GetQueryRecord(query.QueryId.ToHex());
            getQuery.Payment.ShouldBe(query.Payment);
            getQuery.Token.ShouldBe(query.Token);
            // getQuery.AggregateThreshold.ShouldBe(Math.Max(nodes.Div(3).Add(1), 1));
            getQuery.CallbackInfo.ShouldBe(query.CallbackInfo);
            getQuery.QueryInfo.ShouldBe(query.QueryInfo);
            getQuery.IsCancelled.ShouldBeFalse();
            getQuery.AggregatorContractAddress.ShouldBe(query.AggregatorContractAddress);
            getQuery.QuerySender.ShouldBe(query.QuerySender);
            getQuery.IsCommitStageFinished.ShouldBeFalse();
            getQuery.IsSufficientDataCollected.ShouldBeFalse();
            getQuery.IsSufficientCommitmentsCollected.ShouldBeFalse();
        }

        [TestMethod]
        public void QueryWithoutAggregator()
        {
            var payAmount = 100000000;
            var allowance = _tokenContract.GetAllowance(InitAccount, _oracleContract.ContractAddress, Symbol);
            Logger.Info(allowance);
            if (allowance < payAmount)
                _tokenContract.ApproveToken(InitAccount, _oracleContract.ContractAddress, payAmount, Symbol);
            _tokenContract.IssueBalance(InitAccount, InitAccount, payAmount, Symbol);
            var nodes = _regimentContract.CallViewMethod<RegimentMemberList>(RegimentMethod.GetRegimentMemberList,
                _regiment.ConvertAddress()).Value.Count;
            var balance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            var result = ExecutedQuery(payAmount, null, new QueryInfo
            {
                Title = "http://localhost:7080/price/elf",
                Options =
                {
                    "price"
                }
            }, new AddressList
            {
                Value = {_regiment.ConvertAddress()}
            }, new CallbackInfo
            {
                ContractAddress = _oracleUserContract.Contract,
                MethodName = "RecordPrice"
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            afterBalance.ShouldBe(balance - payAmount);
            var byteString = result.Logs.First(l => l.Name.Contains(nameof(QueryCreated))).NonIndexed;
            var query = QueryCreated.Parser.ParseFrom(ByteString.FromBase64(byteString));
            query.Payment.ShouldBe(payAmount);
            query.Token.ShouldBe("Test");
            query.QuerySender.ShouldBe(InitAccount.ConvertAddress());
            query.AggregatorContractAddress.ShouldBeNull();
            query.AggregateThreshold.ShouldBe(Math.Max(nodes.Div(3).Add(1), 1));
            Logger.Info(query.QueryId.ToHex());

            var getQuery = GetQueryRecord(query.QueryId.ToHex());
            getQuery.Payment.ShouldBe(query.Payment);
            getQuery.Token.ShouldBe(query.Token);
            getQuery.AggregateThreshold.ShouldBe(Math.Max(nodes.Div(3).Add(1), 1));
            getQuery.CallbackInfo.ShouldBe(query.CallbackInfo);
            getQuery.QueryInfo.ShouldBe(query.QueryInfo);
            getQuery.IsCancelled.ShouldBeFalse();
            getQuery.AggregatorContractAddress.ShouldBe(query.AggregatorContractAddress);
            getQuery.QuerySender.ShouldBe(query.QuerySender);
            //wait node to push message
            Thread.Sleep(5000);
            var finalRecord = GetQueryRecord(query.QueryId.ToHex());
            finalRecord.IsCommitStageFinished.ShouldBeTrue();
            finalRecord.IsSufficientDataCollected.ShouldBeTrue();
            finalRecord.IsSufficientCommitmentsCollected.ShouldBeTrue();
            CheckRevel();
            // CheckNodeBalance();
        }

        [TestMethod]
        public void QueryList()
        {
            var payAmount = 100000000;
            var allowance = _tokenContract.GetAllowance(InitAccount, _oracleContract.ContractAddress, Symbol);
            Logger.Info(allowance);
            if (allowance < payAmount)
                _tokenContract.ApproveToken(InitAccount, _oracleContract.ContractAddress, payAmount, Symbol);
            _tokenContract.IssueBalance(InitAccount, InitAccount, payAmount, Symbol);
            var balance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            var list = new List<Address>();
            _associationMember.ForEach(l => { list.Add(l.ConvertAddress()); });
            var result = ExecutedQuery(payAmount,
                _integerAggregator,
                new QueryInfo
                {
                    Title = "http://localhost:7080/price/elf",
                    Options =
                    {
                        "price"
                    }
                }, new AddressList {
                    Value = {list}
                }, new CallbackInfo
                {
                    ContractAddress = _oracleUserContract.Contract,
                    MethodName = "RecordPrice"
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            afterBalance.ShouldBe(balance - payAmount);
            var byteString = result.Logs.First(l => l.Name.Contains(nameof(QueryCreated))).NonIndexed;
            var query = QueryCreated.Parser.ParseFrom(ByteString.FromBase64(byteString));
            query.Payment.ShouldBe(payAmount);
            query.Token.ShouldBe("Test");
            query.QuerySender.ShouldBe(InitAccount.ConvertAddress());
            query.AggregatorContractAddress.ShouldBe(_integerAggregator);
            Logger.Info(query.QueryId.ToHex());
        }

        [TestMethod]
        public void Query_Failed()
        {
            var payAmount = 100000000;
            var allowance = _tokenContract.GetAllowance(InitAccount, _oracleContract.ContractAddress, Symbol);
            Logger.Info(allowance);
            if (allowance < payAmount)
                _tokenContract.ApproveToken(InitAccount, _oracleContract.ContractAddress, payAmount, Symbol);
            _tokenContract.IssueBalance(InitAccount, InitAccount, payAmount, Symbol);
            var balance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            {
                var result = ExecutedQuery(payAmount,
                    _integerAggregator,
                    new QueryInfo
                    {
                        Title = "http://localhost:7080/price/elf",
                        Options =
                        {
                            "price"
                        }
                    }, new AddressList
                    {
                        Value = {_regimentNotEnough.ConvertAddress()}
                    }, new CallbackInfo
                    {
                        ContractAddress = _oracleUserContract.Contract,
                        MethodName = "RecordPrice"
                    });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                result.Error.ShouldContain("Invalid designated nodes count, should at least be");
                _tokenContract.GetUserBalance(InitAccount, Symbol).ShouldBe(balance);
            }
            {
                var result = ExecutedQuery(payAmount,
                    _integerAggregator,
                    new QueryInfo
                    {
                        Title = "http://localhost:7080/price/elf",
                        Options =
                        {
                            "price"
                        }
                    }, new AddressList
                    {
                        Value = {_associationMember[0].ConvertAddress()}
                    }, new CallbackInfo
                    {
                        ContractAddress = _oracleUserContract.Contract,
                        MethodName = "RecordPrice"
                    });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                result.Error.ShouldContain("Designated association not exists.");
                _tokenContract.GetUserBalance(InitAccount, Symbol).ShouldBe(balance);
            }



        }

        [TestMethod]
        public void QueryCancel()
        {
            var id = "d4b2c9abb546e2853a40f49e278328ea35d89584b045c1bcdbd9d5f363b0c26b";
            var queryId = Hash.LoadFromHex(id);
            var queryInfo = GetQueryRecord(queryId.ToHex());
            var sender = queryInfo.QuerySender;
            var amount = queryInfo.Payment;

            var balance = _tokenContract.GetUserBalance(sender.ToBase58(), Symbol);
            var cancelResult = _oracleContract.ExecuteMethodWithResult(OracleMethod.CancelQuery, queryId);
            cancelResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var getQuery = GetQueryRecord(queryId.ToHex());
            getQuery.IsCancelled.ShouldBeTrue();
            var afterBalance = _tokenContract.GetUserBalance(sender.ToBase58(), Symbol);

            afterBalance.ShouldBe(balance + amount);
        }

        [TestMethod]
        public void QueryCancel_Failed()
        {
            {
                // query not existed
                var queryId = HashHelper.ComputeFrom("NotExisted");
                var cancelResult = _oracleContract.ExecuteMethodWithResult(OracleMethod.CancelQuery, queryId);
                cancelResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                cancelResult.Error.ShouldContain("Query not exists.");
            }
            {
                // no permission
                var id = "335c5790e8feca8a229737092946f4dee20ba88e44ea6f9aa77ec4c47df31ad4";
                var queryId = Hash.LoadFromHex(id);
                _oracleContract.SetAccount(TestAccount);
                var cancelResult = _oracleContract.ExecuteMethodWithResult(OracleMethod.CancelQuery, queryId);
                cancelResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                cancelResult.Error.ShouldContain("No permission to cancel this query.");
            }
            {
                // already cancel
                var id = "335c5790e8feca8a229737092946f4dee20ba88e44ea6f9aa77ec4c47df31ad4";
                var queryId = Hash.LoadFromHex(id);
                _oracleContract.SetAccount(InitAccount);
                var cancelResult = _oracleContract.ExecuteMethodWithResult(OracleMethod.CancelQuery, queryId);
                cancelResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                cancelResult.Error.ShouldContain("Query already cancelled.");
            }
        }

        [TestMethod]
        public void Commit_Fake()
        {
            _oracleContract.SetAccount(InitAccount);
            {
                // Commit stage of this query is already finished.
                var queryId = "048539fefcb0d207364cbae1a46b030085eeaf88f10485ddd11b881d23e70b72";
                var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.Commit, new CommitInput
                {
                    QueryId = Hash.LoadFromHex(queryId),
                    Commitment = Hash.LoadFromHex("cf47ba005c83cee09af1430d4efb1157f03088093efbb72d2f6e141d39aa327c")
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                result.Error.ShouldContain("Commit stage of this query is already finished.");
            }
        }

        //b2cde62bb42d50349affeefd64aeb231a7bb860d54f402c8ffc5d1de7dc3d07a
        [TestMethod]
        public void Commit_Failed()
        {
            _oracleContract.SetAccount(_associationMember[0]);
            {
                // Commit stage of this query is already finished.
                var queryId = "ff2b18b4389fc991c54c20e1fc4241ccff6f0bbd2c5f5cc89034c1d60145c54a";
                var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.Commit, new CommitInput
                {
                    QueryId = Hash.LoadFromHex(queryId),
                    Commitment = Hash.LoadFromHex("dea54f09d9ec153e3928addd327ba4ea08e2752c5fca1942c967a157fe0c4184")
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                result.Error.ShouldContain("Commit stage of this query is already finished.");
            }
            {
                // Query expired.
                var queryId = "b2cde62bb42d50349affeefd64aeb231a7bb860d54f402c8ffc5d1de7dc3d07a";
                var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.Commit, new CommitInput
                {
                    QueryId = Hash.LoadFromHex(queryId),
                    Commitment = Hash.LoadFromHex("dea54f09d9ec153e3928addd327ba4ea08e2752c5fca1942c967a157fe0c4184")
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                result.Error.ShouldContain("Query expired.");
            }
        }

        [TestMethod]
        public void Reveal_Failed()
        {
            _oracleContract.SetAccount(OtherNode);
            {
                // Commit stage of this query is already finished.
                var queryId = "e37fd49232e37e4c050871645cd8898cff33bdb4ca08e1d6568ec877dff374d6";
                var date = "CiEiRUxGIjsiMjAyMS0wNC0zMCAwODozMTo1NiI7NC4yMzY=";
                var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.Reveal, new RevealInput
                {
                    QueryId = Hash.LoadFromHex(queryId),
                    Data = StringValue.Parser.ParseFrom(ByteString.FromBase64(date)).Value
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                result.Error.ShouldContain("This query hasn't collected sufficient commitments.");
            }
            {
                _oracleContract.SetAccount(_associationMember[2]);
                // No permission to reveal for this query. Sender hasn't submit commitment.
                var queryId = "e37fd49232e37e4c050871645cd8898cff33bdb4ca08e1d6568ec877dff374d6";
                var date = "CiEiRUxGIjsiMjAyMS0wNC0zMCAwODozMTo1NiI7NC4yMzY=";
                var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.Reveal, new RevealInput
                {
                    QueryId = Hash.LoadFromHex(queryId),
                    Data = date
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                result.Error.ShouldContain("No permission to reveal for this query. Sender hasn't submit commitment.");
            }
        }

        [TestMethod]
        public void GetQueryRecord()
        {
            var id = "f7e907f61997f8a4e5d04b390422cde08bdc1bfd4394e3d7f38ceb8471f6eda9";
            var record = GetQueryRecord(id);
            record.IsPaidToOracleContract.ShouldBeTrue();
            if (record.IsCancelled)
                Logger.Info($"this query {id} is already cancelled");
            else if (record.IsSufficientDataCollected)
                Logger.Info($"this query {id} is already finished");
            else if (record.IsSufficientCommitmentsCollected)
                Logger.Info($"this query {id} is committed, it is ready to reveal");
            var price = record.FinalResult;
            Logger.Info(price);
        }

        [TestMethod]
        public void CheckRevel()
        {
            var price = _oracleUserContract.CallViewMethod<PriceRecordList>(OracleUserMethod.GetHistoryPrices,
                new Empty());
            Logger.Info(price.Value.First());
        }

        [TestMethod]
        public void GetHelpfulNodeList()
        {
            var id = "e7d60d76819199a15c56694e276d779dfe051ea70e7cb296c9ae81145135b322";
            var helpfulNode = _oracleUserContract.CallViewMethod<PriceRecordList>(OracleUserMethod.GetHelpfulNodeList,
                Hash.LoadFromHex(id));
            foreach (var h in helpfulNode.Value)
                Logger.Info(h.Price);
        }

        #endregion

        [TestMethod]
        public void LockToken()
        {
            var amount = 10000000000;
            foreach (var member in _associationMember)
            {
                _tokenContract.IssueBalance(InitAccount, member, amount, Symbol);
                _tokenContract.TransferBalance(InitAccount, member, amount);
                _tokenContract.ApproveToken(member, _oracleContract.ContractAddress, amount, Symbol);
                var allowance = _tokenContract.GetAllowance(member, _oracleContract.ContractAddress, Symbol);
                Logger.Info(allowance);
                var beforeBalance = _oracleContract.CallViewMethod<Int64Value>(OracleMethod.GetLockedTokensAmount,
                    Address.FromBase58(member));
                _oracleContract.SetAccount(member);
                var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.LockTokens, new LockTokensInput
                {
                    LockAmount = amount,
                    OracleNodeAddress = Address.FromBase58(member)
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var balance = _oracleContract.CallViewMethod<Int64Value>(OracleMethod.GetLockedTokensAmount,
                    Address.FromBase58(member));
                balance.Value.ShouldBe(beforeBalance.Value + amount);
            }
        }

        [TestMethod]
        public void UnLockToken()
        {
            foreach (var member in _associationMember)
            {
                var beforeBalance = _oracleContract.CallViewMethod<Int64Value>(OracleMethod.GetLockedTokensAmount,
                    Address.FromBase58(member));
                _oracleContract.SetAccount(member);
                var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.UnlockTokens, new UnlockTokensInput
                {
                    WithdrawAmount = beforeBalance.Value / 2,
                    OracleNodeAddress = Address.FromBase58(member)
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var balance = _oracleContract.CallViewMethod<Int64Value>(OracleMethod.GetLockedTokensAmount,
                    Address.FromBase58(member));
                balance.Value.ShouldBe(beforeBalance.Value / 2);
            }
        }

        [TestMethod]
        public void CheckNodeBalance()
        {
            foreach (var member in _associationMember)
            {
                var balance = _tokenContract.GetUserBalance(member, Symbol);
                Logger.Info($"{member}: {balance}");
            }

            var otherBalance = _tokenContract.GetUserBalance(OtherNode, Symbol);
            Logger.Info($"{OtherNode}: {otherBalance}");

            var miners = AuthorityManager.GetCurrentMiners();
            foreach (var miner in miners)
            {
                var balance = _tokenContract.GetUserBalance(miner, Symbol);
                Logger.Info($"{miner}: {balance}");
            }
        }

        [TestMethod]
        public void Initialize_Failed_Test()
        {
            {
                //MinimumOracleNodesCount < RevealThreshold
                var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.Initialize, new InitializeInput
                {
                    MinimumOracleNodesCount = 1,
                    DefaultAggregateThreshold = 1,
                    DefaultRevealThreshold = 2,
                    DefaultExpirationSeconds = 1200
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                result.Error.ShouldContain(
                    "MinimumOracleNodesCount should be greater than or equal to DefaultRevealThreshold.");
            }
            {
                //RevealThreshold < AggregateThreshold 
                var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.Initialize, new InitializeInput
                {
                    MinimumOracleNodesCount = 1,
                    DefaultAggregateThreshold = 2,
                    DefaultRevealThreshold = 1,
                    DefaultExpirationSeconds = 1200
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                result.Error.ShouldContain(
                    "DefaultRevealThreshold should be greater than or equal to DefaultAggregateThreshold.");
            }
            {
                //DefaultAggregateThreshold < 0
                var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.Initialize, new InitializeInput
                {
                    MinimumOracleNodesCount = -1,
                    DefaultAggregateThreshold = -10,
                    DefaultRevealThreshold = -2,
                    DefaultExpirationSeconds = 1200
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                result.Error.ShouldContain("DefaultAggregateThreshold should be positive.");
            }
            {
                // DefaultAggregateThreshold == 0
                var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.Initialize, new InitializeInput
                {
                    MinimumOracleNodesCount = 0,
                    DefaultRevealThreshold = 0,
                    DefaultAggregateThreshold = 0,
                    DefaultExpirationSeconds = 1200
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var oracleNodeThreshold =
                    _oracleContract.CallViewMethod<OracleNodeThreshold>(OracleMethod.GetThreshold, new Empty());
                oracleNodeThreshold.DefaultAggregateThreshold.ShouldBe(1);
                oracleNodeThreshold.MinimumOracleNodesCount.ShouldBe(3);
                oracleNodeThreshold.DefaultRevealThreshold.ShouldBe(2);
            }
        }

        #region controller

        [TestMethod]
        public void SetThreshold()
        {
            var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.SetThreshold, new OracleNodeThreshold
            {
                DefaultAggregateThreshold = 1,
                DefaultRevealThreshold = 1,
                MinimumOracleNodesCount = 3
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var oracleNodeThreshold =
                _oracleContract.CallViewMethod<OracleNodeThreshold>(OracleMethod.GetThreshold, new Empty());
            oracleNodeThreshold.DefaultAggregateThreshold.ShouldBe(1);
            oracleNodeThreshold.MinimumOracleNodesCount.ShouldBe(3);
            oracleNodeThreshold.DefaultRevealThreshold.ShouldBe(1);
        }

        [TestMethod]
        public void GetThreshold()
        {
            var threshold = _oracleContract.CallViewMethod<OracleNodeThreshold>(OracleMethod.GetThreshold, new Empty());
            Logger.Info($"\naggregate: {threshold.DefaultAggregateThreshold}\n" +
                        $"reveal: {threshold.DefaultRevealThreshold} \n" +
                        $"node count: {threshold.MinimumOracleNodesCount}");
        }

        [TestMethod]
        [DataRow(1, 2, 1)]
        [DataRow(2, 1, 2)]
        [DataRow(3, 2, 0)]
        public void SetThreshold_Failed(int minimumOracleNodesCount, int revealThreshold, int aggregateThreshold)
        {
            {
                var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.SetThreshold, new OracleNodeThreshold
                {
                    MinimumOracleNodesCount = minimumOracleNodesCount,
                    DefaultRevealThreshold = revealThreshold,
                    DefaultAggregateThreshold = aggregateThreshold
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
            }
        }

        [TestMethod]
        public void ChangeDefaultExpirationSeconds()
        {
            var value = 600;
            var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.ChangeDefaultExpirationSeconds,
                new Int32Value
                {
                    Value = value
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var getExpirationSeconds =
                _oracleContract.CallViewMethod<Int32Value>(OracleMethod.GetDefaultExpirationSeconds, new Empty());
            getExpirationSeconds.Value.ShouldBe(value);
        }

        [TestMethod]
        public void ChangeController()
        {
            var result =
                _oracleContract.ExecuteMethodWithResult(OracleMethod.ChangeController, _defaultParliamentOrganization);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var changeFee = _oracleContract.ExecuteMethodWithResult(OracleMethod.EnableChargeFee, new Empty());
            changeFee.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
            changeFee.Error.ShouldContain("Not authorized");

            var changeAgain = AuthorityManager.ExecuteTransactionWithAuthority(_oracleContract
                .ContractAddress, nameof(OracleMethod.ChangeController), Address.FromBase58(InitAccount), InitAccount);
            changeAgain.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void AddPostPayAddress()
        {
            var report = "2WHXRoLRjbUTDQsuqR5CntygVfnDb125qdJkudev4kVNbLhTdG";
            var result =
                _oracleContract.ExecuteMethodWithResult(OracleMethod.AddPostPayAddress,
                    Address.FromBase58(report));
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        #endregion

        #region Task

        [TestMethod]
        public void CreateQueryTask()
        {
            var payAmount = 100000000;
            var times = 3;
            var allowance = _tokenContract.GetAllowance(InitAccount, _oracleContract.ContractAddress, Symbol);
            Logger.Info(allowance);
            if (allowance < (long)payAmount * times * 10)
                _tokenContract.ApproveToken(InitAccount, _oracleContract.ContractAddress, (long)payAmount * times * 10, Symbol);
            // _tokenContract.IssueBalance(InitAccount, InitAccount, payAmount * times, Symbol);
            var balance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            var result =
                _oracleContract.ExecuteMethodWithResult(OracleMethod.CreateQueryTask, new CreateQueryTaskInput
                {
                    EachPayment = payAmount,
                    SupposedQueryTimes = times,
                    QueryInfo = new QueryInfo
                    {
                        Title = "https://api.coincap.io/v2/assets/aelf",
                        Options =
                        {
                            "data/priceUsd"
                        }
                    },
                    EndTime = Timestamp.FromDateTime(DateTime.UtcNow.AddHours(1)),
                    AggregatorContractAddress = _integerAggregator,
                    AggregateOption = 0,
                    CallbackInfo = new CallbackInfo
                    {
                        ContractAddress = _oracleUserContract.Contract,
                        MethodName = "RecordPrice"
                    }
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logEventDto = result.Logs.First(l => l.Name.Equals(nameof(QueryTaskCreated))).NonIndexed;
            var queryTaskCreated = QueryTaskCreated.Parser.ParseFrom(ByteString.FromBase64(logEventDto));
            Logger.Info(queryTaskCreated.Creator.ToBase58());
            queryTaskCreated.Creator.ShouldBe(_oracleContract.CallAccount);
            queryTaskCreated.SupposedQueryTimes.ShouldBe(times);
            queryTaskCreated.AggregatorContractAddress.ShouldBe(_integerAggregator);
            var taskId = Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            Logger.Info($"{taskId.ToHex()}");
            var afterBalance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            balance.ShouldBe(afterBalance);

            var getTask = _oracleContract.CallViewMethod<QueryTask>(OracleMethod.GetQueryTask, taskId);
            getTask.Creator.ShouldBe(queryTaskCreated.Creator);
        }


        [TestMethod]
        public void CompleteQueryTask()
        {
            var taskId = "4a6c490aff74083ffd339c69db1dfe644174836af072efcd5cc1ce403d5f0ea3";
            var result =
                _oracleContract.ExecuteMethodWithResult(OracleMethod.CompleteQueryTask,
                    new CompleteQueryTaskInput
                    {
                        TaskId = Hash.LoadFromHex(taskId),
                        AggregateThreshold = 1,
                        DesignatedNodeList = new AddressList{Value = { _regiment.ConvertAddress()}}
                    });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var getTask = _oracleContract.CallViewMethod<QueryTask>(OracleMethod.GetQueryTask, Hash.LoadFromHex(taskId));
            getTask.DesignatedNodeList.ShouldBe(new AddressList{Value = { _regiment.ConvertAddress()}});
            getTask.AggregateThreshold.ShouldBe(3);
        }

        [TestMethod]
        public void TaskQuery()
        {
            var taskId = "4a6c490aff74083ffd339c69db1dfe644174836af072efcd5cc1ce403d5f0ea3";
            var payAmount = 100000000;

            var getTask = _oracleContract.CallViewMethod<QueryTask>(OracleMethod.GetQueryTask, Hash.LoadFromHex(taskId));
            var times = getTask.SupposedQueryTimes;
            var amount = 3000000000;
            Logger.Info(amount);
            _tokenContract.ApproveToken(InitAccount, _oracleContract.ContractAddress, amount, Symbol);

            Logger.Info(times);
            for (int i = 0; i < times + 1; i++)
            {
                var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.TaskQuery, new TaskQueryInput
                {
                    TaskId = Hash.LoadFromHex(taskId)
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var logEventDto = result.Logs.First(l => l.Name.Contains(nameof(QueryCreated))).NonIndexed;
                var queryCreated = QueryCreated.Parser.ParseFrom(ByteString.FromBase64(logEventDto));
                Logger.Info($"{queryCreated.QueryId}");
                Thread.Sleep(60000);
                CheckRevel();
            }
        }

        #endregion

        #region token

        [TestMethod]
        public void CreateToken()
        {
            var tokenInfo = _tokenContract.GetTokenInfo(Symbol);
            if (!tokenInfo.Equals(new TokenInfo()))
            {
                Logger.Info($"{Symbol} is already created");
                return;
            }

            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create,
                new CreateInput
                {
                    TokenName = "Portal Token",
                    Symbol = Symbol,
                    TotalSupply = 100_000_000_00000000,
                    Issuer = InitAccount.ConvertAddress(),
                    Decimals = 8,
                    IsBurnable = true
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void IssueToken()
        {
            var amount = 1000_00000000;
            var tokenInfo = _tokenContract.GetTokenInfo(Symbol);
            tokenInfo.ShouldNotBe(new TokenInfo());
            tokenInfo.Issuer.ShouldBe(_defaultParliamentOrganization);
            var balance = _tokenContract.GetUserBalance(InitAccount, Symbol);

            //change issuer to sender
            var res = AuthorityManager.ExecuteTransactionWithAuthority(_tokenContract.ContractAddress,
                nameof(TokenMethod.Issue),
                new IssueInput {Symbol = Symbol, Amount = amount, To = InitAccount.ConvertAddress()},
                InitAccount);
            res.Status.ShouldBe(TransactionResultStatus.Mined);

            var afterBalance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            balance.ShouldBe(afterBalance - amount);
        }

        [TestMethod]
        public void ChangeTokenIssuer()
        {
            var tokenInfo = _tokenContract.GetTokenInfo(Symbol);
            tokenInfo.ShouldNotBe(new TokenInfo());
            tokenInfo.Issuer.ShouldBe(_defaultParliamentOrganization);

            //change issuer to sender
            var res = AuthorityManager.ExecuteTransactionWithAuthority(_tokenContract.ContractAddress,
                nameof(TokenMethod.ChangeTokenIssuer),
                new ChangeTokenIssuerInput {Symbol = Symbol, NewTokenIssuer = InitAccount.ConvertAddress()},
                InitAccount);
            res.Status.ShouldBe(TransactionResultStatus.Mined);

            tokenInfo = _tokenContract.GetTokenInfo(Symbol);
            tokenInfo.Issuer.ShouldBe(InitAccount.ConvertAddress());
        }

        #endregion

        private QueryRecord GetQueryRecord(string hash)
        {
            return _oracleContract.CallViewMethod<QueryRecord>(OracleMethod.GetQueryRecord,
                Hash.LoadFromHex(hash));
        }

        [TestMethod]
        public void InitializeTestContract()
        {
            _oracleUserContract.ExecuteMethodWithResult(OracleUserMethod.Initialize, _oracleContract.Contract);
        }

        private TransactionResultDto ExecutedQuery(long payAmount, Address aggregator, QueryInfo queryInfo,
            AddressList addressList, CallbackInfo callbackInfo = null, string sender = null)
        {
            _oracleContract.SetAccount(sender ?? InitAccount);
            var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.Query, new QueryInput
            {
                Payment = payAmount,
                AggregateThreshold = 1,
                AggregatorContractAddress = aggregator,
                QueryInfo = queryInfo,
                CallbackInfo = callbackInfo,
                DesignatedNodeList = addressList,
                AggregateOption = 0,
                Token = "Test"
            });
            return result;
        }

        private void CheckMemberBalance()
        {
            foreach (var member in from member in _associationMember
                let balance = _tokenContract.GetUserBalance(member)
                where balance < 1000000000
                select member)
            {
                _tokenContract.TransferBalance(InitAccount, member, 100000000000);
            }
        }
    }
}