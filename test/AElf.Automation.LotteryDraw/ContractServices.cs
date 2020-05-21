using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;

namespace AElf.Automation.LotteryTest
{
    public class ContractServices
    {
        public readonly INodeManager NodeManager;

        public ContractServices(string url, string callAddress, string password, string lotteryContract)
        {
            NodeManager = new NodeManager(url);
            CallAddress = callAddress.ConvertAddress();
            CallAccount = callAddress;
            
            LotteryContract = lotteryContract;
            NodeManager.UnlockAccount(CallAccount, password);
            GetContractServices();
        }

        public GenesisContract GenesisService { get; set; }
        public TokenContract TokenService { get; set; }
        public LotteryContract LotteryService { get; set;}
        
        public string CallAccount { get; set; }
        public Address CallAddress { get; set; }
        public string LotteryContract { get; set; }

        private void GetContractServices()
        {
            GenesisService = GenesisContract.GetGenesisContract(NodeManager, CallAccount);

            //Token contract
            TokenService = GenesisService.GetTokenContract();
            
            if(LotteryContract == "")
                LotteryService = new LotteryContract(NodeManager, CallAccount);
            else
                LotteryService = new LotteryContract(NodeManager, CallAccount, LotteryContract);
        }
    }
}