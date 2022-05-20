using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using Awaken.Contracts.AToken;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
{
    public enum ATokenMethod
    {
        Initialize,
        Create,
        Mint,
        Redeem,
        RedeemUnderlying,
        Borrow,
        RepayBorrow,
        RepayBorrowBehalf,
        LiquidateBorrow,
        AddReserves,
        AccrueInterest,
        Seize,
        
        Transfer,
        TransferFrom,
        Approve,
        
        SetAdmin,
        SetComptroller,
        SetReserveFactor,
        SetInterestRateModel,
        
        GetUnderlyingBalance,
        GetAccountSnapshot,
        GetBorrowRatePerBlock,
        GetSupplyRatePerBlock,
        GetTotalBorrows,
        GetCurrentBorrowBalance,
        GetBorrowBalanceStored,
        GetCurrentExchangeRate,
        GetExchangeRateStored,
        GetCash,
        GetReserveFactor,
        
        GetAdmin,
        GetComptroller,
        GetInterestRateModel,
        GetInitialExchangeRate,
        GetATokenAddress,
        
        GetTotalReserves,
        GetAccrualBlockNumber,
        GetBorrowIndex,
        GetUnderlying,

        GetBalance,
        GetBalances,
        GetAllowance,
        GetTotalSupply,
        GetDecimals
    }
    public class AwakenFinanceATokenContract : BaseContract<ATokenMethod>
    {
        public AwakenFinanceATokenContract(INodeManager nodeManager, string callAddress) : base(nodeManager,
            "Awaken.Contracts.AToken", callAddress)
        {
        }

        public AwakenFinanceATokenContract(INodeManager nodeManager, string contractAddress, string callAddress) : base(
            nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }
        
        //View
        public Address GetAdmin()
        {
            return CallViewMethod<Address>(ATokenMethod.GetAdmin, new Empty());
        }

        public Address GetComptroller()
        {
            return CallViewMethod<Address>(ATokenMethod.GetComptroller, new Empty());
        }

        public Address GetATokenAddress(string symbol)
        {
            return CallViewMethod<Address>(ATokenMethod.GetATokenAddress, new StringValue{Value = symbol});
        }

        public long GetReserveFactor(Address address)
        {
            return CallViewMethod<Int64Value>(ATokenMethod.GetReserveFactor, address).Value;
        }

        public long GetTotalBorrows(Address address)
        {
            return CallViewMethod<Int64Value>(ATokenMethod.GetTotalBorrows, address).Value;
        }
        
        public long GetTotalReserves(Address address)
        {
            return CallViewMethod<Int64Value>(ATokenMethod.GetTotalReserves, address).Value;
        }
        
        public long GetTotalSupply(Address address)
        {
            return CallViewMethod<Int64Value>(ATokenMethod.GetTotalSupply, address).Value;
        }

        public long GetCash(Address address)
        {
            return CallViewMethod<Int64Value>(ATokenMethod.GetCash, address).Value;
        }
        
        public long GetExchangeRateStored(Address address)
        {
            return CallViewMethod<Int64Value>(ATokenMethod.GetExchangeRateStored, address).Value;
        }

        public long GetInitialExchangeRate(Address address)
        {
            return CallViewMethod<Int64Value>(ATokenMethod.GetInitialExchangeRate, address).Value;
        }

        public long GetBalance(string user, Address aToken)
        {
            return CallViewMethod<Int64Value>(ATokenMethod.GetBalance, new Awaken.Contracts.AToken.Account
            {
                User = user.ConvertAddress(),
                AToken = aToken
            }).Value;
        }

        public long GetCurrentBorrowBalance(string user, Address aToken)
        {
            return CallViewMethod<Int64Value>(ATokenMethod.GetCurrentBorrowBalance, new Awaken.Contracts.AToken.Account
            {
                User = user.ConvertAddress(),
                AToken = aToken
            }).Value;
        }

        public long GetAccrualBlockNumber(Address aToken)
        {
            return CallViewMethod<Int64Value>(ATokenMethod.GetAccrualBlockNumber, aToken).Value;
        }
        
        public long GetBorrowBalanceStored(string user, Address aToken)
        {
            return CallViewMethod<Int64Value>(ATokenMethod.GetBorrowBalanceStored, new Awaken.Contracts.AToken.Account
            {
                User = user.ConvertAddress(),
                AToken = aToken
            }).Value;
        }

        public GetAccountSnapshotOutput GetAccountSnapshot(string user, Address aToken)
        {
            return CallViewMethod<GetAccountSnapshotOutput>(ATokenMethod.GetAccountSnapshot,
                new Awaken.Contracts.AToken.Account
                {
                    User = user.ConvertAddress(),
                    AToken = aToken
                });
        }
    }
}