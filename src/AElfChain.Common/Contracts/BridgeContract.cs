using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum BridgeMethod
    {
        Initialize,
        CreateSwap,
        SwapToken,
        ChangeSwapRatio,
        Deposit,
        Withdraw,

        ChangeMaximalLeafCount,
        
        GetSwapInfo,
        GetSwapAmounts,
        GetSwapPair,
        GetSwappedReceiptIdList,
        GetRegimentAddressByRecorderId,

        GetReceiptCount,
        GetReceiptHash,
        GetReceiptHashList,
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