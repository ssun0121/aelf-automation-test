using System;
using System.Threading;
using AElf.Standards.ACS10;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class IdoContractTest
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;
        private IdoContract _idoContract;
        private string Owner { get; } = "J6zgLjGwd1bxTBpULLXrGVeV74tnS2n74FFJJz7KNdjTYkDF6";
        private string InitAccount { get; } = "2bs2uYMECtHWjB57RqgqQ3X2LrxgptWHtzCqGEU11y45aWimh4";
        private string User { get; } = "YUW9zH5GhRboT5JK4vXp5BLAfCDv28rRmTQwo418FuaJmkSg8";

        private static string RpcUrl { get; } = "192.168.66.9:8000";
        private string ISTAR { get; } = "ISTAR";
        private string USDT { get; } = "USDT";

        private string ido = "iupiTuL2cshxB9UNauXNXe9iyCcqka7jCotodcEHGpNXeLzqG";

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("IdoContractTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env2-main");

            NodeManager = new NodeManager(RpcUrl);
            if (ido.Equals(""))
                _idoContract = new IdoContract(NodeManager, InitAccount);
            else
                _idoContract = new IdoContract(NodeManager, InitAccount, ido);
        }

        [TestMethod]
        public void InitializeTest()
        {
            var result = _idoContract.Initialize(Owner);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Check owner
            var owner = _idoContract.GetOwner();
            Logger.Info($"owner is {owner}");
            owner.ShouldBe(Owner.ConvertAddress());

            Thread.Sleep(30 * 1000);
            // The second initialization failed
            var initialize = _idoContract.Initialize(Owner);
            initialize.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            initialize.Error.ShouldContain("Already initialized.");
        }

        [TestMethod]
        public void ResetTimeSpanTest()
        {
            // Not initialized,setting failed
            var maxTimespan = 172800;
            var minTimespan = 86400;
            var resultInit = _idoContract.ResetTimeSpan(maxTimespan, minTimespan);
            resultInit.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            resultInit.Error.ShouldContain("Contract not initialized.");

            // Initialize
            InitializeTest();

            // Not Owner
            var result = _idoContract.ResetTimeSpan(maxTimespan, minTimespan);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("Not Owner.");

            // Set successfully
            _idoContract.SetAccount(Owner);
            var result1 = _idoContract.ResetTimeSpan(maxTimespan, minTimespan);
            result1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var getTimespan = _idoContract.GetTimespan();
            Logger.Info($"MaxTimespan is {getTimespan.MaxTimespan}");
            Logger.Info($"MinTimespan is {getTimespan.MinTimespan}");

            // MaxTimespan < MinTimespan
            var maxTimespan1 = 86400;
            var minTimespan1 = 172800;
            var result2 = _idoContract.ResetTimeSpan(maxTimespan1, minTimespan1);
            result2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result2.Error.ShouldContain("Invalid parameter.");

            // MaxTimespan = MinTimespan
            var maxTimespan2 = 86400;
            var minTimespan2 = 86400;
            var result3 = _idoContract.ResetTimeSpan(maxTimespan2, minTimespan2);
            result3.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result3.Error.ShouldContain("Invalid parameter.");
        }

        [TestMethod]
        public void AddPublicOfferingTest()
        {
            // contract:2w13DqbuuiadvaSY2ZyKi2UoXg354zfHLM3kwRKKy85cViw4ZF

            // get timeSpan
            var getTimespan = _idoContract.GetTimespan();
            Logger.Info($"MaxTimespan is {getTimespan.MaxTimespan}");
            Logger.Info($"MinTimespan is {getTimespan.MinTimespan}");

            _idoContract.SetAccount(Owner);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, Owner);
            _tokenContract = _genesisContract.GetTokenContract(Owner);
            var approve =
                _tokenContract.ApproveToken(Owner, _idoContract.ContractAddress, 100000_00000000, ISTAR);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var offeringTokenSymbol = ISTAR;
            var offeringTokenAmount = 10_00000000;
            var wantTokenSymbol = USDT;
            var wantTokenAmount = 10_0000;
            var startTime = DateTime.Now.AddSeconds(10);
            var endTime = startTime.AddSeconds(getTimespan.MinTimespan);
            var addPublicOffering = _idoContract.AddPublicOffering(offeringTokenSymbol, offeringTokenAmount,
                wantTokenSymbol,
                wantTokenAmount, startTime, endTime);
            addPublicOffering.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // offing token amount is zero
            var addPublicOffering1 = _idoContract.AddPublicOffering(offeringTokenSymbol, 0, wantTokenSymbol,
                wantTokenAmount, startTime, endTime);
            addPublicOffering1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addPublicOffering1.Error.ShouldContain("Need deposit some offering token.");

            // Invaild start time.
            var addPublicOffering2 = _idoContract.AddPublicOffering(offeringTokenSymbol, offeringTokenAmount,
                wantTokenSymbol,
                wantTokenAmount, new DateTime(2020, 12, 8, 00, 00, 00, 00), endTime);
            addPublicOffering2.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addPublicOffering2.Error.ShouldContain("Invaild start time.");

            // Invaild end time
            var addPublicOffering3 = _idoContract.AddPublicOffering(offeringTokenSymbol, offeringTokenAmount,
                wantTokenSymbol,
                wantTokenAmount, DateTime.Now.AddSeconds(10), DateTime.Now.AddSeconds(10));
            addPublicOffering3.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addPublicOffering3.Error.ShouldContain("Invaild end time.");

            var addPublicOffering4 = _idoContract.AddPublicOffering(offeringTokenSymbol, offeringTokenAmount,
                wantTokenSymbol,
                wantTokenAmount, DateTime.Now.AddSeconds(10),
                DateTime.Now.AddSeconds(1000).AddSeconds(getTimespan.MaxTimespan));
            addPublicOffering4.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addPublicOffering4.Error.ShouldContain("Invaild end time.");
        }

        [TestMethod]
        public void ChangeAscriptionTest()
        {
            // contract:2w13DqbuuiadvaSY2ZyKi2UoXg354zfHLM3kwRKKy85cViw4ZF

            // other change ascription failed
            _idoContract.SetAccount(InitAccount);
            var changeAscription = _idoContract.ChangeAscription(ISTAR, InitAccount);
            changeAscription.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            changeAscription.Error.ShouldContain("No right to assign.");
        }

        [TestMethod]
        public void InvestTest()
        {
            // contract:iupiTuL2cshxB9UNauXNXe9iyCcqka7jCotodcEHGpNXeLzqG

            // Transfer
            Transfer(Owner, User, USDT, 1000_000);
            Transfer(Owner, User, "ELF", 1000_00000000000);

            // Activity not exist
            _idoContract.SetAccount(User);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, User);
            _tokenContract = _genesisContract.GetTokenContract(User);
            var approve =
                _tokenContract.ApproveToken(User, _idoContract.ContractAddress, 100000_00000000, USDT);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var changeAscription = _idoContract.Invest(0, 10_000);
            changeAscription.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            changeAscription.Error.ShouldContain("Activity not exist.");
        }

        [TestMethod]
        public void GetBalance()
        {
            // initAccount's balance
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
            var elfBalance = _tokenContract.GetUserBalance(InitAccount, "ELF");
            Logger.Info($"elfBalance(InitAccount) is {elfBalance}");

            var istarBalance = _tokenContract.GetUserBalance(InitAccount, "ISTAR");
            Logger.Info($"istarBalance(InitAccount) is {istarBalance}");

            var usdtBalance = _tokenContract.GetUserBalance(InitAccount, "USDT");
            Logger.Info($"usdtBalance(InitAccount) is {usdtBalance}");

            // owner's balance
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, Owner);
            _tokenContract = _genesisContract.GetTokenContract(Owner);
            var elfBalance1 = _tokenContract.GetUserBalance(Owner, "ELF");
            Logger.Info($"elfBalance(Owner) is {elfBalance1}");

            var istarBalance1 = _tokenContract.GetUserBalance(Owner, "ISTAR");
            Logger.Info($"istarBalance1(Owner) is {istarBalance1}");

            var usdtBalance1 = _tokenContract.GetUserBalance(Owner, "USDT");
            Logger.Info($"usdtBalance1(Owner) is {usdtBalance1}");
        }

        [TestMethod]
        public void Transfer(string from, string to, string symbol, long amount)
        {
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, from);
            _tokenContract = _genesisContract.GetTokenContract(from);
            var account1BalanceBefore = _tokenContract.GetUserBalance(from, symbol);
            var targetBalanceBefore = _tokenContract.GetUserBalance(to, symbol);
            Logger.Info($"account1BalanceBefore is {account1BalanceBefore}");
            Logger.Info($"targetBalanceBefore is {targetBalanceBefore}");

            _tokenContract.TransferBalance(from, to, amount, symbol);
            var account1BalanceAfter = _tokenContract.GetUserBalance(from, symbol);
            var targetBalanceAfter = _tokenContract.GetUserBalance(to, symbol);
            Logger.Info($"account1BalanceAfter is {account1BalanceAfter}");
            Logger.Info($"targetBalanceAfter is {targetBalanceAfter}");
        }
    }
}