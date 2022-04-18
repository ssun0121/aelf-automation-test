using AElf.Client.Dto;
using AElf.Contracts.Profit;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Sinodac.Contracts.Delegator;

namespace AElfChain.Common.Contracts
{
    public enum DelegatorMethod
    {
        Initialize,
        RegisterSenders,
        RegisterMethods,
        Forward,
        ForwardCheck,

        //Action
        CreateDAC,
        CreateSeries,
        AddProtocolToSeries,
        AuditDAC,
        ListDAC,
        MintDAC,
        DelistDAC,
        BindRedeemCode,
        Buy,
        Redeem,
        Box,
        Unbox,
        Give,

        //View
        IsPermittedAddress,
        IsPermittedMethod
    }

    public class DelegatorContract : BaseContract<DelegatorMethod>
    {
        public DelegatorContract(INodeManager nodeManager, string callAddress) :
            base(nodeManager, "Sinodac.Contracts.Delegator", callAddress)
        {
            Logger = Log4NetHelper.GetLogger();
        }

        public DelegatorContract(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
            Logger = Log4NetHelper.GetLogger();
        }

        public TransactionResultDto Initialize(string adminAddress, string dacContractAddress,
            string dacMarketContractAddress)
        {
            var result = ExecuteMethodWithResult(DelegatorMethod.Initialize, new InitializeInput
            {
                AdminAddress = adminAddress.ConvertAddress(),
                DacContractAddress = dacContractAddress.ConvertAddress(),
                DacMarketContractAddress = dacMarketContractAddress.ConvertAddress()
            });

            return result;
        }

        public TransactionResultDto CreateDAC(string fromId, string creatorId, string dacName, long price,
            long circulation, string dacType, string dacShape, long reserveForLottery, string seriesName)
        {
            var result = ExecuteMethodWithResult(DelegatorMethod.CreateDAC, new CreateDACInput
            {
                FromId = fromId,
                CreatorId = creatorId,
                DacName = dacName,
                Price = price,
                Circulation = circulation,
                DacType = dacType,
                DacShape = dacShape,
                ReserveForLottery = reserveForLottery,
                SeriesName = seriesName
            });

            return result;
        }

        public TransactionResultDto CreateSeries(string fromId, string seriesName, string seriesDescription,
            string creatorId)
        {
            var result = ExecuteMethodWithResult(DelegatorMethod.CreateSeries, new CreateSeriesInput
            {
                FromId = fromId,
                SeriesName = seriesName,
                SeriesDescription = seriesDescription,
                CreatorId = creatorId,
            });

            return result;
        }

        public TransactionResultDto AddProtocolToSeries(string fromId, string seriesName, string dacName)
        {
            var result = ExecuteMethodWithResult(DelegatorMethod.AddProtocolToSeries, new AddProtocolToSeriesInput
            {
                FromId = fromId,
                SeriesName = seriesName,
                DacName = dacName
            });

            return result;
        }

        public TransactionResultDto AuditDAC(string fromId, string dacName, bool isApprove)
        {
            var result = ExecuteMethodWithResult(DelegatorMethod.AuditDAC, new AuditDACInput
            {
                FromId = fromId,
                DacName = dacName,
                IsApprove = isApprove
            });

            return result;
        }

        public TransactionResultDto ListDAC(string fromId, string dacName, Timestamp publicTime)
        {
            var result = ExecuteMethodWithResult(DelegatorMethod.ListDAC, new ListDACInput
            {
                FromId = fromId,
                DacName = dacName,
                PublicTime = publicTime
            });

            return result;
        }

        public TransactionResultDto MintDAC(string fromId, string dacName, long fromDacId, long quantity)
        {
            var result = ExecuteMethodWithResult(DelegatorMethod.MintDAC, new MintDACInput
            {
                FromId = fromId,
                DacName = dacName,
                FromDacId = fromDacId,
                Quantity = quantity
            });

            return result;
        }

        public TransactionResultDto DelistDAC(string fromId, string dacName)
        {
            var result = ExecuteMethodWithResult(DelegatorMethod.DelistDAC, new DelistDACInput
            {
                FromId = fromId,
                DacName = dacName
            });

            return result;
        }

        public TransactionResultDto Buy(string fromId, string dacName, long dacId, long price)
        {
            var result = ExecuteMethodWithResult(DelegatorMethod.Buy, new BuyInput
            {
                FromId = fromId,
                DacName = dacName,
                DacId = dacId,
                Price = price
            });

            return result;
        }

        public TransactionResultDto Redeem(string fromId, string redeemCode)
        {
            var result = ExecuteMethodWithResult(DelegatorMethod.Redeem, new RedeemInput
            {
                FromId = fromId,
                RedeemCode = redeemCode
            });

            return result;
        }

        public TransactionResultDto Box(string fromId, string dacName)
        {
            var result = ExecuteMethodWithResult(DelegatorMethod.Box, new BoxInput
            {
                FromId = fromId,
                DacName = dacName
            });

            return result;
        }

        public TransactionResultDto Unbox(string fromId, string dacName, string boxId)
        {
            var result = ExecuteMethodWithResult(DelegatorMethod.Unbox, new UnboxInput
            {
                FromId = fromId,
                DacName = dacName,
                BoxId = boxId
            });

            return result;
        }

        public TransactionResultDto Give(string fromId, string toId, string dacName, long dacId)
        {
            var result = ExecuteMethodWithResult(DelegatorMethod.Give, new GiveInput
            {
                FromId = fromId,
                ToId = toId,
                DacName = dacName,
                DacId = dacId
            });

            return result;
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