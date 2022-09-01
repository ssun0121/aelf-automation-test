using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum BridgeMethod
    {
        Initialize,
        ChangeController,
        ChangeAdmin,
        ChangeTransactionFeeController,
        
        //To AElf
        CreateSwap,
        SwapToken,
        ChangeSwapRatio,
        Deposit,
        Withdraw,
        
        //To others
        AddToken,
        RemoveToken,
        CreateReceipt,
        
        //Gas Fee
        SetGasFee,
        SetGasPrice,
        SetPriceRatio,
        SetFeeFloatingRatio,
        GetGasFee,
        GetGasPrice,
        GetPriceRatio,
        GetFeeFloatingRatio,
        
        //View
        GetReceiptIdInfo,
        GetOwnerLockReceipt,
        GetLockTokens,
        GetReceiptInfo,
        GetSwapPairInfo,

        GetSwapInfo,
        GetSwapAmounts,
        GetRegimentIdBySpaceId,
        GetSwappedReceiptIdList,
        GetSwappedReceiptInfoList,
        GetSpaceIdBySwapId,
        GetContractController,
        GetContractAdmin,
        GetTransactionFeeRatioController
    }

    public class BridgeContract : BaseContract<BridgeMethod>
    {
        public BridgeContract(INodeManager nodeManager, string callAddress) : base(nodeManager,
            "AElf.Contracts.Bridge", callAddress)
        {
        }

        public BridgeContract(INodeManager nodeManager, string callAddress, string contractAddress,
            string password = "") : base(nodeManager, contractAddress)
        {
            SetAccount(callAddress, password);
        }
    }
}