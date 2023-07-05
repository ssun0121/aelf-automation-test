using System;
using AElf.Client.Dto;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
{
    public enum SwapMethod
    {
        //Action
        Initialize,
        CreatePair,
        AddLiquidity,
        RemoveLiquidity,
        SwapExactTokensForTokens,
        SwapTokensForExactTokens,
        SetFeeRate,
        SetFeeTo,
        SwapExactTokensForTokensSupportingFeeOnTransferTokens,
        ChangeOwner,
        SwapExactTokensForTokensSupportingFeeOnTransferTokensVerify,
        
        
        //View
        GetPairs,
        GetReserves,
        GetTotalSupply,
        GetAmountIn,
        GetAmountOut,
        Quote,
        GetKLast,
        GetAdmin,
        GetPairAddress,
        GetFeeTo,
        GetFeeRate,
        GetAmountsOut,
        GetAmountsIn
    }

    public class SwapContract : BaseContract<SwapMethod>
    {
        public SwapContract(INodeManager nodeManager, string callAddress) :
            base(nodeManager, "Awaken.Contracts.Swap", callAddress)
        {
            Logger = Log4NetHelper.GetLogger();
        }

        public SwapContract(INodeManager nodeManager, string callAddress, string contractAddress,string password ="") :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress,password);
            Logger = Log4NetHelper.GetLogger();
        }
    }
}