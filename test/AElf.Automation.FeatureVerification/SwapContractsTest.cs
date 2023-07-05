using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Awaken.Contracts.Swap;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class SwapContractsTests
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }

        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;
        private ParliamentContract _parliament;

        private SwapTokenContract _swapTokenContract;
        private SwapContract _swapContract;

        private string BpAccount { get; } = "";
        private string InitAccount { get; } = "2aaHMZ5dQX41usNXF6Xo4EUZoCRr9fHPUhgWwRbzcxEdTwc87R";
        private string TestAccount { get; } = "";
        private string Owner { get; } = "2aaHMZ5dQX41usNXF6Xo4EUZoCRr9fHPUhgWwRbzcxEdTwc87R";
        
        private string mode = "side";


        private string _swapTokenContractAddress = "fU9csLqXtnSbcyRJs3fPYLFTz2S9EZowUqkYe4zrJgp1avXK2";
        private string _swapContractAddress = "AZBBDe2asKTPNPN6n3b4wn6P6nMMDQS5yXQ2yhyjGodr7Qqwe";
        

        private string Password { get; } = "12345678";

        private static string RpcUrl { get; set; } = "";

        // private static string MainRpcUrl { get; } = "http://192.168.67.18:8000";
        // private static string MainRpcUrl { get; } = "https://aelf-test-node.aelf.io";

        private static string SideRpcUrl { get; } = "http://192.168.66.106:8000";
        // private static string SideRpcUrl { get; } = "https://tdvw-test-node.aelf.io";
        
        private readonly bool isNeedInitialize = false;

        private readonly bool isNeedCreate = false;
        
        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("Awaken SwapTest");
            Logger = Log4NetHelper.GetLogger();

            NodeInfoHelper.SetConfig("nodes");

            if (mode == "side")
            {
                RpcUrl = SideRpcUrl;
                NodeManager = new NodeManager(RpcUrl);
                AuthorityManager = new AuthorityManager(NodeManager, InitAccount, Password);
                _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount, Password);
                _tokenContract = _genesisContract.GetTokenContract(InitAccount, Password);
                _parliament = _genesisContract.GetParliamentContract(InitAccount, Password);
            }

            _swapTokenContract = _swapTokenContractAddress == ""
                ? new SwapTokenContract(NodeManager, InitAccount)
                : new SwapTokenContract(NodeManager, InitAccount, _swapTokenContractAddress);
            _swapContract = _swapContractAddress == ""
                ? new SwapContract(NodeManager, InitAccount)
                : new SwapContract(NodeManager, InitAccount, _swapContractAddress);
            Logger.Info(
                $"\nSwap Token: {_swapTokenContract.ContractAddress}" +
                $"\nSwap: {_swapContract.ContractAddress}");
            
            if (!isNeedCreate) return;
        }
        

        [TestMethod]
        public void InitializeSwapTokenContract()
        {
            Logger.Info("Initialize Swap token: ");
            _swapTokenContract.SetAccount(InitAccount);
            var initializeSwapToken = _swapTokenContract.ExecuteMethodWithResult(SwapTokenMethod.Initialize,
                new Awaken.Contracts.Token.InitializeInput
                {
                    Owner = Owner.ConvertAddress()
                });
            initializeSwapToken.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info(_swapTokenContract.CallViewMethod<Address>(SwapTokenMethod.GetOwner, new Empty()));
        }
        [TestMethod]
        public void InitializeSwapContract()
        {
            Logger.Info("Initialize Swap: ");
            _swapContract.SetAccount(InitAccount);
            var initializeSwap = _swapContract.ExecuteMethodWithResult(SwapMethod.Initialize,
                new InitializeInput
                {
                    AwakenTokenContractAddress = _swapTokenContractAddress.ConvertAddress(),
                    Admin = Owner.ConvertAddress()
                });
            initializeSwap.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info(_swapContract.CallViewMethod<Address>(SwapMethod.GetAdmin, new Empty()));
        }
        
        [TestMethod]
        public void SetFeeRate()
        {
            Logger.Info("Initialize Swap fee rate: ");
            _swapContract.SetAccount(InitAccount);
            var swap = _swapContract.ExecuteMethodWithResult(SwapMethod.SetFeeRate,
                new Int64Value
                {
                    Value = 30
                });
            swap.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info(_swapContract.CallViewMethod<Address>(SwapMethod.GetFeeRate, new Empty()));
        }
        
        [TestMethod]
        public void CreatPair()
        {
            Logger.Info("Create Pair: ");
            _swapContract.SetAccount(InitAccount);
            var pair = _swapContract.ExecuteMethodWithResult(SwapMethod.CreatePair,
                new CreatePairInput
                {
                    SymbolPair = "ELF-USDT"
                });
            pair.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info(_swapContract.CallViewMethod<StringList>(SwapMethod.GetPairs, new Empty()));
        }
    }
}