using System;
using System.Collections.Generic;
using System.IO;
using AElf.Client.Dto;
using AElf.Contracts.Treasury;
using AElf.Standards.ACS10;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using Awaken.Contracts.PoolTwoContract;
using Awaken.Contracts.SwapExchangeContract;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using InitializeInput = Awaken.Contracts.SwapExchangeContract.InitializeInput;
using Path = Awaken.Contracts.SwapExchangeContract.Path;
using PendingOutput = Gandalf.Contracts.DividendPoolContract.PendingOutput;
using PoolInfoStruct = Gandalf.Contracts.DividendPoolContract.PoolInfoStruct;
using TokenList = Awaken.Contracts.SwapExchangeContract.TokenList;
using UserInfoStruct = Gandalf.Contracts.DividendPoolContract.UserInfoStruct;


namespace AElfChain.Common.Contracts
{
    public enum SwapExchangeMethod
    {
        Initialize,
        SetReceivor,
        Receivor,
        TargetToken,
        SetTargetToken,
        SetSwapToTargetTokenThreshold,
        Threshold,
        SwapCommonTokens,
        SwapLpTokens,
        GetHandlePath
    }

    public class SwapExchangeContract :BaseContract<SwapExchangeMethod>
    {

        public SwapExchangeContract(INodeManager nodeManager, string callAddress) : base(nodeManager,
            "Awaken.Contracts.SwapExchange", callAddress)
        {
        }

        public SwapExchangeContract(INodeManager nodeManager, string callAddress, string contractAddress) : base(
            nodeManager,
            contractAddress)
        {
            SetAccount(callAddress);
        }


        public TransactionResultDto Initialize(string targettoken, string swapcontract, string lptokencontract, string receiver)
        {
            return ExecuteMethodWithResult(SwapExchangeMethod.Initialize, new InitializeInput
            {
                TargetToken = targettoken,
                SwapContract = swapcontract.ConvertAddress(),
                LpTokenContract = lptokencontract.ConvertAddress(),
                Receivor = receiver.ConvertAddress()
            });
        }

        public TransactionResultDto SwapCommonTokens(Dictionary<string, Path> path,  TokenList tokenList)
        {
            return ExecuteMethodWithResult(SwapExchangeMethod.SwapCommonTokens, new SwapTokensInput
            {
                PathMap = {path},
                SwapTokenList = tokenList
            });
        }

        public TransactionResultDto SwapLpTokens(Dictionary<string, Path> path, TokenList tokenList)
        {
            return ExecuteMethodWithResult(SwapExchangeMethod.SwapLpTokens, new SwapTokensInput
            {
                PathMap = {path},
                SwapTokenList = tokenList
            });
        }

        public RepeatedField<string> GetHandlePath(string symbol, Path pathPair)
        {
            return CallViewMethod<HandlePathOutput>(SwapExchangeMethod.GetHandlePath, new HandlePathInput
            {
                Symbol = symbol,
                PathPair = pathPair
            }).Path;
        }
    }
}