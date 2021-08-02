using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Regiment;
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

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class RegimentContractTest
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }

        //2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS
        private string _regimentContractAddress = "2WHXRoLRjbUTDQsuqR5CntygVfnDb125qdJkudev4kVNbLhTdG";
        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;
        private ParliamentContract _parliamentContract;
        private AssociationContract _associationContract;
        private RegimentContract _regimentContract;
        private Address _defaultParliamentOrganization;

        private readonly List<Address> _member = new List<Address>
        {
            "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK".ConvertAddress(),
            "28qLVdGMokanMAp9GwfEqiWnzzNifh8LS9as6mzJFX1gQBB823".ConvertAddress(),
            "eFU9Quc8BsztYpEHKzbNtUpu9hGKgwGD2tyL13MqtFkbnAoCZ".ConvertAddress()
        };

        private readonly List<Address> _addMemberList = new List<Address>
        {
            "sjzNpr5bku3ZyvMqQrXeBkXGEvG2CTLA2cuNDfcDMaPTTAqEy".ConvertAddress(),
            "2RehEQSpXeZ5DUzkjTyhAkr9csu7fWgE5DAuB2RaKQCpdhB8zC".ConvertAddress(),
            "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6".ConvertAddress()
        };

        private string InitAccount { get; } = "zptx91dhHVJjJRxf5Wg5KAoMrDrWX6i1H2FAyKAiv2q8VZfbg";
        private string AddAccount1 { get; } = "sjzNpr5bku3ZyvMqQrXeBkXGEvG2CTLA2cuNDfcDMaPTTAqEy";
        private string AddAccount2 { get; } = "2RehEQSpXeZ5DUzkjTyhAkr9csu7fWgE5DAuB2RaKQCpdhB8zC";

        private string OtherAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";

        private string Password { get; } = "12345678";
        private static string RpcUrl { get; } = "127.0.0.1:8000";

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("RegimentContactTest");
            Logger = Log4NetHelper.GetLogger();
            NodeManager = new NodeManager(RpcUrl);

            NodeInfoHelper.SetConfig("nodes");
            AuthorityManager = new AuthorityManager(NodeManager, InitAccount, Password);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount, Password);
            _parliamentContract = _genesisContract.GetParliamentContract(InitAccount, Password);
            _associationContract = _genesisContract.GetAssociationAuthContract(InitAccount, Password);
            _defaultParliamentOrganization = _parliamentContract.GetGenesisOwnerAddress();
            _tokenContract = _genesisContract.GetTokenContract(InitAccount, Password);

            _regimentContract = _regimentContractAddress == ""
                ? new RegimentContract(NodeManager, InitAccount)
                : new RegimentContract(NodeManager, InitAccount, _regimentContractAddress);
        }

        [TestMethod]
        public void InitializeRegiment()
        {
            var result = _regimentContract.ExecuteMethodWithResult(RegimentMethod.Initialize, new InitializeInput
            {
                Controller = InitAccount.ConvertAddress(),
                MemberJoinLimit = 5,
                RegimentLimit = 7,
                MaximumAdminsCount = 2
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var getController = _regimentContract.CallViewMethod<Address>(RegimentMethod.GetController, new Empty());
            getController.ShouldBe(InitAccount.ConvertAddress());
            var getConfig =
                _regimentContract.CallViewMethod<RegimentContractConfig>(RegimentMethod.GetConfig, new Empty());
            getConfig.RegimentLimit.ShouldBe(7);
            getConfig.MaximumAdminsCount.ShouldBe(2);
            getConfig.MemberJoinLimit.ShouldBe(5);
        }

        [TestMethod]
        public void CreateRegiment()
        {
            var result =
                _regimentContract.ExecuteMethodWithResult(RegimentMethod.CreateRegiment, new CreateRegimentInput
                {
                    IsApproveToJoin = false,
                    Manager = InitAccount.ConvertAddress(),
                    InitialMemberList = {_member}
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logEventDto = result.Logs.First(l => l.Name.Contains(nameof(RegimentCreated))).NonIndexed;
            var regimentCreated = RegimentCreated.Parser.ParseFrom(ByteString.FromBase64(logEventDto));
            Logger.Info($"Manager: {regimentCreated.Manager}\n" +
                        $"Create Time: {regimentCreated.CreateTime}\n" +
                        $"Regiment Address: {regimentCreated.RegimentAddress}\n");
            var organization = _associationContract.GetOrganization(regimentCreated.RegimentAddress);
            organization.OrganizationMemberList.OrganizationMembers.Count.ShouldBe(1);
            organization.OrganizationMemberList.OrganizationMembers.ShouldContain(_regimentContract.Contract);
            organization.ProposerWhiteList.Proposers.ShouldContain(_regimentContract.Contract);
            organization.ProposerWhiteList.Proposers.Count.ShouldBe(1);

            var getRegimentInfo = _regimentContract.CallViewMethod<RegimentInfo>(RegimentMethod.GetRegimentInfo,
                regimentCreated.RegimentAddress);
            getRegimentInfo.Manager.ShouldBe(InitAccount.ConvertAddress());
            getRegimentInfo.IsApproveToJoin.ShouldBeFalse();

            var getRegimentMemberList =
                _regimentContract.CallViewMethod<RegimentMemberList>(RegimentMethod.GetRegimentMemberList,
                    regimentCreated.RegimentAddress);
            getRegimentMemberList.Value.All(g => _member.Any(m => m.Equals(g)));
            getRegimentMemberList.Value.ShouldContain(InitAccount.ConvertAddress());

            var isRegimentMember =
                _regimentContract.CallViewMethod<BoolValue>(RegimentMethod.IsRegimentMember,
                    new IsRegimentMemberInput
                    {
                        Address = InitAccount.ConvertAddress(),
                        RegimentAddress = regimentCreated.RegimentAddress
                    });
            isRegimentMember.Value.ShouldBeTrue();
            //2tw9nWpFG8XrQVjSTtW3esVNAzouJief5tr63UJkePLNuSdxud
        }

        [TestMethod]
        public void CreateRegiment_ApproveToJoin()
        {
            var result =
                _regimentContract.ExecuteMethodWithResult(RegimentMethod.CreateRegiment, new CreateRegimentInput
                {
                    IsApproveToJoin = true,
                    Manager = InitAccount.ConvertAddress(),
                    InitialMemberList = {_member}
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logEventDto = result.Logs.First(l => l.Name.Contains(nameof(RegimentCreated))).NonIndexed;
            var regimentCreated = RegimentCreated.Parser.ParseFrom(ByteString.FromBase64(logEventDto));
            Logger.Info($"Manager: {regimentCreated.Manager}\n" +
                        $"Create Time: {regimentCreated.CreateTime}\n" +
                        $"Regiment Address: {regimentCreated.RegimentAddress}\n");
            var organization = _associationContract.GetOrganization(regimentCreated.RegimentAddress);
            organization.OrganizationMemberList.OrganizationMembers.Count.ShouldBe(1);
            organization.OrganizationMemberList.OrganizationMembers.ShouldContain(_regimentContract.Contract);
            organization.ProposerWhiteList.Proposers.ShouldContain(_regimentContract.Contract);
            organization.ProposerWhiteList.Proposers.Count.ShouldBe(1);

            var getRegimentInfo = _regimentContract.CallViewMethod<RegimentInfo>(RegimentMethod.GetRegimentInfo,
                regimentCreated.RegimentAddress);
            getRegimentInfo.Manager.ShouldBe(InitAccount.ConvertAddress());
            getRegimentInfo.IsApproveToJoin.ShouldBeTrue();

            var getRegimentMemberList =
                _regimentContract.CallViewMethod<RegimentMemberList>(RegimentMethod.GetRegimentMemberList,
                    regimentCreated.RegimentAddress);
            getRegimentMemberList.Value.All(g => _member.Any(m => m.Equals(g)));
            var isRegimentMember =
                _regimentContract.CallViewMethod<BoolValue>(RegimentMethod.IsRegimentMember,
                    new IsRegimentMemberInput
                    {
                        Address = InitAccount.ConvertAddress(),
                        RegimentAddress = regimentCreated.RegimentAddress
                    });
            isRegimentMember.Value.ShouldBeTrue();
            //LzjQjsjd74DLdaiGA882t4nbZc7bXqSihzmH2uWCodwnwMpoB
        }

        [TestMethod]
        public void JoinRegiment_IsApproveToJoinFalse()
        {
            var regimentAddress = "2tw9nWpFG8XrQVjSTtW3esVNAzouJief5tr63UJkePLNuSdxud";
            var getRegimentInfo = _regimentContract.CallViewMethod<RegimentInfo>(RegimentMethod.GetRegimentInfo,
                regimentAddress.ConvertAddress());
            getRegimentInfo.IsApproveToJoin.ShouldBeFalse();
            var getOriginRegimentMemberList =
                _regimentContract.CallViewMethod<RegimentMemberList>(RegimentMethod.GetRegimentMemberList,
                    regimentAddress.ConvertAddress());
            var result =
                _regimentContract.ExecuteMethodWithResult(RegimentMethod.JoinRegiment, new JoinRegimentInput
                {
                    RegimentAddress = regimentAddress.ConvertAddress(),
                    NewMemberAddress = AddAccount1.ConvertAddress()
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logEventDto = result.Logs.First(l => l.Name.Contains(nameof(NewMemberAdded))).NonIndexed;
            var newMemberAdded = NewMemberAdded.Parser.ParseFrom(ByteString.FromBase64(logEventDto));
            Logger.Info($"RegimentAddress: {newMemberAdded.RegimentAddress}\n" +
                        $"NewMemberAddress: {newMemberAdded.NewMemberAddress}\n" +
                        $"OperatorAddress: {newMemberAdded.OperatorAddress}");
            var getRegimentMemberList =
                _regimentContract.CallViewMethod<RegimentMemberList>(RegimentMethod.GetRegimentMemberList,
                    regimentAddress.ConvertAddress());
            getOriginRegimentMemberList.Value.Count.ShouldBe(getRegimentMemberList.Value.Count + 1);
            getRegimentMemberList.Value.ShouldContain(AddAccount1.ConvertAddress());        
        }

        [TestMethod]
        public void JoinRegiment_IsApproveToJoinTrue()
        {
            var regimentAddress = "LzjQjsjd74DLdaiGA882t4nbZc7bXqSihzmH2uWCodwnwMpoB";
            var getRegimentInfo = _regimentContract.CallViewMethod<RegimentInfo>(RegimentMethod.GetRegimentInfo,
                regimentAddress.ConvertAddress());
            getRegimentInfo.IsApproveToJoin.ShouldBeTrue();

            var getOriginRegimentMemberList =
                _regimentContract.CallViewMethod<RegimentMemberList>(RegimentMethod.GetRegimentMemberList,
                    regimentAddress.ConvertAddress());

            var result =
                _regimentContract.ExecuteMethodWithResult(RegimentMethod.JoinRegiment, new JoinRegimentInput
                {
                    RegimentAddress = regimentAddress.ConvertAddress(),
                    NewMemberAddress = AddAccount1.ConvertAddress()
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logEventDto = result.Logs.First(l => l.Name.Contains(nameof(NewMemberApplied))).NonIndexed;
            var newMemberApplied = NewMemberApplied.Parser.ParseFrom(ByteString.FromBase64(logEventDto));
            Logger.Info($"RegimentAddress: {newMemberApplied.RegimentAddress}\n" +
                        $"Apply MemberAddress: {newMemberApplied.ApplyMemberAddress}\n");
            var getRegimentMemberList =
                _regimentContract.CallViewMethod<RegimentMemberList>(RegimentMethod.GetRegimentMemberList,
                    regimentAddress.ConvertAddress());
            getOriginRegimentMemberList.Value.ShouldBe(getRegimentMemberList.Value);
        }

        [TestMethod]
        public void LeaveRegiment()
        {
            var regimentAddress = "2tw9nWpFG8XrQVjSTtW3esVNAzouJief5tr63UJkePLNuSdxud";
            var result =
                _regimentContract.ExecuteMethodWithResult(RegimentMethod.LeaveRegiment, new LeaveRegimentInput
                {
                    RegimentAddress = regimentAddress.ConvertAddress(),
                    LeaveMemberAddress = AddAccount1.ConvertAddress(),
                    OriginSenderAddress = AddAccount1.ConvertAddress()
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logEventDto = result.Logs.First(l => l.Name.Equals(nameof(RegimentMemberLeft))).NonIndexed;
            var regimentMemberLeft = RegimentMemberLeft.Parser.ParseFrom(ByteString.FromBase64(logEventDto));
            Logger.Info($"RegimentAddress: {regimentMemberLeft.RegimentAddress}\n" +
                        $"LeftMemberAddress:{regimentMemberLeft.LeftMemberAddress}");
            var getRegimentMemberList =
                _regimentContract.CallViewMethod<RegimentMemberList>(RegimentMethod.GetRegimentMemberList,
                    regimentAddress.ConvertAddress());
            getRegimentMemberList.Value.ShouldNotContain(AddAccount1.ConvertAddress());
            var isRegimentMember =
                _regimentContract.CallViewMethod<BoolValue>(RegimentMethod.IsRegimentMember,
                    new IsRegimentMemberInput
                    {
                        RegimentAddress = regimentAddress.ConvertAddress(),
                        Address = AddAccount1.ConvertAddress()
                    });
            isRegimentMember.Value.ShouldBeFalse();
        }

        [TestMethod]
        public void AddRegimentMember()
        {
            var regimentAddress = "2tw9nWpFG8XrQVjSTtW3esVNAzouJief5tr63UJkePLNuSdxud";
            var result =
                _regimentContract.ExecuteMethodWithResult(RegimentMethod.AddRegimentMember, new AddRegimentMemberInput
                {
                    RegimentAddress = regimentAddress.ConvertAddress(),
                    NewMemberAddress = AddAccount2.ConvertAddress(),
                    OriginSenderAddress = OtherAccount.ConvertAddress()
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logEventDto = result.Logs.First(l => l.Name.Contains(nameof(NewMemberAdded))).NonIndexed;
            var newMemberAdded = NewMemberAdded.Parser.ParseFrom(ByteString.FromBase64(logEventDto));
            Logger.Info($"RegimentAddress: {newMemberAdded.RegimentAddress}\n" +
                        $"NewMemberAddress: {newMemberAdded.NewMemberAddress}\n" +
                        $"OperatorAddress: {newMemberAdded.OperatorAddress}");
            var getRegimentMemberList =
                _regimentContract.CallViewMethod<RegimentMemberList>(RegimentMethod.GetRegimentMemberList,
                    regimentAddress.ConvertAddress());
            getRegimentMemberList.Value.ShouldContain(AddAccount2.ConvertAddress());
        }

        [TestMethod]
        public void DeleteRegimentMember()
        {
            var regimentAddress = "LzjQjsjd74DLdaiGA882t4nbZc7bXqSihzmH2uWCodwnwMpoB";
            var result =
                _regimentContract.ExecuteMethodWithResult(RegimentMethod.DeleteRegimentMember, new DeleteRegimentMemberInput
                {
                    RegimentAddress = regimentAddress.ConvertAddress(),
                    DeleteMemberAddress = AddAccount2.ConvertAddress(),
                    OriginSenderAddress = InitAccount.ConvertAddress()
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined); 
            var logEventDto = result.Logs.First(l => l.Name.Equals(nameof(RegimentMemberLeft))).NonIndexed;
            var regimentMemberLeft = RegimentMemberLeft.Parser.ParseFrom(ByteString.FromBase64(logEventDto));
            Logger.Info($"RegimentAddress: {regimentMemberLeft.RegimentAddress}\n" +
                        $"LeftMemberAddress:{regimentMemberLeft.LeftMemberAddress}");
            
            var getRegimentMemberList =
                _regimentContract.CallViewMethod<RegimentMemberList>(RegimentMethod.GetRegimentMemberList,
                    regimentAddress.ConvertAddress());
            getRegimentMemberList.Value.ShouldNotContain(AddAccount1.ConvertAddress());
            var isRegimentMember =
                _regimentContract.CallViewMethod<BoolValue>(RegimentMethod.IsRegimentMember,
                    new IsRegimentMemberInput
                    {
                        RegimentAddress = regimentAddress.ConvertAddress(),
                        Address = AddAccount1.ConvertAddress()
                    });
            isRegimentMember.Value.ShouldBeFalse();
        }
        
        [TestMethod]
        public void AddAdmin()
        {
            var regimentAddress = "LzjQjsjd74DLdaiGA882t4nbZc7bXqSihzmH2uWCodwnwMpoB";
            var getOriginRegimentInfo = _regimentContract.CallViewMethod<RegimentInfo>(RegimentMethod.GetRegimentInfo,
                regimentAddress.ConvertAddress());

            var addAdmin = _regimentContract.ExecuteMethodWithResult(RegimentMethod.AddAdmins, new AddAdminsInput
            {
                NewAdmins = {InitAccount.ConvertAddress(),OtherAccount.ConvertAddress()},
                RegimentAddress = regimentAddress.ConvertAddress(),
                OriginSenderAddress = InitAccount.ConvertAddress()
            });
            addAdmin.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var getRegimentInfo = _regimentContract.CallViewMethod<RegimentInfo>(RegimentMethod.GetRegimentInfo,
                regimentAddress.ConvertAddress());
            getRegimentInfo.Admins.Count.ShouldBe(getOriginRegimentInfo.Admins.Count + 2);
            getRegimentInfo.Admins.ShouldContain(InitAccount.ConvertAddress());
            getRegimentInfo.Admins.ShouldContain(OtherAccount.ConvertAddress());
        }

        [TestMethod]
        public void DeleteAdmin()
        {
            var regimentAddress = "LzjQjsjd74DLdaiGA882t4nbZc7bXqSihzmH2uWCodwnwMpoB";
            var getOriginRegimentInfo = _regimentContract.CallViewMethod<RegimentInfo>(RegimentMethod.GetRegimentInfo,
                regimentAddress.ConvertAddress());

            var addAdmin = _regimentContract.ExecuteMethodWithResult(RegimentMethod.DeleteAdmins, new DeleteAdminsInput
            {
                DeleteAdmins = {getOriginRegimentInfo.Admins.First()},
                RegimentAddress = regimentAddress.ConvertAddress(),
                OriginSenderAddress = InitAccount.ConvertAddress()
            });
            addAdmin.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var getRegimentInfo = _regimentContract.CallViewMethod<RegimentInfo>(RegimentMethod.GetRegimentInfo,
                regimentAddress.ConvertAddress());
            getRegimentInfo.Admins.Count.ShouldBe(getOriginRegimentInfo.Admins.Count -1);
            getRegimentInfo.Admins.ShouldNotContain(getOriginRegimentInfo.Admins.First());
        }

        [TestMethod]
        public void TransferRegimentOwnership()
        {
            var regimentAddress = "LzjQjsjd74DLdaiGA882t4nbZc7bXqSihzmH2uWCodwnwMpoB";
            var getOriginRegimentInfo = _regimentContract.CallViewMethod<RegimentInfo>(RegimentMethod.GetRegimentInfo,
                regimentAddress.ConvertAddress());
            var setManager = _regimentContract.ExecuteMethodWithResult(RegimentMethod.TransferRegimentOwnership,
                new TransferRegimentOwnershipInput
                {
                    RegimentAddress = regimentAddress.ConvertAddress(),
                    NewManagerAddress = OtherAccount.ConvertAddress(),
                    OriginSenderAddress = InitAccount.ConvertAddress()
                });
            setManager.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var getRegimentInfo = _regimentContract.CallViewMethod<RegimentInfo>(RegimentMethod.GetRegimentInfo,
                regimentAddress.ConvertAddress());
            getRegimentInfo.Manager.ShouldBe(OtherAccount.ConvertAddress());
        }

        [TestMethod]
        public void ResetConfig()
        {
            var resetConfig =
                _regimentContract.ExecuteMethodWithResult(RegimentMethod.ResetConfig,
                    new RegimentContractConfig {RegimentLimit = 7});
            resetConfig.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterConfig = _regimentContract.CallViewMethod<RegimentContractConfig>(RegimentMethod.GetConfig, new Empty());
            afterConfig.RegimentLimit.ShouldBe(7);
        }
        
        [TestMethod]
        public void ChangeController()
        {
            var resetConfig =
                _regimentContract.ExecuteMethodWithResult(RegimentMethod.ChangeController,AddAccount1.ConvertAddress());
            resetConfig.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var controller = _regimentContract.CallViewMethod<Address>(RegimentMethod.GetController, new Empty());
            controller.ShouldBe(AddAccount1.ConvertAddress());
        }


        [TestMethod]
        public void GetController()
        {
            var controller = _regimentContract.CallViewMethod<Address>(RegimentMethod.GetController, new Empty());
            Logger.Info(controller);
        }
    }
}