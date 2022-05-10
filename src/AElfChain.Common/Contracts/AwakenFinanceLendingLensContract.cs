using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts;

public enum LendingLensMethod
{
    Initialize,
    GetATokenMetadata,
    GetATokenMetadataInline,
    GetATokenMetadataAll,
    GetATokenBalances,
    GetATokenBalancesAll,
    GetATokenUnderlyingPrice,
    GetATokenUnderlyingPriceAll,
    GetAccountLimits,
    GetPlatformTokenBalanceMetadata,
    GetPlatformTokenBalanceMetadataExt,
    GetPlatformTokenBalanceInline
}

public class AwakenFinanceLendingLensContract : BaseContract<LendingLensMethod>
{
    public AwakenFinanceLendingLensContract(INodeManager nodeManager, string callAddress) : base(
        nodeManager, "Awaken.Contracts.AwakenLendingLens", callAddress)
    {
    }

    public AwakenFinanceLendingLensContract(INodeManager nodeManager, string contractAddress, string callAddress) :
        base(nodeManager,
            contractAddress)
    {
        SetAccount(callAddress);
    }
}