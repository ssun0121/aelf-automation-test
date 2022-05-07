using System.Net.NetworkInformation;
using AElf.Client.Dto;
using AElf.Contracts.Profit;
using AElf.Contracts.TokenConverter;
using AElfChain.Common.Managers;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using Awaken.Contracts.Investment;
using Awaken.Contracts.Provider;
using Awaken.Contracts.Shadowfax;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
{
    public enum AwakenProviderContractMethod
    {
        //Action
        AddSupportToken,
        Initialize,
        

        //View
        
    }

    public class AwakenProviderContract : BaseContract<AwakenProviderContractMethod>
    {
        public AwakenProviderContract(INodeManager nodeManager, string callAddress) : base(nodeManager,
            ContractFileName, callAddress)
        {
        }
        public AwakenProviderContract(INodeManager nodeManager, string callAddress, string contractAddress) : base(
            nodeManager,
            contractAddress)
        {
            SetAccount(callAddress);
        }
        public static string ContractFileName => "Awaken.Contracts.Provider";

        public TransactionResultDto Initialize()
        {
            var result = ExecuteMethodWithResult(AwakenProviderContractMethod.Initialize,new Empty());
            
            return result;
        }

        
        //set
        public TransactionResultDto AddSupportToken(string tokenSymbol, Address lend, Address lendingLens, string profitTokenSymbol)
        {
            var result = ExecuteMethodWithResult(AwakenProviderContractMethod.AddSupportToken, new AddSupportTokenInput
            {
               TokenSymbol = tokenSymbol,
               Lend= lend,
               LendingLens = lendingLens,
               ProfitTokenSymbol = profitTokenSymbol
                   
            });
            return result;
        }
        
        
        
        
        
        
        
        
    }

    
} 
