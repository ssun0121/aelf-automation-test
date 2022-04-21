using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Sinodac.Contracts.DAC;
using Sinodac.Contracts.DACMarket;
using Sinodac.Contracts.Delegator;
using BuyInput = Sinodac.Contracts.Delegator.BuyInput;

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
        private List<string> RedeemCodeList { get; set; }

        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }

        private string InitAccount { get; } = "nn659b9X1BLhnu5RWmEUbuuV7J9QKVVSN54j9UmeCbF3Dve5D";
        private string AdminAccount { get; } = "YUW9zH5GhRboT5JK4vXp5BLAfCDv28rRmTQwo418FuaJmkSg8";
        private string BuyerAccount { get; } = "FHdcx45K5kovWsAKSb3rrdyNPFus8eoJ1XTQE7aXFHTgfpgzN";

        private string delegatorAddress = "xwfQDMdE5xCmyKcDDKV8EDTJdmVfSY6zxiojUxjYKvWpkeu65";
        private string DACContractAddress = "2QtXdKR1ap9Sxgvz3ksiozXx88xf12rfQhk7kNGYuamveDh1ZX";
        private string DACMarketContractAddress = "xhhLqDthzC4rmyNURwQpASKWWF6o2eJHbGF5j9H1MyxAxNLap";

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
        [DataRow(10, 1, "尚方宝剑54号")]
        public void CreateTest(long circulation, long reserveForLottery, string dacName)
        {
            var fromId = "北京故宫博物馆管理员";
            var creatorId = "北京故宫博物馆";
            var price = 9999;
            var dacType = "图片";
            var dacShape = "长方形(纵向)";
            var seriesName = "故宫尚方宝剑系列3";

            var dacProtocolInfo = _dacContract.GetDACProtocolInfo(dacName);
            if (dacProtocolInfo.DacName != "")
            {
                var result = _delegatorContract.CreateDAC(fromId, creatorId, dacName, price, circulation, dacType,
                    dacShape, reserveForLottery, seriesName);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("已经存在了");
            }
            else
            {
                var result = _delegatorContract.CreateDAC(fromId, creatorId, dacName, price, circulation, dacType,
                    dacShape, reserveForLottery, seriesName);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                // Check event: DACProtocolCreated
                var logs = result.Logs.First(l => l.Name.Equals("DACProtocolCreated")).Indexed;
                var index1 = DACProtocolCreated.Parser.ParseFrom(ByteString.FromBase64(logs[0]));
                index1.DacName.ShouldBe(dacName);
                var logs2 = result.Logs.First(l => l.Name.Equals("DACProtocolCreated")).NonIndexed;
                var dacProtocolCreated = DACProtocolCreated.Parser.ParseFrom(ByteString.FromBase64(logs2));
                dacProtocolCreated.DacProtocolInfo.CreatorUserId.ShouldBe(fromId);
                dacProtocolCreated.DacProtocolInfo.CreatorId.ShouldBe(creatorId);
                dacProtocolCreated.DacProtocolInfo.DacName.ShouldBe(dacName);
                dacProtocolCreated.DacProtocolInfo.Price.ShouldBe(price);
                dacProtocolCreated.DacProtocolInfo.DacType.ShouldBe(dacType);
                dacProtocolCreated.DacProtocolInfo.DacShape.ShouldBe(dacShape);
                dacProtocolCreated.DacProtocolInfo.ReserveForLottery.ShouldBe(reserveForLottery);
                dacProtocolCreated.DacProtocolInfo.ReserveFrom.ShouldNotBeNull();

                // Check event: ProtocolAdded
                var protocolAddedLogs = result.Logs.First(l => l.Name.Equals("ProtocolAdded")).Indexed;
                var protocolAddedIndex1 = ProtocolAdded.Parser.ParseFrom(ByteString.FromBase64(protocolAddedLogs[0]));
                protocolAddedIndex1.SeriesName.ShouldBe(seriesName);
                var protocolAddedIndex2 = ProtocolAdded.Parser.ParseFrom(ByteString.FromBase64(protocolAddedLogs[1]));
                protocolAddedIndex2.DacName.ShouldBe(dacName);

                dacProtocolInfo = _dacContract.GetDACProtocolInfo(dacName);
                Logger.Info($"\ndacProtocolInfo.CreatorUserId: {dacProtocolInfo.ReserveFrom}\n" +
                            $"dacProtocolInfo.CreatorId: {dacProtocolInfo.CreatorId}\n" +
                            $"dacProtocolInfo.DacName: {dacProtocolInfo.DacName}\n" +
                            $"dacProtocolInfo.Price: {dacProtocolInfo.Price}\n" +
                            $"dacProtocolInfo.Circulation: {dacProtocolInfo.Circulation}\n" +
                            $"dacProtocolInfo.DacType: {dacProtocolInfo.DacType}\n" +
                            $"dacProtocolInfo.DacShape: {dacProtocolInfo.DacShape}\n" +
                            $"dacProtocolInfo.ReserveForLottery: {dacProtocolInfo.ReserveForLottery}\n" +
                            $"dacProtocolInfo.ReserveFrom: {dacProtocolInfo.ReserveFrom}");
                var DACSeries = _dacMarketContract.GetDACSeries(seriesName);
                Logger.Info($"\nDACSeries.SeriesName: {DACSeries.SeriesName}\n" +
                            $"DACSeries.CollectionList: {DACSeries.CollectionList}\n" +
                            $"DACSeries.CollectionCount: {DACSeries.CollectionCount}");

                dacProtocolInfo.CreatorUserId.ShouldBe(fromId);
                dacProtocolInfo.CreatorId.ShouldBe(creatorId);
                dacProtocolInfo.DacName.ShouldBe(dacName);
                dacProtocolInfo.Price.ShouldBe(price);
                dacProtocolInfo.DacType.ShouldBe(dacType);
                dacProtocolInfo.DacShape.ShouldBe(dacShape);
                dacProtocolInfo.ReserveForLottery.ShouldBe(reserveForLottery);
                dacProtocolInfo.ReserveFrom.ShouldNotBeNull();

                DACSeries.SeriesName.ShouldBe(seriesName);
                DACSeries.CollectionList.Value.Contains(dacName);
                DACSeries.CollectionCount.ShouldBePositive();
            }
        }

        [TestMethod]
        public void CreateSeriesTest()
        {
            var fromId = "故宫博物馆管理员";
            var seriesDescription = "描述：故宫尚方宝剑";
            var creatorId = Guid.NewGuid().ToString();
            var seriesName = "故宫尚方宝剑系列3";

            var result = _delegatorContract.CreateSeries(fromId, seriesName, seriesDescription, creatorId);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var DACSeries = _dacMarketContract.GetDACSeries(seriesName);
            DACSeries.SeriesName.ShouldBe(seriesName);
            DACSeries.SeriesDescription.ShouldBe(seriesDescription);
            DACSeries.CreatorId.ShouldBe(creatorId);
            DACSeries.CreatorUserId.ShouldBe(fromId);
            DACSeries.CollectionList.Value.ShouldBeEmpty();
            DACSeries.CollectionCount.ShouldBe(0);
            DACSeries.CreateTime.ShouldNotBeNull();
        }

        [TestMethod]
        public void BindRedeemCodeTest()
        {
            var fromId = "故宫博物馆管理员";
            var dacName = "尚方宝剑45号";
            var dacProtocolInfo = _dacContract.GetDACProtocolInfo(dacName);
            var reserveFrom = (int) dacProtocolInfo.ReserveFrom;
            var reserveForLottery = dacProtocolInfo.ReserveForLottery;
            var fromDacId = reserveFrom;
            RedeemCodeList = Enumerable.Range(1, 10).Select(i => Guid.NewGuid().ToString()).ToList();
            var redeemCodeHashList = RedeemCodeList.Select(HashHelper.ComputeFrom).ToList();

            var result = _delegatorContract.BindRedeemCode(fromId, dacName, redeemCodeHashList.Take(2), fromDacId);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var isBindCompleted = _dacContract.IsBindCompleted(dacName);
            isBindCompleted.Value.ShouldBe(false);

            result = _delegatorContract.BindRedeemCode(fromId, dacName, redeemCodeHashList.Skip(2), fromDacId.Add(2));
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            isBindCompleted = _dacContract.IsBindCompleted(dacName);
            isBindCompleted.Value.ShouldBe(true);

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

            // Check event: RedeemCodeCreated
            var dacIdEvent = reserveFrom + 2;
            var DACInfoEvent = _dacContract.GetDACInfo(dacName, dacIdEvent);
            var redeemCodeCreatedLogs = result.Logs.First(l => l.Name.Equals("RedeemCodeCreated")).Indexed;
            var redeemCodeCreatedIndex1 =
                RedeemCodeCreated.Parser.ParseFrom(ByteString.FromBase64(redeemCodeCreatedLogs[0]));
            redeemCodeCreatedIndex1.DacName.ShouldBe(dacName);
            var protocolAddedIndex2 =
                RedeemCodeCreated.Parser.ParseFrom(ByteString.FromBase64(redeemCodeCreatedLogs[1]));
            protocolAddedIndex2.DacId.ShouldBe(dacIdEvent);
            var protocolAddedIndex3 =
                RedeemCodeCreated.Parser.ParseFrom(ByteString.FromBase64(redeemCodeCreatedLogs[2]));
            protocolAddedIndex3.RedeemCodeHash.ShouldBe(DACInfoEvent.RedeemCodeHash);

            // Check event: DACMinted
            var dacMintedLogs = result.Logs.First(l => l.Name.Equals("DACMinted")).Indexed;
            var dacMintedLogsIndex1 = DACMinted.Parser.ParseFrom(ByteString.FromBase64(dacMintedLogs[0]));
            dacMintedLogsIndex1.DacName.ShouldBe(dacName);
            var dacMintedLogsIndex2 = DACMinted.Parser.ParseFrom(ByteString.FromBase64(dacMintedLogs[1]));
            dacMintedLogsIndex2.FromDacId.ShouldBe(dacIdEvent);
            var dacMintedLogsIndex3 = DACMinted.Parser.ParseFrom(ByteString.FromBase64(dacMintedLogs[2]));
            dacMintedLogsIndex3.Quantity.ShouldBe(1);
        }

        [TestMethod]
        [DataRow("尚方宝剑53号")]
        public void BindRedeemCodeOnceTest(string dacName)
        {
            var fromId = "故宫博物馆管理员";
            var dacProtocolInfo = _dacContract.GetDACProtocolInfo(dacName);
            var reserveFrom = (int) dacProtocolInfo.ReserveFrom;
            var reserveForLottery = dacProtocolInfo.ReserveForLottery;
            var fromDacId = reserveFrom;
            RedeemCodeList = Enumerable.Range(1, (int) reserveForLottery).Select(i => Guid.NewGuid().ToString())
                .ToList();
            var redeemCodeHashList = RedeemCodeList.Select(HashHelper.ComputeFrom).ToList();

            var result = _delegatorContract.BindRedeemCode(fromId, dacName, redeemCodeHashList, fromDacId);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var isBindCompleted = _dacContract.IsBindCompleted(dacName);
            isBindCompleted.Value.ShouldBe(true);

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
        public void BindRedeemCodeErrorTest()
        {
            var fromId = "故宫博物馆管理员";
            var dacName = "尚方宝剑40号";
            var dacProtocolInfo = _dacContract.GetDACProtocolInfo(dacName);
            var reserveFrom = (int) dacProtocolInfo.ReserveFrom;
            var reserveForLottery = dacProtocolInfo.ReserveForLottery;
            var fromDacId = reserveFrom;
            RedeemCodeList = Enumerable.Range(1, 10).Select(i => Guid.NewGuid().ToString()).ToList();
            var redeemCodeHashList = RedeemCodeList.Select(HashHelper.ComputeFrom).ToList();

            {
                var result = _delegatorContract.BindRedeemCode(fromId, dacName, redeemCodeHashList, fromDacId);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("抽奖码给多了");
            }

            {
                var result = _delegatorContract.BindRedeemCode(fromId, dacName, redeemCodeHashList.Take(2), fromDacId);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                result = _delegatorContract.BindRedeemCode(fromId, dacName, redeemCodeHashList.Take(1), fromDacId);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("已经绑定过哈希值");
            }

            // 重新绑定新的抽奖码
            {
                var result = _delegatorContract.BindRedeemCode(fromId, dacName, redeemCodeHashList.Skip(2), fromDacId);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }
        }

        [TestMethod]
        public void AddProtocolToSeriesTest()
        {
            var fromId = "故宫博物馆管理员";
            var seriesName = "故宫尚方宝剑系列3";
            var dacName = "尚方宝剑41号";

            var DACSeriesBefore = _dacMarketContract.GetDACSeries(seriesName);
            DACSeriesBefore.SeriesName.ShouldBe(seriesName);
            var collectionCountBefore = (int) DACSeriesBefore.CollectionCount;

            var result = _delegatorContract.AddProtocolToSeries(fromId, seriesName, dacName);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var DACSeriesAfter = _dacMarketContract.GetDACSeries(seriesName);
            DACSeriesAfter.SeriesName.ShouldBe(seriesName);
            DACSeriesAfter.CreatorUserId.ShouldBe(fromId);
            DACSeriesAfter.CollectionList.Value[collectionCountBefore].ShouldBe(dacName);
            DACSeriesAfter.CollectionCount.ShouldBe(collectionCountBefore.Add(1));

            Logger.Info($"\nDACSeries.SeriesName: {DACSeriesAfter.SeriesName}\n" +
                        $"DACSeries.SeriesDescription: {DACSeriesAfter.SeriesDescription}\n" +
                        $"DACSeries.CreatorId: {DACSeriesAfter.CreatorId}\n" +
                        $"DACSeries.CreatorUserId: {DACSeriesAfter.CreatorUserId}\n" +
                        $"DACSeries.CollectionList: {DACSeriesAfter.CollectionList}\n" +
                        $"DACSeries.CollectionCount: {DACSeriesAfter.CollectionCount}\n" +
                        $"DACSeries.CreateTime: {DACSeriesAfter.CreateTime}");
        }

        [TestMethod]
        [DataRow("尚方宝剑46号", 1, 2)]
        public void MintDACTest(string dacName, long fromDacId, long quantity)
        {
            var fromId = "故宫博物馆管理员";
            ListDAC(fromId, dacName);

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

            var isMinted = _dacContract.IsMinted(dacName, fromDacId);
            isMinted.Value.ShouldBe(true);

            isMinted = _dacContract.IsMinted(dacName, fromDacId.Add(quantity).Sub(1));
            isMinted.Value.ShouldBe(true);

            // Check event: DACMinted
            var dacMintedLogs = result.Logs.First(l => l.Name.Equals("DACMinted")).Indexed;
            var dacMintedLogsIndex1 = DACMinted.Parser.ParseFrom(ByteString.FromBase64(dacMintedLogs[0]));
            dacMintedLogsIndex1.DacName.ShouldBe(dacName);
            var dacMintedLogsIndex2 = DACMinted.Parser.ParseFrom(ByteString.FromBase64(dacMintedLogs[1]));
            dacMintedLogsIndex2.FromDacId.ShouldBe(fromDacId);
            var dacMintedLogsIndex3 = DACMinted.Parser.ParseFrom(ByteString.FromBase64(dacMintedLogs[2]));
            dacMintedLogsIndex3.Quantity.ShouldBe(quantity);
        }

        [TestMethod]
        [DataRow(1, 10)]
        public void MintDACOtherTest(long fromDacId, long quantity)
        {
            var fromId = "故宫博物馆管理员";
            var dacName = "尚方宝剑53号";
            ListDAC(fromId, dacName);

            var result = _delegatorContract.MintDAC(fromId, dacName, fromDacId, quantity);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var dacProtocolInfo = _dacContract.GetDACProtocolInfo(dacName);
            var circulation = dacProtocolInfo.Circulation;
            var reserveFrom = dacProtocolInfo.ReserveFrom;
            var reserveForLottery = dacProtocolInfo.ReserveForLottery;

            if (quantity == 0 || quantity >= circulation)
            {
                quantity = (int) circulation;
            }

            for (int i = (int) fromDacId; i < fromDacId.Add(quantity); i++)
            {
                var DACInfo = _dacContract.GetDACInfo(dacName, i);
                DACInfo.DacName.ShouldBe(dacName);
                DACInfo.DacId.ShouldBe(i);
                DACInfo.DacHash.ShouldNotBeNull();
                if (i >= reserveFrom && i < reserveFrom.Add(reserveForLottery))
                {
                    DACInfo.RedeemCodeHash.ShouldNotBeNull();
                }
                else
                {
                    DACInfo.RedeemCodeHash.ShouldBeNull();
                }

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
            var dacName = "尚方宝剑53号";
            var dacId = 1;
            var dacProtocolInfo = _dacContract.GetDACProtocolInfo(dacName);
            var price = dacProtocolInfo.Price;
            var user = "张三";
            var userAddress = _delegatorContract.CalculateUserAddress(user);
            var balance = _dacContract.GetBalance(userAddress, dacName);
            var isOwner = _dacContract.IsOwner(userAddress, dacName, dacId);
            Logger.Info($"userAddress: {userAddress}");
            Logger.Info($"balance: {balance}");
            Logger.Info($"isOwner: {isOwner}");

            var result = _delegatorContract.Buy(user, dacName, dacId, price);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var balanceAfter = _dacContract.GetBalance(userAddress, dacName);
            Logger.Info($"balanceAfter: {balanceAfter}");
            balanceAfter.Owner.ShouldBe(userAddress);
            balanceAfter.DacName.ShouldBe(dacName);
            balanceAfter.Balance.ShouldBe(dacId);
            balanceAfter.Balance.ShouldBe(balance.Balance + 1);
        }

        [TestMethod]
        public void BuyDACErrorTest()
        {
            var fromId = "故宫博物馆管理员";
            var dacName = "尚方宝剑号66号";
            var dacId = 1;
            var dacProtocolInfo = _dacContract.GetDACProtocolInfo(dacName);
            var price = dacProtocolInfo.Price;
            var user = "张三";
            var circulation = 100;
            var reserveForLottery = 1;

            CreateTest(circulation, reserveForLottery, dacName);
            // 检查是否上架,是否到上架时间
            {
                var result = _delegatorContract.Buy(user, dacName, dacId, price);
                result.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("没有设置上架时间");
            }

            ListDAC(fromId, dacName);
            // 检查是否绑定兑换码
            {
                var isBindCompleted = _dacContract.IsBindCompleted(dacName);
                if (!isBindCompleted.Value)
                {
                    var result = _delegatorContract.Buy(user, dacName, dacId, price);
                    result.Status.ConvertTransactionResultStatus()
                        .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                    result.Error.ShouldContain("兑换码还没有完成绑定");
                }
            }

            // 检查是否存在DAC
            {
                var dacNameReName = "不存在的DAC";
                var result = _delegatorContract.Buy(user, dacNameReName, dacId, price);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain($"查无此DAC：{dacNameReName}");
            }

            BindRedeemCodeOnceTest(dacName);
            {
                var result = _delegatorContract.Buy(user, dacName, dacId, price);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("还没有mint到初始地址");
            }

            MintDACTest(dacName, 1, 2);
            // 检查是否转移
            {
                var result = _delegatorContract.Buy(user, dacName, dacId, price);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                result = _delegatorContract.Buy("李四", dacName, dacId, price);
                result.Error.ShouldContain("已经从初始地址转给");

                result = _delegatorContract.Buy(user, dacName, dacId, price);
                result.Error.ShouldContain("已经从初始地址转给");
            }
        }

        [TestMethod]
        public void BuyDACWithoutMintTest()
        {
            var fromId = "故宫博物馆管理员";
            var dacName = "尚方宝剑54号";
            var dacId = 1;
            var dacProtocolInfo = _dacContract.GetDACProtocolInfo(dacName);
            var price = dacProtocolInfo.Price;
            var user = "张三";
            var userAddress = _delegatorContract.CalculateUserAddress(user);

            // 检查是否mint
            var isMinted = _dacContract.IsMinted(dacName, dacId);
            if (isMinted.Value)
            {
                var result = _delegatorContract.Buy(user, dacName, dacId, price);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                result = _delegatorContract.Buy("李四", dacName, dacId, price);
                result.Error.ShouldContain("已经从初始地址转给");

                result = _delegatorContract.Buy(user, dacName, dacId, price);
                result.Error.ShouldContain("已经从初始地址转给");
            }
            else
            {
                var result = _delegatorContract.Buy(user, dacName, dacId, price);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("还没有mint到初始地址");
            }
        }

        [TestMethod]
        public void RedeemTest()
        {
            var fromId = "故宫博物馆管理员";
            var dacName = "尚方宝剑3号";
            var dacProtocolInfo = _dacContract.GetDACProtocolInfo(dacName);
            var reserveFrom = dacProtocolInfo.ReserveFrom;
            var dacId = reserveFrom;
            var dacInfo = _dacContract.GetDACInfo(dacName, dacId);
            var redeemCode = dacInfo.RedeemCodeHash.ToString();
            var user = "张三";
            var redeemCodeDAC = _dacContract.GetRedeemCodeDAC(dacInfo.RedeemCodeHash);
            Logger.Info($"\redeemCodeDAC.DacName: {redeemCodeDAC.DacName}\n" +
                        $"redeemCodeDAC.DacId: {redeemCodeDAC.DacId}\n" +
                        $"redeemCodeDAC.DacHash: {redeemCodeDAC.DacHash}\n" +
                        $"redeemCodeDAC.RedeemCodeHash: {redeemCodeDAC.RedeemCodeHash}");
            var userAddress = _delegatorContract.CalculateUserAddress(user);
            var balance = _dacContract.GetBalance(userAddress, dacName);
            var result = _delegatorContract.Redeem("张三", "8fe8803b-fd1d-475c-99de-d2a18a0fb8dc");
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var balanceAfter = _dacContract.GetBalance(userAddress, dacName);
            Logger.Info($"balanceAfter: {balanceAfter}");
            balanceAfter.Owner.ShouldBe(userAddress);
            balanceAfter.DacName.ShouldBe(dacName);
            balanceAfter.Balance.ShouldBe(balance.Balance + 1);
        }

        [TestMethod]
        public void GiveTest()
        {
            var fromId = "故宫博物馆管理员";
            var dacName = "尚方宝剑3号";
            var dacProtocolInfo = _dacContract.GetDACProtocolInfo(dacName);
            var reserveFrom = dacProtocolInfo.ReserveFrom;
            var dacId = reserveFrom;
            var dacInfo = _dacContract.GetDACInfo(dacName, dacId);
            var redeemCode = dacInfo.RedeemCodeHash.ToString();
            var user1 = "张三";
            var user2 = "李四";
            var user1Address = _delegatorContract.CalculateUserAddress(user1);
            var user2Address = _delegatorContract.CalculateUserAddress(user2);
            var user1BalanceBefore = _dacContract.GetBalance(user1Address, dacName);
            var user2BalanceBefore = _dacContract.GetBalance(user2Address, dacName);
            Logger.Info($"user1BalanceBefore: {user1BalanceBefore}");
            Logger.Info($"user2BalanceBefore: {user2BalanceBefore}");

            var result = _delegatorContract.Give(user1, user2, dacName, dacId);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var user1BalanceAfter = _dacContract.GetBalance(user1Address, dacName);
            var user2BalanceAfter = _dacContract.GetBalance(user2Address, dacName);
            Logger.Info($"user1BalanceAfter: {user1BalanceAfter}");
            Logger.Info($"user2BalanceAfter: {user2BalanceAfter}");
            user1BalanceAfter.Balance.ShouldBe(user1BalanceBefore.Balance - 1);
            user2BalanceAfter.Balance.ShouldBe(user2BalanceBefore.Balance + 1);
        }

        private void ListDAC(string fromId, string dacName)
        {
            AuditDACTest(fromId, dacName);
            var publicTime = DateTime.UtcNow.AddSeconds(5).ToTimestamp();

            var result = _delegatorContract.ListDAC(fromId, dacName, publicTime);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var dacPublicTime = _dacMarketContract.GetPublicTime(dacName);
            dacPublicTime.ShouldBe(publicTime);
            Logger.Info($"dacPublicTime:{dacPublicTime.Seconds}");
        }

        [TestMethod]
        public void DelistDAC()
        {
            var fromId = "故宫博物馆管理员";
            var dacName = "尚方宝剑1号";
            var result = _delegatorContract.DelistDAC(fromId, dacName);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        public void AuditDACTest(string fromId, string dacName)
        {
            var isApprove = true;
            var result = _delegatorContract.AuditDAC(fromId, dacName, isApprove);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var isDACProtocolApproved = _dacContract.IsDACProtocolApproved(dacName);
            isDACProtocolApproved.Value.ShouldBe(isApprove);

            // Check event: DACProtocolApproved
            var logs = result.Logs.First(l => l.Name.Equals("DACProtocolApproved")).Indexed;
            var index1 = DACProtocolApproved.Parser.ParseFrom(ByteString.FromBase64(logs[0]));
            index1.DacName.ShouldBe(dacName);
        }
    }
}