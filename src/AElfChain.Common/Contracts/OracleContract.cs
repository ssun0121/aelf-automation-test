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

        //View
        GetController,
        GetQueryRecord,
        GetCommitmentMap,
        GetOracleTokenSymbol,
        GetThreshold
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