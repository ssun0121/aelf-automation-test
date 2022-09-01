using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;

namespace AElf.Automation.SwapTokenTest
{
    public class ContractServices
    {
        public readonly INodeManager NodeManager;

        public ContractServices(string url, string callAddress, string password, string bridge, string mtr, string mtg, string regiment)
        {
            NodeManager = new NodeManager(url);
            CallAddress = callAddress.ConvertAddress();
            CallAccount = callAddress;
            Bridge = bridge;
            MerkleTreeRecorder = mtr;
            MerkleTreeGenerator = mtg;
            Regiment = regiment;

            NodeManager.UnlockAccount(CallAccount, password);
            GetContractServices();
        }

        public GenesisContract GenesisService { get; set; }
        public TokenContract TokenService { get; set; }
        public BridgeContract BridgeService { get; set; }
        public RegimentContract RegimentService { get; set; }

        public string CallAccount { get; set; }
        public Address CallAddress { get; set; }
        public string Bridge { get; set; }
        public string MerkleTreeRecorder { get; set; }
        public string MerkleTreeGenerator { get; set; }
        public string Regiment { get; set; }


        private void GetContractServices()
        {
            GenesisService = GenesisContract.GetGenesisContract(NodeManager, CallAccount);

            //Token contract
            TokenService = GenesisService.GetTokenContract();
            BridgeService = new BridgeContract(NodeManager, CallAccount,
                Bridge);
            RegimentService = new RegimentContract(NodeManager, CallAccount,
                Regiment);
        }
    }
}
