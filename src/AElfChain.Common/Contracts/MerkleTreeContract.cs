using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts;

public enum MerkleTreeMethod
{
    Initialize,
    ChangeOwner,
    CreateSpace,
    RecordMerkleTree,
    
    //Regiment related
    CreateRegiment,
    JoinRegiment,
    LeaveRegiment,
    AddRegimentMember,
    DeleteRegimentMember,
    TransferRegimentOwnership,
    AddAdmins,
    DeleteAdmins,

    ConstructMerkleTree,
    GetMerklePath,
    MerkleProof,
    GetRegimentSpaceCount,
    GetRegimentSpaceIdList,
    GetSpaceInfo,
    GetMerkleTreeByIndex,
    GetMerkleTreeCountBySpace,
    GetLastMerkleTreeIndex,
    GetLastLeafIndex,
    GetFullTreeCount,
    GetLeafLocatedMerkleTree,
    GetRemainLeafCount
}

public class MerkleTreeContract : BaseContract<MerkleTreeMethod>
{
    public MerkleTreeContract(INodeManager nodeManager, string callAddress) : base(nodeManager,
        "AElf.Contracts.MerkleTree", callAddress)
    {
    }

    public MerkleTreeContract(INodeManager nodeManager, string callAddress, string contractAddress,
        string password = "") : base(nodeManager, contractAddress)
    {
        SetAccount(callAddress, password);
    }
}