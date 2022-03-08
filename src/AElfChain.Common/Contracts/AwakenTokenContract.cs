using AElf.Client.Dto;
using AElf.Client.Proto;
using AElf.Standards.ACS10;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using Awaken.Contracts.Token;
using Address = AElf.Types.Address;

namespace AElfChain.Common.Contracts
{
    public enum AwakenTokenMethod
    {
        Initialize,
        Create,
        Issue,
        Transfer,
        TransferFrom,
        Approve,
        UnApprove,
        Burn,
        ResetExternalInfo,

        //view
        GetBalance,
        GetTokenInfo,
        GetBalances,
        GetAllowance,
        GetOwner
    }

    public class AwakenTokenContract : BaseContract<AwakenTokenMethod>
    {
        public AwakenTokenContract(INodeManager nodeManager, string callAddress) : base(nodeManager,
            "Awaken.Contracts.Token", callAddress)
        {
        }

        public AwakenTokenContract(INodeManager nodeManager, string callAddress, string contractAddress) : base(
            nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }
        
        public TransactionResultDto ApproveLPToken(string spender, string owner, long amount, string symbol)
        {
            SetAccount(owner);
            var result = ExecuteMethodWithResult(AwakenTokenMethod.Approve, new ApproveInput
            {
                Symbol = symbol,
                Amount = amount,
                Spender = spender.ConvertAddress()
            });

            return result;
        }
        public Balance GetBalance(string symbol, Address owner)
        {
            return CallViewMethod<Balance>(AwakenTokenMethod.GetBalance, new GetBalanceInput
            {
                Symbol = symbol,
                Owner = owner
            });
        }
        
        public TokenInfo GetTokenInfo(string symbol)
        {
            return CallViewMethod<TokenInfo>(AwakenTokenMethod.GetTokenInfo, new GetTokenInfoInput
            {
                Symbol = symbol
            });
        }
    }
}