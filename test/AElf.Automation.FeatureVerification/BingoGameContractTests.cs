using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.BingoContract;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class BingoGameContractTests
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }

        private TokenContract _tokenContract;
        private GenesisContract _genesisContract;
        private BingoGameContract _bingoContract;
        private BingoContractContainer.BingoContractStub _bingoGameContractStub;
        
        private string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        private List<string> Tester = new List<string>();
        private static string RpcUrl { get; } = "192.168.197.21:8001";
        
        // private static string RpcUrl { get; } = "192.168.199.205:8000";

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("ContractTest_");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env2-side1");
            Tester = NodeOption.AllNodes.Select(l => l.Account).ToList();
            NodeManager = new NodeManager(RpcUrl);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
//            _bingoContract = new BingoGameContract(NodeManager, InitAccount);
//            Logger.Info($"Bingo contract : {_bingoContract}");
            _bingoContract = new BingoGameContract(NodeManager, InitAccount,
                "2wRDbyVF28VBQoSPgdSEFaL4x7CaXz8TCBujYhgWc9qTMxBE3n");
            _bingoGameContractStub =
                _bingoContract.GetTestStub<BingoContractContainer.BingoContractStub>(InitAccount);
//            if (!_tokenContract.GetTokenInfo(Symbol).Symbol.Equals(Symbol))
//                CreateTokenAndIssue();
        }

        [TestMethod]
        public async Task Play()
        {
            var transferToBingo =
                _tokenContract.TransferBalance(InitAccount, _bingoContract.ContractAddress, 10000_000,"AEUSD");
            _tokenContract.ApproveToken(InitAccount, _bingoContract.ContractAddress, 1000,"AEUSD");
            var play = await _bingoGameContractStub.Play.SendAsync(new PlayInput
            {
                BuyAmount = 1000,
                BuyType = 1,
                TokenSymbol = "AEUSD"
            });
            var bingo = await _bingoGameContractStub.Bingo.SendAsync(Hash.Empty);
        }
    }
}