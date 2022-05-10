using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
{
    public enum InterestRateModelMethod
    {
        Initialize,
        UpdateJumpRateModel,
        GetBorrowRate,
        GetSupplyRate,
        GetUtilizationRate,
        GetMultiplierPerBlock,
        GetBaseRatePerBlock,
        GetJumpMultiplierPerBlock,
        GetKink,
        GetOwner
    }

    public class AwakenFinanceInterestRateModelContract : BaseContract<InterestRateModelMethod>
    {
        public AwakenFinanceInterestRateModelContract(INodeManager nodeManager,string callAddress) :
            base(nodeManager, "Awaken.Contracts.InterestRateModel", callAddress)
        {
        }

        public AwakenFinanceInterestRateModelContract(INodeManager nodeManager, string contractAddress, string callAddress) : base(
            nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }
        
        //View
        public long GetKink()
        {
            return CallViewMethod<Int64Value>(InterestRateModelMethod.GetKink,
                new Empty()).Value;
        }
        
        public long GetBaseRatePerBlock()
        {
            return CallViewMethod<Int64Value>(InterestRateModelMethod.GetBaseRatePerBlock,
                new Empty()).Value;
        }
        
        public long GetMultiplierPerBlock()
        {
            return CallViewMethod<Int64Value>(InterestRateModelMethod.GetMultiplierPerBlock,
                new Empty()).Value;
        }
        
        public long GetJumpMultiplierPerBlock()
        {
            return CallViewMethod<Int64Value>(InterestRateModelMethod.GetJumpMultiplierPerBlock,
                new Empty()).Value;
        }
    }
}