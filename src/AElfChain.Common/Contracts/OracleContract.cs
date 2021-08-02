using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum OracleMethod
    {
        //Action
        Initialize,
        InitializeAndCreateToken,
        Query,
        Commit,
        Reveal,

        CancelQuery,
        ChangeController,

        SetThreshold,
        EnableChargeFee,
        ChangeDefaultExpirationSeconds,
        
        AddPostPayAddress,
        RemovePostPayAddress,
        
        LockTokens,
        UnlockTokens,
        GetLockedTokensAmount,
        
        //Regiment
        CreateRegiment,
        JoinRegiment,
        LeaveRegiment,
        AddRegimentMember,
        DeleteRegimentMember,
        TransferRegimentOwnership,
        AddAdmins,
        DeleteAdmins,

        //Task
        CreateQueryTask,
        CompleteQueryTask,
        TaskQuery,
        GetQueryTask,
        
        //View
        GetController,
        GetQueryRecord,
        GetCommitmentMap,
        GetOracleTokenSymbol,
        GetThreshold,
        GetDefaultExpirationSeconds,
        GetRegimentMemberList
    }

    public class OracleContract : BaseContract<OracleMethod>
    {
        public OracleContract(INodeManager nodeManager, string callAddress) : base(nodeManager,
            "AElf.Contracts.Oracle", callAddress)
        {
        }

        public OracleContract(INodeManager nodeManager, string callAddress, string contractAddress,string password = "") : base(
            nodeManager, contractAddress)
        {
            SetAccount(callAddress,password);
        }
    }
}