using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using AElf.Client.Dto;
using AElf.Client.MultiToken;
using AElf.Contracts.Genesis;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using log4net;
using log4net.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.Asn1.X509;
using Shouldly;
using Shouldly.Configuration;
using Sinodac.Contracts.DAC;
using TimestampHelper = AElf.Kernel.TimestampHelper;
using AElf.CSharp.Core.Extension;
using Microsoft.Extensions.Localization;
using Sinodac.Contracts.DACMarket;
using Sinodac.Contracts.Delegator;
using StringList = Sinodac.Contracts.Delegator.StringList;


namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class DacMarketTest
    {
        private DACContract _dacContract;
        private DacMarketContract _dacMarketContract;
        private DelegatorContract _delegatorContract;
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }

        //private string dacAddr = "iupiTuL2cshxB9UNauXNXe9iyCcqka7jCotodcEHGpNXeLzqG";
        private string dacAddr = "";
        //private string dacMarketAddr = "AtCnocGN47ZCUscwHYxJNh8G8jVmbgjgy1MR62uoXGohd67wu";
        private string dacMarketAddr = "";
        //private string delegatorAddr = "2TXvtjgTiMwjvEyWGEvfbeQ9P6zVK55pTPcmzvLFBDCMLNUYXV";
        private string delegatorAddr = "";
        private string InitAccount { get; } = "nn659b9X1BLhnu5RWmEUbuuV7J9QKVVSN54j9UmeCbF3Dve5D";
        private string UserA { get; } = "YUW9zH5GhRboT5JK4vXp5BLAfCDv28rRmTQwo418FuaJmkSg8";
        private string Receiver { get; } = "FHdcx45K5kovWsAKSb3rrdyNPFus8eoJ1XTQE7aXFHTgfpgzN";
        private static string RpcUrl { get; } = "192.168.67.166:8000";


        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("DACMarketTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env2-main");

            NodeManager = new NodeManager(RpcUrl);
            _dacContract = dacAddr == ""
                ? new DACContract(NodeManager, InitAccount)
                : new DACContract(NodeManager, InitAccount, dacAddr);
            _dacMarketContract = dacMarketAddr == ""
                ? new DacMarketContract(NodeManager, InitAccount)
                : new DacMarketContract(NodeManager, InitAccount, dacMarketAddr);
            _delegatorContract = delegatorAddr == ""
                ? new DelegatorContract(NodeManager, InitAccount)
                : new DelegatorContract(NodeManager, InitAccount, delegatorAddr);
        }

        [TestMethod]
        public void InitializeContracts()
        {
            var init = _delegatorContract.Initialize(InitAccount,
                _dacContract.ContractAddress, _dacMarketContract.ContractAddress);
            init.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void CreateMysteryBoxTest()
        {
            var createResult = _delegatorContract.CreateDAC(
                "用户1",
                "机构1",
                "珊瑚色",
                398_00,
                100,
                "实物",
                "圆柱",
                100,
                ""
            );
            createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var result = _dacContract.CallViewMethod<DACProtocolInfo>(DACMethod.GetDACProtocolInfo, new StringValue
            {
                Value = "珊瑚色"
            });
            result.Circulation.ShouldBe(100);
            result.ReserveForLottery.ShouldBe(100);
            result.ReserveFrom.ShouldBe(1);
            Logger.Info(result);

        }

        [TestMethod]
        public void ListMysteryBoxTest()
        {
            var audit = _delegatorContract.AuditDAC(
                "用户1",
                "珊瑚色",
                true
            );
            
            audit.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var box = _delegatorContract.Box("用户1", "珊瑚色");
            box.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var list = _delegatorContract.ListDAC(
                "用户1", 
                "珊瑚色", 
                TimestampHelper.GetUtcNow().AddSeconds(10));
            list.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var mint = _delegatorContract.MintDAC(
                "用户1",
                "珊瑚色",
                1,
                100);
            mint.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        }

        [TestMethod]
        public void BuyMysteryBoxTest()
        {
            
            var buy = _delegatorContract.Buy(
                "用户2", 
                "珊瑚色", 
                100, 
                397_00);
            buy.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var userAddr = _delegatorContract.CallViewMethod<Address>(DelegatorMethod.CalculateUserAddress, new StringValue
            {
                Value = "用户2"
            });

            var ownBoxList = _dacMarketContract.CallViewMethod<StringList>(DACMarketMethod.GetOwnBoxIdList, userAddr);
            ownBoxList.Value.Count.ShouldBe(1);

            var boxInfo = _dacMarketContract.CallViewMethod<BoxInfo>(DACMarketMethod.GetBoxInfo, new StringValue
            {
                Value = ownBoxList.Value.First()
            });
            boxInfo.Price.ShouldBe(39700);
            boxInfo.DacId.ShouldBe(100);
            boxInfo.DacName.ShouldBe("珊瑚色");
            
        }

        [TestMethod]
        public void UnboxMysteryBoxTest()
        {
            var userAddr = _delegatorContract.CallViewMethod<Address>(DelegatorMethod.CalculateUserAddress, new StringValue
            {
                Value = "用户2"
            });
            var ownBoxList = _dacMarketContract.CallViewMethod<StringList>(DACMarketMethod.GetOwnBoxIdList, userAddr);

            var unbox = _delegatorContract.Unbox(
                "用户2", 
                "珊瑚色",
                ownBoxList.Value.First());
            unbox.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var balance = _dacContract.CallViewMethod<DACBalance>(DACMethod.GetBalance,new Sinodac.Contracts.DAC.GetBalanceInput
            {
                DacName = "珊瑚色",
                Owner = userAddr
            });
            balance.Balance.ShouldBe(1);

            var isOwner = _dacContract.CallViewMethod<BoolValue>(DACMethod.IsOwner, new IsOwnerInput
            {
                DacName = "珊瑚色",
                DacId = 100,
                Owner = userAddr
            });
            isOwner.Value.ShouldBeTrue();
        }

        [TestMethod]
        public void CreateMysteryBoxError()
        {
            //不合规数值在API接口做判断，合约中不做判断

            {
                var createResult = _delegatorContract.CreateDAC(
                    "用户3",
                    "机构",
                    "战斗机",
                    999,
                    0,
                    "3D模型",
                    "长方形",
                    0,
                    ""
                );
                createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                var result = _dacContract.CallViewMethod<DACProtocolInfo>(DACMethod.GetDACProtocolInfo, new StringValue
                {
                    Value = "战斗机"
                });
                result.Circulation.ShouldBe(0);    
            }

            {
                var createResult = _delegatorContract.CreateDAC(
                    "用户3",
                    "机构",
                    "轰炸机",
                    999,
                    -1,
                    "3D模型",
                    "长方形",
                    -1,
                    ""
                );
                createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                var result = _dacContract.CallViewMethod<DACProtocolInfo>(DACMethod.GetDACProtocolInfo, new StringValue
                {
                    Value = "轰炸机"
                });
                result.Circulation.ShouldBe(-1);   
            }
        }

        [TestMethod]
        public void BoxMysteryBoxError()
        {
            {
                //打包别人创的盲盒，权限在delegator里限制，market合约不做限制
                var createResult = _delegatorContract.CreateDAC(
                    "用户3",
                    "机构",
                    "青岛大姨",
                    777,
                    3,
                    "图片",
                    "正方形",
                    3,
                    ""
                );
                createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                var box = _delegatorContract.Box("赵四", "青岛大姨");
                box.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }
            {
                //打包不存在的藏品，不做限制，没有影响
                var box = _delegatorContract.Box("赵四", "东北大姨");
                box.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }
            {
                //重复打包，不在合约中判断，没有影响
                var createResult = _delegatorContract.CreateDAC(
                    "用户3",
                    "机构",
                    "山东大姨",
                    777,
                    3,
                    "图片",
                    "正方形",
                    3,
                    ""
                );
                createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                
                var boxResult = _delegatorContract.Box("用户3", "山东大姨");
                boxResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                var box = _delegatorContract.Box("用户3", "山东大姨");
                box.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }
        }

        [TestMethod]
        public void ListMysteryBoxError()
        {
            var createSeries = _delegatorContract.CreateSeries(
                "用户3",
                "海贼王",
                "日本漫画",
                "动漫社"
            );
            createSeries.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            {
                //审核未通过时上架
                var createResult = _delegatorContract.CreateDAC(
                "用户3",
                "机构",
                "凯多",
                777,
                3,
                "图片",
                "长方形",
                3,
                "海贼王"
                );
                createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                
                var list = _delegatorContract.ListDAC(
                    "用户3", 
                    "凯多", 
                    TimestampHelper.GetUtcNow().AddSeconds(10));
                list.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                list.Error.ShouldContain("还没有通过审核");
            }

            {
                //上架时间为过去时间
                var createResult = _delegatorContract.CreateDAC(
                    "用户3",
                    "机构",
                    "黑胡子",
                    777,
                    3,
                    "图片",
                    "长方形",
                    3,
                    "海贼王"
                );
                createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                
                var audit = _delegatorContract.AuditDAC(
                    "用户3",
                    "黑胡子",
                    true
                );
                audit.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                var list = _delegatorContract.ListDAC(
                    "用户3", 
                    "黑胡子", 
                    TimestampHelper.GetUtcNow());
                list.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                list.Error.ShouldContain("上架时间不能是过去的时间");
            }

            {
                //上架不存在藏品
                var audit = _delegatorContract.AuditDAC(
                    "用户3",
                    "大妈妈",
                    true
                );
                audit.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                var list = _delegatorContract.ListDAC(
                    "用户3", 
                    "大妈妈", 
                    TimestampHelper.GetUtcNow().AddSeconds(10));
                list.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                list.Error.ShouldContain("尚未创建");
            }

            {
                //重复上架：合约中不做判断
                var createResult = _delegatorContract.CreateDAC(
                    "用户3",
                    "机构",
                    "五档路飞",
                    777,
                    3,
                    "图片",
                    "长方形",
                    3,
                    ""
                );
                createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                
                var auditresult = _delegatorContract.AuditDAC(
                    "用户3",
                    "五档路飞",
                    true
                );
                auditresult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                
                var list = _delegatorContract.ListDAC(
                    "用户3", 
                    "五档路飞", 
                    TimestampHelper.GetUtcNow().AddDays(1));
                list.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                
                
                var listtwice = _delegatorContract.ListDAC(
                    "用户3", 
                    "五档路飞", 
                    TimestampHelper.GetUtcNow().AddSeconds(10));
                _dacMarketContract.CallViewMethod<Timestamp>(DACMarketMethod.GetPublicTime, new StringValue
                {
                    Value = "五档路飞"
                });

                listtwice.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            }

            {
                //上架别人的盲盒：需从delegator中设置权限,market合约不做判断
                var createResult2 = _delegatorContract.CreateDAC(
                    "用户3",
                    "机构",
                    "罗杰",
                    777,
                    3,
                    "图片",
                    "长方形",
                    3,
                    ""
                );
                createResult2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                
                var auditresult2 = _delegatorContract.AuditDAC(
                    "用户3",
                    "罗杰",
                    true
                );
                auditresult2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                
                var list = _delegatorContract.ListDAC(
                    "赵四", 
                    "罗杰", 
                    TimestampHelper.GetUtcNow().AddDays(1));
                list.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            }

        }

        [TestMethod]
        public void AddProtocolToSeries()
        {
            var createSeries = _delegatorContract.CreateSeries(
                "社员1",
                "鬼灭",
                "日本漫画",
                "动漫社"
            );
            createSeries.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var createDac = _delegatorContract.CreateDAC(
                "社员1",
                "动漫社",
                "上弦伍",
                10000,
                10,
                "模型",
                "长方形",
                10,
                ""
            );
            createDac.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var seriesInfoBefore = _dacMarketContract.CallViewMethod<DACSeries>(DACMarketMethod.GetDACSeries, new StringValue
            {
                Value = "鬼灭"
            });
            
            var addProtocol = _delegatorContract.AddProtocolToSeries("社员1", "鬼灭", "上弦伍");
            addProtocol.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var seriesInfo = _dacMarketContract.CallViewMethod<DACSeries>(DACMarketMethod.GetDACSeries, new StringValue
            {
                Value = "鬼灭"
            });
            
            seriesInfo.CollectionList.Value.ShouldContain("上弦伍");
            seriesInfo.CollectionCount.ShouldBe(seriesInfoBefore.CollectionCount.Add(1));

        }
        

        [TestMethod]
        public void AddProtocolToSeriesError()
        {
            var createSeries = _delegatorContract.CreateSeries(
                "用户3",
                "海贼王",
                "日本漫画",
                "动漫社"
            );
            createSeries.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //dac not exsit
            {
                var addProtocol =
                    _delegatorContract.AddProtocolToSeries("用户1", "海贼王", "罗宾");
                addProtocol.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                addProtocol.Error.ShouldContain("尚未创建");
                var series = _dacMarketContract.CallViewMethod<DACSeries>(DACMarketMethod.GetDACSeries, new StringValue
                {
                    Value = "海贼王"
                });
                series.CollectionList.Value.ShouldNotContain("罗宾");
            }
            
            //dacseries not exsit
            {
                var addProtocol =
                    _delegatorContract.AddProtocolToSeries("用户1", "OnePiece", "罗宾");
                addProtocol.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                addProtocol.Error.ShouldContain("尚未创建");
                var series = _dacMarketContract.CallViewMethod<DACSeries>(DACMarketMethod.GetDACSeries, new StringValue
                {
                    Value = "OnePiece"
                });
                series.ShouldBe(new DACSeries());
                
            }
        }

        [TestMethod]
        public void BuyMysteryBoxError()
        {
            {
                //购买不存在的盲盒
                var buy = _delegatorContract.Buy(
                    "用户1",
                    "不存在的盲盒",
                    0,
                    999);
                
                buy.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            }

            {
                //dacId不存在：market合约不做判断
                var createResult = _delegatorContract.CreateDAC(
                    "用户3",
                    "机构",
                    "老虎",
                    777,
                    3,
                    "图片",
                    "长方形",
                    3,
                    ""
                );
                createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                
                var audit = _delegatorContract.AuditDAC(
                    "用户3",
                    "老虎",
                    true
                );
                audit.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                var list = _delegatorContract.ListDAC(
                    "用户3", 
                    "老虎", 
                    TimestampHelper.GetUtcNow().AddSeconds(10));
                list.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                
                var mint = _delegatorContract.MintDAC(
                    "用户3",
                    "老虎",
                    1,
                    3);
                mint.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                
                var box = _delegatorContract.Box("用户1", "老虎");
                box.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                var buyOnce = _delegatorContract.Buy("王五",
                    "老虎",
                    3,
                    788);
                buyOnce.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                
                var buyTwice = _delegatorContract.Buy("王五",
                    "老虎",
                    2,
                    788);
                buyTwice.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                
                var buyThrid = _delegatorContract.Buy("王五",
                    "老虎",
                    4,
                    788);
                buyThrid.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }
        }

        [TestMethod]
        public void UnboxMysteryError()
        {
            CreatAndListMysteryBox("A",111,10);
            CreatAndListMysteryBox("B",222,15);
            CreatAndListMysteryBox("C",333,5);
            
            {
                //boxId不存在
                var unbox = _delegatorContract.Unbox(
                    "用户2", 
                    "A",
                    "FakeBoxId");
                unbox.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            }
            {

                var buy = _delegatorContract.Buy(
                    "用户2",
                    "A",
                    3,
                    110);
                buy.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);


                var boxId = GetOwnBoxList("用户2").Value.Last();

                var balanceSharkBefore = GetOwnerDacBalance("B", "用户2");
                var balanceDolphinBefore = GetOwnerDacBalance("A", "用户2");
                //boxId存在但不属于dacName
                var unbox = _delegatorContract.Unbox(
                    "用户2", 
                    "B",
                    boxId);
                
                var balanceSharkAfterUnbox = GetOwnerDacBalance("B", "用户2");
                var balanceDolphinAfterUnbox = GetOwnerDacBalance("A", "用户2");
                balanceSharkAfterUnbox.ShouldBe(balanceSharkBefore.Add(0));
                balanceDolphinAfterUnbox.ShouldBe(balanceDolphinBefore.Add(1));
                
                unbox.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }
            {
                var buy = _delegatorContract.Buy(
                    "用户2",
                    "C",
                    1,
                    777);
                buy.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);


                var boxId = GetOwnBoxList("用户2").Value.Last();
                var unbox = _delegatorContract.Unbox(
                    "用户3",
                    "C",
                    boxId);
                unbox.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                unbox.Error.ShouldContain("名下没有盲盒");
            }
            
            {
                var buy = _delegatorContract.Buy(
                    "用户3",
                    "C",
                    2,
                    777);
                buy.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                
                var boxId = GetOwnBoxList("用户2").Value.Last();
                var unbox = _delegatorContract.Unbox(
                    "用户3",
                    "C",
                    boxId);
                unbox.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                unbox.Error.ShouldContain("盲盒不属于用户");    
            }
            
        }

        [TestMethod]
        public void DelistMysteryBoxTest()
        {
            CreatAndListMysteryBox("你的名字", 3344,2);
            var delist = _delegatorContract.DelistDAC("管理员", "你的名字");
            delist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var publicTime = _dacMarketContract.CallViewMethod<Timestamp>(DACMarketMethod.GetPublicTime, new StringValue
            {
                Value = "你的名字"   
            });
            Logger.Info(publicTime);
            publicTime.Nanos.ShouldBe(0);
            publicTime.Seconds.ShouldBe(0);
        }

        [TestMethod]
        public void DelistError()
        {
            {
                //dacName 不存在
                var delist = _delegatorContract.DelistDAC("管理员", "不存在");
                delist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                delist.Error.ShouldContain("没有上架");
            }
            {
                //dacName 存在但未上架
                OnlyCreateMysteryBox("天气之子", 700, 5);
                var delist = _delegatorContract.DelistDAC("管理员", "天气之子");
                delist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                delist.Error.ShouldContain("没有上架");

            }
            {
                //重复下架
                CreatAndListMysteryBox("千与千寻", 600,2);
                var delist = _delegatorContract.DelistDAC("管理员", "千与千寻");
                delist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                
                var publicTime = _dacMarketContract.CallViewMethod<Timestamp>(DACMarketMethod.GetPublicTime, new StringValue
                {
                    Value = "千与千寻"   
                });
                publicTime.Nanos.ShouldBe(0);
                publicTime.Seconds.ShouldBe(0);
                
                var delisttwice = _delegatorContract.DelistDAC("管理员2", "千与千寻");
                delisttwice.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                delisttwice.Error.ShouldContain("千与千寻");

            }
        }

        [TestMethod]
        public void ConfirmCopyrightTest()
        {
            // 当前delegator合约没有实现这个接口方法，直接调用market合约方法结果 NodeValidationFailed
            var copyRightResult = _dacMarketContract.ExecuteMethodWithResult(DACMarketMethod.ConfirmCopyright,
                new ConfirmCopyrightInput
                {
                    DacName = "梅长苏",
                    CopyrightId = "meichangsu",
                    IsConfirm = true

                });
            copyRightResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var copyright = _dacMarketContract.CallViewMethod<DACCopyright>(DACMarketMethod.GetDACCopyright, new StringValue
            {
                Value = "梅长苏"
            });
            
            copyright.CopyrightId.ShouldBe("meichangsu");
            copyright.IsConfirmed.ShouldBe(true);
        }
        private void OnlyCreateMysteryBox(string dacName, long price, long circulation)
        {
            var protocol = _dacContract.CallViewMethod<DACProtocolInfo>(DACMethod.GetDACProtocolInfo, new StringValue
            {
                Value = dacName
            });
            if (!string.IsNullOrEmpty(protocol.DacName))
            {
                return;
            }

            var createResult = _delegatorContract.CreateDAC(
                "管理员",
                "机构",
                dacName,
                price, 
                circulation,
                "图片",
                "长方形",
                circulation,
                ""
            );
            createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        }
        private void CreatAndListMysteryBox(string dacName, long price, long circulation)
        {
            var protocol = _dacContract.CallViewMethod<DACProtocolInfo>(DACMethod.GetDACProtocolInfo, new StringValue
            {
                Value = dacName
            });
            if (!string.IsNullOrEmpty(protocol.DacName))
            {
                return;
            }

            var createResult = _delegatorContract.CreateDAC(
                "管理员",
                "机构",
                dacName,
                price, 
                circulation,
                "图片",
                "长方形",
                circulation,
                ""
            );
            createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                
            var auditresult = _delegatorContract.AuditDAC(
                "管理员",
                dacName,
                true
            );
            auditresult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var box = _delegatorContract.Box("管理员", dacName);
            box.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);


            var list = _delegatorContract.ListDAC(
                "管理员",
                dacName,
                TimestampHelper.GetUtcNow().AddSeconds(5));
            list.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var mint = _delegatorContract.MintDAC(
                "管理员",
                dacName,
                1,
                circulation);
            mint.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        }
        private long GetOwnerDacBalance(string dacName, string user)
        {
            
            var balance = _dacContract.CallViewMethod<DACBalance>(DACMethod.GetBalance,new Sinodac.Contracts.DAC.GetBalanceInput
            {
                DacName = dacName,
                Owner = GetUserAddress(user)
            });
            return balance.Balance;
        }
        private Address GetUserAddress(string user)
        {
            return _delegatorContract.CallViewMethod<Address>(DelegatorMethod.CalculateUserAddress,
                new StringValue
                {
                    Value = user
                });
        }
        private StringList GetOwnBoxList(string user)
        {
            var userAddr = _delegatorContract.CallViewMethod<Address>(DelegatorMethod.CalculateUserAddress,
                new StringValue
                {
                    Value = user
                });
                    
            var ownBoxList = _dacMarketContract.CallViewMethod<StringList>(DACMarketMethod.GetOwnBoxIdList, userAddr);
            Logger.Info(ownBoxList.Value);
            return ownBoxList;
        }
        
    }
}