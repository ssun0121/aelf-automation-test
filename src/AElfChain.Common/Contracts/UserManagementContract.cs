using AElf.Client.Dto;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElf.Contracts.UserManagement;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using Google.Protobuf.WellKnownTypes;
using Shouldly;

namespace AElfChain.Common.Contracts
{
    public enum UserManagementMethod
    {
        //Action
        Initialize,
        SetOwner,
        SetDelegatorContract,
        SetAdminDelegators,
        SetUserDelegators,
        Approve,
        Reject,
        Register,
        ChangeUserInfo,

        //View
        GetAdminDelegators,
        GetUserDelegators,
        GetApprovalList,
        GetUser
    }

    public class UserManagementContract : BaseContract<UserManagementMethod>
    {
        public UserManagementContract(INodeManager nodeManager, string callAddress) :
            base(nodeManager, "AElf.Contracts.UserManagement", callAddress)
        {
            Logger = Log4NetHelper.GetLogger();
        }

        public UserManagementContract(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
            Logger = Log4NetHelper.GetLogger();
        }

        public TransactionResultDto Initialize(string owner)
        {
            var result = ExecuteMethodWithResult(UserManagementMethod.Initialize, new InitializeInput
            {
                Owner = owner.ConvertAddress()
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            return result;
        }

        public TransactionResultDto SetOwner(string owner)
        {
            var result = ExecuteMethodWithResult(UserManagementMethod.SetOwner, new StringValue {Value = owner});
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            return result;
        }

        public TransactionResultDto SetDelegatorContract(string delegatorContract)
        {
            var result = ExecuteMethodWithResult(UserManagementMethod.SetDelegatorContract,
                delegatorContract.ConvertAddress());

            return result;
        }

        public TransactionResultDto SetAdminDelegators(AddressList adminAddressList)
        {
            var result = ExecuteMethodWithResult(UserManagementMethod.SetAdminDelegators, adminAddressList);

            return result;
        }

        public TransactionResultDto SetUserDelegators(AddressList userAddressList)
        {
            var result = ExecuteMethodWithResult(UserManagementMethod.SetUserDelegators, userAddressList);

            return result;
        }

        public TransactionResultDto Approve(Hash userApprovalId)
        {
            var result = ExecuteMethodWithResult(UserManagementMethod.Approve, userApprovalId);

            return result;
        }

        public TransactionResultDto Reject(Hash userApprovalId)
        {
            var result = ExecuteMethodWithResult(UserManagementMethod.Reject, userApprovalId);

            return result;
        }

        public TransactionResultDto Register(UserInfo user)
        {
            var result = ExecuteMethodWithResult(UserManagementMethod.Register, user);

            return result;
        }

        public TransactionResultDto ChangeUserInfo(UserInfo userInfo)
        {
            var result = ExecuteMethodWithResult(UserManagementMethod.ChangeUserInfo, userInfo);

            return result;
        }

        public AddressList GetAdminDelegators()
        {
            return CallViewMethod<AddressList>(UserManagementMethod.GetAdminDelegators, new Empty());
        }

        public AddressList GetUserDelegators()
        {
            return CallViewMethod<AddressList>(UserManagementMethod.GetUserDelegators, new Empty());
        }

        public UserApprovalList GetApprovalList()
        {
            return CallViewMethod<UserApprovalList>(UserManagementMethod.GetApprovalList, new Empty());
        }

        public UserInfo GetUser(string userName)
        {
            return CallViewMethod<UserInfo>(UserManagementMethod.GetUser, new StringValue {Value = userName});
        }
    }
}