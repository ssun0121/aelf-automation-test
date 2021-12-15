using System;
using System.Collections.Generic;
using AElf;
using AElf.Client.Dto;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using Gandalf.Contracts.Swap;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
{
    public enum SwapMethod
    {
        Initialize,
        CreatePair,
        AddLiquidity,
        RemoveLiquidity,
        SwapExactTokenForToken,
        SwapTokenForExactToken,
        TransferLiquidityTokens,
        SetFeeRate,
        SetFeeTo,

        //view
        GetPairs,
        GetReserves,
        GetTotalSupply,
        GetLiquidityTokenBalance,
        GetAccountAssets,
        GetAmountIn,
        GetAmountOut,
        Quote,
        GetKLast,
        GetFeeTo,
        GetFeeRate,
        GetAdmin,
        GetPairAddress
    }

    public class GandalfSwapContract : BaseContract<SwapMethod>
    {
        public GandalfSwapContract(INodeManager nodeManager, string callAddress) : base(nodeManager,
            "Gandalf.Contracts.Swap", callAddress)
        {
        }

        public GandalfSwapContract(INodeManager nodeManager, string callAddress, string contractAddress) : base(
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
            pairAddress = CreatePairOutput.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue))
                .PairAddress;
            return result;
        }

        public TransactionResultDto AddLiquidity(out AddLiquidityOutput output, string symbolA, string symbolB,
            long amountADesired,
            long amountBDesired, long aMin = 1, long bMin = 1, string channel = "", Timestamp timestamp = null)
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
                Channel = channel,
                Deadline = timestamp
            });
            output = AddLiquidityOutput.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            return result;
        }

        public TransactionResultDto RemoveLiquidity(out RemoveLiquidityOutput output, string symbolA, string symbolB,
            long liquidityRemove, long aMin = 1, long bMin = 1, string channel = "", Timestamp timestamp = null)
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
                Deadline = timestamp
            });
            output = RemoveLiquidityOutput.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            return result;
        }

        public TransactionResultDto SwapTokenForExactToken(out SwapOutput output, string symbolIn, string symbolOut,
            long amountOut, long amountInMax = 1000000000000000000, string channel = "", Timestamp timestamp = null)
        {
            if (timestamp == null)
                timestamp = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(1));
            var result = ExecuteMethodWithResult(SwapMethod.SwapTokenForExactToken, new SwapTokenForExactTokenInput
            {
                SymbolIn = symbolIn,
                SymbolOut = symbolOut,
                AmountOut = amountOut,
                AmountInMax = amountInMax,
                Channel = channel,
                Deadline = timestamp
            });
            output = SwapOutput.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            return result;
        }
        
        public TransactionResultDto SwapExactTokenForToken(out SwapOutput output, string symbolIn, string symbolOut,
            long amountIn, long amountOutMin = 1, string channel = "", Timestamp timestamp = null)
        {
            if (timestamp == null)
                timestamp = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(1));
            var result = ExecuteMethodWithResult(SwapMethod.SwapExactTokenForToken, new SwapExactTokenForTokenInput
            {
                SymbolIn = symbolIn,
                SymbolOut = symbolOut,
                AmountIn = amountIn,
                AmountOutMin = amountOutMin,
                Channel = channel,
                Deadline = timestamp
            });
            output = SwapOutput.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
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


        //view
        public PairList GetPairs()
        {
            return CallViewMethod<PairList>(SwapMethod.GetPairs, new Empty());
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
            return CallViewMethod<GetTotalSupplyOutput>(SwapMethod.GetTotalSupply, new PairList
            {
                SymbolPair = {pair}
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

        public GetLiquidityTokenBalanceOutput GetLiquidityTokenBalance(List<string> pairs, string account)
        {
            SetAccount(account);
            var result = CallViewMethod<GetLiquidityTokenBalanceOutput>(SwapMethod.GetLiquidityTokenBalance,
                new PairList
                {
                    SymbolPair = {pairs}
                });
            return result;
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

        public BigIntValue GetKLast(Address pair)
        {
            return CallViewMethod<BigIntValue>(SwapMethod.GetKLast, pair);
        }

        public PairList GetAccountAssets()
        {
            return CallViewMethod<PairList>(SwapMethod.GetAccountAssets, new Empty());
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