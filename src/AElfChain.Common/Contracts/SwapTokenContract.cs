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
    public enum SwapTokenMethod
    {
        //Action
        Initialize,
        Create,
        Issue,
        Transfer,
        TransferFrom,
        Approve,
        UnApprove,
        Burn,
        
        //View
        GetTokenInfo,
        GetBalance,
        GetAllowance,
        GetOwner
    }

    public class SwapTokenContract : BaseContract<SwapTokenMethod>
    {
        public SwapTokenContract(INodeManager nodeManager, string callAddress) :
            base(nodeManager, "Awaken.Contracts.Token", callAddress)
        {
            Logger = Log4NetHelper.GetLogger();
        }

        public SwapTokenContract(INodeManager nodeManager, string callAddress, string contractAddress,string password ="") :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress,password);
            Logger = Log4NetHelper.GetLogger();
        }

        public TransactionResultDto TransferBalance(string from, string to, long amount, string symbol = "",
            string password = "")
        {
            var tester = GetNewTester(from, password);
            var result = tester.ExecuteMethodWithResult(SwapTokenMethod.Transfer, new TransferInput
            {
                Symbol = NodeOption.GetTokenSymbol(symbol),
                To = to.ConvertAddress(),
                Amount = amount,
                Memo = $"T-{Guid.NewGuid().ToString()}"
            });

            return result;
        }

        public TransactionResultDto IssueBalance(string from, string to, long amount, string symbol = "")
        {
            var tester = GetNewTester(from);
            tester.SetAccount(from);
            var result = tester.ExecuteMethodWithResult(SwapTokenMethod.Issue, new IssueInput
            {
                Symbol = NodeOption.GetTokenSymbol(symbol),
                To = to.ConvertAddress(),
                Amount = amount,
                Memo = $"I-{Guid.NewGuid()}"
            });

            return result;
        }

        public TransactionResultDto ApproveToken(string from, string to, long amount, string symbol = "",
            string password = "")
        {
            SetAccount(from, password);
            var result = ExecuteMethodWithResult(SwapTokenMethod.Approve, new ApproveInput
            {
                Symbol = NodeOption.GetTokenSymbol(symbol),
                Amount = amount,
                Spender = to.ConvertAddress()
            });

            return result;
        }


        public TokenInfo GetTokenInfo(string symbol)
        {
            return CallViewMethod<TokenInfo>(SwapTokenMethod.GetTokenInfo, new GetTokenInfoInput
            {
                Symbol = symbol
            });
        }

    }
}