using System;
using AElf.Contracts.UserManagement;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using AddressList = AElf.Contracts.UserManagement.AddressList;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class DelegatorForwardContractTest
    {
        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;
        private DelegatorContract _delegatorContract;
        private UserManagementContract _userManagementContract;
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }

        private string InitAccount { get; } = "nn659b9X1BLhnu5RWmEUbuuV7J9QKVVSN54j9UmeCbF3Dve5D";
        private string OwnerAccount { get; } = "nn659b9X1BLhnu5RWmEUbuuV7J9QKVVSN54j9UmeCbF3Dve5D";
        private string AdminAccount { get; } = "YUW9zH5GhRboT5JK4vXp5BLAfCDv28rRmTQwo418FuaJmkSg8";
        private string UserAccount { get; } = "FHdcx45K5kovWsAKSb3rrdyNPFus8eoJ1XTQE7aXFHTgfpgzN";

        private string delegatorAddress = "SsSqZWLf7Dk9NWyWyvDwuuY5nzn5n99jiscKZgRPaajZP5p8y";
        private string userManagementAddress = "GwsSp1MZPmkMvXdbfSCDydHhZtDpvqkFpmPvStYho288fb7QZ";

        private static string RpcUrl { get; } = "192.168.67.166:8000";

        private UserInfo user = new UserInfo
        {
            UserName = "UserName",
            Name = "Name",
            Email = "Email"
        };

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("UserManagementContractTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env2-main");

            NodeManager = new NodeManager(RpcUrl);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);

            _delegatorContract = delegatorAddress == ""
                ? new DelegatorContract(NodeManager, InitAccount)
                : new DelegatorContract(NodeManager, InitAccount, delegatorAddress);

            _userManagementContract = userManagementAddress == ""
                ? new UserManagementContract(NodeManager, InitAccount)
                : new UserManagementContract(NodeManager, InitAccount, userManagementAddress);

            if (_tokenContract.GetUserBalance(AdminAccount) < 100_00000000)
                _tokenContract.TransferBalance(InitAccount, AdminAccount, 10000_00000000);
            if (_tokenContract.GetUserBalance(UserAccount) < 100_00000000)
                _tokenContract.TransferBalance(InitAccount, UserAccount, 10000_00000000);
        }

        [TestMethod]
        public void InitializeTest()
        {
            var result = _userManagementContract.Initialize(OwnerAccount);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void SetDelegatorContractTest()
        {
            var result = _userManagementContract.SetDelegatorContract(_delegatorContract.ContractAddress);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void SetAdminDelegatorsTest()
        {
            _userManagementContract.SetAccount(OwnerAccount);
            var result = _userManagementContract.SetAdminDelegators(new AddressList
            {
                Value = {AdminAccount.ConvertAddress()}
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var adminDelegators = _userManagementContract.GetAdminDelegators();
            adminDelegators.Value[0].ShouldBe(AdminAccount.ConvertAddress());

            var isPermittedAddress =
                _delegatorContract.IsPermittedAddress(_userManagementContract.ContractAddress, "Admin", AdminAccount);
            isPermittedAddress.Value.ShouldBeTrue();

            var isPermittedMethodApprove =
                _delegatorContract.IsPermittedMethod(_userManagementContract.ContractAddress, "Admin", "Approve");
            isPermittedMethodApprove.Value.ShouldBeTrue();
            var isPermittedAddressReject =
                _delegatorContract.IsPermittedMethod(_userManagementContract.ContractAddress, "Admin", "Reject");
            isPermittedAddressReject.Value.ShouldBeTrue();
        }

        [TestMethod]
        public void SetUserDelegatorsTest()
        {
            var result = _userManagementContract.SetUserDelegators(new AddressList
            {
                Value = {UserAccount.ConvertAddress()}
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var userDelegators = _userManagementContract.GetUserDelegators();
            userDelegators.Value[0].ShouldBe(UserAccount.ConvertAddress());

            var isPermittedAddress =
                _delegatorContract.IsPermittedAddress(_userManagementContract.ContractAddress, "User", UserAccount);
            isPermittedAddress.Value.ShouldBeTrue();

            var isPermittedMethodRegister =
                _delegatorContract.IsPermittedMethod(_userManagementContract.ContractAddress, "User", "Register");
            isPermittedMethodRegister.Value.ShouldBeTrue();
            var isPermittedAddressChangeUserInfo =
                _delegatorContract.IsPermittedMethod(_userManagementContract.ContractAddress, "User", "ChangeUserInfo");
            isPermittedAddressChangeUserInfo.Value.ShouldBeTrue();
        }

        [TestMethod]
        public void RegisterTest()
        {
            _delegatorContract.SetAccount(UserAccount);

            var result = _delegatorContract.Forward(Guid.NewGuid().ToString(), _userManagementContract.ContractAddress,
                "Register", user.ToByteString(), "User");
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var approvalList = _userManagementContract.GetApprovalList().Value;
            for (var i = 0; i < approvalList.Count; i++)
            {
                Logger.Info($"\napprovalList.Count: {i + 1}\n" +
                            $"approvalList[{i}].Id: {approvalList[i].Id}\n" +
                            $"approvalList[{i}].UserName: {approvalList[i].UserName}\n" +
                            $"approvalList[{i}].Name: {approvalList[i].Name}\n" +
                            $"approvalList[{i}].Email: {approvalList[i].Email}");
            }

            var maxIndex = approvalList.Count - 1;
            approvalList[maxIndex].UserName.ShouldBe(user.UserName);
            approvalList[maxIndex].Name.ShouldBe(user.Name);
            approvalList[maxIndex].Email.ShouldBe(user.Email);
        }

        [TestMethod]
        public void ChangeUserInfoTest()
        {
            user.Name = "new Name";
            user.Email = "new Email";

            _delegatorContract.SetAccount(UserAccount);

            var userInfo = _userManagementContract.GetUser(user.UserName);
            userInfo.UserName.ShouldBe(user.UserName);

            if (userInfo.UserName != "")
            {
                Logger.Info("user is not empty");
                var result = _delegatorContract.Forward(Guid.NewGuid().ToString(),
                    _userManagementContract.ContractAddress,
                    "ChangeUserInfo", user.ToByteString(), "User");
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                userInfo = _userManagementContract.GetUser(user.UserName);
                userInfo.UserName.ShouldBe(user.UserName);
                userInfo.Name.ShouldBe(user.Name);
                userInfo.Email.ShouldBe(user.Email);

                Logger.Info($"\nuserInfo.UserName: {user.UserName}\n" +
                            $"userInfo.Name: {user.Name}\n" +
                            $"userInfo.Email: {user.Email}");
            }
        }

        [TestMethod]
        public void ApproveTest()
        {
            _delegatorContract.SetAccount(AdminAccount);

            var approvalList = _userManagementContract.GetApprovalList().Value;
            Logger.Info($"approvalList : {approvalList.Count}");

            foreach (var approval in approvalList)
            {
                var result = _delegatorContract.Forward(Guid.NewGuid().ToString(),
                    _userManagementContract.ContractAddress,
                    "Approve", approval.Id.ToByteString(), "Admin");
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }

            approvalList = _userManagementContract.GetApprovalList().Value;
            approvalList.Count.ShouldBe(0);

            var userInfo = _userManagementContract.GetUser(user.UserName);
            userInfo.UserName.ShouldBe(user.UserName);
            userInfo.Name.ShouldBe(user.Name);
            userInfo.Email.ShouldBe(user.Email);

            Logger.Info($"\nuserInfo.UserName: {user.UserName}\n" +
                        $"userInfo.Name: {user.Name}\n" +
                        $"userInfo.Email: {user.Email}");
        }

        [TestMethod]
        public void RejectTest()
        {
            _delegatorContract.SetAccount(AdminAccount);

            var approvalList = _userManagementContract.GetApprovalList().Value;
            Logger.Info($"approvalList : {approvalList.Count}");

            foreach (var approval in approvalList)
            {
                var result = _delegatorContract.Forward(Guid.NewGuid().ToString(),
                    _userManagementContract.ContractAddress,
                    "Reject", approval.Id.ToByteString(), "Admin");
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }

            approvalList = _userManagementContract.GetApprovalList().Value;
            approvalList.Count.ShouldBe(0);
        }

        [TestMethod]
        public void RegisterErrorTest()
        {
            _delegatorContract.SetAccount(AdminAccount);

            {
                var result = _delegatorContract.Forward(Guid.NewGuid().ToString(),
                    _userManagementContract.ContractAddress,
                    "Register", user.ToByteString(), "User");
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("[Sender] No permission.");
            }
            {
                var result = _delegatorContract.Forward(Guid.NewGuid().ToString(),
                    _userManagementContract.ContractAddress,
                    "Register", user.ToByteString(), "Admin");
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("[MethodName] No permission.");
            }

            _userManagementContract.SetAccount(UserAccount);
            {
                var result = _userManagementContract.Register(user);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Forward check failed.");
            }
        }

        [TestMethod]
        public void ChangeUserInfoErrorTest()
        {
            user.Name = "new Name";
            user.Email = "new Email";

            _delegatorContract.SetAccount(AdminAccount);

            var userInfo = _userManagementContract.GetUser(user.UserName);
            userInfo.UserName.ShouldBe(user.UserName);

            if (userInfo.UserName != "")
            {
                Logger.Info("user is not empty");
                {
                    var result = _delegatorContract.Forward(Guid.NewGuid().ToString(),
                        _userManagementContract.ContractAddress,
                        "ChangeUserInfo", user.ToByteString(), "User");
                    result.Status.ConvertTransactionResultStatus()
                        .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                    result.Error.ShouldContain("[Sender] No permission.");
                }
                {
                    var result = _delegatorContract.Forward(Guid.NewGuid().ToString(),
                        _userManagementContract.ContractAddress,
                        "ChangeUserInfo", user.ToByteString(), "Admin");
                    result.Status.ConvertTransactionResultStatus()
                        .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                    result.Error.ShouldContain("[MethodName] No permission.");
                }

                _userManagementContract.SetAccount(UserAccount);
                {
                    var result = _userManagementContract.ChangeUserInfo(user);
                    result.Status.ConvertTransactionResultStatus()
                        .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                    result.Error.ShouldContain("Forward check failed.");
                }
            }
        }

        [TestMethod]
        public void ApproveErrorTest()
        {
            _delegatorContract.SetAccount(UserAccount);

            var approvalList = _userManagementContract.GetApprovalList().Value;
            Logger.Info($"approvalList : {approvalList.Count}");

            {
                var result = _delegatorContract.Forward(Guid.NewGuid().ToString(),
                    _userManagementContract.ContractAddress,
                    "Approve", user.ToByteString(), "User");
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("[MethodName] No permission.");
            }
            {
                var result = _delegatorContract.Forward(Guid.NewGuid().ToString(),
                    _userManagementContract.ContractAddress,
                    "Approve", user.ToByteString(), "Admin");
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("[Sender] No permission.");
            }

            _userManagementContract.SetAccount(AdminAccount);
            {
                var result = _userManagementContract.Approve(approvalList[0].Id);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Forward check failed.");
            }
        }

        [TestMethod]
        public void RejectErrorTest()
        {
            _delegatorContract.SetAccount(UserAccount);

            var approvalList = _userManagementContract.GetApprovalList().Value;
            Logger.Info($"approvalList : {approvalList.Count}");

            {
                var result = _delegatorContract.Forward(Guid.NewGuid().ToString(),
                    _userManagementContract.ContractAddress,
                    "Reject", user.ToByteString(), "User");
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("[MethodName] No permission.");
            }
            {
                var result = _delegatorContract.Forward(Guid.NewGuid().ToString(),
                    _userManagementContract.ContractAddress,
                    "Reject", user.ToByteString(), "Admin");
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("[Sender] No permission.");
            }

            _userManagementContract.SetAccount(AdminAccount);
            {
                var result = _userManagementContract.Reject(approvalList[0].Id);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("Forward check failed.");
            }
        }
    }
}