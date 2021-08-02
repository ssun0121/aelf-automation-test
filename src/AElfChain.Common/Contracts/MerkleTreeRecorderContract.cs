using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum MerkleTreeRecorderMethod
    {
        GetRecorder,
        GetMerkleTree,
        MerkleProof,
        GetOwner,
        GetRecorderCount,
        GetLeafLocatedMerkleTree,
        GetLastRecordedLeafIndex,
        GetSatisfiedTreeCount
    }

    public class MerkleTreeRecorderContract : BaseContract<MerkleTreeRecorderMethod>
    {
        public MerkleTreeRecorderContract(INodeManager nodeManager, string callAddress) : base(nodeManager,
            "AElf.Contracts.MerkleTreeRecorder", callAddress)
        {
        }

        public MerkleTreeRecorderContract(INodeManager nodeManager, string callAddress, string contractAddress,
            string password = "") : base(nodeManager, contractAddress)
        {
            SetAccount(callAddress, password);
        }
    }
}