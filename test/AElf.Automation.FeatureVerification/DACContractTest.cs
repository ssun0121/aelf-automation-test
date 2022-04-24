using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class DelegatorForwardContractTest
    {
        private DelegatorContract _delegatorContract;
        private DACContract _dacContract;
        private DacMarketContract _dacMarketContract;
        private List<string> RedeemCodeList { get; set; }

        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }

        private string InitAccount { get; } = "nn659b9X1BLhnu5RWmEUbuuV7J9QKVVSN54j9UmeCbF3Dve5D";
        private string AdminAccount { get; } = "YUW9zH5GhRboT5JK4vXp5BLAfCDv28rRmTQwo418FuaJmkSg8";

        private string delegatorAddress = "2CUt9QP4SH25GubsBiVvW17nAwfyHRSMkk8GQ6r7Ez5aUJttG5";
        private string DACContractAddress = "vqRuJR3LDDMHbrgqaLmsLAhKSQgrbH1r5xrs4aVDx9EezViGF";
        private string DACMarketContractAddress = "62j1oMP2D8y4f6YHHL9WyhdtcFiLhMtrs7tBqXkMwJudz9AY5";

        private static string RpcUrl { get; } = "192.168.67.166:8000";

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("DACContractTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env2-main");

            NodeManager = new NodeManager(RpcUrl);

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
        [DataRow(10000, 10, "尚方宝剑32号")]
        public void CreateTest(long circulation, long reserveForLottery, string dacName)
        {
            var fromId = "北京故宫博物馆管理员";
            var creatorId = "北京故宫博物馆";
            var price = 9999;
            var dacType = "图片";
            var dacShape = "长方形(纵向)";
            var seriesName = "故宫尚方宝剑系列1";

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
            var seriesName = "故宫尚方宝剑系列1";

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
        [DataRow("尚方宝剑31号")]
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

            for (int i = 0; i < reserveForLottery; i++)
            {
                Logger.Info($"\nRedeemCodeList{i}: {RedeemCodeList[i]}");
            }
        }

        [TestMethod]
        public void BindRedeemCodeErrorTest()
        {
            var fromId = "故宫博物馆管理员";
            var dacName = "尚方宝剑11号";
            var dacProtocolInfo = _dacContract.GetDACProtocolInfo(dacName);
            var reserveFrom = (int) dacProtocolInfo.ReserveFrom;
            var reserveForLottery = dacProtocolInfo.ReserveForLottery;
            var fromDacId = reserveFrom;
            RedeemCodeList = Enumerable.Range(1, 11).Select(i => Guid.NewGuid().ToString()).ToList();
            var redeemCodeHashList = RedeemCodeList.Select(HashHelper.ComputeFrom).ToList();

            {
                var result = _delegatorContract.BindRedeemCode(fromId, dacName, redeemCodeHashList.Take(11), fromDacId);
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
                var result =
                    _delegatorContract.BindRedeemCode(fromId, dacName, redeemCodeHashList.Skip(2).Take(2), fromDacId);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("已经绑定过哈希值");
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
        [DataRow("尚方宝剑23号", 1, 10)]
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
        [DataRow(4, 2)]
        public void MintDACOtherTest(long fromDacId, long quantity)
        {
            var fromId = "故宫博物馆管理员";
            var dacName = "尚方宝剑9号";
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
            var dacName = "尚方宝剑23号";
            var dacId = 3;
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

            // Check event: DACSold
            var logs = result.Logs.First(l => l.Name.Equals("DACSold")).Indexed;
            var index1 = DACSold.Parser.ParseFrom(ByteString.FromBase64(logs[0]));
            index1.UserId.ShouldBe(user);
            var index2 = DACSold.Parser.ParseFrom(ByteString.FromBase64(logs[1]));
            index2.DacName.ShouldBe(dacName);
            var index3 = DACSold.Parser.ParseFrom(ByteString.FromBase64(logs[2]));
            index3.DacId.ShouldBe(dacId);
            var index4 = DACSold.Parser.ParseFrom(ByteString.FromBase64(logs[3]));
            index4.Price.ShouldBe(price);
            var index5 = DACSold.Parser.ParseFrom(ByteString.FromBase64(logs[4]));
            index5.UserAddress.ShouldBe(userAddress);
        }

        [TestMethod]
        public void BuyDACErrorTest()
        {
            var fromId = "故宫博物馆管理员";
            var dacName = "尚方宝剑号23号";
            var dacId = 1;
            var dacProtocolInfo = _dacContract.GetDACProtocolInfo(dacName);
            var price = dacProtocolInfo.Price;
            var user = "张三";
            var circulation = 100;
            var reserveForLottery = 1;

            CreateTest(circulation, reserveForLottery, dacName);
            ListDAC(fromId, dacName);

            Thread.Sleep(10 * 1000);
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

            MintDACTest(dacName, 1, 10);
            // 检查是否转移
            {
                var result = _delegatorContract.Buy(user, dacName, dacId, price);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                result = _delegatorContract.Buy("李四", dacName, dacId, price);
                result.Error.ShouldContain("已经从初始地址转给");
            }

            {
                Thread.Sleep(30 * 1000);
                var result = _delegatorContract.Buy(user, dacName, dacId, price);
                // result.Error.ShouldContain("已经从初始地址转给");
            }
        }

        [TestMethod]
        public void BuyDACPublishTimeTest()
        {
            var fromId = "故宫博物馆管理员";
            var dacName = "尚方宝剑号13号";
            var dacId = 1;
            var dacProtocolInfo = _dacContract.GetDACProtocolInfo(dacName);
            var price = dacProtocolInfo.Price;
            var user = "张三";
            var circulation = 100;
            var reserveForLottery = 1;

            CreateTest(circulation, reserveForLottery, dacName);

            // 检查是否上架
            {
                var result = _delegatorContract.Buy(user, dacName, dacId, price);
                result.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("没有设置上架时间");
            }

            // 是否到上架时间
            {
                AuditDACTest(fromId, dacName);
                var publicTime = DateTime.UtcNow.AddYears(1).ToTimestamp();

                var result = _delegatorContract.ListDAC(fromId, dacName, publicTime);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                result = _delegatorContract.Buy(user, dacName, dacId, price);
                result.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("还没有上架");
            }
        }

        [TestMethod]
        [DataRow("尚方宝剑31号", "534fb5b5-0abf-4633-b9df-608bc2c7ed45")]
        public void RedeemTest(string dacName, string redeemCode)
        {
            var user = "张三";
            var dacProtocolInfo = _dacContract.GetDACProtocolInfo(dacName);
            var reserveFrom = dacProtocolInfo.ReserveFrom;
            var dacId = reserveFrom;
            var dacInfo = _dacContract.GetDACInfo(dacName, dacId);
            var redeemCodeDAC = _dacContract.GetRedeemCodeDAC(dacInfo.RedeemCodeHash);
            Logger.Info($"\nredeemCodeDAC.DacName: {redeemCodeDAC.DacName}\n" +
                        $"redeemCodeDAC.DacId: {redeemCodeDAC.DacId}\n" +
                        $"redeemCodeDAC.DacHash: {redeemCodeDAC.DacHash}\n" +
                        $"redeemCodeDAC.RedeemCodeHash: {redeemCodeDAC.RedeemCodeHash}");
            var userAddress = _delegatorContract.CalculateUserAddress(user);
            var balance = _dacContract.GetBalance(userAddress, dacName);

            var result = _delegatorContract.Redeem("张三", redeemCode);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var balanceAfter = _dacContract.GetBalance(userAddress, dacName);
            Logger.Info($"balanceAfter: {balanceAfter}");
            balanceAfter.Owner.ShouldBe(userAddress);
            balanceAfter.DacName.ShouldBe(dacName);
            balanceAfter.Balance.ShouldBe(balance.Balance + 1);

            // Check event: CodeRedeemed
            var logs = result.Logs.First(l => l.Name.Equals("CodeRedeemed")).Indexed;
            var index1 = CodeRedeemed.Parser.ParseFrom(ByteString.FromBase64(logs[0]));
            index1.UserId.ShouldBe(user);
            var index2 = CodeRedeemed.Parser.ParseFrom(ByteString.FromBase64(logs[1]));
            index2.DacName.ShouldBe(dacName);
            var index3 = CodeRedeemed.Parser.ParseFrom(ByteString.FromBase64(logs[2]));
            index3.RedeemCode.ShouldBe(redeemCode);
            var index4 = CodeRedeemed.Parser.ParseFrom(ByteString.FromBase64(logs[3]));
            index4.DacId.ShouldBe(dacId);
            var index5 = CodeRedeemed.Parser.ParseFrom(ByteString.FromBase64(logs[4]));
            index5.UserAddress.ShouldBe(userAddress);
        }

        [TestMethod]
        [DataRow("尚方宝剑31号", "534fb5b5-0abf-4633-b9df-608bc2c7ed45")]
        public void RedeemErrorTest(string dacName, string redeemCode)
        {
            var user = "张三";
            var dacProtocolInfo = _dacContract.GetDACProtocolInfo(dacName);
            var reserveFrom = dacProtocolInfo.ReserveFrom;
            var dacId = reserveFrom;
            var dacInfo = _dacContract.GetDACInfo(dacName, dacId);
            var redeemCodeDAC = _dacContract.GetRedeemCodeDAC(dacInfo.RedeemCodeHash);
            Logger.Info($"\nredeemCodeDAC.DacName: {redeemCodeDAC.DacName}\n" +
                        $"redeemCodeDAC.DacId: {redeemCodeDAC.DacId}\n" +
                        $"redeemCodeDAC.DacHash: {redeemCodeDAC.DacHash}\n" +
                        $"redeemCodeDAC.RedeemCodeHash: {redeemCodeDAC.RedeemCodeHash}");

            // 检查兑换码是否有效
            {
                var redeemCodeNew = "xxxxxxxx-201b-4b81-93a6-9a8fcf79547f";
                var result = _delegatorContract.Redeem("张三", redeemCodeNew);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                // result.Error.ShouldContain($"兑换码 {redeemCode} 无效");
            }

            // 检查是否上架
            {
                var result = _delegatorContract.Redeem("张三", redeemCode);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain($"{dacInfo.DacName} 还没有上架");
            }

            // 上架
            ListDAC("故宫博物馆管理员", dacName);

            Thread.Sleep(10 * 1000);
            // 检查是否重复兑换
            {
                // 兑换
                var result = _delegatorContract.Redeem("张三", redeemCode);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                // 重复兑换
                result = _delegatorContract.Redeem("李四", redeemCode);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain($"已经从初始地址转给");
            }
        }

        [TestMethod]
        public void GiveTest()
        {
            var fromId = "故宫博物馆管理员";
            var dacName = "尚方宝剑23号";
            var dacId = 1;
            var user1 = "张三";
            var user2 = "李四";
            var user1Address = _delegatorContract.CalculateUserAddress(user1);
            var user2Address = _delegatorContract.CalculateUserAddress(user2);
            var user1BalanceBefore = _dacContract.GetBalance(user1Address, dacName);
            var user2BalanceBefore = _dacContract.GetBalance(user2Address, dacName);
            Logger.Info($"user1BalanceBefore: {user1BalanceBefore}");
            Logger.Info($"user2BalanceBefore: {user2BalanceBefore}");

            var isOwner = _dacContract.IsOwner(user1Address, dacName, dacId);
            Logger.Info($"isOwner: {isOwner}");

            if (isOwner.Value)
            {
                var result = _delegatorContract.Give(user1, user1, dacName, dacId);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var user1BalanceAfter1 = _dacContract.GetBalance(user1Address, dacName);
                var user2BalanceAfter1 = _dacContract.GetBalance(user2Address, dacName);
                Logger.Info($"user1BalanceAfter1: {user1BalanceAfter1}");
                Logger.Info($"user2BalanceAfter1: {user2BalanceAfter1}");
                user1BalanceAfter1.Balance.ShouldBe(user1BalanceBefore.Balance);
                user2BalanceAfter1.Balance.ShouldBe(user2BalanceBefore.Balance);

                result = _delegatorContract.Give(user1, user2, dacName, dacId);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var user1BalanceAfter = _dacContract.GetBalance(user1Address, dacName);
                var user2BalanceAfter = _dacContract.GetBalance(user2Address, dacName);
                Logger.Info($"user1BalanceAfter: {user1BalanceAfter}");
                Logger.Info($"user2BalanceAfter: {user2BalanceAfter}");
                user1BalanceAfter.Balance.ShouldBe(user1BalanceBefore.Balance - 1);
                user2BalanceAfter.Balance.ShouldBe(user2BalanceBefore.Balance + 1);

                // Check event: DACTransferred
                var logs = result.Logs.First(l => l.Name.Equals("DACTransferred")).Indexed;
                var index1 = DACTransferred.Parser.ParseFrom(ByteString.FromBase64(logs[0]));
                index1.From.ShouldBe(user1Address);
                var index2 = DACTransferred.Parser.ParseFrom(ByteString.FromBase64(logs[1]));
                index2.To.ShouldBe(user2Address);
                var index3 = DACTransferred.Parser.ParseFrom(ByteString.FromBase64(logs[2]));
                index3.DacName.ShouldBe(dacName);
                var index4 = DACTransferred.Parser.ParseFrom(ByteString.FromBase64(logs[3]));
                index4.DacId.ShouldBe(dacId);
            }
            else
            {
                var result = _delegatorContract.Give(user1, user2, dacName, dacId);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain("不拥有 DAC");
            }
        }

        [TestMethod]
        [DataRow("北京故宫博物馆管理员", "尚方宝剑30号")]
        public void ListDAC(string fromId, string dacName)
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

        [TestMethod]
        public Timestamp GetPublicTimeTest(string dacName)
        {
            var dacPublicTime = _dacMarketContract.GetPublicTime(dacName);
            Logger.Info($"dacPublicTime:{dacPublicTime.Seconds}");
            return dacPublicTime;
        }
    }
}