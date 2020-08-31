using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum FinanceMethod
    {
        //admin Action
        Initialize,
        SupportMarket,
        AcceptAdmin, //pending admin
        SetPendingAdmin,
        SetUnderlyingPrice,
        SetMaxAssets,
        SetInterestRate,
        SetReserveFactor,
        SetLiquidationIncentive,
        SetCollateralFactor,
        
        SetPauseGuardian,
        SetMintPaused,
        SetSeizePaused,
        SetBorrowPaused,
        
        AddReserves,
        ReduceReserves,
        LiquidateCalculateSeizeTokens,
        
        //UserAction
        Mint,
        EnterMarket,
        AccrueInterest,
        Borrow,
        ExitMarket,
        RepayBorrow,
        RepayBorrowBehalf,
        Redeem,
        RedeemUnderlying,
        LiquidateBorrow,


        //View
        GetCloseFactor,
        GetMaxAssets,
        GetLiquidationIncentive,
        GetCurrentExchangeRate,
        GetAccountSnapshot,
        GetBalance,
        GetCash,
        GetAssetsIn,
        CheckMembership,
        GetUnderlyingPrice,
        GetCollateralFactor,
        GetSupplyRatePerBlock,
        GetBorrowRatePerBlock,
        GetCurrentBorrowBalance,
        GetBorrowBalanceStored,
        GetTotalBorrows,
        GetTotalReserves,
        GetInterestRate,
        GetReserveFactor,
        GetPauseGuardian
    }

    public class FinanceContract : BaseContract<FinanceMethod>
    {
        public FinanceContract(INodeManager nodeManager, string callAddress) : base(nodeManager,
            "AElf.Contracts.FinanceContract.dll", callAddress)
        {
        }

        public FinanceContract(INodeManager nodeManager, string callAddress, string contractAddress,string password) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress,password);
        }
    }
}