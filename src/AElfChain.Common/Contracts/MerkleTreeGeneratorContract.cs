using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum MerkleTreeGeneratorMethod
    {
        GetReceiptMaker,
        GetMerkleTree,
        GetFullTreeCount,
        GetMerklePath,
        GetLastRecordedLeafIndex,
        GetLeafLocatedMerkleTree
    }

    public class MerkleTreeGeneratorContract : BaseContract<MerkleTreeGeneratorMethod>
    {
        public MerkleTreeGeneratorContract(INodeManager nodeManager, string callAddress) : base(nodeManager,
            "AElf.Contracts.MerkleTreeGeneratorContract", callAddress)
        {
        }

        public MerkleTreeGeneratorContract(INodeManager nodeManager, string callAddress, string contractAddress,
            string password = "") : base(nodeManager, contractAddress)
        {
            SetAccount(callAddress, password);
        }
    }
}