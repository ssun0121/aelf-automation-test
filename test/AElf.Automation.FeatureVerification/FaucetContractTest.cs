using AElf.Contracts.Faucet;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.Contracts.FaucetTest
{
    [TestClass]
    public class FaucetContractTest
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }
        private FaucetContract _faucetContract;


        private string InitAccount { get; } = "J6zgLjGwd1bxTBpULLXrGVeV74tnS2n74FFJJz7KNdjTYkDF6";

        private static string RpcUrl { get; } = "192.168.66.9:8000";
        private string Symbol { get; } = "ELF";
        private string faucet = "2WHXRoLRjbUTDQsuqR5CntygVfnDb125qdJkudev4kVNbLhTdG";
        private FaucetContractContainer.FaucetContractStub faucetStub;

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("FaucetContractTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env2-main");

            NodeManager = new NodeManager(RpcUrl);
            if (faucet.Equals(""))
                _faucetContract = new FaucetContract(NodeManager, InitAccount);
            else
                _faucetContract = new FaucetContract(NodeManager, InitAccount, faucet);

            // faucetStub = _faucetContract.GetTestStub<FaucetContractContainer.FaucetContractStub>(InitAccount);
        }

        [TestMethod]
        public void Initialize_Default()
        {
            var result =
                _faucetContract.ExecuteMethodWithResult(FaucetContractMethod.Initialize, new InitializeInput());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void GetFaucetStatus()
        {
            // init
            var initialize = _faucetContract.Initialize(admin: "", amountLimit: 0, intervalMinutes: 0);
            initialize.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // get faucet status before turn on
            var faucetStatus = _faucetContract.GetFaucetStatus("ELF");
            Logger.Info($"faucetStatus.is_on is {faucetStatus.IsOn}");
            Logger.Info($"faucetStatus.turn_at is {faucetStatus.TurnAt}");
            faucetStatus.IsOn.ShouldBeFalse();

            // turn on
            var turnOn = _faucetContract.TurnOn(symbol: "", at: null);
            turnOn.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var faucetStatusAfterTurnOn = _faucetContract.GetFaucetStatus("ELF");
            Logger.Info($"faucetStatusAfterTurnOn.is_on is {faucetStatusAfterTurnOn.IsOn}");
            Logger.Info($"faucetStatusAfterTurnOn.turn_at is {faucetStatusAfterTurnOn.TurnAt}");
            faucetStatusAfterTurnOn.IsOn.ShouldBeTrue();

            // turn off
            var turnOff = _faucetContract.TurnOff(symbol: "", at: null);
            turnOff.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var faucetStatusAfterTurnOff = _faucetContract.GetFaucetStatus("ELF");
            Logger.Info($"faucetStatusAfterTurnOff.is_on is {faucetStatusAfterTurnOff.IsOn}");
            Logger.Info($"faucetStatusAfterTurnOff.turn_at is {faucetStatusAfterTurnOff.TurnAt}");
            faucetStatusAfterTurnOff.IsOn.ShouldBeFalse();
        }

        // [TestMethod]
        // public async Task GetOwnerStub()
        // {
        //     var owner = await faucetStub.GetOwner.CallAsync(new StringValue {Value = "ELF"});
        //     Logger.Info($"owner is {owner}");
        // }

        // result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
        // result.Error.ShouldContain("");
        // // get owner
        // var owner = _faucetContract.GetOwner("ELF");
        // Logger.Info($"owner is {owner}");
        // owner.ShouldBe(InitAccount.ToString());
    }
}