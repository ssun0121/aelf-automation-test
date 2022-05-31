using AElf.Client.Dto;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using Awaken.Contracts.Controller;
using Google.Protobuf.WellKnownTypes;
using Org.BouncyCastle.Asn1.X509;

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
        GetPlatformTokenAccrued,
        GetMintGuardianPaused,
        GetPlatformTokenClaimAmount
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

        public long GetPlatformTokenSpeeds(Address address)
        {
            return CallViewMethod<Int64Value>(ControllerMethod.GetPlatformTokenSpeeds, address).Value;
        }

        public bool GetMintGuardianPaused(Address address)
        {
            return CallViewMethod<BoolValue>(ControllerMethod.GetMintGuardianPaused, address).Value;
        }

        public long GetPlatformTokenClaimAmount(Address holder, bool borrowers, bool suppliers)
        {
            return CallViewMethod<Int64Value>(ControllerMethod.GetPlatformTokenClaimAmount,
                new GetClaimPlatformTokenInput
                {
                    Holder = holder,
                    Borrowers = borrowers,
                    Suppliers = suppliers
                }).Value;
        }

        public bool CheckMembership(Address address)
        {
            return CallViewMethod<BoolValue>(ControllerMethod.CheckMembership, address).Value;
        }

        public long GetPlatformTokenAccrued(Address address)
        {
            return CallViewMethod<Int64Value>(ControllerMethod.GetPlatformTokenAccrued, address).Value;
        }
        
        public GetAccountLiquidityOutput GetAccountLiquidity(Address address)
        {
            return CallViewMethod<GetAccountLiquidityOutput>(ControllerMethod.GetAccountLiquidity, address);
        }

        public GetHypotheticalAccountLiquidityOutput GetHypotheticalAccountLiquidity(Address user, Address aToken, long borrowAmount, long redeemTokenAmount)
        {
            return CallViewMethod<GetHypotheticalAccountLiquidityOutput>(
                ControllerMethod.GetHypotheticalAccountLiquidity,
                new GetHypotheticalAccountLiquidityInput
                {
                    RedeemTokens = redeemTokenAmount,
                    BorrowAmount = borrowAmount,
                    Account = user,
                    ATokenModify = aToken
                });
        }

        public long GetPlatformTokenClaimAmount(string holder, bool isBorrowers, bool isSuppliers)
        {
            return CallViewMethod<Int64Value>(ControllerMethod.GetPlatformTokenClaimAmount, new GetClaimPlatformTokenInput
            {
                Holder = holder.ConvertAddress(),
                Borrowers = isBorrowers,
                Suppliers = isSuppliers
            }).Value;
        }
    }
}