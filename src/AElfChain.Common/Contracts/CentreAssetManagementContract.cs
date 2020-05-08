using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum CentreAssertMethod
    {
        //Action
        Hello,
        CreateHolder,
        RequestUpdateHolder,
        ApproveUpdateHolder,
        RebootHolder,
        ShutdownHolder,
        MoveAssetToMainAddress,
        MoveAssetFromMainAddress,
        Initialize,
        RequestWithdraw,
        ApproveWithdraw,
        CancelWithdraws,
        SendTransactionByUserVirtualAddress,
        

        //View
        GetVirtualAddress,
        GetHolderInfo,
        GetCategoryContractCallAllowance
    }
    public class CentreAssetManagementContract : BaseContract<CentreAssertMethod>
    {
        public CentreAssetManagementContract(INodeManager nodeManager, string callAddress) :
            base(nodeManager, "AElf.Contracts.CentreAssetManagement", callAddress)
        {
        }

        public CentreAssetManagementContract(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }
    }
}