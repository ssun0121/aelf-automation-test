using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTRecorder;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class MerkleTreeRecorderContractTest
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }

        private TokenContract _tokenContract;
        private GenesisContract _genesisContract;
        private MerkleTreeRecorder _merkleTreeRecorder;
        private MerkleTreeRecorderContractContainer.MerkleTreeRecorderContractStub _adminMerkleStub;

        private string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        private string TestAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        private List<string> Tester = new List<string>();
        private static string RpcUrl { get; } = "192.168.197.60:8001"; 
        private const long MaximalLeafCount = 1024;

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("ContractTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes");
            Tester = NodeOption.AllNodes.Select(l => l.Account).ToList();
            NodeManager = new NodeManager(RpcUrl);
            Logger.Info(RpcUrl);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
            _merkleTreeRecorder = new MerkleTreeRecorder(NodeManager, InitAccount,
                "RXcxgSXuagn8RrvhQAV81Z652EEYSwR6JLnqHYJ5UVpEptW8Y");
            Logger.Info($"MerkleTreeRecorder contract : {_merkleTreeRecorder.ContractAddress}");

            _adminMerkleStub =
                _merkleTreeRecorder.GetTestStub<MerkleTreeRecorderContractContainer.MerkleTreeRecorderContractStub>(
                    InitAccount);
