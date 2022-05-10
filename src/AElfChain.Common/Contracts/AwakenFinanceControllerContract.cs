using AElf.Types;
using AElfChain.Common.Managers;
using Awaken.Contracts.Controller;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
{
    public enum ControllerMethod
    {
        Initialize,
        EnterMarkets,
        ExitMarket,
        MintAllowed,
        MintVerify,
        RedeemAllowed,
        RedeemVerify,
        BorrowAllowed,
        BorrowVerify,
        RepayBorrowAllowed,
        RepayBorrowVerify,
        LiquidateBorrowAllowed,
        LiquidateBorrowVerify,
        SeizeAllowed,
        SeizeVerify,
        TransferAllowed,
        TransferVerify,
        LiquidateCalculateSeizeTokens,
        
        SetCloseFactor,
        SetCollateralFactor,
        SetMaxAssets,
        SetLiquidationIncentive,

        SupportMarket,
        SetMarketBorrowCaps,
        SetBorrowCapGuardian,
        SetPauseGuardian,
        SetMintPaused,
        SetBorrowPaused,
        SetTransferPaused,
        SetSeizePaused,
        SetPriceOracle,
        RefreshPlatformTokenSpeeds,
        ClaimPlatformToken,
        SetPlatformTokenRate,
        AddPlatformTokenMarkets,
        DropPlatformTokenMarket,
        
        GetAssetsIn,
        CheckMembership,
        GetAllMarkets,
        GetAdmin,
        GetPendingAdmin,
        GetCloseFactor,
        GetCollateralFactor,
        GetMaxAssets,
        GetLiquidationIncentive,
        GetMarketBorrowCaps,
        GetBorrowCapGuardian,
        GetHypotheticalAccountLiquidity,
        GetPriceOracle,
        GetMarket,
        GetAccountLiquidity,
        GetPlatformTokenRate,
        GetPlatformTokenSpeeds,
        GetPlatformTokenSupplyState,
        GetPlatformTokenBorrowState,
        GetPlatformTokenSupplierIndex,
        GetPlatformTokenBorrowerIndex,
        GetPlatformTokenAccrued
    }

    public class AwakenFinanceControllerContract : BaseContract<ControllerMethod>
    {
        public AwakenFinanceControllerContract(INodeManager nodeManager, string callAddress) : base(
            nodeManager, "Awaken.Contracts.Controller", callAddress)
        {
        }

        public AwakenFinanceControllerContract(INodeManager nodeManager, string contractAddress, string callAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }

        public Address GetAdmin()
        {
            return CallViewMethod<Address>(ControllerMethod.GetAdmin, new Empty());
        }

        public ATokens GetAllMarkets()
        {
            return CallViewMethod<ATokens>(ControllerMethod.GetAllMarkets, new Empty());
        }

        public Market GetMarket(Address address)
        {
            return CallViewMethod<Market>(ControllerMethod.GetMarket, address);
        }

        public long GetCollateralFactor(Address address)
        {
            return CallViewMethod<Int64Value>(ControllerMethod.GetCollateralFactor, address).Value;
        }

        public long GetCloseFactor()
        {
            return CallViewMethod<Int64Value>(ControllerMethod.GetCloseFactor, new Empty()).Value;
        }
        
        public long GetLiquidationIncentive()
        {
            return CallViewMethod<Int64Value>(ControllerMethod.GetLiquidationIncentive, new Empty()).Value;
        }
        
        public long GetMaxAssets()
        {
            return CallViewMethod<Int64Value>(ControllerMethod.GetMaxAssets, new Empty()).Value;
        }

        public long GetPlatformTokenRate()
        {
            return CallViewMethod<Int64Value>(ControllerMethod.GetPlatformTokenRate, new Empty()).Value;
        }

        public AssetList GetAssetsIn(Address address)
        {
            return CallViewMethod<AssetList>(ControllerMethod.GetAssetsIn, address);
        }
    }
}