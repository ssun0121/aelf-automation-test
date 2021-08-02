using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Bridge;
using AElf.Contracts.MerkleTreeGeneratorContract;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ReceiptMakerContract;
using AElf.Contracts.Regiment;
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
using MTRecorder;
using Shouldly;
using GetMerkleTreeInput = AElf.Contracts.MerkleTreeGeneratorContract.GetMerkleTreeInput;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class SwapTokenContractsTests
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }

        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;
        private ParliamentContract _parliament;

        private OracleContract _oracleContract;
        private BridgeContract _bridgeContract;
        private MerkleTreeRecorderContract _merkleTreeRecorderContract;
        private MerkleTreeGeneratorContract _merkleTreeGeneratorContract;
        private RegimentContract _regimentContract;
        private Address _stringAggregator;
        private Address _defaultParliament;

        private string TestAccount { get; } = "2RehEQSpXeZ5DUzkjTyhAkr9csu7fWgE5DAuB2RaKQCpdhB8zC";
        //2qjoi1ZxwmH2XqQFyMhykjW364Ce2cBxxRp47zrvZFT4jqW8Ru
        //ZrAFaqdr79MWYkxA49Hp2LUdSVHdP6fJh3kDw4jmgC7HTgrni
        private string InitAccount { get; } = "ZrAFaqdr79MWYkxA49Hp2LUdSVHdP6fJh3kDw4jmgC7HTgrni";
        private string OtherNode { get; } = "sjzNpr5bku3ZyvMqQrXeBkXGEvG2CTLA2cuNDfcDMaPTTAqEy";

        private readonly List<string> _associationMember = new List<string>
        {
            "bBEDoBnPK28bYFf1M28hYLFVuGnkPkNR6r59XxGNmYfr7aRff",
            "FHdcx45K5kovWsAKSb3rrdyNPFus8eoJ1XTQE7aXFHTgfpgzN",
            "Gazx8ei5CnzPRnaFGojZemak8VJsC5ETqzC1CGqNb76ZM3BMY",
            "Muca5ZVorWCV51BNATadyC6f72871aZm2WnHfsrkioUHwyP8j",
            "bP7RkGBN5vK1wDFjuUbWh49QVLMWAWMuccYK1RSh9hRrVcP7v"
        };
        
        private readonly List<string> _stableMember = new List<string>
        {
            "2sKRVAjvtMcdKLA21qr1i59M57GX69QjKKSbJ2LY2SAQeSdsgS",
            "2nd5YY9cPbz2VkXAMKMSo4ezaKqJBo3EA73t7fBqan59RrxUnS",
            "5GEAaW3NjLQe5D3VfwLaW9PchNwuBJARVGSDfvLwbb5XDUXMZ",
            "GsiwFtm9K2iRWrPUsRSCriqRAcfTqUurp5kmWokjQJcf5TcSG",
            "UdDGar8wEkrgEv8zNnmrFFT7NSMvEimyjqxoPTQnkQ2AW69w5"
        };

        //2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS
        //2WHXRoLRjbUTDQsuqR5CntygVfnDb125qdJkudev4kVNbLhTdG
        //2RHf2fxsnEaM3wb6N1yGqPupNZbcCY98LgWbGSFWmWzgEs5Sjo
        //2NxwCPAGJr4knVdmwhb1cK7CkZw5sMJkRDLnT7E2GoDP2dy5iZ
        //sr4zX6E7yVVL7HevExVcWv2ru3HSZakhsJMXfzxzfpnXofnZw
        //xsnQafDAhNTeYcooptETqWnYBksFGGXxfcQyJJ5tmu6Ak9ZZt
        private string _oracleContractAddress = "2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS";
        private string _bridgeContractAddress = "2WHXRoLRjbUTDQsuqR5CntygVfnDb125qdJkudev4kVNbLhTdG";
        private string _merkleTreeRecorderContractAddress = "2RHf2fxsnEaM3wb6N1yGqPupNZbcCY98LgWbGSFWmWzgEs5Sjo";
        private string _merkleTreeGeneratorContractAddress = "2NxwCPAGJr4knVdmwhb1cK7CkZw5sMJkRDLnT7E2GoDP2dy5iZ";
        private string _regimentContractAddress = "sr4zX6E7yVVL7HevExVcWv2ru3HSZakhsJMXfzxzfpnXofnZw";
        private string _stringAggregatorAddress = "xsnQafDAhNTeYcooptETqWnYBksFGGXxfcQyJJ5tmu6Ak9ZZt";

        private string Password { get; } = "12345678";
        private static string RpcUrl { get; } = "http://13.232.173.152:8000";
        private string Symbol { get; } = "PORT";
        private string SwapSymbol { get; } = "ELF";
        private readonly bool isNeedInitialize = false;
        private string _regiment = "FbHtRPx1jLsCRWFudZh63BsjmFJXRi9gFBBgS1Y9PnYW7KACe";
        private long payment = 100000000;
        private int maximalLeafCount = 256;
        private string PairId = "324ee6644b77496e5589a49443fad506fdc968223ce94e1fdfdd9ef1d4b4dcb2";

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("OracleContactTest");
            Logger = Log4NetHelper.GetLogger();

            NodeInfoHelper.SetConfig("nodes");
            NodeManager = new NodeManager(RpcUrl);
            AuthorityManager = new AuthorityManager(NodeManager, InitAccount, Password);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount, Password);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount, Password);
            _parliament = _genesisContract.GetParliamentContract(InitAccount, Password);
            CreateToken();
            _oracleContract = _oracleContractAddress == ""
                ? new OracleContract(NodeManager, InitAccount)
                : new OracleContract(NodeManager, InitAccount, _oracleContractAddress);
            _bridgeContract = _bridgeContractAddress == ""
                ? new BridgeContract(NodeManager, InitAccount)
                : new BridgeContract(NodeManager, InitAccount, _bridgeContractAddress);
            _merkleTreeRecorderContract = _merkleTreeRecorderContractAddress == ""
                ? new MerkleTreeRecorderContract(NodeManager, InitAccount)
                : new MerkleTreeRecorderContract(NodeManager, InitAccount, _merkleTreeRecorderContractAddress);
            _merkleTreeGeneratorContract = _merkleTreeGeneratorContractAddress == ""
                ? new MerkleTreeGeneratorContract(NodeManager, InitAccount)
                : new MerkleTreeGeneratorContract(NodeManager, InitAccount, _merkleTreeGeneratorContractAddress);
            _regimentContract = _regimentContractAddress == ""
                ? new RegimentContract(NodeManager, InitAccount)
                : new RegimentContract(NodeManager, InitAccount, _regimentContractAddress);
            _stringAggregator = _stringAggregatorAddress == ""
                ? AuthorityManager.DeployContractWithAuthority(InitAccount, "AElf.Contracts.StringAggregator")
                : Address.FromBase58(_stringAggregatorAddress);
             //Transfer();
            if (!isNeedInitialize) return;
            InitializeContract();
        }

        [TestMethod]
        public void InitializeContract()
        {
            var initializeOracle = _oracleContract.ExecuteMethodWithResult(OracleMethod.Initialize,
                new AElf.Contracts.Oracle.InitializeInput
                {
                    RegimentContractAddress = _regimentContract.Contract,
                    MinimumOracleNodesCount = 5,
                    DefaultRevealThreshold = 3,
                    DefaultAggregateThreshold = 3
                });
            initializeOracle.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var initializeBridge = _bridgeContract.ExecuteMethodWithResult(BridgeMethod.Initialize,
                new AElf.Contracts.Bridge.InitializeInput
                {
                    OracleContractAddress = _oracleContract.Contract,
                    RegimentContractAddress = _regimentContract.Contract,
                    MerkleTreeRecorderContractAddress = _merkleTreeRecorderContract.Contract,
                    MerkleTreeGeneratorContractAddress = _merkleTreeGeneratorContract.Contract,
                    MerkleTreeLeafLimit = maximalLeafCount
                });
            initializeBridge.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        [DataRow(false)]
        public void CreateRegiment(bool isApproveToJoin)
        {
            var list = new List<Address>();
            _associationMember.ForEach(l => { list.Add(l.ConvertAddress()); });
            var result =
                _oracleContract.ExecuteMethodWithResult(OracleMethod.CreateRegiment, new CreateRegimentInput
                {
                    IsApproveToJoin = isApproveToJoin,
                    InitialMemberList = {list}
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var logEventDto = result.Logs.First(l => l.Name.Contains(nameof(RegimentCreated))).NonIndexed;
            var regimentCreated = RegimentCreated.Parser.ParseFrom(ByteString.FromBase64(logEventDto));
            Logger.Info($"Manager: {regimentCreated.Manager}\n" +
                        $"Create Time: {regimentCreated.CreateTime}\n" +
                        $"Regiment Address: {regimentCreated.RegimentAddress}\n");
            regimentCreated.Manager.ShouldBe(_oracleContract.CallAccount);
        }

        #region bridge

        [TestMethod]
        public void CreateSwap()
        {
            var swapRatio = new SwapRatio
            {
                OriginShare = 100_00000000,
                TargetShare = 1
            };
            var depositAmount = 10000000_00000000;
            var originTokenSizeInByte = 32;
            _tokenContract.ApproveToken(InitAccount, _bridgeContractAddress, depositAmount, SwapSymbol);
            var balance = _tokenContract.GetUserBalance(InitAccount, SwapSymbol);
            var result = _bridgeContract.ExecuteMethodWithResult(BridgeMethod.CreateSwap, new CreateSwapInput
            {
                OriginTokenNumericBigEndian = true,
                OriginTokenSizeInByte = originTokenSizeInByte,
                RegimentAddress = _regiment.ConvertAddress(),
                SwapTargetTokenList =
                {
                    new SwapTargetToken
                    {
                        DepositAmount = depositAmount,
                        SwapRatio = swapRatio,
                        TargetTokenSymbol = SwapSymbol
                    }
                }
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var pairId = Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            var swapId = SwapPairAdded.Parser
                .ParseFrom(ByteString.FromBase64(result.Logs.First(l => l.Name.Contains(nameof(SwapPairAdded)))
                    .NonIndexed))
                .SwapId;
            var recorder = Recorder.Parser.ParseFrom(ByteString.FromBase64(result.Logs
                .First(l => l.Name.Contains(nameof(RecorderCreated)))
                .NonIndexed));
            Logger.Info($"{pairId}");
            
            pairId.ShouldBe(swapId);
            recorder.Admin.ShouldBe(_bridgeContract.Contract);
            recorder.MaximalLeafCount.ShouldBe(maximalLeafCount);
            var afterBalance = _tokenContract.GetUserBalance(InitAccount, SwapSymbol);
            afterBalance.ShouldBe(balance - depositAmount);
            
            var swapPair = GetSwapPair(swapId);
            swapPair.DepositAmount.ShouldBe(depositAmount);
            swapPair.SwappedAmount.ShouldBe(0);
            swapPair.SwappedTimes.ShouldBe(0);
            swapPair.SwapRatio.ShouldBe(swapRatio);
            swapPair.TargetTokenSymbol.ShouldBe(SwapSymbol);
            swapPair.OriginTokenSizeInByte.ShouldBe(originTokenSizeInByte);
        }

        [TestMethod]
        public void SwapToken()
        {
            var sender = "WRy3ADLZ4bEQTn86ENi5GXi5J1YyHp9e99pPso84v2NJkfn5k";
            var receiveAddress = sender;
            var originAmount = "2182943732911645400000";
            var receiptId = 512;
            var balance = _tokenContract.GetUserBalance(receiveAddress, SwapSymbol);
            var expectedAmount = long.Parse(originAmount.Substring(0, originAmount.Length - 10));
            _bridgeContract.SetAccount(receiveAddress);
            var result = _bridgeContract.ExecuteMethodWithResult(BridgeMethod.SwapToken, new SwapTokenInput
            {
                OriginAmount = originAmount,
                ReceiptId = receiptId,
                SwapId = Hash.LoadFromHex(PairId)
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logs = result.Logs.First(l => l.Name.Equals(nameof(Transferred)));
            var amount = Transferred.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed)).Amount;
            amount.ShouldBe(expectedAmount);
            var after = _tokenContract.GetUserBalance(receiveAddress, SwapSymbol);
            after.ShouldBe(balance + expectedAmount);
            
            var checkAmount = _bridgeContract.CallViewMethod<SwapAmounts>(BridgeMethod.GetSwapAmounts, new GetSwapAmountsInput
            {
                SwapId = Hash.LoadFromHex(PairId),
                ReceiptId = receiptId
            });
            checkAmount.Receiver.ShouldBe(receiveAddress.ConvertAddress());
            checkAmount.ReceivedAmounts[SwapSymbol].ShouldBe(expectedAmount);
            
            var checkSwappedReceiptIdList = _bridgeContract.CallViewMethod<ReceiptIdList>(BridgeMethod.GetSwappedReceiptIdList,
                new GetSwappedReceiptIdListInput
                {
                    ReceiverAddress = receiveAddress.ConvertAddress(),
                    SwapId = Hash.LoadFromHex(PairId)
                });
            checkSwappedReceiptIdList.Value.ShouldContain(receiptId);
        }


        [TestMethod]
        public void Deposit()
        {
            var depositAmount = 5000000_00000000;
            var pairId = Hash.LoadFromHex(PairId);
            _tokenContract.ApproveToken(InitAccount, _bridgeContractAddress, depositAmount, SwapSymbol);

            var swapPairInfo =
                _bridgeContract.CallViewMethod<SwapInfo>(BridgeMethod.GetSwapInfo, Hash.LoadFromHex(PairId));
            var manager =
                _regimentContract.CallViewMethod<RegimentInfo>(RegimentMethod.GetRegimentInfo,
                    swapPairInfo.RegimentAddress).Manager ;
            var swapPair = _bridgeContract.CallViewMethod<SwapPair>(BridgeMethod.GetSwapPair, new GetSwapPairInput
                {SwapId = pairId, TargetTokenSymbol = SwapSymbol});
            
            _bridgeContract.SetAccount(manager.ToBase58());
            var result = _bridgeContract.ExecuteMethodWithResult(BridgeMethod.Deposit, new DepositInput
            {
                SwapId = pairId,
                TargetTokenSymbol = SwapSymbol,
                Amount = depositAmount
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var afterSwapPair = _bridgeContract.CallViewMethod<SwapPair>(BridgeMethod.GetSwapPair, new GetSwapPairInput
                {SwapId = pairId, TargetTokenSymbol = SwapSymbol});
            afterSwapPair.DepositAmount.ShouldBe(swapPair.DepositAmount + depositAmount);
        }

        [TestMethod]
        public void Withdraw()
        {
            var pairId = Hash.LoadFromHex(PairId);

            var swapPairInfo =
                _bridgeContract.CallViewMethod<SwapInfo>(BridgeMethod.GetSwapInfo, Hash.LoadFromHex(PairId));
            var manager =
                _regimentContract.CallViewMethod<RegimentInfo>(RegimentMethod.GetRegimentInfo, swapPairInfo.RegimentAddress).Manager;
            var swapPair = _bridgeContract.CallViewMethod<SwapPair>(BridgeMethod.GetSwapPair, new GetSwapPairInput
                {SwapId = pairId, TargetTokenSymbol = SwapSymbol});

            var managerBalance = _tokenContract.GetUserBalance(manager.ToBase58(), SwapSymbol);
            var result = _bridgeContract.ExecuteMethodWithResult(BridgeMethod.Withdraw,new WithdrawInput
            {
                SwapId = pairId,
                Amount = swapPair.DepositAmount,
                TargetTokenSymbol = SwapSymbol
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var afterSwapPair = _bridgeContract.CallViewMethod<SwapPair>(BridgeMethod.GetSwapPair, new GetSwapPairInput
                {SwapId = pairId, TargetTokenSymbol = SwapSymbol});
            afterSwapPair.DepositAmount.ShouldBe(0);

            var afterManagerBalance = _tokenContract.GetUserBalance(manager.ToBase58(), SwapSymbol);
            afterManagerBalance.ShouldBe(managerBalance + swapPair.DepositAmount);
        }

        [TestMethod]
        public void GetReceiptHash()
        {
            var receiptHash = _bridgeContract.CallViewMethod<Hash>(BridgeMethod.GetReceiptHash, new Int64Value{Value = 172});
            Logger.Info(receiptHash);
        }

        [TestMethod]
        public void GetReceiptHashList()
        {
            var receiptHashList =
                _bridgeContract.CallViewMethod<GetReceiptHashListOutput>(BridgeMethod.GetReceiptHashList, new GetReceiptHashListInput
                {
                    FirstLeafIndex = 0,
                    LastLeafIndex = 599
                });
            Logger.Info(receiptHashList.ReceiptHashList.Count);
        }

        #endregion

        #region Merkle

        /*
        //Generator
        GetReceiptMaker,
        GetMerkleTree,
        GetFullTreeCount,
        GetMerklePath
        //Recorder
        GetRecorder,
        GetMerkleTree,
        MerkleProof,
        GetOwner,
        GetRecorderCount,
        GetLeafLocatedMerkleTree,
        GetLastRecordedLeafIndex,
        GetSatisfiedTreeCount
         */

        [TestMethod]
        public void GetLastRecordedLeafIndex_GetLocatedMerkleTree()
        {
            var leaf = _merkleTreeRecorderContract.CallViewMethod<Int64Value>(MerkleTreeRecorderMethod.GetLastRecordedLeafIndex, 
                new RecorderIdInput { RecorderId = 0 });
            var localTree = _merkleTreeRecorderContract.CallViewMethod<GetLeafLocatedMerkleTreeOutput>(
                MerkleTreeRecorderMethod.GetLeafLocatedMerkleTree, new GetLeafLocatedMerkleTreeInput
                {
                    LeafIndex = leaf.Value,
                    RecorderId = 0
                });
            Logger.Info(localTree);
        }

        [TestMethod]
        public void GetMerkleTree()
        {
            var receiptCount = _bridgeContract.CallViewMethod<Int64Value>(BridgeMethod.GetReceiptCount, new Empty());
            Logger.Info(receiptCount.Value);
            var recorderId = 0;
            var leaf = _merkleTreeRecorderContract.CallViewMethod<Int64Value>(MerkleTreeRecorderMethod.GetLastRecordedLeafIndex, 
                new RecorderIdInput { RecorderId = recorderId });
            var localTree = _merkleTreeRecorderContract.CallViewMethod<GetLeafLocatedMerkleTreeOutput>(
                MerkleTreeRecorderMethod.GetLeafLocatedMerkleTree, new GetLeafLocatedMerkleTreeInput
                {
                    LeafIndex = leaf.Value,
                    RecorderId = recorderId
                });
            Logger.Info(localTree);
            
            receiptCount.Value.ShouldBe(leaf.Value + 1);

            var fullTree = _merkleTreeGeneratorContract.CallViewMethod<Int64Value>(MerkleTreeGeneratorMethod.GetFullTreeCount,
                _bridgeContract.Contract);
            Logger.Info(fullTree.Value);
            var merkle = _merkleTreeGeneratorContract.CallViewMethod<GetMerkleTreeOutput>(MerkleTreeGeneratorMethod.GetMerkleTree,
                new GetMerkleTreeInput
                {
                    ReceiptMakerAddress = _bridgeContract.Contract,
                    ExpectedFullTreeIndex = fullTree.Value
                });
            merkle.IsFullTree.ShouldBeFalse();
            merkle.FirstIndex.ShouldBe(localTree.FirstLeafIndex);
            merkle.LastIndex.ShouldBe(leaf.Value);

            var merkleTree =
                _merkleTreeRecorderContract.CallViewMethod<MTRecorder.MerkleTree>(MerkleTreeRecorderMethod.GetMerkleTree,
                    new MTRecorder.GetMerkleTreeInput
                    {
                        RecorderId = recorderId,
                        LastLeafIndex = leaf.Value,
                    });
            merkleTree.FirstLeafIndex.ShouldBe(merkle.FirstIndex);
            merkleTree.LastLeafIndex.ShouldBe(merkle.LastIndex);
            merkleTree.MerkleTreeRoot.ShouldBe(merkle.MerkleTreeRoot);
        }

        [TestMethod]
        public void GetMerkleTreePath()
        {
            var receiptCount = _bridgeContract.CallViewMethod<Int64Value>(BridgeMethod.GetReceiptCount, new Empty());
            Logger.Info(receiptCount.Value);
            var recorderId = 0;
            var leaf = _merkleTreeRecorderContract.CallViewMethod<Int64Value>(MerkleTreeRecorderMethod.GetLastRecordedLeafIndex, 
                new RecorderIdInput { RecorderId = recorderId });
            var maker = _merkleTreeGeneratorContract.CallViewMethod<GetReceiptMakerOutput>(
                MerkleTreeGeneratorMethod.GetReceiptMaker, _bridgeContract.Contract);
            var merkleTree =
                _merkleTreeRecorderContract.CallViewMethod<MTRecorder.MerkleTree>(MerkleTreeRecorderMethod.GetMerkleTree,
                    new MTRecorder.GetMerkleTreeInput
                    {
                        RecorderId = recorderId,
                        LastLeafIndex = 255,
                    });
            Logger.Info($"{leaf.Value} {merkleTree.FirstLeafIndex} {merkleTree.LastLeafIndex}");
            long id = 0;
            var merklePath = _merkleTreeGeneratorContract.CallViewMethod<MerklePath>(MerkleTreeGeneratorMethod.GetMerklePath,
                new GetMerklePathInput
                {
                    ReceiptId = id,
                    FirstLeafIndex = 0,
                    LastLeafIndex = 0,
                    ReceiptMaker = maker.ReceiptMakerAddress
                });
            var receiptHash = _bridgeContract.CallViewMethod<Hash>(BridgeMethod.GetReceiptHash, new Int64Value{Value = id});            
            var root = merklePath.ComputeRootWithLeafNode(receiptHash);
            root.ShouldBe(merkleTree.MerkleTreeRoot);
            
            var merkleProof =
                _merkleTreeRecorderContract.CallViewMethod<BoolValue>(MerkleTreeRecorderMethod.MerkleProof,
                    new MerkleProofInput
                    {
                        RecorderId = recorderId,
                        MerklePath = merklePath,
                        LeafNode = receiptHash,
                        LastLeafIndex = 255
                    });
            merkleProof.Value.ShouldBeTrue();
        }

        #endregion

        [TestMethod]
        public void GetReceiptCount()
        {
            var receiptCount = _bridgeContract.CallViewMethod<Int64Value>(BridgeMethod.GetReceiptCount, new Empty());
            var maker = _merkleTreeGeneratorContract.CallViewMethod<GetReceiptMakerOutput>(
                MerkleTreeGeneratorMethod.GetReceiptMaker, _bridgeContract.Contract);
            maker.ReceiptMakerAddress.ShouldBe(_bridgeContract.Contract);
            Logger.Info(receiptCount);
            Logger.Info(maker);
        }

        [TestMethod]
        public void GetSwappedReceiptIdList()
        {
            var list = _bridgeContract.CallViewMethod<ReceiptIdList>(BridgeMethod.GetSwappedReceiptIdList,
                new GetSwappedReceiptIdListInput
                {
                    ReceiverAddress = OtherNode.ConvertAddress(),
                    SwapId = Hash.LoadFromHex(PairId)
                });
            Logger.Info(list);
        }

        [TestMethod]
        public void GetSwapInfo()
        {
            var info = _bridgeContract.CallViewMethod<SwapInfo>(BridgeMethod.GetSwapInfo, Hash.LoadFromHex(PairId));
            Logger.Info(info);
            var swapPair = _bridgeContract.CallViewMethod<SwapPair>(BridgeMethod.GetSwapPair, new GetSwapPairInput
                {SwapId = Hash.LoadFromHex(PairId), TargetTokenSymbol = SwapSymbol});
            Logger.Info(swapPair);
        }

        [TestMethod]
        public void ChangeMaximalLeafCount()
        {
            var authority = new AuthorityManager(NodeManager, InitAccount);
            var result = authority.ExecuteTransactionWithAuthority(_bridgeContract.ContractAddress,
                nameof(ChangeMaximalLeafCount), new Int32Value {Value = 1024}, InitAccount);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void Transfer()
        {
            foreach (var member in _associationMember)
            {
                _tokenContract.TransferBalance(InitAccount, member, payment * 10, SwapSymbol);
            }
            _tokenContract.IssueBalance(InitAccount, _associationMember.First(), payment * 10000, Symbol);
            _tokenContract.ApproveToken(_associationMember.First(), _oracleContract.ContractAddress, payment * 10000, Symbol);
        }

        [TestMethod]
        public void GetRecord()
        {
            var recorderId = 0;
            var record =
                _merkleTreeRecorderContract.CallViewMethod<Recorder>(MerkleTreeRecorderMethod.GetRecorder,
                    new RecorderIdInput {RecorderId = recorderId});
            Logger.Info(record);
            var getLastRecordedLeafIndex =
                _merkleTreeRecorderContract.CallViewMethod<Int64Value>(
                    MerkleTreeRecorderMethod.GetLastRecordedLeafIndex, new RecorderIdInput
                    { RecorderId = recorderId });
            Logger.Info(getLastRecordedLeafIndex);
        }

        [TestMethod]
        public void Check()
        {
            var get = _bridgeContract.CallViewMethod<Address>(BridgeMethod.GetRegimentAddressByRecorderId,
                new Int64Value {Value = 0});
            Logger.Info(get.ToBase58());
        }

        private SwapPair GetSwapPair(Hash pairId)
        {
            return _bridgeContract.CallViewMethod<SwapPair>(BridgeMethod.GetSwapPair, new GetSwapPairInput
            {
                SwapId = pairId,
                TargetTokenSymbol = SwapSymbol
            });
        }

        [TestMethod]
        public void CreateToken()
        {
            var tokenInfo = _tokenContract.GetTokenInfo(Symbol);
            if (!tokenInfo.Equals(new TokenInfo()))
            {
                Logger.Info($"{Symbol} is already created");
                return;
            }

            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create,
                new CreateInput
                {
                    TokenName = "Portal Token",
                    Symbol = Symbol,
                    TotalSupply = 100_000_000_00000000,
                    Issuer = InitAccount.ConvertAddress(),
                    Decimals = 8,
                    IsBurnable = true
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
    }
}