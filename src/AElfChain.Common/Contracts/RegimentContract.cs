using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum RegimentMethod
    {
        //Action
        Initialize,
        
        CreateRegiment,
        JoinRegiment,
        LeaveRegiment,
        AddRegimentMember,
        DeleteRegimentMember,
        TransferRegimentOwnership,
        AddAdmins,
        DeleteAdmins,
        
        ChangeController,
        ResetConfig,
        
        //View
        GetController,
        GetConfig,
        GetRegimentId,
        GetRegimentAddress,
        GetRegimentInfo,
        IsRegimentMember,
        GetRegimentMemberList
    }

    public class RegimentContract : BaseContract<RegimentMethod>
    {
        public RegimentContract(INodeManager nodeManager, string callAddress) : base(nodeManager,
            "AElf.Contracts.Regiment", callAddress)
        {
        }

        public RegimentContract(INodeManager nodeManager, string callAddress, string contractAddress,string password = "") : base(
            nodeManager, contractAddress)
        {
            SetAccount(callAddress,password);
        }
    }
}