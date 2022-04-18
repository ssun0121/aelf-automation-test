using AElf.Client.Dto;
using AElf.Client.Proto;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using Sinodac.Contracts.DAC;

namespace AElfChain.Common.Contracts
{
    public enum DACMethod
    {
        Initialize,

        //Action
        Create,
        Mint,
        MintForRedeemCode,
        InitialTransfer,
        Transfer,
        TransferFrom,
        ApproveProtocol,

        //View
        GetDACProtocolInfo,
        GetDACInfo,
        GetBalance,
        GetRedeemCodeDAC,
        IsOwner,
        CalculateDACHash,
        IsDACProtocolApproved,
        IsMinted,
        IsBindCompleted
    }

    public class DACContract : BaseContract<DACMethod>
    {
        public DACContract(INodeManager nodeManager, string callAddress) :
            base(nodeManager, "Sinodac.Contracts.DAC", callAddress)
        {
            Logger = Log4NetHelper.GetLogger();
        }

        public DACContract(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
            Logger = Log4NetHelper.GetLogger();
        }

        public TransactionResultDto Initialize(string adminAddress, string delegatorContractAddress,
            string dacMarketContractAddress)
        {
            var result = ExecuteMethodWithResult(DACMethod.Initialize, new InitializeInput
            {
                AdminAddress = adminAddress.ConvertAddress(),
                DelegatorContractAddress = delegatorContractAddress.ConvertAddress(),
                DacMarketContractAddress = dacMarketContractAddress.ConvertAddress()
            });

            return result;
        }

        public TransactionResultDto Create(string creatorUserId, string creatorId, string dacName, long price,
            long circulation, string dacType, string dacShape, long reserveForLottery, long reserveFrom)
        {
            var result = ExecuteMethodWithResult(DACMethod.Create, new CreateInput
            {
                CreatorUserId = creatorUserId,
                CreatorId = creatorId,
                DacName = dacName,
                Price = price,
                Circulation = circulation,
                DacType = dacType,
                DacShape = dacShape,
                ReserveForLottery = reserveForLottery,
                ReserveFrom = reserveFrom
            });

            return result;
        }

        public TransactionResultDto Mint(string dacName, long fromDacId, long quantity)
        {
            var result = ExecuteMethodWithResult(DACMethod.Mint, new MintInput
            {
                DacName = dacName,
                FromDacId = fromDacId,
                Quantity = quantity
            });

            return result;
        }

        public TransactionResultDto MintForRedeemCode(string dacName, long skip)
        {
            var result = ExecuteMethodWithResult(DACMethod.MintForRedeemCode, new MintForRedeemCodeInput
            {
                DacName = dacName,
                RedeemCodeHashList = { },
                Skip = skip
            });

            return result;
        }

        public TransactionResultDto InitialTransfer(string toAddress, string dacName, long dacId)
        {
            var result = ExecuteMethodWithResult(DACMethod.InitialTransfer, new InitialTransferInput
            {
                To = toAddress.ConvertAddress(),
                DacName = dacName,
                DacId = dacId
            });

            return result;
        }

        public TransactionResultDto Transfer(string toAddress, string dacName, long dacId, string memo)
        {
            var result = ExecuteMethodWithResult(DACMethod.Transfer, new TransferInput
            {
                To = toAddress.ConvertAddress(),
                DacName = dacName,
                DacId = dacId,
                Memo = memo
            });

            return result;
        }

        public TransactionResultDto TransferFrom(string fromAddress, string toAddress, string dacName, long dacId,
            string memo)
        {
            var result = ExecuteMethodWithResult(DACMethod.TransferFrom, new TransferFromInput
            {
                From = fromAddress.ConvertAddress(),
                To = toAddress.ConvertAddress(),
                DacName = dacName,
                DacId = dacId,
                Memo = memo
            });

            return result;
        }

        public TransactionResultDto ApproveProtocol(string dacName, bool isApprove)
        {
            var result = ExecuteMethodWithResult(DACMethod.ApproveProtocol, new ApproveProtocolInput
            {
                DacName = dacName,
                IsApprove = isApprove
            });

            return result;
        }

        public DACProtocolInfo GetDACProtocolInfo(string dacName)
        {
            return CallViewMethod<DACProtocolInfo>(DACMethod.GetDACProtocolInfo, new StringValue {Value = dacName});
        }

        public DACInfo GetDACInfo(string dacName, long dacId)
        {
            return CallViewMethod<DACInfo>(DACMethod.GetDACInfo, new GetDACInfoInput
            {
                DacName = dacName,
                DacId = dacId,
            });
        }

        public DACBalance GetBalance(string owner, string dacName)
        {
            return CallViewMethod<DACBalance>(DACMethod.GetBalance, new GetBalanceInput
            {
                Owner = owner.ConvertAddress(),
                DacName = dacName
            });
        }

        public DACInfo GetRedeemCodeDAC(Hash hash)
        {
            return CallViewMethod<DACInfo>(DACMethod.GetRedeemCodeDAC, hash);
        }

        public BoolValue IsOwner(string owner, string dacName, long dacId)
        {
            return CallViewMethod<BoolValue>(DACMethod.IsOwner, new IsOwnerInput
            {
                Owner = owner.ConvertAddress(),
                DacName = dacName,
                DacId = dacId
            });
        }

        public Hash CalculateDACHash(string dacName, long dacId)
        {
            return CallViewMethod<Hash>(DACMethod.CalculateDACHash, new CalculateDACHashInput
            {
                DacName = dacName,
                DacId = dacId
            });
        }

        public BoolValue IsDACProtocolApproved(string dacName)
        {
            return CallViewMethod<BoolValue>(DACMethod.IsDACProtocolApproved, new StringValue
            {
                Value = dacName
            });
        }

        public BoolValue IsMinted(string dacName, long dacId)
        {
            return CallViewMethod<BoolValue>(DACMethod.IsMinted, new IsMintedInput
            {
                DacName = dacName,
                DacId = dacId
            });
        }

        public BoolValue IsBindCompleted(string dacName)
        {
            return CallViewMethod<BoolValue>(DACMethod.IsBindCompleted, new StringValue
            {
                Value = dacName
            });
        }
    }
}