using AElfChain.Common.Managers;
using Awaken.Contracts.InterestRateModel;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
{
    public enum InterestRateModelMethod
    {
        Initialize,
        UpdateRateModel,
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

        public long GetUtilizationRate(long cash,long borrows,long reserves)
        {
            return CallViewMethod<Int64Value>(InterestRateModelMethod.GetUtilizationRate,
                new GetUtilizationRateInput
                {
                    Cash = cash,
                    Borrows = borrows,
                    Reserves = reserves
                }).Value;
        }
        
        public long GetBorrowRate(long cash,long borrows,long reserves)
        {
            return CallViewMethod<Int64Value>(InterestRateModelMethod.GetBorrowRate,
                new GetBorrowRateInput
                {
                    Cash = cash,
                    Borrows = borrows,
                    Reserves = reserves
                }).Value;
        }
        
        public long GetSupplyRate(long cash,long borrows,long reserves, long reserveFactor)
        {
            return CallViewMethod<Int64Value>(InterestRateModelMethod.GetSupplyRate,
                new GetSupplyRateInput
                {
                    Cash = cash,
                    Borrows = borrows,
                    Reserves = reserves,
                    ReserveFactor = reserveFactor
                }).Value;
        }
    }
}