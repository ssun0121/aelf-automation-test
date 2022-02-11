using System;
using System.Collections.Generic;
using AElf;
using AElf.Client.Dto;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using Awaken.Contracts.Swap;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
{
    public enum SwapMethod
    {
        Initialize,
        CreatePair,
        AddLiquidity,
        RemoveLiquidity,
        SwapExactTokensForTokens,
        SwapTokensForExactTokens,
        SwapExactTokensForTokensSupportingFeeOnTransferTokens,
        SwapExactTokensForTokensSupportingFeeOnTransferTokensVerify,
        TransferLiquidityTokens,
        SetFeeRate,
        SetFeeTo,
        
        ChangeOwner,
        SetVault,
        Take,

        //view
        GetPairs,
        GetReserves,
        GetTotalSupply,
        GetAccountAssets,
        GetAmountIn,
        GetAmountOut,
        GetAmountsIn,
        GetAmountsOut,
        Quote,
        GetKLast,
        GetFeeTo,
        GetFeeRate,
        GetAdmin,
        GetPairAddress,
        GetVault
    }

    public class AwakenSwapContract : BaseContract<SwapMethod>
    {
        public AwakenSwapContract(INodeManager nodeManager, string callAddress) : base(nodeManager,
            "Awaken.Contracts.Swap", callAddress)
        {
        }

        public AwakenSwapContract(INodeManager nodeManager, string callAddress, string contractAddress) : base(
            nodeManager,
            contractAddress)
        {
            SetAccount(callAddress);
        }

        public TransactionResultDto CreatePair(string symbolPair, out Address pairAddress)
        {
            var result = ExecuteMethodWithResult(SwapMethod.CreatePair, new CreatePairInput
            {
                SymbolPair = symbolPair
            });
            pairAddress = Address.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            return result;
        }

        public TransactionResultDto AddLiquidity(out AddLiquidityOutput output, string symbolA, string symbolB,
            long amountADesired,
            long amountBDesired, Address to, long aMin = 1, long bMin = 1, string channel = "", Timestamp timestamp = null)
        {
            if (timestamp == null)
                timestamp = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(1));
            var result = ExecuteMethodWithResult(SwapMethod.AddLiquidity, new AddLiquidityInput
            {
                SymbolA = symbolA,
                SymbolB = symbolB,
                AmountADesired = amountADesired,
                AmountBDesired = amountBDesired,
                AmountAMin = aMin,
                AmountBMin = bMin,
                To = to,
                Channel = channel,
                Deadline = timestamp
            });
            output = AddLiquidityOutput.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            return result;
        }

        public TransactionResultDto RemoveLiquidity(out RemoveLiquidityOutput output, string symbolA, string symbolB,
            long liquidityRemove, Address account, long aMin = 1, long bMin = 1, string channel = "", Timestamp timestamp = null)
        {
            if (timestamp == null)
                timestamp = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(1));
            var result = ExecuteMethodWithResult(SwapMethod.RemoveLiquidity, new RemoveLiquidityInput
            {
                SymbolA = symbolA,
                SymbolB = symbolB,
                AmountAMin = aMin,
                AmountBMin = bMin,
                LiquidityRemove = liquidityRemove,
                Deadline = timestamp,
                To = account
            });
            output = RemoveLiquidityOutput.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            return result;
        }

        public TransactionResultDto SwapTokensForExactTokens(out SwapOutput output, Address toAddress, List<string> path,
            long amountOut, long amountInMax = 1000000000000000000, string channel = "", Timestamp timestamp = null)
        {
            if (timestamp == null)
                timestamp = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(1));
            var result = ExecuteMethodWithResult(SwapMethod.SwapTokensForExactTokens, new SwapTokensForExactTokensInput
            {
                Path = { path },
                AmountOut = amountOut,
                AmountInMax = amountInMax,
                Channel = channel,
                Deadline = timestamp,
                To = toAddress
            });
            output = SwapOutput.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            return result;
        }
        
        public TransactionResultDto SwapExactTokensForTokens(out SwapOutput output, Address toAddress, List<string> path,
            long amountIn, long amountOutMin = 1, string channel = "", Timestamp timestamp = null)
        {
            if (timestamp == null)
                timestamp = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(1));
            var result = ExecuteMethodWithResult(SwapMethod.SwapExactTokensForTokens, new SwapExactTokensForTokensInput
            {
                Path = { path },
                AmountIn = amountIn,
                AmountOutMin = amountOutMin,
                To = toAddress,
                Channel = channel,
                Deadline = timestamp
            });
            output = SwapOutput.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            return result;
        }
        
        public TransactionResultDto SwapSupportingFeeOnTransferTokens(Address toAddress, List<string> path,
            long amountIn, long amountOutMin = 1, string channel = "", Timestamp timestamp = null)
        {
            if (timestamp == null)
                timestamp = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(1));
            var result = ExecuteMethodWithResult(SwapMethod.SwapExactTokensForTokensSupportingFeeOnTransferTokens, 
                new SwapExactTokensForTokensSupportingFeeOnTransferTokensInput
            {
                Path = { path },
                AmountIn = amountIn,
                AmountOutMin = amountOutMin,
                To = toAddress,
                Channel = channel,
                Deadline = timestamp
            });
            return result;
        }

        public TransactionResultDto TransferLiquidityTokens(string pair, string to, long amount)
        {
            var result = ExecuteMethodWithResult(SwapMethod.TransferLiquidityTokens, new TransferLiquidityTokensInput
            {
                SymbolPair = pair,
                To = to.ConvertAddress(),
                Amount = amount
            });
            return result;
        }

        public TransactionResultDto Take(string token, long amount)
        {
            var result = ExecuteMethodWithResult(SwapMethod.Take, new TakeInput
            {
                Token = token,
                Amount = amount
            });
            return result;
        }
        
        public TransactionResultDto SetVault(Address vault)
        {
            var result = ExecuteMethodWithResult(SwapMethod.SetVault, vault);
            return result;
        }

        //view
        public StringList GetPairs()
        {
            return CallViewMethod<StringList>(SwapMethod.GetPairs, new Empty());
        }

        public GetReservesOutput GetReserves(string pair)
        {
            return CallViewMethod<GetReservesOutput>(SwapMethod.GetReserves, new GetReservesInput
            {
                SymbolPair = {pair}
            });
        }

        public GetTotalSupplyOutput GetTotalSupply(string pair)
        {
            return CallViewMethod<GetTotalSupplyOutput>(SwapMethod.GetTotalSupply, new StringList
            {
                Value = { pair }
            });
        }

        public long Quote(string symbolA, string symbolB, long amountA)
        {
            var amount = CallViewMethod<Int64Value>(SwapMethod.Quote, new QuoteInput
            {
                AmountA = amountA,
                SymbolA = symbolA,
                SymbolB = symbolB
            });
            return amount.Value;
        }
        
        public long GetAmountIn(string symbolIn, string symbolOut, long amountOut)
        {
            var result = CallViewMethod<Int64Value>(SwapMethod.GetAmountIn, new GetAmountInInput
            {
                AmountOut = amountOut,
                SymbolIn = symbolIn,
                SymbolOut = symbolOut
            });
            return result.Value;
        }

        public long GetAmountOut(string symbolIn, string symbolOut, long amountIn)
        {
            var result = CallViewMethod<Int64Value>(SwapMethod.GetAmountOut, new GetAmountOutInput
            {
                SymbolIn = symbolIn,
                SymbolOut = symbolOut,
                AmountIn = amountIn
            });
            return result.Value;
        }
        
        public GetAmountsInOutput GetAmountsIn(List<string> path, long amountOut)
        {
            var result = CallViewMethod<GetAmountsInOutput>(SwapMethod.GetAmountsIn, new GetAmountsInInput
            {
                AmountOut = amountOut,
                Path = { path }
            });
            return result;
        }

        public GetAmountsOutOutput GetAmountsOut(List<string> path, long amountIn)
        {
            var result = CallViewMethod<GetAmountsOutOutput>(SwapMethod.GetAmountsOut, new GetAmountsOutInput
            {
                AmountIn = amountIn,
                Path = { path }
            });
            return result;
        }

        public BigIntValue GetKLast(Address pair)
        {
            return CallViewMethod<BigIntValue>(SwapMethod.GetKLast, pair);
        }

        public StringList GetAccountAssets()
        {
            return CallViewMethod<StringList>(SwapMethod.GetAccountAssets, new Empty());
        }

        public Address GetPairAddress(string symbolA, string symbolB)
        {
            return CallViewMethod<Address>(SwapMethod.GetPairAddress, new GetPairAddressInput
            {
                SymbolA = symbolA,
                SymbolB = symbolB
            });
        }

        public Address GetAdmin()
        {
            return CallViewMethod<Address>(SwapMethod.GetAdmin, new Empty());
        }

        public Address GetFeeTo()
        {
            return CallViewMethod<Address>(SwapMethod.GetFeeTo, new Empty());
        }
        
        public long GetFeeRate()
        {
            return CallViewMethod<Int64Value>(SwapMethod.GetFeeRate, new Empty()).Value;
        }
    }
}