//            _tokenContract.TransferBalance(InitAccount, TestAccount, 1000_00000000, "ELF");
//            foreach (var tester in Tester)
//            {
//                var balance = _tokenContract.GetUserBalance(tester, "ELF");
//                if (balance < 100_00000000)
//                {
//                    _tokenContract.TransferBalance(InitAccount, tester, 1000_00000000, "ELF");
//                }
//            }
        }

        [TestMethod]
        public async Task InitializeContract()
        {
            var result = await _adminMerkleStub.Initialize.SendAsync(new Empty());
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var owner = await _adminMerkleStub.GetOwner.CallAsync(new Empty());
            owner.ShouldBe(InitAccount.ConvertAddress());
        }

        [TestMethod]
        public async Task CreateRecorder()
        {
            var result = await _adminMerkleStub.CreateRecorder.SendAsync(new Recorder
            {
                Admin = InitAccount.ConvertAddress(),
                MaximalLeafCount = MaximalLeafCount
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var logs = result.TransactionResult.Logs.First(l => l.Name.Equals(nameof(RecorderCreated))).NonIndexed;
            var recorderInfo = RecorderCreated.Parser.ParseFrom(logs);
            var admin = recorderInfo.Admin;
            var recorderId = recorderInfo.RecorderId;
            var maxLeaf = recorderInfo.MaximalLeafCount;

            admin.ShouldBe(InitAccount.ConvertAddress());
            maxLeaf.ShouldBe(MaximalLeafCount);

            var getInfo = await _adminMerkleStub.GetRecorder.CallAsync(new RecorderIdInput
            {
                RecorderId = recorderId
            });
            getInfo.Admin.ShouldBe(admin);
            getInfo.MaximalLeafCount.ShouldBe(maxLeaf);
            Logger.Info($"recorder id: {recorderId}");
        }

        [TestMethod]
        public async Task GetRecorderCount()
        {
            var count = await _adminMerkleStub.GetRecorderCount.CallAsync(new Empty());
            Logger.Info($"For now contract has recorder count: {count.Value}");
        }

        [TestMethod]
        public async Task RecordMerkleTree()
        {
            var id = 0;
            var lastRecordedLeafIndex =
                await _adminMerkleStub.GetLastRecordedLeafIndex.CallAsync(new RecorderIdInput {RecorderId = id});
            Logger.Info($"{lastRecordedLeafIndex.Value}");

            var satisfiedTreeCount =
                await _adminMerkleStub.GetSatisfiedTreeCount.CallAsync(new RecorderIdInput {RecorderId = id});
            Logger.Info($"{satisfiedTreeCount.Value}");

            var lastLeafIndex = 127;
            var root = Hash.LoadFromHex("0x11c65a703cc98bbaa940e24047558f20e7bf24b661c0b8dede8a51034f1fb04b");
            var result = await _adminMerkleStub.RecordMerkleTree.SendAsync(new RecordMerkleTreeInput
            {
                RecorderId = id,
                LastLeafIndex = lastLeafIndex,
                MerkleTreeRoot = root
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var logs = result.TransactionResult.Logs.First(l => l.Name.Equals(nameof(MerkleTreeRecorded))).NonIndexed;
            var recordInfo = MerkleTreeRecorded.Parser.ParseFrom(logs);
            recordInfo.LastLeafIndex.ShouldBe(lastLeafIndex);

            var aLastRecordedLeafIndex =
                await _adminMerkleStub.GetLastRecordedLeafIndex.CallAsync(new RecorderIdInput {RecorderId = id});
            aLastRecordedLeafIndex.Value.ShouldBe(lastLeafIndex);

            var leafLocatedMerkleTree =
                await _adminMerkleStub.GetLeafLocatedMerkleTree.CallAsync(new GetLeafLocatedMerkleTreeInput
                {
                    RecorderId = id,
                    LeafIndex = lastLeafIndex
                });
            leafLocatedMerkleTree.MerkleTreeRoot.ShouldBe(root);
        }

        //GetLeafLocatedMerkleTree
        //GetMerkleTree
        //MerkleProof
        [TestMethod]
        public async Task GetLeafLocatedMerkleTree()
        {
            var id = 0;
            var leafIndex = 120;

            var leafLocatedMerkleTree =
                await _adminMerkleStub.GetLeafLocatedMerkleTree.CallAsync(new GetLeafLocatedMerkleTreeInput
                {
                    RecorderId = id,
                    LeafIndex = leafIndex
                });
            Logger.Info($"First leaf index: {leafLocatedMerkleTree.FirstLeafIndex} " +
                        $"Last leaf index: {leafLocatedMerkleTree.LastLeafIndex} " +
                        $"Merkle root : {leafLocatedMerkleTree.MerkleTreeRoot}");
        }

        [TestMethod]
        public async Task GetMerkleTree()
        {
            var id = 0;
            var lastLeafIndex = 255;
//            var root = Hash.LoadFromHex("0x737eb9095b7ed089749d14ec1ef9df2d1049daaa317e2787f94144fb4f3e9618");

            var merkleTree =
                await _adminMerkleStub.GetMerkleTree.CallAsync(new GetMerkleTreeInput
                {
                    RecorderId = id,
                    LastLeafIndex = lastLeafIndex
                });
//            merkleTree.MerkleTreeRoot.ShouldBe(root);
            merkleTree.LastLeafIndex.ShouldBe(lastLeafIndex);
            (merkleTree.FirstLeafIndex%128).ShouldBe(0);
            Logger.Info($"{merkleTree.FirstLeafIndex}, {merkleTree.LastLeafIndex}, \n" +
                        $"{merkleTree.MerkleTreeRoot}");
        }

        [TestMethod]
        public async Task MerkleProof()
        {
            var id = 0;
            var lastLeafIndex = 190;
            var uid = "0xa82872b96246dac512ddf0515f5da862a92ecebebcb92537b6e3e73199694c45";
            var leafNode = "0xf6c417da1c48c976e3c75d3b7ba11212b667cedd15a289505dd3de3266787ab8";
            var stringInfo =
                "0xf6c417da1c48c976e3c75d3b7ba11212b667cedd15a289505dd3de3266787ab8," +
                "0xea6de2cdea4eaaf68875faba19c447a1efcfff2b9085678ed6822568fbc23d87," +
                "0xcf7a350c6253d0d8a0938b2f094fee9c444c297c7984aac0786022ec7a3bf16b," +
                "0x39be09329acb35cfa58a9505fecff2b7d5fbd35b521291c3928ed61aa43df67d," +
                "0x2b825c35f0c0d329bf53999c117598b7a75b8bc7ee2fb21df6cdce6577cde80b," +
                "0x4efb16c18d59ae5626fe808343b433fb602f55c32942d5fa69a16f0fed778c32";
            var isLeftInfo = "false,true,true,true,true,true";

            var hashList = stringInfo.Split(",").ToList();
            var boolList = isLeftInfo.Split(",").Take(hashList.Count).ToList();
            var merklePathNodes = new List<MerklePathNode>();
            for (int i = 0; i < hashList.Count; i++)
            {
                var merkle = new MerklePathNode();
                merkle.Hash = Hash.LoadFromHex(hashList[i]);
                merkle.IsLeftChildNode = Boolean.Parse(boolList[i]);
                merklePathNodes.Add(merkle);
            }

            var merklePath = new MerklePath
            {
                MerklePathNodes = {merklePathNodes}
            };
            var merkleProof = await _adminMerkleStub.MerkleProof.CallAsync(new MerkleProofInput
            {
                RecorderId = id,
                LastLeafIndex = lastLeafIndex,
                LeafNode = Hash.LoadFromHex(leafNode),
                MerklePath = merklePath
            });
            merkleProof.Value.ShouldBeTrue();
        }

        [TestMethod]
        public async Task RecordMerkleTree_ErrorMerkle()
        {
            var id = 0;
            var root = Hash.LoadFromHex("0x9d09d3903c718819f4ac12bc28746c31eba3eec18cdd631c0bf39f4028add391");
            {
                var result = await _adminMerkleStub.RecordMerkleTree.SendAsync(new RecordMerkleTreeInput
                {
                    RecorderId = id,
                    MerkleTreeRoot = root,
                    LastLeafIndex = 1024
                });
                result.TransactionResult.Status.ShouldNotBe(TransactionResultStatus.Mined);
                result.TransactionResult.Error.ShouldContain("Satisfied MerkleTree absent.");
            }
            //lastRecordedLeafIndex > 32
            {
                var result = await _adminMerkleStub.RecordMerkleTree.SendAsync(new RecordMerkleTreeInput
                {
                    RecorderId = id,
                    MerkleTreeRoot = root,
                    LastLeafIndex = 32
                });
                result.TransactionResult.Status.ShouldNotBe(TransactionResultStatus.Mined);
                result.TransactionResult.Error.ShouldContain("It is not a new tree.");
            }
        }


        [TestMethod]
        public async Task GetLastRecordedLeafIndex()
        {
            var id = 0;
            var lastRecordedLeafIndex =
                await _adminMerkleStub.GetLastRecordedLeafIndex.CallAsync(new RecorderIdInput {RecorderId = id});
            Logger.Info($"{lastRecordedLeafIndex.Value}");

            var satisfiedTreeCount =
                await _adminMerkleStub.GetSatisfiedTreeCount.CallAsync(new RecorderIdInput {RecorderId = id});
            Logger.Info($"{satisfiedTreeCount.Value}");
        }
    }
}