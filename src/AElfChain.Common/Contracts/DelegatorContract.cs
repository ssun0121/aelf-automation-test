using AElf.Client.Dto;
using AElf.Contracts.Delegator;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElfChain.Common.DtoExtension;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
{
    public enum DelegatorMethod
    {
        //Action
        Forward,

        //View
        IsPermittedAddress,
        IsPermittedMethod
    }

    public class DelegatorContract : BaseContract<DelegatorMethod>
    {
        public DelegatorContract(INodeManager nodeManager, string callAddress) :
            base(nodeManager, "AElf.Contracts.Delegator", callAddress)
        {
            Logger = Log4NetHelper.GetLogger();
        }

        public DelegatorContract(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
            Logger = Log4NetHelper.GetLogger();
        }

        public TransactionResultDto Forward(string fromId, string toAddress, string methodName, ByteString parameter,
            string scopeId)
        {
            var result = ExecuteMethodWithResult(DelegatorMethod.Forward, new ForwardInput
            {
                FromId = fromId,
                ToAddress = toAddress.ConvertAddress(),
                MethodName = methodName,
                Parameter = parameter,
                ScopeId = scopeId
            });

            return result;
        }

        public BoolValue IsPermittedAddress(string sender, string scopeId, string contractAddress)
        {
            return CallViewMethod<BoolValue>(DelegatorMethod.IsPermittedAddress, new IsPermittedAddressInput
            {
                ToAddress = sender.ConvertAddress(),
                ScopeId = scopeId,
                Address = contractAddress.ConvertAddress()
            });
        }

        public BoolValue IsPermittedMethod(string sender, string scopeId, string methodName)
        {
            return CallViewMethod<BoolValue>(DelegatorMethod.IsPermittedMethod, new IsPermittedMethodInput
            {
                ToAddress = sender.ConvertAddress(),
                ScopeId = scopeId,
                MethodName = methodName
            });
        }
    }
}