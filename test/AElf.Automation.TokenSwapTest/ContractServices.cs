using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;

namespace AElf.Automation.TokenSwapTest
{
    public class ContractServices
    {
        public readonly INodeManager NodeManager;

        public ContractServices(string url, string callAddress, string password, string tokenSwapContract)
        {
            NodeManager = new NodeManager(url);
            CallAddress = callAddress.ConvertAddress();
            CallAccount = callAddress;
            
            TokenSwapContract = tokenSwapContract;
            NodeManager.UnlockAccount(CallAccount, password);
            GetContractServices();
        }

        public GenesisContract GenesisService { get; set; }
        public TokenContract TokenService { get; set; }
        public TokenSwapContract TokenSwapService { get; set;}
        
        public string CallAccount { get; set; }
        public Address CallAddress { get; set; }
        public string TokenSwapContract { get; set; }

        private void GetContractServices()
        {
            GenesisService = GenesisContract.GetGenesisContract(NodeManager, CallAccount);

            //Token contract
            TokenService = GenesisService.GetTokenContract();
            
            if(TokenSwapContract == null)
                TokenSwapService = new TokenSwapContract(NodeManager, CallAccount);
            else
                TokenSwapService = new TokenSwapContract(NodeManager, CallAccount,
                TokenSwapContract);
        }
    }
}