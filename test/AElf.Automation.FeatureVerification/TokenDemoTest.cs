using System.Threading.Tasks;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class TokenDemoTest
    { 
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private ContractManager ContractManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }
        private TokenContractContainer.TokenContractStub TokenStub;
        private BasicContractZeroContainer.BasicContractZeroStub GenesisStub;
        private string InitAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        private string TestAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";

        private static string RpcUrl { get; } = "192.168.197.51:8000";

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("TokenDemoTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env-single");
            NodeManager = new NodeManager(RpcUrl);
            AuthorityManager = new AuthorityManager(NodeManager,InitAccount);
            ContractManager = new ContractManager(NodeManager, InitAccount);
            //token contract
            TokenStub = ContractManager.TokenStub;
            //genesis contract
            GenesisStub = ContractManager.GenesisStub;
        }
        
        //Demo 
        [TestMethod]
        public async Task TransferTest()
        {
            var symbol = "ELF";
            var amount = 1000_00000000;
            var result = await TokenStub.Transfer.SendAsync(new TransferInput
            {
                To = TestAccount.ConvertAddress(),
                Symbol = symbol,
                Amount = amount,
                Memo = "Demo"
            });
            // transaction result 
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }
}