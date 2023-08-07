using System;
using AElf.Client.Dto;
using AElf.Contracts.MultiToken;
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
using StringList = Awaken.Contracts.Swap.StringList;

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
        private string InitAccount { get; } = "JRe8wjZGXaPp2jyyi13x7JvVHQnqKSi8RrAqj5pGjDaBbYmZJ";
        private string TestAccount { get; } = "";
        private string Owner { get; } = "JRe8wjZGXaPp2jyyi13x7JvVHQnqKSi8RrAqj5pGjDaBbYmZJ";
        
        private string mode = "side";


        // private string _swapTokenContractAddress = "2L8uLZRJDUNdmeoA7RT6QbB7TZvu2xHra2gTz2bGrv9Wxs7KPS";
        // private string _swapContractAddress = "2YnkipJ9mty5r6tpTWQAwnomeeKUT7qCWLHKaSeV1fejYEyCdX";
        // private string _swapTokenContractAddress = "pVHzzPLV8U3XEAb3utFPnuFL7p6AZtxemgX1yX4tCvKQDQNud";
        // private string _swapContractAddress = "fGa81UPViGsVvTM13zuAAwk1QHovL3oSqTrCznitS4hAawPpk";
        // private string _swapTokenContractAddress = "5KN5uqSC1vz521Lpfh9H1ZLWpU96x6ypEdHrTZF8WdjMmQFQ5";
        // private string _swapContractAddress = "LzkrbEK2zweeuE4P8Y23BMiFY2oiKMWyHuy5hBBbF1pAPD2hh";
        // private string _swapTokenContractAddress = "2iFrdeaSKHwpNGWviSMVacjHjdgtZbfrkNeoV1opRzsfBrPVsm";
        // private string _swapContractAddress = "EG73zzQqC8JencoFEgCtrEUvMBS2zT22xoRse72XkyhuuhyTC";
        private string _swapTokenContractAddress = "T25QvHLdWsyHaAeLKu9hvk33MTZrkWD1M7D4cZyU58JfPwhTh";
        private string _swapContractAddress = "23dh2s1mXnswi4yNW7eWNKWy7iac8KrXJYitECgUctgfwjeZwP";
        
        private string Password { get; } = "12345678";

        private static string RpcUrl { get; set; } = "";

        private static string MainRpcUrl { get; } = "http://192.168.67.18:8000";
        // private static string MainRpcUrl { get; } = "https://aelf-test-node.aelf.io";

        // private static string SideRpcUrl { get; } = "http://192.168.66.106:8000";
        private static string SideRpcUrl { get; } = "https://tdvw-test-node.aelf.io";
        
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
                    Owner = _swapContractAddress.ConvertAddress()
                });
            initializeSwapToken.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info(_swapTokenContract.CallViewMethod<Address>(SwapTokenMethod.GetOwner, new Empty()));
            
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
            
            
            Logger.Info("Initialize Swap fee rate: ");
            _swapContract.SetAccount(InitAccount);
            var swap = _swapContract.ExecuteMethodWithResult(SwapMethod.SetFeeRate,
                new Int64Value
                {
                    Value = 500
                });
            swap.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info(_swapContract.CallViewMethod<Int64Value>(SwapMethod.GetFeeRate, new Empty()));
            
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
                    Value = 300
                });
            swap.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info(_swapContract.CallViewMethod<Int64Value>(SwapMethod.GetFeeRate, new Empty()));
        }
        
        [TestMethod]
        public void CreatPair()
        {
            Logger.Info("Create Pair: ");
            _swapContract.SetAccount(InitAccount);
            var pair = _swapContract.ExecuteMethodWithResult(SwapMethod.CreatePair,
                new CreatePairInput
                {
                    SymbolPair = "ELF-NET"
                });
            pair.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info(_swapContract.CallViewMethod<StringList>(SwapMethod.GetPairs, new Empty()));
        }
        
        [TestMethod]
        public void Swap()
        {
            _swapContract.SetAccount(InitAccount);
            _tokenContract.ApproveToken(InitAccount, _swapContractAddress, 100000000000);
            var pair = _swapContract.ExecuteMethodWithResult(SwapMethod.SwapExactTokensForTokens,
                new SwapExactTokensForTokensInput
                {
                    AmountIn = 100000000,
                    AmountOutMin = 0,
                    Path = { "ELF","USDT" },
                    To = InitAccount.ConvertAddress(),
                    Deadline = Timestamp.FromDateTime(DateTime.UtcNow.Add(new TimeSpan(0, 0, 3)))
                });
            pair.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info(pair.TransactionId);
        }
        
        [TestMethod]
        public void TransferTo()
        {
            var balance = _tokenContract.GetUserBalance("2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV", "ELF");
            Logger.Info(balance);
            // _tokenContract.TransferBalance(InitAccount, "2aaHMZ5dQX41usNXF6Xo4EUZoCRr9fHPUhgWwRbzcxEdTwc87R", 500_00000000, "ELF");
            // var allowance = _tokenContract.GetAllowance("zNhjv1DiNcRkDAaEUvryCgG9sNQstTk2eyCexGHc9eZ5so7bz",
                // "JQkVTWz5HXxEmNXzTtsAVHC7EUTeiFktzoFUu9TyA6MWngkem", "ELF");
            // Logger.Info(allowance);
            var address = _genesisContract.GetContractAddressByName(NameProvider.CrossChain);
            Logger.Info(address);
        }
    }
}