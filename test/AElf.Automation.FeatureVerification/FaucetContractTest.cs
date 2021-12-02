using System;
using System.Threading.Tasks;
using AElf.Contracts.Faucet;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class FaucetContractTest
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;
        private FaucetContract _faucetContract;
        private FaucetContract _faucetContract2;
        private TokenContractContainer.TokenContractStub _tokenSub;
        private string InitAccount { get; } = "J6zgLjGwd1bxTBpULLXrGVeV74tnS2n74FFJJz7KNdjTYkDF6";
        private string Others { get; } = "2bs2uYMECtHWjB57RqgqQ3X2LrxgptWHtzCqGEU11y45aWimh4";
        private string Account1 { get; } = "2bs2uYMECtHWjB57RqgqQ3X2LrxgptWHtzCqGEU11y45aWimh4";
        private string Target { get; } = "YUW9zH5GhRboT5JK4vXp5BLAfCDv28rRmTQwo418FuaJmkSg8";

        private static string RpcUrl { get; } = "192.168.66.9:8000";
        private string Symbol { get; } = "ELF";
        private string faucet = "2M24EKAecggCnttZ9DUUMCXi4xC67rozA87kFgid9qEwRUMHTs";
        private string faucet2 = "2imqjpkCwnvYzfnr61Lp2XQVN2JU17LPkA9AZzmRZzV5LRRWmR";

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

            if (faucet2.Equals(""))
                _faucetContract2 = new FaucetContract(NodeManager, InitAccount);
            else
                _faucetContract2 = new FaucetContract(NodeManager, InitAccount, faucet2);
        }

        [TestMethod]
        public void Initialize_Default()
        {
            var result =
                _faucetContract.ExecuteMethodWithResult(FaucetContractMethod.Initialize, new InitializeInput());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // get owner
            var owner = _faucetContract.GetOwner("ELF");
            Logger.Info($"owner is {owner}");
            owner.ShouldBe(InitAccount.ConvertAddress());

            // get limit amount and interval minutes
            var limitAmount = _faucetContract.GetLimitAmount("ELF");
            var intervalMinutes = _faucetContract.GetIntervalMinutes("ELF");
            Logger.Info($"limitAmount is {limitAmount}");
            Logger.Info($"intervalMinutes is {intervalMinutes}");
            limitAmount.ShouldBe(100_00000000);
            intervalMinutes.ShouldBe(180);

            // get faucet status
            var faucetStatus = _faucetContract.GetFaucetStatus("ELF");
            Logger.Info($"faucetStatus.is_on is {faucetStatus.IsOn}");
            Logger.Info($"faucetStatus.turn_at is {faucetStatus.TurnAt}");
            faucetStatus.IsOn.ShouldBeFalse();
            faucetStatus.TurnAt.ShouldBeNull();
        }

        [TestMethod]
        public void ErrorTest()
        {
            // others call some methods(turnOn、turnOff、NewFaucet、Pour、SetLimit、Ban、Send)
            _faucetContract.SetAccount(Others);
            var turnOn = _faucetContract.TurnOn("ELF", at: null);
            turnOn.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            Logger.Info($"turnOn.Error is {turnOn.Error}");
            turnOn.Error.ShouldContain("No permission to operate faucet of ELF.");

            var turnOff = _faucetContract.TurnOff(symbol: "ELF", at: null);
            turnOff.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            Logger.Info($"turnOff.Error is {turnOff.Error}");
            turnOff.Error.ShouldContain("No permission to operate faucet of ELF.");

            var newFaucet = _faucetContract.NewFaucet("ELF", 1000_00000000, Others, 0, 0);
            newFaucet.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            Logger.Info($"newFaucet.Error is {newFaucet.Error}");
            newFaucet.Error.ShouldContain("No permission.");

            var pour = _faucetContract.Pour("ELF", 1000_00000000);
            pour.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            Logger.Info($"pour.Error is {pour.Error}");
            pour.Error.ShouldContain("No permission to operate faucet of ELF.");

            var setLimit = _faucetContract.SetLimit("ELF", 0, 0);
            setLimit.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            Logger.Info($"setLimit.Error is {setLimit.Error}");
            setLimit.Error.ShouldContain("No permission to operate faucet of ELF.");

            var ban = _faucetContract.Ban("ELF", Target, true);
            ban.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            Logger.Info($"ban.Error is {ban.Error}");
            ban.Error.ShouldContain("No permission to operate faucet of ELF.");

            var send = _faucetContract.Send(Target, "ELF", 10_00000000);
            send.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            Logger.Info($"send.Error is {send.Error}");
            send.Error.ShouldContain("No permission to operate faucet of ELF.");
        }

        [TestMethod]
        public void Test()
        {
            // init
            var initialize = _faucetContract2.Initialize(Account1, 50_00000000, 300);
            initialize.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            _faucetContract2.SetAccount(Account1);
            // get owner
            var owner = _faucetContract2.GetOwner("ELF");
            Logger.Info($"owner is {owner}");
            owner.ShouldBe(Account1.ConvertAddress());

            // get limit amount and interval minutes
            var limitAmount = _faucetContract2.GetLimitAmount("ELF");
            var intervalMinutes = _faucetContract2.GetIntervalMinutes("ELF");
            Logger.Info($"limitAmount is {limitAmount}");
            Logger.Info($"intervalMinutes is {intervalMinutes}");
            limitAmount.ShouldBe(50_00000000);
            intervalMinutes.ShouldBe(300);

            // turn on(set time before)
            var turnOnTimestamp = Timestamp.FromDateTime(new DateTime(2020, 12, 2, 17, 00, 00, 00).ToUniversalTime());
            var turnOnTimeBefore = _faucetContract2.TurnOn("ELF", turnOnTimestamp);
            var faucetStatusTurnOnTimeBefore = _faucetContract2.GetFaucetStatus("ELF");
            Logger.Info($"faucetStatusTurnOnTimeBefore.is_on is {faucetStatusTurnOnTimeBefore.IsOn}");
            Logger.Info($"faucetStatusTurnOnTimeBefore.turn_at is {faucetStatusTurnOnTimeBefore.TurnAt}");
            faucetStatusTurnOnTimeBefore.IsOn.ShouldBeTrue();
            faucetStatusTurnOnTimeBefore.TurnAt.ShouldNotBeNull();

            // turn off(set time before)
            var turnOffTimestamp = Timestamp.FromDateTime(new DateTime(2020, 12, 3, 17, 00, 00, 00).ToUniversalTime());
            var turnOffTimeBefore = _faucetContract2.TurnOff("ELF", turnOffTimestamp);
            var faucetStatusTurnOffTimeBefore = _faucetContract2.GetFaucetStatus("ELF");
            Logger.Info($"faucetStatusTurnOffTimeBefore.is_on is {faucetStatusTurnOffTimeBefore.IsOn}");
            Logger.Info($"faucetStatusTurnOffTimeBefore.turn_at is {faucetStatusTurnOffTimeBefore.TurnAt}");
            faucetStatusTurnOffTimeBefore.IsOn.ShouldBeFalse();
            faucetStatusTurnOffTimeBefore.TurnAt.ShouldNotBeNull();

            // turn off(init)
            var turnOff = _faucetContract2.ExecuteMethodWithResult(FaucetContractMethod.TurnOff, new TurnInput());
            var faucetStatusTurnOff = _faucetContract2.GetFaucetStatus("ELF");
            Logger.Info($"faucetStatusTurnOff.is_on is {faucetStatusTurnOff.IsOn}");
            Logger.Info($"faucetStatusTurnOff.turn_at is {faucetStatusTurnOff.TurnAt}");
            faucetStatusTurnOff.IsOn.ShouldBeFalse();
            faucetStatusTurnOff.TurnAt.ShouldNotBeNull();

            // turn on(init)
            var turnOn = _faucetContract2.ExecuteMethodWithResult(FaucetContractMethod.TurnOn, new TurnInput());
            var faucetStatusTurnOn = _faucetContract2.GetFaucetStatus("ELF");
            Logger.Info($"faucetStatusTurnOn.is_on is {faucetStatusTurnOn.IsOn}");
            Logger.Info($"faucetStatusTurnOn.turn_at is {faucetStatusTurnOn.TurnAt}");
            faucetStatusTurnOn.IsOn.ShouldBeTrue();
            faucetStatusTurnOn.TurnAt.ShouldNotBeNull();

            // faucet'balance before pour
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, Account1);
            _tokenContract = _genesisContract.GetTokenContract(Account1);
            var faucetBalanceBeforePour = _tokenContract.GetUserBalance(_faucetContract2.ContractAddress, "ELF");
            Logger.Info($"faucetBalanceBeforePour is {faucetBalanceBeforePour}");
            faucetBalanceBeforePour.ShouldBe(0);

            // approve
            var approve1 =
                _tokenContract.ApproveToken(Account1, _faucetContract2.ContractAddress, 10000000000_00000000);
            approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //pour
            var pour = _faucetContract2.Pour("ELF", 100_00000000);
            pour.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var faucetBalanceAfterPour = _tokenContract.GetUserBalance(_faucetContract2.ContractAddress, "ELF");
            Logger.Info($"faucetBalanceAfterPour is {faucetBalanceAfterPour}");
            var targetBalanceBeforeTake = _tokenContract.GetUserBalance(Target, "ELF");
            Logger.Info($"targetBalanceBeforeTake is {targetBalanceBeforeTake}");
            faucetBalanceAfterPour.ShouldBe(100_00000000);
            targetBalanceBeforeTake.ShouldBe(0);

            // take
            _faucetContract2.SetAccount(Target);
            var approve = _tokenContract.ApproveToken(Target, _faucetContract2.ContractAddress, 10000000000_00000000);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var take = _faucetContract2.Take("ELF", 10_00000000);
            take.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var targetBalanceAfterTake = _tokenContract.GetUserBalance(Target, "ELF");
            Logger.Info($"targetBalanceAfterTake is {targetBalanceAfterTake}");
            var faucetBalanceAfterTake = _tokenContract.GetUserBalance(_faucetContract2.ContractAddress, "ELF");
            Logger.Info($"faucetBalanceAfterTake is {faucetBalanceAfterTake}");
            faucetBalanceAfterTake.ShouldBe(90_00000000);
            targetBalanceAfterTake.ShouldBe(10_00000000);

            // return
            _tokenContract.SetAccount(Target);
            var approve2 = _tokenContract.ApproveToken(Target, _faucetContract2.ContractAddress, 10000000000_00000000);
            approve2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var targetReturn = _faucetContract2.Return("ELF", 2_00000000);
            targetReturn.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var targetBalanceAfterReturn = _tokenContract.GetUserBalance(Target, "ELF");
            Logger.Info($"targetBalanceAfterReturn is {targetBalanceAfterReturn}");
            var faucetBalanceAfterReturn = _tokenContract.GetUserBalance(_faucetContract2.ContractAddress, "ELF");
            Logger.Info($"faucetBalanceAfterReturn is {faucetBalanceAfterReturn}");
            faucetBalanceAfterReturn.ShouldBe(92_00000000);
            targetBalanceAfterReturn.ShouldBe(8_00000000);

            // return all
            var approve3 = _tokenContract.ApproveToken(Target, _faucetContract2.ContractAddress, 10000000000_00000000);
            approve3.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var targetReturnAll =
                _faucetContract2.ExecuteMethodWithResult(FaucetContractMethod.Return, new ReturnInput {Symbol = "ELF"});
            targetReturnAll.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var targetBalanceAfterReturnAll = _tokenContract.GetUserBalance(Target, "ELF");
            Logger.Info($"targetBalanceAfterReturnAll is {targetBalanceAfterReturnAll}");
            var faucetBalanceAfterReturnAll = _tokenContract.GetUserBalance(_faucetContract2.ContractAddress, "ELF");
            Logger.Info($"faucetBalanceAfterReturnAll is {faucetBalanceAfterReturnAll}");
            faucetBalanceAfterReturnAll.ShouldBe(100_00000000);
            targetBalanceAfterReturnAll.ShouldBe(0);
        }

        [TestMethod]
        public void Transfer()
        {
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, Target);
            _tokenContract = _genesisContract.GetTokenContract(Target);
            var account1BalanceBefore = _tokenContract.GetUserBalance(Account1, "ELF");
            var targetBalanceBefore = _tokenContract.GetUserBalance(Target, "ELF");
            Logger.Info($"account1BalanceBefore is {account1BalanceBefore}");
            Logger.Info($"targetBalanceBefore is {targetBalanceBefore}");

            _tokenContract.TransferBalance(Account1, Target, 90000000, "ELF");
            var account1BalanceAfter = _tokenContract.GetUserBalance(Account1, "ELF");
            var targetBalanceAfter = _tokenContract.GetUserBalance(Target, "ELF");
            Logger.Info($"account1BalanceAfter is {account1BalanceAfter}");
            Logger.Info($"targetBalanceAfter is {targetBalanceAfter}");
        }
    }
}