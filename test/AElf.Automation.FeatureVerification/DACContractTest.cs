using System;
using System.Linq;
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
    public class DelegatorForwardContractTest
    {
        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;
        private DelegatorContract _delegatorContract;
        private DACContract _dacContract;
        private DacMarketContract _dacMarketContract;

        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }

        private string InitAccount { get; } = "nn659b9X1BLhnu5RWmEUbuuV7J9QKVVSN54j9UmeCbF3Dve5D";
        private string AdminAccount { get; } = "YUW9zH5GhRboT5JK4vXp5BLAfCDv28rRmTQwo418FuaJmkSg8";
        private string BuyerAccount { get; } = "FHdcx45K5kovWsAKSb3rrdyNPFus8eoJ1XTQE7aXFHTgfpgzN";

        private string delegatorAddress = "2TXvtjgTiMwjvEyWGEvfbeQ9P6zVK55pTPcmzvLFBDCMLNUYXV";
        private string DACContractAddress = "2sFCkQs61YKVkHpN3AT7887CLfMvzzXnMkNYYM431RK5tbKQS9";
        private string DACMarketContractAddress = "2RvZEzZTrj5BXEuMtHmfVDn2fLwKdBYD7CT5LxBCccqwGN7akY";

        private static string RpcUrl { get; } = "192.168.67.166:8000";

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("DACContractTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env2-main");

            NodeManager = new NodeManager(RpcUrl);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);

            _delegatorContract = delegatorAddress == ""
                ? new DelegatorContract(NodeManager, InitAccount)
                : new DelegatorContract(NodeManager, InitAccount, delegatorAddress);

            _dacContract = DACContractAddress == ""
                ? new DACContract(NodeManager, InitAccount)
                : new DACContract(NodeManager, InitAccount, DACContractAddress);

            _dacMarketContract = DACMarketContractAddress == ""
                ? new DacMarketContract(NodeManager, InitAccount)
                : new DacMarketContract(NodeManager, InitAccount, DACMarketContractAddress);
        }

        [TestMethod]
        public void InitializeTest()
        {
            var result = _delegatorContract.Initialize(AdminAccount, _dacContract.ContractAddress,
                _dacMarketContract.ContractAddress);

            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        [DataRow(10000, 10)]
        public void CreateTest(long circulation, long reserveForLottery)
        {
            var fromId = Guid.NewGuid().ToString();
            var creatorId = Guid.NewGuid().ToString();
            var dacName = "尚方宝剑28号";
            var price = 9999;
            var dacType = "图片";
            var dacShape = "长方形(纵向)";
            var seriesName = "故宫尚方宝剑";

            var result = _delegatorContract.CreateDAC(fromId, creatorId, dacName, price, circulation, dacType, dacShape,
                reserveForLottery, seriesName);

            if (reserveForLottery >= 0 && reserveForLottery <= circulation)
            {
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }
            else if (reserveForLottery > circulation)
            {
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("");
            }

            var dacProtocolInfo = _dacContract.GetDACProtocolInfo(dacName);
            Logger.Info($"\ndacProtocolInfo.CreatorUserId: {dacProtocolInfo.CreatorUserId}\n" +
                        $"dacProtocolInfo.CreatorId: {dacProtocolInfo.CreatorId}\n" +
                        $"dacProtocolInfo.DacName: {dacProtocolInfo.DacName}\n" +
                        $"dacProtocolInfo.Price: {dacProtocolInfo.Price}\n" +
                        $"dacProtocolInfo.Circulation: {dacProtocolInfo.Circulation}\n" +
                        $"dacProtocolInfo.DacType: {dacProtocolInfo.DacType}\n" +
                        $"dacProtocolInfo.DacShape: {dacProtocolInfo.DacShape}\n" +
                        $"dacProtocolInfo.ReserveForLottery: {dacProtocolInfo.ReserveForLottery}\n" +
                        $"dacProtocolInfo.ReserveFrom: {dacProtocolInfo.ReserveFrom}");
        }

        [TestMethod]
        public void CreateSeriesTest()
        {
            var fromId = "故宫博物馆管理员";
            var seriesName = "故宫尚方宝剑";
            var seriesDescription = "描述：故宫尚方宝剑";
            var creatorId = Guid.NewGuid().ToString();

            var result = _delegatorContract.CreateSeries(fromId, seriesName, seriesDescription, creatorId);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var DACSeries = _dacMarketContract.GetDACSeries(seriesName);
            DACSeries.SeriesName.ShouldBe(seriesName);
            DACSeries.SeriesDescription.ShouldBe(seriesDescription);
            DACSeries.CreatorId.ShouldBe(creatorId);
            DACSeries.CreatorUserId.ShouldBe(fromId);
            DACSeries.CollectionList.Value[0].ShouldBeEmpty();
            DACSeries.CollectionCount.ShouldBe(1);
            // DACSeries.CreateTime.ShouldBe();

            Logger.Info($"\nDACSeries.SeriesName: {DACSeries.SeriesName}\n" +
                        $"DACSeries.SeriesDescription: {DACSeries.SeriesDescription}\n" +
                        $"DACSeries.CreatorId: {DACSeries.CreatorId}\n" +
                        $"DACSeries.CreatorUserId: {DACSeries.CreatorUserId}\n" +
                        $"DACSeries.CollectionList: {DACSeries.CollectionList}\n" +
                        $"DACSeries.CollectionCount: {DACSeries.CollectionCount}\n" +
                        $"DACSeries.CreateTime: {DACSeries.CreateTime}");
        }

        [TestMethod]
        public void BindRedeemCodeTest()
        {
            var fromId = "故宫博物馆管理员";
            var dacName = "尚方宝剑28号";
            var skip = 0;

            var result = _delegatorContract.BindRedeemCode(fromId, dacName, skip);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var isBindCompleted = _dacContract.IsBindCompleted(dacName);
            // isBindCompleted.Value.ShouldBe(true);
            Logger.Info($"isBindCompleted: {isBindCompleted}");

            var dacProtocolInfo = _dacContract.GetDACProtocolInfo(dacName);
            var reserveFrom = (int) dacProtocolInfo.ReserveFrom;
            var reserveForLottery = dacProtocolInfo.ReserveForLottery;
            for (int i = reserveFrom; i < reserveFrom + reserveForLottery; i++)
            {
                var DACInfo = _dacContract.GetDACInfo(dacName, i);
                DACInfo.DacName.ShouldBe(dacName);
                DACInfo.DacId.ShouldBe(i);
                DACInfo.DacHash.ShouldNotBeNull();
                DACInfo.RedeemCodeHash.ShouldNotBeNull();
                Logger.Info($"\nDACInfo.DacName: {DACInfo.DacName}\n" +
                            $"DACInfo.DacId: {DACInfo.DacId}\n" +
                            $"DACInfo.DacHash: {DACInfo.DacHash}\n" +
                            $"DACInfo.RedeemCodeHash: {DACInfo.RedeemCodeHash}");
            }
        }

        [TestMethod]
        public void AddProtocolToSeriesTest()
        {
            var fromId = "故宫博物馆管理员";
            var seriesName = "故宫尚方宝剑";
            var dacName = "尚方宝剑1号";

            var result = _delegatorContract.AddProtocolToSeries(fromId, seriesName, dacName);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var DACSeries = _dacMarketContract.GetDACSeries(seriesName);
            DACSeries.SeriesName.ShouldBe(seriesName);
            DACSeries.CreatorUserId.ShouldBe(fromId);
            DACSeries.CollectionList.Value[0].ShouldBe(dacName);
            DACSeries.CollectionCount.ShouldBe(1);

            Logger.Info($"\nDACSeries.SeriesName: {DACSeries.SeriesName}\n" +
                        $"DACSeries.SeriesDescription: {DACSeries.SeriesDescription}\n" +
                        $"DACSeries.CreatorId: {DACSeries.CreatorId}\n" +
                        $"DACSeries.CreatorUserId: {DACSeries.CreatorUserId}\n" +
                        $"DACSeries.CollectionList: {DACSeries.CollectionList}\n" +
                        $"DACSeries.CollectionCount: {DACSeries.CollectionCount}\n" +
                        $"DACSeries.CreateTime: {DACSeries.CreateTime}");
        }

        [TestMethod]
        public void AuditDACTest()
        {
            var fromId = "故宫博物馆管理员";
            var dacName = "尚方宝剑28号";
            var isApprove = true;

            var result = _delegatorContract.AuditDAC(fromId, dacName, isApprove);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var isDACProtocolApproved = _dacContract.IsDACProtocolApproved(dacName);
            isDACProtocolApproved.Value.ShouldBe(isApprove);
        }

        [TestMethod]
        public void MintDACTest()
        {
            var fromId = "故宫博物馆管理员";
            var dacName = "尚方宝剑28号";
            var fromDacId = 1;
            var quantity = 1;

            var result = _delegatorContract.MintDAC(fromId, dacName, fromDacId, quantity);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            for (int i = 1; i <= quantity; i++)
            {
                var DACInfo = _dacContract.GetDACInfo(dacName, i);
                DACInfo.DacName.ShouldBe(dacName);
                DACInfo.DacId.ShouldBe(i);
                DACInfo.DacHash.ShouldNotBeNull();
                DACInfo.RedeemCodeHash.ShouldBeNull();
                Logger.Info($"\nDACInfo.DacName: {DACInfo.DacName}\n" +
                            $"DACInfo.DacId: {DACInfo.DacId}\n" +
                            $"DACInfo.DacHash: {DACInfo.DacHash}\n" +
                            $"DACInfo.RedeemCodeHash: {DACInfo.RedeemCodeHash}");
            }
        }

        [TestMethod]
        public void BuyDACTest()
        {
            var fromId = "故宫博物馆管理员";
            var dacName = "尚方宝剑28号";
            var dacId = 1;
            var dacProtocolInfo = _dacContract.GetDACProtocolInfo(dacName);
            var price = dacProtocolInfo.Price;

            var userAddress = _delegatorContract.CalculateUserAddress(dacName);
            var balance = _dacContract.GetBalance(userAddress, dacName);
            var isOwner = _dacContract.IsOwner(userAddress, dacName, dacId);
            Logger.Info($"userAddress: {userAddress}");
            Logger.Info($"balance: {balance}");
            Logger.Info($"isOwner: {isOwner}");

            var result = _delegatorContract.Buy("张三", dacName, dacId, price);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void RedeemTest()
        {
            var fromId = "故宫博物馆管理员";
            var dacName = "尚方宝剑28号";
            var dacId = 1;
            var dacProtocolInfo = _dacContract.GetDACProtocolInfo(dacName);

            var userAddress = _delegatorContract.CalculateUserAddress(dacName);
            var balance = _dacContract.GetBalance(userAddress, dacName);
            Logger.Info($"userAddress: {userAddress}");
            Logger.Info($"balance: {balance}");

            var redeemCode = "";
            var result = _delegatorContract.Redeem("张三", redeemCode);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void ListDACTest()
        {
            var fromId = "故宫博物馆管理员";
            var dacName = "尚方宝剑28号";
            var publicTime = DateTime.UtcNow.AddSeconds(5).ToTimestamp();

            var result = _delegatorContract.ListDAC(fromId, dacName, publicTime);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void DelistDACTest()
        {
            var fromId = "故宫博物馆管理员";
            var dacName = "尚方宝剑1号";

            var result = _delegatorContract.DelistDAC(fromId, dacName);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
    }
}