using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.NFTMarket;
using AElf.Contracts.Whitelist;
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
using AddressList = AElf.Contracts.Whitelist.AddressList;


namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class WhiteListContractTest
    {
        private WhiteListContract _whiteListContract;

        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }

        private string InitAccount { get; } = "nn659b9X1BLhnu5RWmEUbuuV7J9QKVVSN54j9UmeCbF3Dve5D";
        private string ManagersAddress { get; } = "YUW9zH5GhRboT5JK4vXp5BLAfCDv28rRmTQwo418FuaJmkSg8";
        private string ManagersAddress1 { get; } = "FHdcx45K5kovWsAKSb3rrdyNPFus8eoJ1XTQE7aXFHTgfpgzN";
        private string ManagersAddress2 { get; } = "2NKnGrarMPTXFNMRDiYH4hqfSoZw72NLxZHzgHD1Q3xmNoqdmR";
        private string ManagersAddress3 { get; } = "NUddzDNy8PBMUgPCAcFW7jkaGmofDTEmr5DUoddXDpdR6E85X";

        private string UserAddress { get; } = "2Pm6opkpmnQedgc5yDUoVmnPpTus5vSjFNDmSbckMzLw22W6Er";
        private string UserAddress1 { get; } = "29qJkBMWU2Sv6mTqocPiN8JTjcsSkBKCC8oUt11fTE7oHmfG3n";
        private string UserAddress2 { get; } = "puEKG7zUqusWZRiULssPnwKDc2ZSL3q1oWFfatHisGnD9P1EL";
        private string UserAddress3 { get; } = "1DskqyVKjWQm6iev5GtSegv1bP8tz1ZTWQZC2MTogTQoMhv4q";
        private string UserAddress4 { get; } = "W6YQXwoGHM25DZgCB2dsB95Zzb7LbUkYdEe347q8J1okMgB9z";


        private static string RpcUrl { get; } = "http://172.25.127.105:8000";
        private string WhitelistAddress = "2VTusxv6BN4SQDroitnWyLyQHWiwEhdWU76PPiGBqt5VbyF27J"; 
        //correctTestContract
        //GwsSp1MZPmkMvXdbfSCDydHhZtDpvqkFpmPvStYho288fb7QZ

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("InvestmentTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-new-env-main");

            NodeManager = new NodeManager(RpcUrl);
            AuthorityManager = new AuthorityManager(NodeManager, InitAccount);

            if (WhitelistAddress.Equals(""))
                _whiteListContract = new WhiteListContract(NodeManager, InitAccount);
            else
                _whiteListContract = new WhiteListContract(NodeManager, InitAccount, WhitelistAddress);
        }

        [TestMethod]
        public void InitializeTest()
        {
            _whiteListContract.SetAccount(InitAccount);
            var initialize =
                _whiteListContract.ExecuteMethodWithResult(WhiteListContractMethod.Initialize, new Empty());

            initialize.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }


        [TestMethod]
        public void CreateWhitelist()
        {
            var isCloneable = true;
            var remark = "111111";
            var creator = InitAccount.ConvertAddress();
            var info1 = new PriceTag {Symbol = "ELF", Amount = 10_00000000}.ToByteString();
            var info2 = new PriceTag {Symbol = "ELF", Amount = 20_00000000}.ToByteString();
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");

            _whiteListContract.SetAccount(InitAccount);
            var createWhitelist = _whiteListContract.CreateWhitelist(
                new ExtraInfoList
                {
                    Value =
                    {
                        new ExtraInfo
                        {
                            Address = UserAddress.ConvertAddress(),
                            Info = new TagInfo
                            {
                                TagName = "First",
                                Info = info1
                            }
                        },
                        new ExtraInfo
                        {
                            Address = UserAddress1.ConvertAddress(),
                            Info = new TagInfo
                            {
                                TagName = "TWO",
                                Info = info2
                            }
                        }
                    }
                },
                isCloneable,
                remark,
                creator,
                new AddressList
                {
                    Value = {ManagersAddress.ConvertAddress(), ManagersAddress1.ConvertAddress()}
                },
                projectId, 
                StrategyType.Price,
                out var output
            );
            Logger.Info($"output is {output}");
            createWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var id = HashHelper.ComputeFrom($"{InitAccount.ConvertAddress()}{projectId}{"First"}");
            var whitelist = _whiteListContract.GetWhitelist(output);
            Logger.Info($"whitelist is {whitelist}");
            whitelist.WhitelistId.ShouldBe(output);
            whitelist.ExtraInfoIdList.Value[0].Address.ShouldBe(UserAddress.ConvertAddress());
            whitelist.ExtraInfoIdList.Value[0].Id.ShouldBe(id);
            whitelist.IsAvailable.ShouldBe(true);
            whitelist.IsCloneable.ShouldBe(isCloneable);
            whitelist.Remark.ShouldBe(remark);
            whitelist.CloneFrom.ShouldBeNull();
            whitelist.Creator.ShouldBe(creator);
            whitelist.Manager.Value[0].ShouldBe(ManagersAddress.ConvertAddress());
            whitelist.Manager.Value[1].ShouldBe(ManagersAddress1.ConvertAddress());

            var whitelistIdList = _whiteListContract.GetWhitelistByManager(InitAccount.ConvertAddress());
            Logger.Info($"whitelistIdList is {whitelistIdList}");
            whitelistIdList.WhitelistId[0].ShouldBe(output);

            var addressList = _whiteListContract.GetManagerList(output);
            Logger.Info($"addressList is {addressList}");
            addressList.Value[2].ShouldBe(InitAccount.ConvertAddress());
            addressList.Value[0].ShouldBe(ManagersAddress.ConvertAddress());
        }

        [TestMethod]
        public void AddExtraInfo()
        {
            var whitelistId = Hash.LoadFromHex("2e399d3e913557ba3b032f4014f6523bf941e138df7413fcb3b06c628f98c232");
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");
            var info = new Price {Symbol = "ELF", Amount = 30_00000000}.ToByteString();
            var owner = InitAccount.ConvertAddress();
            var id = HashHelper.ComputeFrom($"{owner}{projectId}{"Three"}");
     
            _whiteListContract.SetAccount(InitAccount);
            var addExtraInfo = _whiteListContract.AddExtraInfo
            (
                whitelistId,
                projectId,
                new TagInfo
                {
                    TagName = "Three",
                    Info = info
                },
                new AddressList
                {
                    Value = {UserAddress4.ConvertAddress()}
                }
            );
            addExtraInfo.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var getExtraInfoIdList = _whiteListContract.GetExtraInfoIdList(whitelistId, owner, projectId);
            Logger.Info($"getExtraInfoIdList is {getExtraInfoIdList}");

            var whitelist = _whiteListContract.GetWhitelist(whitelistId);
            Logger.Info($"whitelist is {whitelist}");
            var getTagInfoByHash = _whiteListContract.GetTagInfoByHash(id);
            Logger.Info($"getTagInfoByHash is {getTagInfoByHash}");
            getTagInfoByHash.TagName.ShouldBe("Three");
            getTagInfoByHash.Info.ShouldBe(info);
        }

        [TestMethod]
        public void AddAddressInfoToWhitelist()
        {
            var info = new Price {Symbol = "ELF", Amount = 10_00000000}.ToByteString();
            var whitelistId = Hash.LoadFromHex("2e399d3e913557ba3b032f4014f6523bf941e138df7413fcb3b06c628f98c232");
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");
            var id = HashHelper.ComputeFrom($"{InitAccount.ConvertAddress()}{projectId}{"First"}");
            Logger.Info($"id is {id}");
            
            _whiteListContract.SetAccount(InitAccount);
            var addAddressInfoToWhitelist = _whiteListContract.AddAddressInfoToWhitelist
            (
                whitelistId,
                new ExtraInfoId
                {
                    Address = UserAddress2.ConvertAddress(),
                    Id = id
                }
            );
            addAddressInfoToWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var whitelist = _whiteListContract.GetWhitelist(whitelistId);
            Logger.Info($"whitelist is {whitelist}");
            var extraInfoList = _whiteListContract.GetWhitelistDetail(whitelistId);
            Logger.Info($"addressList is {extraInfoList}");
            extraInfoList.Value[3].Address.ShouldBe(UserAddress2.ConvertAddress());
            extraInfoList.Value[3].Info.TagName.ShouldBe("First");
            extraInfoList.Value[3].Info.Info.ShouldBe(info);
           
        }


        [TestMethod]
        public void RemoveAddressInfoFromWhitelist()
        {
            var whitelistId = Hash.LoadFromHex("2e399d3e913557ba3b032f4014f6523bf941e138df7413fcb3b06c628f98c232");
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");
            var id = HashHelper.ComputeFrom($"{InitAccount.ConvertAddress()}{projectId}{"First"}");
            
            _whiteListContract.SetAccount(InitAccount);
            var removeAddressInfoFromWhitelist = _whiteListContract.RemoveAddressInfoFromWhitelist
            (
                whitelistId,
                new ExtraInfoId
                {
                    Address = UserAddress2.ConvertAddress(),
                    Id = id
                }
            );
            removeAddressInfoFromWhitelist.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var extraInfoList = _whiteListContract.GetWhitelistDetail(whitelistId);
            Logger.Info($"extraInfoList is {extraInfoList}");
            extraInfoList.Value.Count.ShouldBe(3);
            extraInfoList.Value[2].Address.ShouldBe(UserAddress4.ConvertAddress());
            
            // Check event--WhitelistDisabled
            var logs = removeAddressInfoFromWhitelist.Logs.First(l => l.Name.Equals("WhitelistAddressInfoRemoved")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var whitelistAddressInfoRemoved = WhitelistAddressInfoRemoved.Parser.ParseFrom(byteString);
            whitelistAddressInfoRemoved.WhitelistId.ShouldBe(whitelistId);
            whitelistAddressInfoRemoved.ExtraInfoIdList.Value[0].ShouldBe
                (new ExtraInfoId
                {
                    Address = UserAddress2.ConvertAddress(), Id = id
                });
                
        }

        [TestMethod]
        public void AddAddressInfoListToWhitelist()
        {
            var info = new Price {Symbol = "ELF", Amount = 10_00000000}.ToByteString();
            var whitelistId = Hash.LoadFromHex("2e399d3e913557ba3b032f4014f6523bf941e138df7413fcb3b06c628f98c232");
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");
            var id = HashHelper.ComputeFrom($"{InitAccount.ConvertAddress()}{projectId}{"First"}");

            _whiteListContract.SetAccount(InitAccount);
            var addAddressInfoListToWhitelist = _whiteListContract.AddAddressInfoListToWhitelist
            (
                whitelistId,
                new ExtraInfoIdList
                {
                    Value =
                    {
                        new ExtraInfoId
                        {
                            Address = UserAddress2.ConvertAddress(),
                            Id = id
                        },
                        new ExtraInfoId
                        {
                            Address = UserAddress3.ConvertAddress(),
                            Id = id
                        }
                    }
                }
            );
            addAddressInfoListToWhitelist.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var extraInfoList = _whiteListContract.GetWhitelistDetail(whitelistId);
            Logger.Info($"extraInfoList is {extraInfoList}");
            extraInfoList.Value.Count.ShouldBe(5);
            extraInfoList.Value[3].Address.ShouldBe(UserAddress2.ConvertAddress());
            extraInfoList.Value[3].Info.Info.ShouldBe(info);
            extraInfoList.Value[4].Address.ShouldBe(UserAddress3.ConvertAddress());
            extraInfoList.Value[4].Info.Info.ShouldBe(info);
            // Check event--WhitelistDisabled
            var logs = addAddressInfoListToWhitelist.Logs.First(l => l.Name.Equals("WhitelistAddressInfoAdded")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var whitelistAddressInfoAdded = WhitelistAddressInfoAdded.Parser.ParseFrom(byteString);
            Logger.Info($"whitelistAddressInfoAdded is {whitelistAddressInfoAdded}");
            whitelistAddressInfoAdded.WhitelistId.ShouldBe(whitelistId);
            whitelistAddressInfoAdded.ExtraInfoIdList.Value[3].ShouldBe
            (new ExtraInfoId
            {
                Address = UserAddress2.ConvertAddress(), Id = id
            });       
            whitelistAddressInfoAdded.ExtraInfoIdList.Value[4].ShouldBe
            (new ExtraInfoId
            {
                Address = UserAddress3.ConvertAddress(), Id = id
            }); 
        }

        [TestMethod]
        public void RemoveAddressInfoListFromWhitelist()
        {
            var whitelistId = Hash.LoadFromHex("2e399d3e913557ba3b032f4014f6523bf941e138df7413fcb3b06c628f98c232");
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");
            var id = HashHelper.ComputeFrom($"{InitAccount.ConvertAddress()}{projectId}{"First"}");
            
            _whiteListContract.SetAccount(InitAccount);
            var removeAddressInfoListFromWhitelist = _whiteListContract.RemoveAddressInfoListFromWhitelist
            (
                whitelistId,
                new ExtraInfoIdList
                {
                    Value =
                    {
                        new ExtraInfoId
                        {
                            Address = UserAddress2.ConvertAddress(),
                            Id = id
                        },
                        new ExtraInfoId
                        {
                            Address = UserAddress3.ConvertAddress(),
                            Id = id
                        }
                    }
                }
            );
            removeAddressInfoListFromWhitelist.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var extraInfoList = _whiteListContract.GetWhitelistDetail(whitelistId);
            Logger.Info($"extraInfoList is {extraInfoList}");
            extraInfoList.Value.Count.ShouldBe(3);
            // Check event--WhitelistDisabled
            var logs = removeAddressInfoListFromWhitelist.Logs.First(l => l.Name.Equals("WhitelistAddressInfoRemoved")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var whitelistAddressInfoRemoved = WhitelistAddressInfoRemoved.Parser.ParseFrom(byteString);
            Logger.Info($"whitelistAddressInfoRemoved is {whitelistAddressInfoRemoved}");
            whitelistAddressInfoRemoved.WhitelistId.ShouldBe(whitelistId);
            whitelistAddressInfoRemoved.ExtraInfoIdList.Value[0].ShouldBe
            (new ExtraInfoId
            {
                Address = UserAddress2.ConvertAddress(), Id = id
            });       
            whitelistAddressInfoRemoved.ExtraInfoIdList.Value[1].ShouldBe
            (new ExtraInfoId
            {
                Address = UserAddress3.ConvertAddress(), Id = id
            }); 
        }
        
        [TestMethod]
        public void RemoveTagInfo()
        {
            var whitelistId = Hash.LoadFromHex("2e399d3e913557ba3b032f4014f6523bf941e138df7413fcb3b06c628f98c232");
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");
            var info = new Price {Symbol = "ELF", Amount = 30_00000000}.ToByteString();
            var owner = InitAccount.ConvertAddress();
            var tagId = HashHelper.ComputeFrom($"{owner}{projectId}{"Three"}");
            Logger.Info($"tagId is {tagId}");
            
            _whiteListContract.SetAccount(InitAccount);
            var removeAddressInfoFromWhitelist = _whiteListContract.RemoveAddressInfoFromWhitelist
            (
                whitelistId,
                new ExtraInfoId
                {
                    Address = UserAddress4.ConvertAddress(),
                    Id = tagId
                }
            );
            removeAddressInfoFromWhitelist.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            //RemoveTagInfo
            var removeTagInfo = _whiteListContract.RemoveTagInfo
                (
                whitelistId,
                projectId,
                tagId
                );
            removeTagInfo.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            // Check event--WhitelistDisabled
            var logs = removeTagInfo.Logs.First(l => l.Name.Equals("TagInfoRemoved")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var tagInfoRemoved = TagInfoRemoved.Parser.ParseFrom(byteString);
            Logger.Info($"tagInfoRemoved is {tagInfoRemoved}");
            tagInfoRemoved.TagInfoId.ShouldBe(tagId);
            tagInfoRemoved.TagInfo.TagName.ShouldBe("Three");
            tagInfoRemoved.TagInfo.Info.ShouldBe(info);
            tagInfoRemoved.ProjectId.ShouldBe(projectId);
            tagInfoRemoved.WhitelistId.ShouldBe(whitelistId);
        }

        [TestMethod]
        public void DisableWhitelist()
        {
            var whitelistId = Hash.LoadFromHex("2e399d3e913557ba3b032f4014f6523bf941e138df7413fcb3b06c628f98c232");
            _whiteListContract.SetAccount(InitAccount);
            var disableWhitelist = _whiteListContract.DisableWhitelist(whitelistId);
            disableWhitelist.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            
            // Check event--WhitelistDisabled
            var logs = disableWhitelist.Logs.First(l => l.Name.Equals("WhitelistDisabled")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var whiteListDisabled = WhitelistDisabled.Parser.ParseFrom(byteString);
            whiteListDisabled.WhitelistId.ShouldBe(whitelistId);
            whiteListDisabled.IsAvailable.ShouldBe(false);
        }
        
        [TestMethod]
        public void EnableWhitelist()
        {
            var whitelistId = Hash.LoadFromHex("2e399d3e913557ba3b032f4014f6523bf941e138df7413fcb3b06c628f98c232");
            _whiteListContract.SetAccount(InitAccount);
            var enableWhitelist = _whiteListContract.EnableWhitelist(whitelistId);
            enableWhitelist.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            
            // Check event--WhitelistReenable
            var logs = enableWhitelist.Logs.First(l => l.Name.Equals("WhitelistReenable")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var whitelistReenable = WhitelistReenable.Parser.ParseFrom(byteString);
            whitelistReenable.WhitelistId.ShouldBe(whitelistId);
            whitelistReenable.IsAvailable.ShouldBe(true);
        }

        [TestMethod]
        public void ChangeWhitelistCloneable()
        {
            var whitelistId = Hash.LoadFromHex("2e399d3e913557ba3b032f4014f6523bf941e138df7413fcb3b06c628f98c232");
            var isCloneable = false;
            var isCloneable1 = true;
            
            _whiteListContract.SetAccount(InitAccount);
           
            //例：关闭克隆
            var changeWhitelistCloneable = _whiteListContract.ChangeWhitelistCloneable
            (
                whitelistId,
                isCloneable
                );
            changeWhitelistCloneable.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            // Check event--IsCloneableChanged
            var logs = changeWhitelistCloneable.Logs.First(l => l.Name.Equals("IsCloneableChanged")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var isCloneableChanged = IsCloneableChanged.Parser.ParseFrom(byteString);
            isCloneableChanged.WhitelistId.ShouldBe(whitelistId);
            isCloneableChanged.IsCloneable.ShouldBe(false);
            

            //例：开启克隆
            var changeWhitelistCloneable1 = _whiteListContract.ChangeWhitelistCloneable
            (
                whitelistId,
                isCloneable1
            );
            changeWhitelistCloneable1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            // Check event
            logs = changeWhitelistCloneable1.Logs.First(l => l.Name.Equals("IsCloneableChanged")).NonIndexed; 
            byteString = ByteString.FromBase64(logs); 
            isCloneableChanged = IsCloneableChanged.Parser.ParseFrom(byteString);
            isCloneableChanged.WhitelistId.ShouldBe(whitelistId);
            isCloneableChanged.IsCloneable.ShouldBe(true);
        }

        [TestMethod]
        public void UpdateExtraInfo()
        {
        }
        
        [TestMethod]
        public void TransferManager()
        {
            var whitelistId = Hash.LoadFromHex("2e399d3e913557ba3b032f4014f6523bf941e138df7413fcb3b06c628f98c232");
            var manager = UserAddress.ConvertAddress();
            var manager1 = ManagersAddress.ConvertAddress();
            
            
            _whiteListContract.SetAccount(ManagersAddress);
            var transferManager = _whiteListContract.TransferManager
            (
                whitelistId,
                manager
            );
            transferManager.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var getManagerList = _whiteListContract.GetManagerList(whitelistId);
            Logger.Info($"getManagerList is {getManagerList}");
            //getManagerList.Value[0].ShouldBe(UserAddress.ConvertAddress());
            // Check event--ManagerTransferred
            var logs = transferManager.Logs.First(l => l.Name.Equals("ManagerTransferred")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var managerTransferred = ManagerTransferred.Parser.ParseFrom(byteString);
            managerTransferred.WhitelistId.ShouldBe(whitelistId);
            managerTransferred.TransferFrom.ShouldBe(ManagersAddress.ConvertAddress());
            managerTransferred.TransferTo.ShouldBe(UserAddress.ConvertAddress());


            _whiteListContract.SetAccount(UserAddress);
            var transferManager1 = _whiteListContract.TransferManager
            (
                whitelistId,
                manager1
            );
            transferManager1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void AddManagers()
        {
            var whitelistId = Hash.LoadFromHex("2e399d3e913557ba3b032f4014f6523bf941e138df7413fcb3b06c628f98c232");
            _whiteListContract.SetAccount(InitAccount);
            var addManagers = _whiteListContract.AddManagers
            (
                whitelistId,
                new AddressList
                {
                    Value = {ManagersAddress2.ConvertAddress(), ManagersAddress3.ConvertAddress()}
                }
            );
            addManagers.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var addressList = _whiteListContract.GetManagerList(whitelistId);
            Logger.Info($"addressList is {addressList}");
            addressList.Value.Count.ShouldBe(5);
            addressList.Value[3].ShouldBe(ManagersAddress2.ConvertAddress());
            addressList.Value[4].ShouldBe(ManagersAddress3.ConvertAddress());
            // Check event--ManagerAdded
            var logs = addManagers.Logs.First(l => l.Name.Equals("ManagerAdded")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var managerAdded = ManagerAdded.Parser.ParseFrom(byteString);
            managerAdded.WhitelistId.ShouldBe(whitelistId);
            managerAdded.ManagerList.Value[0].ShouldBe(ManagersAddress2.ConvertAddress());
            managerAdded.ManagerList.Value[1].ShouldBe(ManagersAddress3.ConvertAddress());
        }


        [TestMethod]
        public void RemoveManagers()
        {
            var whitelistId = Hash.LoadFromHex("2e399d3e913557ba3b032f4014f6523bf941e138df7413fcb3b06c628f98c232");
            _whiteListContract.SetAccount(InitAccount);
            var removeManagers = _whiteListContract.RemoveManagers
            (
                whitelistId,
                new AddressList
                {
                    Value = {ManagersAddress2.ConvertAddress(), ManagersAddress3.ConvertAddress()}
                }
            );
            removeManagers.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var addressList = _whiteListContract.GetManagerList(whitelistId);
            Logger.Info($"addressList is {addressList}");
            addressList.Value.Count.ShouldBe(3);
            // Check event
            var logs = removeManagers.Logs.First(l => l.Name.Equals("ManagerRemoved")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var managerRemoved = ManagerRemoved.Parser.ParseFrom(byteString);
            Logger.Info($"managerRemoved is {managerRemoved}");
            managerRemoved.WhitelistId.ShouldBe(whitelistId);
            managerRemoved.ManagerList.Value[0].ShouldBe(ManagersAddress2.ConvertAddress());
            managerRemoved.ManagerList.Value[1].ShouldBe(ManagersAddress3.ConvertAddress());
        }

        [TestMethod]
        public void ResetWhitelist()
        {
            //ea663c76b3484a4c0622ea0b01f6bb50a842e3dfb795d39a2ad6351984f501ef_已删除
            var whitelistId = Hash.LoadFromHex("ea663c76b3484a4c0622ea0b01f6bb50a842e3dfb795d39a2ad6351984f501ef"); 
            var projectId = HashHelper.ComputeFrom($"{UserAddress1.ConvertAddress()}");
            
             _whiteListContract.SetAccount(InitAccount);
            var resetWhitelist = _whiteListContract.ResetWhitelist
            (
                whitelistId,
                projectId
            );
            resetWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var whitelist = _whiteListContract.GetWhitelist(whitelistId);
            Logger.Info($"whitelist is {whitelist}");
            var whitelistIdList = _whiteListContract.GetWhitelistByManager(InitAccount.ConvertAddress());
            Logger.Info($"whitelistIdList is {whitelistIdList}");
            
            // Check event
            var logs = resetWhitelist.Logs.First(l => l.Name.Equals("WhitelistReset")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var whitelistReset = WhitelistReset.Parser.ParseFrom(byteString);
            whitelistReset.WhitelistId.ShouldBe(whitelistId);
            whitelistReset.ProjectId.ShouldBe(projectId);
        }
        
        //Subscribers.
        [TestMethod]
        public void SubscribeWhitelist()
        {
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");
            var whitelistId = Hash.LoadFromHex("2e399d3e913557ba3b032f4014f6523bf941e138df7413fcb3b06c628f98c232");
            var id = HashHelper.ComputeFrom($"{InitAccount.ConvertAddress()}{projectId}{"First"}");
            
            _whiteListContract.SetAccount(UserAddress2,"123456");
            var subscribeWhitelist = _whiteListContract.SubscribeWhitelist
                (
                projectId,
                whitelistId , 
                out var output
            );
            Logger.Info($"output is {output}");
            subscribeWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var subscribeId =HashHelper.ComputeFrom($"{UserAddress2.ConvertAddress()}{projectId}{whitelistId}");
            Logger.Info($"subscribeId is {subscribeId}");
            var getSubscribeWhitelist = _whiteListContract.GetSubscribeWhitelist(output);
            Logger.Info($"getSubscribeWhitelist is {getSubscribeWhitelist}");
            getSubscribeWhitelist.SubscribeId.ShouldBe(output);
            getSubscribeWhitelist.WhitelistId.ShouldBe(whitelistId);
            getSubscribeWhitelist.ProjectId.ShouldBe(projectId);
            
            // Check event
            var logs = subscribeWhitelist.Logs.First(l => l.Name.Equals("WhitelistSubscribed")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var whiteListSubscribed = WhitelistSubscribed.Parser.ParseFrom(byteString);
            whiteListSubscribed.WhitelistId.ShouldBe(whitelistId);
            whiteListSubscribed.ProjectId.ShouldBe(projectId);
            whiteListSubscribed.SubscribeId.ShouldBe(output);
        }
        
        [TestMethod]
        public void ConsumeWhitelist()
        {
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");
            var whitelistId = Hash.LoadFromHex("2e399d3e913557ba3b032f4014f6523bf941e138df7413fcb3b06c628f98c232");
            var subscribeId =HashHelper.ComputeFrom($"{UserAddress2.ConvertAddress()}{projectId}{whitelistId}");
            Logger.Info($"subscribeId is {subscribeId}");
            var id = HashHelper.ComputeFrom($"{InitAccount.ConvertAddress()}{projectId}{"First"}");
            
            var getTagInfoByHash =_whiteListContract.GetTagInfoByHash(id);
            Logger.Info($"getTagInfoByHash is {getTagInfoByHash}");
            
            var getExtraInfoFromWhitelist = _whiteListContract.GetExtraInfoFromWhitelist
            ( 
                whitelistId,
                 new ExtraInfoId
                     {
                         Address =UserAddress.ConvertAddress(),
                         Id =id
                     }
                );
            Logger.Info($"getSubscribeWhitelist is {getExtraInfoFromWhitelist}");

            var getAvailableWhitelist =_whiteListContract.GetAvailableWhitelist(subscribeId);
            Logger.Info($"getAvailableWhitelist is {getAvailableWhitelist}");

            //例：      
            _whiteListContract.SetAccount(UserAddress);
            var consumeWhitelist = _whiteListContract.ConsumeWhitelist
            (
                subscribeId,
                whitelistId,
                new ExtraInfoId
                {
                    Address = UserAddress.ConvertAddress(),
                    Id = id
                }
            );
            consumeWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var getConsumedList = _whiteListContract.GetConsumedList(subscribeId);
            Logger.Info($"getConsumedList is {getConsumedList}");
            getConsumedList.SubscribeId.ShouldBe(subscribeId);
            getConsumedList.WhitelistId.ShouldBe(whitelistId);
            getConsumedList.ExtraInfoIdList.Value[0].Address.ShouldBe(UserAddress.ConvertAddress());
            getConsumedList.ExtraInfoIdList.Value[0].Id.ShouldBe(id);

            // Check event
            var logs = consumeWhitelist.Logs.First(l => l.Name.Equals("ConsumedListAdded")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var consumedListAdded = ConsumedListAdded.Parser.ParseFrom(byteString);
            Logger.Info($"consumedListAdded is {consumedListAdded}");
            consumedListAdded.WhitelistId.ShouldBe(whitelistId);
            consumedListAdded.SubscribeId.ShouldBe(subscribeId);
            consumedListAdded.ExtraInfoIdList.Value[0].Address.ShouldBe(UserAddress.ConvertAddress());
            consumedListAdded.ExtraInfoIdList.Value[0].Id.ShouldBe(id);

        }

        [TestMethod]
        public void UnsubscribeWhitelist()
        {
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");
            var whitelistId = Hash.LoadFromHex("2e399d3e913557ba3b032f4014f6523bf941e138df7413fcb3b06c628f98c232");
            var id = HashHelper.ComputeFrom($"{InitAccount.ConvertAddress()}{projectId}{"First"}");
            var subscribeId =HashHelper.ComputeFrom($"{UserAddress2.ConvertAddress()}{projectId}{whitelistId}");
            Logger.Info($"subscribeId is {subscribeId}");
            
            _whiteListContract.SetAccount(UserAddress2,"123456");
            var unsubscribeWhitelist = _whiteListContract.UnsubscribeWhitelist
            (
                subscribeId
            );
            unsubscribeWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            // Check event
            var logs = unsubscribeWhitelist.Logs.First(l => l.Name.Equals("WhitelistUnsubscribed")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var whitelistUnsubscribed = WhitelistUnsubscribed.Parser.ParseFrom(byteString);
            whitelistUnsubscribed.WhitelistId.ShouldBe(whitelistId);
            whitelistUnsubscribed.ProjectId.ShouldBe(projectId);
            whitelistUnsubscribed.SubscribeId.ShouldBe(subscribeId);
        }

        [TestMethod]
        public void CloneWhitelist()
        {
            var whitelistId = Hash.LoadFromHex("2e399d3e913557ba3b032f4014f6523bf941e138df7413fcb3b06c628f98c232");
            var creator = UserAddress1.ConvertAddress();
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");

            _whiteListContract.SetAccount(UserAddress1);
            var cloneWhitelist = _whiteListContract.CloneWhitelist
            (
                whitelistId,
                creator,
                new AddressList
                {
                    Value = {ManagersAddress.ConvertAddress(), ManagersAddress1.ConvertAddress()}
                },
                out var output
            );
            Logger.Info($"output is {output}");
            cloneWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var id = HashHelper.ComputeFrom($"{InitAccount.ConvertAddress()}{projectId}{"First"}");
            var whitelist = _whiteListContract.GetWhitelist(output);
            Logger.Info($"whitelist is {whitelist}");
            whitelist.WhitelistId.ShouldBe(output);
            whitelist.ExtraInfoIdList.Value[0].Address.ShouldBe(UserAddress.ConvertAddress());
            whitelist.ExtraInfoIdList.Value[0].Id.ShouldBe(id);
            whitelist.IsAvailable.ShouldBe(true);
            // Check event
            var logs = cloneWhitelist.Logs.First(l => l.Name.Equals("WhitelistCreated")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var whitelistCreated = WhitelistCreated.Parser.ParseFrom(byteString);
            Logger.Info($"whitelistCloned is {whitelistCreated}");
            whitelistCreated.WhitelistId.ShouldBe(whitelistId);
            whitelistCreated.CloneFrom.ShouldBe(projectId);
        }

        [TestMethod]
        public void CreateWhitelistFail()
        {
            
            var isCloneable = true;
            var remark = "111111";
            var creator = InitAccount.ConvertAddress();
            var info1 = new PriceTag {Symbol = "ELF", Amount = 10_00000000}.ToByteString();
            var info2 = new PriceTag {Symbol = "ELF", Amount = 20_00000000}.ToByteString();
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");
            var projectId1 = HashHelper.ComputeFrom($"{UserAddress1.ConvertAddress()}");

            //whitelistId:5d68136f025f7a4dc7352eca72828e5b5fa1dd9e9d2fbcf1f2689bc1027d312e
            //例：重复创建
            _whiteListContract.SetAccount(InitAccount);
            var createWhitelist = _whiteListContract.CreateWhitelist(
                new ExtraInfoList
                {
                    Value =
                    {
                        new ExtraInfo
                        {
                            Address = UserAddress.ConvertAddress(),
                            Info = new TagInfo
                            {
                                TagName = "First",
                                Info = info1
                            }
                        },
                        new ExtraInfo
                        {
                            Address = UserAddress1.ConvertAddress(),
                            Info = new TagInfo
                            {
                                TagName = "TWO",
                                Info = info2
                            }
                        }
                    }
                },
                isCloneable,
                remark,
                creator,
                new AddressList
                {
                    Value = {ManagersAddress.ConvertAddress(), ManagersAddress1.ConvertAddress()}
                },
                projectId,
                StrategyType.Price,
                out _
                );
            createWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            createWhitelist.Error.ShouldContain("");
            //例：非message创建
            _whiteListContract.SetAccount(UserAddress);
            var createWhitelist1 = _whiteListContract.CreateWhitelist(
                new ExtraInfoList
                {
                    Value =
                    {
                        new ExtraInfo
                        {
                            Address = UserAddress.ConvertAddress(),
                            Info = new TagInfo
                            {
                                TagName = "First",
                                Info = info1
                            }
                        },
                        new ExtraInfo
                        {
                            Address = UserAddress1.ConvertAddress(),
                            Info = new TagInfo
                            {
                                TagName = "TWO",
                                Info = info2
                            }
                        }
                    }
                },
                isCloneable,
                remark,
                creator,
                new AddressList
                {
                    Value = {ManagersAddress.ConvertAddress(), ManagersAddress1.ConvertAddress()}
                },
                projectId1,
                StrategyType.Price,
                out _
                );
            createWhitelist1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            createWhitelist1.Error.ShouldContain("");
        }

        [TestMethod]
        public void AddExtraInfoFail()
        {
            var whitelistId = Hash.LoadFromHex("5d68136f025f7a4dc7352eca72828e5b5fa1dd9e9d2fbcf1f2689bc1027d312e");
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");
            var projectId1 = HashHelper.ComputeFrom($"{UserAddress4.ConvertAddress()}");
            var info = new Price {Symbol = "ELF", Amount = 30_00000000}.ToByteString();
            //例：重复添加
            _whiteListContract.SetAccount(InitAccount);
            var addExtraInfo = _whiteListContract.AddExtraInfo
            (
                whitelistId,
                projectId,
                new TagInfo
                {
                    TagName = "First",
                    Info = info
                },
                new AddressList
                {
                    Value = {UserAddress2.ConvertAddress()}
                }
            );
            addExtraInfo.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addExtraInfo.Error.ShouldContain("");
            //例：错误的信息
            _whiteListContract.SetAccount(InitAccount);
            var addExtraInfo1 = _whiteListContract.AddExtraInfo
            (
                whitelistId,
                projectId1,
                new TagInfo
                {
                    TagName = "111",
                    Info = info
                },
                new AddressList
                {
                    Value = {UserAddress2.ConvertAddress()}
                }
            );
            addExtraInfo1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addExtraInfo1.Error.ShouldContain("");
            //例：TagName下的用户重复
            _whiteListContract.SetAccount(ManagersAddress);
            var addExtraInfo2 = _whiteListContract.AddExtraInfo
            (
                whitelistId,
                projectId1,
                new TagInfo
                {
                    TagName = "111",
                    Info = info
                },
                new AddressList
                {
                    Value = {UserAddress.ConvertAddress()}
                }
            );
            addExtraInfo2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addExtraInfo2.Error.ShouldContain("");
            //例：非ManagersAddress添加
            _whiteListContract.SetAccount(UserAddress);
            var addExtraInfo3 = _whiteListContract.AddExtraInfo
            (
                whitelistId,
                projectId,
                new TagInfo
                {
                    TagName = "111",
                    Info = info
                },
                new AddressList
                {
                    Value = {UserAddress2.ConvertAddress()}
                }
            );
            addExtraInfo3.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addExtraInfo3.Error.ShouldContain("");
            //例：白名单关闭后添加
            _whiteListContract.SetAccount(InitAccount);
            var disableWhitelist = _whiteListContract.DisableWhitelist(whitelistId);
            disableWhitelist.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var addExtraInfo4 = _whiteListContract.AddExtraInfo
            (
                whitelistId,
                projectId,
                new TagInfo
                {
                    TagName = "111",
                    Info = info
                },
                new AddressList
                {
                    Value = {UserAddress2.ConvertAddress()}
                }
            );
            addExtraInfo4.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addExtraInfo4.Error.ShouldContain("");
        }

        [TestMethod]
        public void AddAddressInfoToWhitelistFail()
        {
            var whitelistId = Hash.LoadFromHex("5d68136f025f7a4dc7352eca72828e5b5fa1dd9e9d2fbcf1f2689bc1027d312e");
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");
            var id = HashHelper.ComputeFrom($"{InitAccount.ConvertAddress()}{projectId}{"First"}");
            Logger.Info($"id is {id}");
            
            _whiteListContract.SetAccount(InitAccount);
            var addAddressInfoToWhitelist = _whiteListContract.AddAddressInfoToWhitelist
            (
                whitelistId,
                new ExtraInfoId
                {
                    Address = UserAddress.ConvertAddress(),
                    Id = id
                }
            );
            addAddressInfoToWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addAddressInfoToWhitelist.Error.ShouldContain("");
            
            
            
            
            
            
            
            
            
            
        }
        
        
        
        
        
        
        

    }
}