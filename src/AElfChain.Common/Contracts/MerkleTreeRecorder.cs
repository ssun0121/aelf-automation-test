using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum MerkleTreeRecorderMethod
    {
        //Action
        Initialize,
        CreateRecorder,
        RecordMerkleTree,
        ChangeOwner,
        
        //View
        MerkleProof,
        GetLeafLocatedMerkleTree,
        GetMerkleTree,
        GetOwner,
        GetRecorder,
        GetRecorderCount,
        GetLastRecordedLeafIndex,
        GetSatisfiedTreeCount

    }

    public class MerkleTreeRecorder : BaseContract<MerkleTreeRecorderMethod>
    {
        public MerkleTreeRecorder(INodeManager nodeManager, string callAddress) :
            base(nodeManager, "AElf.Contracts.LotteryContract", callAddress)
        {
        }

        public MerkleTreeRecorder(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }
    }
}