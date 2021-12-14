using System;
using System.Security.Policy;
using System.Threading;
using AElf.Client.Dto;
using AElf.Standards.ACS10;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Gandalf.Contracts.IdoContract;
using Google.Protobuf.WellKnownTypes;
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
        private string NewPublisher { get; } = "2bs2uYMECtHWjB57RqgqQ3X2LrxgptWHtzCqGEU11y45aWimh4";
        private string InitAccount { get; } = "2bs2uYMECtHWjB57RqgqQ3X2LrxgptWHtzCqGEU11y45aWimh4";
        private string User { get; } = "YUW9zH5GhRboT5JK4vXp5BLAfCDv28rRmTQwo418FuaJmkSg8";

        private static string RpcUrl { get; } = "192.168.66.9:8000";
        private string ISTAR { get; } = "ISTAR";
        private string USDT { get; } = "USDT";

        private string ido = "";

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

            Thread.Sleep(60 * 1000);
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
            // contract:GVqTvLDwiDfoWEtq3VeB9g4jMfbP4KbXRyqit4Z1LKsbZrwrV
            // contract：g9xy6gaLtM5WKEe3kSEqXfBQjhhPBbRtgbrVveMURNzf2zVaK
            // contract:2avzCgLG2qQrtFQrtcmcHQtu52mRRGhMeGrnviv77j1qQaKt3d
            ResetTimeSpanTest();

            // get timeSpan
            var getTimespan = _idoContract.GetTimespan();
            Logger.Info($"MaxTimespan is {getTimespan.MaxTimespan}");
            Logger.Info($"MinTimespan is {getTimespan.MinTimespan}");

            _idoContract.SetAccount(Owner);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, Owner);
            _tokenContract = _genesisContract.GetTokenContract(Owner);
            var approve =
                _tokenContract.ApproveToken(Owner, _idoContract.ContractAddress, 10000000000_00000000, ISTAR);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var offeringTokenSymbol = ISTAR;
            var offeringTokenAmount = 100_00000000;
            var wantTokenSymbol = USDT;
            var wantTokenAmount = 10_0000;
            var startTime = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
            var endTime = DateTime.UtcNow.AddSeconds(10).AddSeconds(getTimespan.MinTimespan).ToTimestamp();
            ;
            var addPublicOffering = _idoContract.AddPublicOffering(offeringTokenSymbol, offeringTokenAmount,
                wantTokenSymbol,
                wantTokenAmount, startTime, endTime);
            addPublicOffering.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            CheckPublicOffering(new PublicOfferingOutput
            {
                OfferingTokenSymbol = ISTAR,
                OfferingTokenAmount = 100_00000000,
                WantTokenSymbol = USDT,
                WantTokenAmount = 10_0000,
                StartTime = startTime,
                EndTime = endTime,
                PublicId = 0,
                Publisher = Owner.ConvertAddress(),
                Claimed = false,
                WantTokenBalance = 0,
                SubscribedOfferingAmount = 0
            });

            // offing token amount is zero
            var addPublicOffering1 = _idoContract.AddPublicOffering(offeringTokenSymbol, 0, wantTokenSymbol,
                wantTokenAmount, startTime, endTime);
            addPublicOffering1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addPublicOffering1.Error.ShouldContain("Need deposit some offering token.");

            // Invaild start time.
            var addPublicOffering2 = _idoContract.AddPublicOffering(offeringTokenSymbol, offeringTokenAmount,
                wantTokenSymbol,
                wantTokenAmount, new DateTime(2020, 12, 8, 00, 00, 00, 00, kind: DateTimeKind.Utc).ToTimestamp(),
                endTime);
            addPublicOffering2.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addPublicOffering2.Error.ShouldContain("Invaild start time.");

            // Invaild end time
            var addPublicOffering3 = _idoContract.AddPublicOffering(offeringTokenSymbol, offeringTokenAmount,
                wantTokenSymbol,
                wantTokenAmount, DateTime.UtcNow.AddSeconds(10).ToTimestamp(),
                DateTime.UtcNow.AddSeconds(10).ToTimestamp());
            addPublicOffering3.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addPublicOffering3.Error.ShouldContain("Invaild end time.");

            var addPublicOffering4 = _idoContract.AddPublicOffering(offeringTokenSymbol, offeringTokenAmount,
                wantTokenSymbol,
                wantTokenAmount, DateTime.UtcNow.AddSeconds(10).ToTimestamp(),
                DateTime.UtcNow.AddSeconds(1000).AddSeconds(getTimespan.MaxTimespan).ToTimestamp());
            addPublicOffering4.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addPublicOffering4.Error.ShouldContain("Invaild end time.");

            // offing token amount > balance
            var istarBalance = GetBalance(Owner, ISTAR);
            var addPublicOffering5 = _idoContract.AddPublicOffering(offeringTokenSymbol, istarBalance + 100,
                wantTokenSymbol,
                wantTokenAmount, DateTime.UtcNow.AddSeconds(10).ToTimestamp(),
                DateTime.UtcNow.AddSeconds(10).AddSeconds(getTimespan.MinTimespan).ToTimestamp());
            addPublicOffering5.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addPublicOffering5.Error.ShouldContain("Insufficient balance of ISTAR.");
        }

        [TestMethod]
        public void ActivityIdNotExistTest()
        {
            // contract:233wFn5JbyD4i8R5Me4cW4z6edfFGRn5bpWnGuY8fjR7b2kRsD

            // Transfer
            var usdtBalance = GetBalance(User, USDT);
            var elfBalance = GetBalance(User, "ELF");
            if (usdtBalance == 0 && elfBalance == 0)
            {
                Transfer(Owner, User, USDT, 1000_000);
                Transfer(Owner, User, "ELF", 1000_00000000);
            }

            // Withdraw
            var withdraw = _idoContract.Withdraw(0);
            withdraw.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            withdraw.Error.ShouldContain("Activity id not exist.");

            // Invest
            _idoContract.SetAccount(User);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, User);
            _tokenContract = _genesisContract.GetTokenContract(User);
            var approve =
                _tokenContract.ApproveToken(User, _idoContract.ContractAddress, 100000_00000000, USDT);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var invest = _idoContract.Invest(0, 10_000, "");
            invest.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            invest.Error.ShouldContain("Activity id not exist.");

            // Harvest
            var harvest = _idoContract.Harvest(0);
            harvest.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            harvest.Error.ShouldContain("Activity id not exist.");
        }

        [TestMethod]
        public void ActivityNotExistTest()
        {
        }

        [TestMethod]
        public void ChangeAscriptionTest()
        {
            // Transfer
            var istarBalance = GetBalance(NewPublisher, ISTAR);
            var elfBalance = GetBalance(NewPublisher, "ELF");
            if (istarBalance == 0 || elfBalance == 0)
            {
                Transfer(Owner, NewPublisher, ISTAR, 10000000_00000000);
                Transfer(Owner, NewPublisher, "ELF", 1000_00000000);
            }

            ResetTimeSpanTest();
            // Get timeSpan
            var getTimespan = _idoContract.GetTimespan();
            Logger.Info($"MaxTimespan is {getTimespan.MaxTimespan}");
            Logger.Info($"MinTimespan is {getTimespan.MinTimespan}");

            var startTime = DateTime.UtcNow.AddSeconds(60).ToTimestamp();
            var endTime = DateTime.UtcNow.AddSeconds(60).AddSeconds(getTimespan.MinTimespan).ToTimestamp();
            var addPublicOffering = AddPublicOffering(Owner, startTime, endTime);
            addPublicOffering.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Owner adds publicOffering twice
            Thread.Sleep(60 * 1000);
            var startTime1 = DateTime.UtcNow.AddSeconds(60).ToTimestamp();
            var endTime1 = DateTime.UtcNow.AddSeconds(60).AddSeconds(getTimespan.MinTimespan).ToTimestamp();
            var addPublicOffering2 = AddPublicOffering(Owner, startTime1, endTime1);
            addPublicOffering2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Other adds publicOffering twice
            var addPublicOffering3 = AddPublicOffering(NewPublisher, startTime1, endTime1);
            addPublicOffering3.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addPublicOffering3.Error.ShouldContain("Another has published the token before.");

            // GetPublicOfferingLength
            _idoContract.SetAccount(Owner);
            var publicOfferingLength = _idoContract.GetPublicOfferingLength();
            Logger.Info($"publicOfferingLength is {publicOfferingLength}");
            publicOfferingLength.ShouldBe(2);

            // Other change ascription failed
            Thread.Sleep(30 * 1000);
            _idoContract.SetAccount(NewPublisher);
            var changeAscription = _idoContract.ChangeAscription(ISTAR, NewPublisher);
            changeAscription.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            changeAscription.Error.ShouldContain("No right to assign.");
            var tokenOwnership = _idoContract.GetTokenOwnership(ISTAR);
            Logger.Info($"tokenOwnership is {tokenOwnership}");
            tokenOwnership.ShouldBe(Owner.ConvertAddress());

            // Change successfully
            Thread.Sleep(30 * 1000);
            _idoContract.SetAccount(Owner);
            var changeAscription1 = _idoContract.ChangeAscription(ISTAR, NewPublisher);
            changeAscription1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            // owner changes twice failed
            Thread.Sleep(30 * 1000);
            var changeAscription4 = _idoContract.ChangeAscription(ISTAR, NewPublisher);
            changeAscription4.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var tokenOwnership1 = _idoContract.GetTokenOwnership(ISTAR);
            Logger.Info($"tokenOwnership1 is {tokenOwnership1}");
            tokenOwnership1.ShouldBe(NewPublisher.ConvertAddress());

            // NewPublisher adds publicOffering
            Thread.Sleep(60 * 1000);
            var startTime2 = DateTime.UtcNow.AddSeconds(60).ToTimestamp();
            var endTime2 = DateTime.UtcNow.AddSeconds(60).AddSeconds(getTimespan.MinTimespan).ToTimestamp();
            var addPublicOffering4 = AddPublicOffering(NewPublisher, startTime2, endTime2);
            addPublicOffering4.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            _idoContract.SetAccount(NewPublisher);
            var publicOfferingLength2 = _idoContract.GetPublicOfferingLength();
            Logger.Info($"publicOfferingLength2 is {publicOfferingLength2}");
            publicOfferingLength2.ShouldBe(3);
            CheckPublicOffering(new PublicOfferingOutput
            {
                OfferingTokenSymbol = ISTAR,
                OfferingTokenAmount = 100_00000000,
                WantTokenSymbol = USDT,
                WantTokenAmount = 10_0000,
                StartTime = startTime2,
                EndTime = endTime2,
                PublicId = 2,
                Publisher = NewPublisher.ConvertAddress(),
                Claimed = false,
                WantTokenBalance = 0,
                SubscribedOfferingAmount = 0
            });

            // New publisher change successfully
            Thread.Sleep(30 * 1000);
            _idoContract.SetAccount(NewPublisher);
            var changeAscription2 = _idoContract.ChangeAscription(ISTAR, Owner);
            changeAscription2.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            // change twice failed
            Thread.Sleep(60 * 1000);
            var changeAscription3 = _idoContract.ChangeAscription(ISTAR, Owner);
            changeAscription3.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            changeAscription3.Error.ShouldContain("No right to assign.");

            var tokenOwnership2 = _idoContract.GetTokenOwnership(ISTAR);
            Logger.Info($"tokenOwnership2 is {tokenOwnership2}");
            tokenOwnership2.ShouldBe(Owner.ConvertAddress());
        }

        [TestMethod]
        public void InvestTest()
        {
            // contract:DUUb2CkWpZp6d5UsgjxMhwtWmvfKwkga4osMaUW5igsyQwgjY

            ResetTimeSpanTest();
            // get timeSpan
            var getTimespan = _idoContract.GetTimespan();
            Logger.Info($"MaxTimespan is {getTimespan.MaxTimespan}");
            Logger.Info($"MinTimespan is {getTimespan.MinTimespan}");

            _idoContract.SetAccount(Owner);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, Owner);
            _tokenContract = _genesisContract.GetTokenContract(Owner);
            var approve1 =
                _tokenContract.ApproveToken(Owner, _idoContract.ContractAddress, 10000000000_00000000, ISTAR);
            approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var offeringTokenSymbol = ISTAR;
            var offeringTokenAmount = 100_00000000;
            var wantTokenSymbol = USDT;
            var wantTokenAmount = 10_0000;
            var startTime = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
            var endTime = DateTime.UtcNow.AddSeconds(10).AddSeconds(getTimespan.MinTimespan).ToTimestamp();
            var addPublicOffering = _idoContract.AddPublicOffering(offeringTokenSymbol, offeringTokenAmount,
                wantTokenSymbol,
                wantTokenAmount, startTime, endTime);
            addPublicOffering.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            CheckPublicOffering(new PublicOfferingOutput
            {
                OfferingTokenSymbol = ISTAR,
                OfferingTokenAmount = 100_00000000,
                WantTokenSymbol = USDT,
                WantTokenAmount = 10_0000,
                StartTime = startTime,
                EndTime = endTime,
                PublicId = 0,
                Publisher = Owner.ConvertAddress(),
                Claimed = false,
                WantTokenBalance = 0,
                SubscribedOfferingAmount = 0
            });

            // Transfer
            var usdtBalance = GetBalance(User, USDT);
            var elfBalance = GetBalance(User, "ELF");
            if (usdtBalance == 0 && elfBalance == 0)
            {
                Transfer(Owner, User, USDT, 1000_000);
                Transfer(Owner, User, "ELF", 1000_00000000);
            }

            var usdtBalanceAfter = GetBalance(User, USDT);
            _idoContract.SetAccount(User);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, User);
            _tokenContract = _genesisContract.GetTokenContract(User);
            var approve =
                _tokenContract.ApproveToken(User, _idoContract.ContractAddress, 10000000000_00000000, USDT);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            Thread.Sleep(60 * 1000);
            var invest1 = _idoContract.Invest(0, 0, "");
            invest1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            invest1.Error.ShouldContain("Invalid amount.");

            Thread.Sleep(60 * 1000);
            var invest2 = _idoContract.Invest(0, 1_0000, "");
            invest2.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            CheckPublicOffering(new PublicOfferingOutput
            {
                OfferingTokenSymbol = ISTAR,
                OfferingTokenAmount = 100_00000000,
                WantTokenSymbol = USDT,
                WantTokenAmount = 10_0000,
                StartTime = startTime,
                EndTime = endTime,
                PublicId = 0,
                Publisher = Owner.ConvertAddress(),
                Claimed = false,
                WantTokenBalance = 1_0000,
                SubscribedOfferingAmount = 10_00000000
            });
            CheckUserInfo(0, User, new UserInfo
            {
                Claimed = false,
                ObtainAmount = 10_00000000
            });

            // Over raising
            Thread.Sleep(60 * 1000);
            var invest = _idoContract.Invest(0, usdtBalanceAfter + 10_0000, "");
            invest.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            CheckPublicOffering(new PublicOfferingOutput
            {
                OfferingTokenSymbol = ISTAR,
                OfferingTokenAmount = 100_00000000,
                WantTokenSymbol = USDT,
                WantTokenAmount = 10_0000,
                StartTime = startTime,
                EndTime = endTime,
                PublicId = 0,
                Publisher = Owner.ConvertAddress(),
                Claimed = false,
                WantTokenBalance = 10_0000,
                SubscribedOfferingAmount = 100_00000000
            });
            CheckUserInfo(0, User, new UserInfo
            {
                Claimed = false,
                ObtainAmount = 100_00000000
            });

            // Out of stock
            Thread.Sleep(60 * 1000);
            var invest3 = _idoContract.Invest(0, 5_0000, "");
            invest3.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            invest3.Error.ShouldContain("Out of stock.");

            // Invalid channel.
            var invest4 = _idoContract.Invest(0, 5_0000, null);
            invest4.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            invest4.Error.ShouldContain("Invalid channel.");
        }

        [TestMethod]
        public void InvestWithdrawHarvestBeforeEndTest()
        {
            ResetTimeSpanTest();
            // get timeSpan
            var getTimespan = _idoContract.GetTimespan();
            Logger.Info($"MaxTimespan is {getTimespan.MaxTimespan}");
            Logger.Info($"MinTimespan is {getTimespan.MinTimespan}");

            _idoContract.SetAccount(Owner);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, Owner);
            _tokenContract = _genesisContract.GetTokenContract(Owner);
            var approve1 =
                _tokenContract.ApproveToken(Owner, _idoContract.ContractAddress, 10000000000_00000000, ISTAR);
            approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var offeringTokenSymbol = ISTAR;
            var offeringTokenAmount = 100_00000000;
            var wantTokenSymbol = USDT;
            var wantTokenAmount = 10_0000;
            var startTime = DateTime.UtcNow.AddSeconds(60).ToTimestamp();
            var endTime = DateTime.UtcNow.AddSeconds(60).AddSeconds(getTimespan.MinTimespan).ToTimestamp();
            var addPublicOffering = _idoContract.AddPublicOffering(offeringTokenSymbol, offeringTokenAmount,
                wantTokenSymbol,
                wantTokenAmount, startTime, endTime);
            addPublicOffering.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            // Another has published the token before
            _idoContract.SetAccount(InitAccount);
            var addPublicOffering1 = _idoContract.AddPublicOffering(offeringTokenSymbol, offeringTokenAmount,
                wantTokenSymbol,
                wantTokenAmount, startTime, endTime);
            addPublicOffering1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addPublicOffering1.Error.ShouldContain("Another has published the token before.");

            // Withdraw amount < 0
            _idoContract.SetAccount(Owner);
            var withdrawError = _idoContract.Withdraw(-1);
            withdrawError.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            withdrawError.Error.ShouldContain("Invalid number.");

            // Withdraw
            _idoContract.SetAccount(Owner);
            var withdraw = _idoContract.Withdraw(0);
            withdraw.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            withdraw.Error.ShouldContain("Game not over.");

            // Invest
            _idoContract.SetAccount(User);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, User);
            _tokenContract = _genesisContract.GetTokenContract(User);
            var approve =
                _tokenContract.ApproveToken(User, _idoContract.ContractAddress, 100000_00000000, USDT);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var invest = _idoContract.Invest(0, 10_000, "");
            invest.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            invest.Error.ShouldContain("Not ido time.");

            // Harvest
            _idoContract.SetAccount(User);
            var harvest = _idoContract.Harvest(0);
            harvest.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            harvest.Error.ShouldContain("The activity is not over.");

            Thread.Sleep(90 * 1000);
            // Withdraw
            _idoContract.SetAccount(Owner);
            var withdraw1 = _idoContract.Withdraw(0);
            withdraw1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            withdraw1.Error.ShouldContain("Game not over.");

            // Harvest
            _idoContract.SetAccount(User);
            var harvest1 = _idoContract.Harvest(0);
            harvest1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            harvest1.Error.ShouldContain("The activity is not over.");
        }

        [TestMethod]
        public void InvestErrorTest()
        {
            // Transfer
            var usdtBalance = GetBalance(User, USDT);
            var elfBalance = GetBalance(User, "ELF");
            if (usdtBalance == 0 || elfBalance == 0)
            {
                Transfer(Owner, User, USDT, 1000_0000);
                Transfer(Owner, User, "ELF", 1000_00000000);
            }

            if (usdtBalance > 0)
            {
                Transfer(User, Owner, USDT, usdtBalance - 5_0000);
            }

            // Initialize
            InitializeTest();

            // Set successfully
            _idoContract.SetAccount(Owner);
            // Not initialized,setting failed
            var maxTimespan = 120;
            var minTimespan = 60;
            var result = _idoContract.ResetTimeSpan(maxTimespan, minTimespan);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var getTimespan = _idoContract.GetTimespan();
            Logger.Info($"MaxTimespan is {getTimespan.MaxTimespan}");
            Logger.Info($"MinTimespan is {getTimespan.MinTimespan}");

            var startTime = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
            var endTime = DateTime.UtcNow.AddSeconds(10).AddSeconds(getTimespan.MinTimespan).ToTimestamp();
            var addPublicOffering = AddPublicOffering(Owner, startTime, endTime);
            addPublicOffering.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var usdtBalanceAfter = GetBalance(User, USDT);
            _idoContract.SetAccount(User);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, User);
            _tokenContract = _genesisContract.GetTokenContract(User);
            var approve =
                _tokenContract.ApproveToken(User, _idoContract.ContractAddress, 10000000000_00000000, USDT);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var invest1 = _idoContract.Invest(0, 10_0000, "");
            invest1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            invest1.Error.ShouldContain("Insufficient balance of USDT.");

            Thread.Sleep(90 * 1000);
            var invest2 = _idoContract.Invest(0, 1_0000, "");
            invest2.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            invest2.Error.ShouldContain("Not ido time.");
        }

        [TestMethod]
        public void HarvestWithdrawAfterEndTimeTest()
        {
            // contract:2eKvgivaCmqPXhkvpS2UVo2qpYskPzFVSYsw6s93jTa9kUh44h

            // Transfer
            var usdtBalance = GetBalance(User, USDT);
            var elfBalance = GetBalance(User, "ELF");
            if (usdtBalance == 0 || elfBalance == 0)
            {
                Transfer(Owner, User, USDT, 1000_0000);
                Transfer(Owner, User, "ELF", 1000_00000000);
            }

            if (usdtBalance <= 11_0000)
            {
                Transfer(Owner, User, USDT, 1000_0000);
            }

            // Initialize
            InitializeTest();

            // Set successfully
            _idoContract.SetAccount(Owner);

            var maxTimespan = 120;
            var minTimespan = 60;
            var result = _idoContract.ResetTimeSpan(maxTimespan, minTimespan);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var getTimespan = _idoContract.GetTimespan();
            Logger.Info($"MaxTimespan is {getTimespan.MaxTimespan}");
            Logger.Info($"MinTimespan is {getTimespan.MinTimespan}");

            var startTime = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
            var endTime = DateTime.UtcNow.AddSeconds(10).AddSeconds(getTimespan.MinTimespan).ToTimestamp();
            var addPublicOffering = AddPublicOffering(Owner, startTime, endTime);
            addPublicOffering.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            Thread.Sleep(20 * 1000);
            _idoContract.SetAccount(User);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, User);
            _tokenContract = _genesisContract.GetTokenContract(User);
            var approve =
                _tokenContract.ApproveToken(User, _idoContract.ContractAddress, 10000000000_00000000, USDT);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var invest1 = _idoContract.Invest(0, 1_0000, "");
            invest1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            Thread.Sleep(60 * 1000);
            var startTime1 = DateTime.UtcNow.AddSeconds(10).ToTimestamp();
            var endTime1 = DateTime.UtcNow.AddSeconds(10).AddSeconds(getTimespan.MinTimespan).ToTimestamp();
            var addPublicOffering1 = AddPublicOffering(Owner, startTime1, endTime1);
            addPublicOffering1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            Thread.Sleep(20 * 1000);
            _idoContract.SetAccount(User);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, User);
            _tokenContract = _genesisContract.GetTokenContract(User);
            var approve1 =
                _tokenContract.ApproveToken(User, _idoContract.ContractAddress, 10000000000_00000000, USDT);
            approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var invest2 = _idoContract.Invest(1, 10_0000, "");
            invest2.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            Thread.Sleep(90 * 1000);
            var ownerIstarBefore = GetBalance(Owner, ISTAR);
            var ownerUsdtBefore = GetBalance(Owner, USDT);
            var userIstarBefore = GetBalance(User, ISTAR);
            var userUsdtBefore = GetBalance(User, USDT);

            // Owner withdraw from pool1
            _idoContract.SetAccount(Owner);
            var withdraw = _idoContract.Withdraw(0);
            withdraw.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var ownerIstarAfter = GetBalance(Owner, ISTAR);
            var ownerUsdtAfter = GetBalance(Owner, USDT);
            ownerIstarAfter.ShouldBe(90_00000000 + ownerIstarBefore);
            ownerUsdtAfter.ShouldBe(1_0000 + ownerUsdtBefore);

            // Owner withdraw twice from pool1
            Thread.Sleep(60 * 1000);
            _idoContract.SetAccount(Owner);
            var withdraw1 = _idoContract.Withdraw(0);
            withdraw1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            withdraw1.Error.ShouldContain("Have withdrawn.");

            // Other withdraw from pool1
            Thread.Sleep(60 * 1000);
            _idoContract.SetAccount(InitAccount);
            var withdraw2 = _idoContract.Withdraw(1);
            withdraw2.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            withdraw2.Error.ShouldContain("No rights.");

            // Owner withdraw from pool2
            var ownerIstarBefore1 = GetBalance(Owner, ISTAR);
            var ownerUsdtBefore1 = GetBalance(Owner, USDT);

            _idoContract.SetAccount(Owner);
            var withdraw3 = _idoContract.Withdraw(1);
            withdraw3.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var ownerIstarAfterPool2 = GetBalance(Owner, ISTAR);
            var ownerUsdtAfterPool2 = GetBalance(Owner, USDT);
            ownerIstarAfterPool2.ShouldBe(0 + ownerIstarBefore1);
            ownerUsdtAfterPool2.ShouldBe(10_0000 + ownerUsdtBefore1);

            // User harvest
            _idoContract.SetAccount(User);
            var harvest = _idoContract.Harvest(0);
            harvest.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var userIstar = GetBalance(User, ISTAR);
            var userUsdt = GetBalance(User, USDT);
            userIstar.ShouldBe(10_00000000 + userIstarBefore);
            userUsdt.ShouldBe(0 + userUsdtBefore);

            // User harvest twice
            Thread.Sleep(60 * 1000);
            var harvest1 = _idoContract.Harvest(0);
            harvest1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            harvest1.Error.ShouldContain("Have harvested.");

            // Other harvest
            Thread.Sleep(60 * 1000);
            _idoContract.SetAccount(InitAccount);
            var harvest2 = _idoContract.Harvest(0);
            harvest2.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            harvest2.Error.ShouldContain("Not participate in.");

            CheckPublicOffering(new PublicOfferingOutput
            {
                OfferingTokenSymbol = ISTAR,
                OfferingTokenAmount = 100_00000000,
                WantTokenSymbol = USDT,
                WantTokenAmount = 10_0000,
                StartTime = startTime,
                EndTime = endTime,
                PublicId = 0,
                Publisher = Owner.ConvertAddress(),
                Claimed = true,
                WantTokenBalance = 1_0000,
                SubscribedOfferingAmount = 10_00000000
            });

            CheckPublicOffering(new PublicOfferingOutput
            {
                OfferingTokenSymbol = ISTAR,
                OfferingTokenAmount = 100_00000000,
                WantTokenSymbol = USDT,
                WantTokenAmount = 10_0000,
                StartTime = startTime1,
                EndTime = endTime1,
                PublicId = 1,
                Publisher = Owner.ConvertAddress(),
                Claimed = true,
                WantTokenBalance = 10_0000,
                SubscribedOfferingAmount = 100_00000000
            });

            CheckUserInfo(0, User, new UserInfo
            {
                Claimed = true,
                ObtainAmount = 10_00000000
            });

            CheckUserInfo(1, User, new UserInfo
            {
                Claimed = false,
                ObtainAmount = 100_00000000
            });
        }

        private long GetBalance(string account, string symbol)
        {
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, account);
            _tokenContract = _genesisContract.GetTokenContract(account);
            var balance = _tokenContract.GetUserBalance(account, symbol);
            Logger.Info($"balance of {symbol} is {balance}");
            return balance;
        }

        private void Transfer(string from, string to, string symbol, long amount)
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

        private void CheckPublicOffering(PublicOfferingOutput expextPublicOffering)
        {
            var getPublicOffering =
                _idoContract.GetPublicOffering(new Int64Value {Value = expextPublicOffering.PublicId});
            Logger.Info($"getPublicOffering.OfferingTokenSymbol is {getPublicOffering.OfferingTokenSymbol}");
            Logger.Info($"getPublicOffering.OfferingTokenAmount is {getPublicOffering.OfferingTokenAmount}");
            Logger.Info($"getPublicOffering.WantTokenSymbol is {getPublicOffering.WantTokenSymbol}");
            Logger.Info($"getPublicOffering.WantTokenAmount is {getPublicOffering.WantTokenAmount}");
            Logger.Info($"getPublicOffering.StartTime is {getPublicOffering.StartTime}");
            Logger.Info($"getPublicOffering.EndTime is {getPublicOffering.EndTime}");
            Logger.Info($"getPublicOffering.PublicId is {getPublicOffering.PublicId}");
            Logger.Info($"getPublicOffering.Publisher is {getPublicOffering.Publisher}");
            Logger.Info($"getPublicOffering.Claimed is {getPublicOffering.Claimed}");
            Logger.Info($"getPublicOffering.WantTokenBalance is {getPublicOffering.WantTokenBalance}");
            Logger.Info($"getPublicOffering.SubscribedOfferingAmount is {getPublicOffering.SubscribedOfferingAmount}");

            getPublicOffering.OfferingTokenSymbol.ShouldBe(expextPublicOffering.OfferingTokenSymbol);
            getPublicOffering.OfferingTokenAmount.ShouldBe(expextPublicOffering.OfferingTokenAmount);
            getPublicOffering.WantTokenSymbol.ShouldBe(expextPublicOffering.WantTokenSymbol);
            getPublicOffering.WantTokenAmount.ShouldBe(expextPublicOffering.WantTokenAmount);
            getPublicOffering.StartTime.ShouldBe(expextPublicOffering.StartTime);
            getPublicOffering.EndTime.ShouldBe(expextPublicOffering.EndTime);
            getPublicOffering.PublicId.ShouldBe(expextPublicOffering.PublicId);
            getPublicOffering.Publisher.ShouldBe(expextPublicOffering.Publisher);
            getPublicOffering.Claimed.ShouldBe(expextPublicOffering.Claimed);
            getPublicOffering.WantTokenBalance.ShouldBe(expextPublicOffering.WantTokenBalance);
            getPublicOffering.SubscribedOfferingAmount.ShouldBe(expextPublicOffering.SubscribedOfferingAmount);
        }

        private void CheckUserInfo(int publicId, string user, UserInfo expectUserInfo)
        {
            var userInfo =
                _idoContract.GetUserInfo(publicId, user);
            Logger.Info($"userInfo.Claimed is {userInfo.Claimed}");
            Logger.Info($"userInfo.ObtainAmount is {userInfo.ObtainAmount}");

            userInfo.Claimed.ShouldBe(expectUserInfo.Claimed);
            userInfo.ObtainAmount.ShouldBe(expectUserInfo.ObtainAmount);
        }

        [TestMethod]
        public void Transfer()
        {
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, Owner);
            _tokenContract = _genesisContract.GetTokenContract(Owner);
            var account1BalanceBefore = _tokenContract.GetUserBalance(Owner, "ELF");
            var targetBalanceBefore = _tokenContract.GetUserBalance(InitAccount, "ELF");
            Logger.Info($"account1BalanceBefore is {account1BalanceBefore}");
            Logger.Info($"targetBalanceBefore is {targetBalanceBefore}");

            _tokenContract.TransferBalance(Owner, InitAccount, 10000_00000000, "ELF");
            var account1BalanceAfter = _tokenContract.GetUserBalance(Owner, "ELF");
            var targetBalanceAfter = _tokenContract.GetUserBalance(InitAccount, "ELF");
            Logger.Info($"account1BalanceAfter is {account1BalanceAfter}");
            Logger.Info($"targetBalanceAfter is {targetBalanceAfter}");
        }

        private TransactionResultDto AddPublicOffering(string publisher, Timestamp startTime, Timestamp endTime)
        {
            // get timeSpan
            var getTimespan = _idoContract.GetTimespan();
            Logger.Info($"MaxTimespan is {getTimespan.MaxTimespan}");
            Logger.Info($"MinTimespan is {getTimespan.MinTimespan}");

            _idoContract.SetAccount(publisher);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, publisher);
            _tokenContract = _genesisContract.GetTokenContract(publisher);
            var approve1 =
                _tokenContract.ApproveToken(publisher, _idoContract.ContractAddress, 10000000000_00000000, ISTAR);
            approve1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var offeringTokenSymbol = ISTAR;
            var offeringTokenAmount = 100_00000000;
            var wantTokenSymbol = USDT;
            var wantTokenAmount = 10_0000;
            var addPublicOffering = _idoContract.AddPublicOffering(offeringTokenSymbol, offeringTokenAmount,
                wantTokenSymbol,
                wantTokenAmount, startTime, endTime);
            return addPublicOffering;
        }
    }
}