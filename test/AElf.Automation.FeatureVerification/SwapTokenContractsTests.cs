using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Association;
using AElf.Contracts.Bridge;
using AElf.Contracts.MerkleTreeGeneratorContract;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ReceiptMakerContract;
using AElf.Contracts.Regiment;
using AElf.Standards.ACS3;
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

        private string TestAccount { get; } = "2R1rgEKkG84XGtbx6fvxExzChaXEyJrfSnvMpuKCGrUFoR5SKz";
        //2qjoi1ZxwmH2XqQFyMhykjW364Ce2cBxxRp47zrvZFT4jqW8Ru
        //ZrAFaqdr79MWYkxA49Hp2LUdSVHdP6fJh3kDw4jmgC7HTgrni
        //2R1rgEKkG84XGtbx6fvxExzChaXEyJrfSnvMpuKCGrUFoR5SKz
        //2ExtaRkjDiFhkGH8hwLZYVpRAnXe7awa25C61KVWy47uwnRw4s
        private string InitAccount { get; } = "2R1rgEKkG84XGtbx6fvxExzChaXEyJrfSnvMpuKCGrUFoR5SKz";
        private string Admin { get; } = "2ExtaRkjDiFhkGH8hwLZYVpRAnXe7awa25C61KVWy47uwnRw4s";

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
            "2cGNQwzjG6BMzqkqXKCv68kcMSpURVboFyaxiiKJRURNJEsDUc",
            "xWeDcoaWYYDvN26B6dLR1iknqqCCBmrYpx6ZweLHMiBwGD57e",
            "3W7hGmiBTFfhvVYvsD51qfZaHGVpWucMZV63wPURSa3PQX2LT",
            "Gn2gzLLhTs8i5LTebz2tgPvZnXUPE6NQCnzXrTwkYhpYzBfbo",
            "2eSFiJgbN8SFRnZCNsfPov1MRUZTBrnpZdC4LA2QwzWioEPxez"
        };
        
        private readonly List<string> _newAssociationMember = new List<string>
        {
            "cBf8d7nQHLFsGyaFyim599zSKRxtX8p9A1Zgs6Hc2sCm1C19r",
            "2XjVtnnHG7LgqTP1rFqNu1uk24y5iCCQ8cNBn9q87GqChX1Ffi",
            "7KArmpz3SS2hsH5tqDfBM2TXM1DuqxjRw8w3CCh6yypGvbegC",
            "2R1rgEKkG84XGtbx6fvxExzChaXEyJrfSnvMpuKCGrUFoR5SKz",
            "2aaA2gew3o8kE6v5BDXzJ6fxTM6iec3q2ALav5VGuKX1ztyj3S"
        };

        //2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS
        //2WHXRoLRjbUTDQsuqR5CntygVfnDb125qdJkudev4kVNbLhTdG
        //2RHf2fxsnEaM3wb6N1yGqPupNZbcCY98LgWbGSFWmWzgEs5Sjo
        //2NxwCPAGJr4knVdmwhb1cK7CkZw5sMJkRDLnT7E2GoDP2dy5iZ
        //sr4zX6E7yVVL7HevExVcWv2ru3HSZakhsJMXfzxzfpnXofnZw
        //xsnQafDAhNTeYcooptETqWnYBksFGGXxfcQyJJ5tmu6Ak9ZZt
        //
        private string _oracleContractAddress = "2WHXRoLRjbUTDQsuqR5CntygVfnDb125qdJkudev4kVNbLhTdG";
        private string _bridgeContractAddress = "2RHf2fxsnEaM3wb6N1yGqPupNZbcCY98LgWbGSFWmWzgEs5Sjo";
        private string _merkleTreeRecorderContractAddress = "sr4zX6E7yVVL7HevExVcWv2ru3HSZakhsJMXfzxzfpnXofnZw";
        private string _merkleTreeGeneratorContractAddress = "xsnQafDAhNTeYcooptETqWnYBksFGGXxfcQyJJ5tmu6Ak9ZZt";
        private string _regimentContractAddress = "2NxwCPAGJr4knVdmwhb1cK7CkZw5sMJkRDLnT7E2GoDP2dy5iZ";
        private string _stringAggregatorAddress = "2nyC8hqq3pGnRu8gJzCsTaxXB6snfGxmL2viimKXgEfYWGtjEh";
        
        // private string _oracleContractAddress = "2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS";
        // private string _bridgeContractAddress = "2WHXRoLRjbUTDQsuqR5CntygVfnDb125qdJkudev4kVNbLhTdG";
        // private string _merkleTreeRecorderContractAddress = "2RHf2fxsnEaM3wb6N1yGqPupNZbcCY98LgWbGSFWmWzgEs5Sjo";
        // private string _merkleTreeGeneratorContractAddress = "2NxwCPAGJr4knVdmwhb1cK7CkZw5sMJkRDLnT7E2GoDP2dy5iZ";
        // private string _regimentContractAddress = "sr4zX6E7yVVL7HevExVcWv2ru3HSZakhsJMXfzxzfpnXofnZw";
        // private string _stringAggregatorAddress = "xsnQafDAhNTeYcooptETqWnYBksFGGXxfcQyJJ5tmu6Ak9ZZt";
        private string Password { get; } = "12345678";
        private static string RpcUrl { get; } = "http://18.185.93.36:8000";
        // private static string RpcUrl { get; } = "http://192.168.66.9:8000";
        private string Symbol { get; } = "PORT";
        private string SwapSymbol { get; } = "ELF";
        private string UsdSymbol { get; } = "AEUSD";

        private readonly bool isNeedInitialize = false;
        private readonly bool isNeedCreate = false;
        //DKQJtqZDqCfUDFPysHqqDeZNHdzHBmKTZe1bedcRnY5B147Go
        private string _regiment = "DKQJtqZDqCfUDFPysHqqDeZNHdzHBmKTZe1bedcRnY5B147Go";
        private long payment = 0;
        private int maximalLeafCount = 1024;
        private string ETHELFPairId = "bb16f381b0f2e795a988285dec3a68affacdccd7d3ac2e74edc808c102efcd95";
        private string ETHUSDTPairId = "";
        private string BSCPairId = "77b29565b24c6d4fdcab2d9d192f08354b68f37b06b82c7d3656f349056da0d4";
        //bb16f381b0f2e795a988285dec3a68affacdccd7d3ac2e74edc808c102efcd95
        //caaa8140bb484e1074872350687df0b1262436cdec3042539e78eb615b376d5e
        //03ccf1c18a7a82391f936ce58db7ee4b6fd9eca4a1ae7ee930f4a750a0a5653a
        
        //cf25e2d376b20131622ae1b8c8db72d040efe8cf5ba1fc5316873cff2a8ebebe --USD
        //03ccf1c18a7a82391f936ce58db7ee4b6fd9eca4a1ae7ee930f4a750a0a5653a --ELF
        //vfACnL3CsejPVxCjjensoQBNvDMPtid9cSGZVqjMAXKLtZKgo
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
             // Transfer();
             if(!isNeedCreate) return;
             // CreateToken(UsdSymbol,6,1000000000000000);
             // CreateToken(Symbol,8,100000000000000000);
             if (!isNeedInitialize) return;
             InitializeContract();
        }

        [TestMethod]
        public void CreateAssociation()
        {
            var list = new List<Address>();

            _newAssociationMember.ForEach(l => { list.Add(l.ConvertAddress()); });
            var association = _genesisContract.GetAssociationAuthContract(InitAccount);
            var input = new CreateOrganizationInput
            {
                CreationToken = HashHelper.ComputeFrom("organization"),
                OrganizationMemberList = new OrganizationMemberList
                {
                    OrganizationMembers = {list}
                },
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MaximalAbstentionThreshold = 1,
                    MaximalRejectionThreshold = 1,
                    MinimalApprovalThreshold = 4, 
                    MinimalVoteThreshold = 4
                },
                ProposerWhiteList = new ProposerWhiteList
                {
                    Proposers = {list}
                }
            };
            var organization = association.CreateOrganization(input);
            Logger.Info(organization.ToBase58());
            var info = association.GetOrganization(organization);
            Logger.Info(info.OrganizationMemberList.OrganizationMembers.Count);
            Logger.Info(info.CreationToken.ToHex());
            Logger.Info(info.ProposerWhiteList.Proposers.Count);
        }

        [TestMethod]
        public void InitializeContract()
        {
            // _tokenContract.TransferBalance(InitAccount, Admin, 10000000000);
            _oracleContract.SetAccount(Admin);
            var initializeOracle = _oracleContract.ExecuteMethodWithResult(OracleMethod.Initialize,
                new AElf.Contracts.Oracle.InitializeInput
                {
                    RegimentContractAddress = _regimentContract.Contract,
                    MinimumOracleNodesCount = 5,
                    DefaultRevealThreshold = 3,
                    DefaultAggregateThreshold = 3
                });
            initializeOracle.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _bridgeContract.SetAccount(Admin);
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
            Logger.Info(_oracleContract.CallViewMethod<Address>(OracleMethod.GetController,new Empty()));
        }

        [TestMethod]
        [DataRow(false)]
        public void CreateRegiment(bool isApproveToJoin)
        {
            var list = new List<Address>();
            _associationMember.ForEach(l => { list.Add(l.ConvertAddress()); });
            _oracleContract.SetAccount(Admin);
            var result =
                _oracleContract.ExecuteMethodWithResult(OracleMethod.CreateRegiment, new CreateRegimentInput
                {
                    IsApproveToJoin = isApproveToJoin,
                    InitialMemberList = {list},
                    Manager = Admin.ConvertAddress()
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var logEventDto = result.Logs.First(l => l.Name.Contains(nameof(RegimentCreated))).NonIndexed;
            var regimentCreated = RegimentCreated.Parser.ParseFrom(ByteString.FromBase64(logEventDto));
            Logger.Info($"Manager: {regimentCreated.Manager}\n" +
                        $"Create Time: {regimentCreated.CreateTime}\n" +
                        $"Regiment Address: {regimentCreated.RegimentAddress}\n");
            regimentCreated.Manager.ShouldBe(_oracleContract.CallAccount);
            regimentCreated.Manager.ShouldBe(Admin.ConvertAddress());
        }

        #region bridge

        [TestMethod]
        public void ChangeSwapRatio()
        {
            var pairId = BSCPairId;
            var pairIdHash = Hash.LoadFromHex(pairId);
            _bridgeContract.SetAccount(Admin);
            var result = _bridgeContract.ExecuteMethodWithResult(BridgeMethod.ChangeSwapRatio, new ChangeSwapRatioInput
            {
                SwapId = pairIdHash,
                SwapRatio = new SwapRatio
                {
                    OriginShare = 100_00000000,
                    TargetShare = 1
                },
                TargetTokenSymbol = SwapSymbol
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void CreateSwap()
        {
            var swapRatio = new SwapRatio
            {
                OriginShare = 100_00000000,
                TargetShare = 1
            };
            var depositAmount = 100_00000000;
            var originTokenSizeInByte = 32;
            var symbol = SwapSymbol;
            _tokenContract.TransferBalance(InitAccount, Admin, depositAmount * 2, symbol);
            _tokenContract.ApproveToken(Admin, _bridgeContractAddress, depositAmount, symbol);
            var balance = _tokenContract.GetUserBalance(Admin, symbol);
            _bridgeContract.SetAccount(Admin);
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
                        TargetTokenSymbol = symbol
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
            var afterBalance = _tokenContract.GetUserBalance(Admin, symbol);
            afterBalance.ShouldBe(balance - depositAmount);
            
            var swapPair = GetSwapPair(swapId,symbol);
            swapPair.DepositAmount.ShouldBe(depositAmount);
            swapPair.SwappedAmount.ShouldBe(0);
            swapPair.SwappedTimes.ShouldBe(0);
            swapPair.SwapRatio.ShouldBe(swapRatio);
            swapPair.TargetTokenSymbol.ShouldBe(symbol);
            swapPair.OriginTokenSizeInByte.ShouldBe(originTokenSizeInByte);
        }

        [TestMethod]
        public void SwapToken()
        {
            var sender = "zptx91dhHVJjJRxf5Wg5KAoMrDrWX6i1H2FAyKAiv2q8VZfbg";
            var receiveAddress = sender;
            var pairId = ETHUSDTPairId;
            var swapSymbol = UsdSymbol;
            var originAmount = "80000000";
            var receiptId = 7;
            var balance = _tokenContract.GetUserBalance(receiveAddress, swapSymbol);
            // var expectedAmount = long.Parse(originAmount.Substring(0, originAmount.Length - 10));
            // var expectedAmount = Convert.ToInt64(long.Parse(originAmount.Substring(0, originAmount.Length - 10))*1.05);            
            var expectedAmount = Convert.ToInt64(long.Parse(originAmount));
            
            _bridgeContract.SetAccount(receiveAddress);
            var result = _bridgeContract.ExecuteMethodWithResult(BridgeMethod.SwapToken, new SwapTokenInput
            {
                OriginAmount = originAmount,
                ReceiptId = receiptId,
                SwapId = Hash.LoadFromHex(pairId),
                
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logs = result.Logs.First(l => l.Name.Equals(nameof(Transferred)));
            var amount = Transferred.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed)).Amount;
            amount.ShouldBe(expectedAmount);
            var after = _tokenContract.GetUserBalance(receiveAddress, swapSymbol);
            after.ShouldBe(balance + expectedAmount);
            Logger.Info($"after {swapSymbol} balance is {after}");

            var checkAmount = _bridgeContract.CallViewMethod<SwapAmounts>(BridgeMethod.GetSwapAmounts, new GetSwapAmountsInput
            {
                SwapId = Hash.LoadFromHex(pairId),
                ReceiptId = receiptId
            });
            checkAmount.Receiver.ShouldBe(receiveAddress.ConvertAddress());
            checkAmount.ReceivedAmounts[swapSymbol].ShouldBe(expectedAmount);
            
            var checkSwappedReceiptIdList = _bridgeContract.CallViewMethod<ReceiptIdList>(BridgeMethod.GetSwappedReceiptIdList,
                new GetSwappedReceiptIdListInput
                {
                    ReceiverAddress = receiveAddress.ConvertAddress(),
                    SwapId = Hash.LoadFromHex(pairId)
                });
            checkSwappedReceiptIdList.Value.ShouldContain(receiptId);
            Logger.Info(expectedAmount);
        }

        [TestMethod]
        public void CheckManager()
        {
            var pairId = BSCPairId;
            var pairIdHash = Hash.LoadFromHex(pairId);
            var swapPairInfo =
                _bridgeContract.CallViewMethod<SwapInfo>(BridgeMethod.GetSwapInfo, pairIdHash);
            var manager =
                _regimentContract.CallViewMethod<RegimentInfo>(RegimentMethod.GetRegimentInfo,
                    swapPairInfo.RegimentAddress).Manager ;
            Logger.Info(manager.ToBase58());
            var swapPair = _bridgeContract.CallViewMethod<SwapPair>(BridgeMethod.GetSwapPair, new GetSwapPairInput
                {SwapId = pairIdHash, TargetTokenSymbol = SwapSymbol});
            Logger.Info(swapPair.DepositAmount);
        }

        [TestMethod]
        public void Deposit()
        {
            var depositAmount = 1000000_00000000;
            var pairId = BSCPairId;
            var token = SwapSymbol;
            var pairIdHash = Hash.LoadFromHex(pairId);     
            // _tokenContract.IssueBalance(InitAccount, Admin, depositAmount, token);

            var swapPairInfo =
                _bridgeContract.CallViewMethod<SwapInfo>(BridgeMethod.GetSwapInfo, pairIdHash);
            var manager =
                _regimentContract.CallViewMethod<RegimentInfo>(RegimentMethod.GetRegimentInfo,
                    swapPairInfo.RegimentAddress).Manager ;
            var swapPair = _bridgeContract.CallViewMethod<SwapPair>(BridgeMethod.GetSwapPair, new GetSwapPairInput
                {SwapId = pairIdHash, TargetTokenSymbol = token});
            _tokenContract.ApproveToken(manager.ToBase58(), _bridgeContractAddress, depositAmount, token);

            _bridgeContract.SetAccount(manager.ToBase58());
            var result = _bridgeContract.ExecuteMethodWithResult(BridgeMethod.Deposit, new DepositInput
            {
                SwapId = pairIdHash,
                TargetTokenSymbol = token,
                Amount = depositAmount
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var afterSwapPair = _bridgeContract.CallViewMethod<SwapPair>(BridgeMethod.GetSwapPair, new GetSwapPairInput
                {SwapId = pairIdHash, TargetTokenSymbol = token});
            afterSwapPair.DepositAmount.ShouldBe(swapPair.DepositAmount + depositAmount);
        }

        [TestMethod]
        public void Withdraw()
        {
            var pairId = ETHUSDTPairId;
            var pairIdHash = Hash.LoadFromHex(pairId);
            var symbol = UsdSymbol;
            var swapPairInfo =
                _bridgeContract.CallViewMethod<SwapInfo>(BridgeMethod.GetSwapInfo, pairIdHash);
            var manager =
                _regimentContract.CallViewMethod<RegimentInfo>(RegimentMethod.GetRegimentInfo, swapPairInfo.RegimentAddress).Manager;
            var swapPair = _bridgeContract.CallViewMethod<SwapPair>(BridgeMethod.GetSwapPair, new GetSwapPairInput
                {SwapId = pairIdHash, TargetTokenSymbol = symbol});

            var managerBalance = _tokenContract.GetUserBalance(manager.ToBase58(), symbol);
            var result = _bridgeContract.ExecuteMethodWithResult(BridgeMethod.Withdraw,new WithdrawInput
            {
                SwapId = pairIdHash,
                Amount = swapPair.DepositAmount,
                TargetTokenSymbol = symbol
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var afterSwapPair = _bridgeContract.CallViewMethod<SwapPair>(BridgeMethod.GetSwapPair, new GetSwapPairInput
                {SwapId = pairIdHash, TargetTokenSymbol = symbol});
            afterSwapPair.DepositAmount.ShouldBe(0);

            var afterManagerBalance = _tokenContract.GetUserBalance(manager.ToBase58(), symbol);
            afterManagerBalance.ShouldBe(managerBalance + swapPair.DepositAmount);
        }

        [TestMethod]
        public void GetReceiptHash()
        {
            var receiptHash = _bridgeContract.CallViewMethod<Hash>(BridgeMethod.GetReceiptHash, new Int64Value{Value = 174});
            Logger.Info(receiptHash.ToHex());
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
                new RecorderIdInput { RecorderId = 1 });
            Logger.Info(leaf);
            var localTree = _merkleTreeRecorderContract.CallViewMethod<GetLeafLocatedMerkleTreeOutput>(
                MerkleTreeRecorderMethod.GetLeafLocatedMerkleTree, new GetLeafLocatedMerkleTreeInput
                {
                    LeafIndex = leaf.Value,
                    RecorderId = 1
                });
            Logger.Info(localTree);
        }

        [TestMethod]
        public void GetMerkleTree()
        {
            var recorderId = 2;

            var receiptCount = _bridgeContract.CallViewMethod<Int64Value>(BridgeMethod.GetReceiptCount, new Int64Value {Value = recorderId});
            Logger.Info(receiptCount.Value);
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

            var fullTree = _merkleTreeGeneratorContract.CallViewMethod<Int64Value>(
                MerkleTreeGeneratorMethod.GetFullTreeCount,
                new GetFullTreeCountInput
                {
                    RecorderId = recorderId,
                    ReceiptMaker = _bridgeContract.Contract
                });
            Logger.Info(fullTree.Value);
            var merkle = _merkleTreeGeneratorContract.CallViewMethod<GetMerkleTreeOutput>(MerkleTreeGeneratorMethod.GetMerkleTree,
                new GetMerkleTreeInput
                {
                    RecorderId = recorderId,
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
            var recorderId = 1;
            var receiptCount = _bridgeContract.CallViewMethod<Int64Value>(BridgeMethod.GetReceiptCount, new Int64Value{Value = recorderId} );
            Logger.Info(receiptCount.Value);
            var leaf = _merkleTreeRecorderContract.CallViewMethod<Int64Value>(MerkleTreeRecorderMethod.GetLastRecordedLeafIndex, 
                new RecorderIdInput { RecorderId = recorderId });
            var maker = _merkleTreeGeneratorContract.CallViewMethod<GetReceiptMakerOutput>(
                MerkleTreeGeneratorMethod.GetReceiptMaker, _bridgeContract.Contract);
            var merkleTree =
                _merkleTreeRecorderContract.CallViewMethod<MTRecorder.MerkleTree>(MerkleTreeRecorderMethod.GetMerkleTree,
                    new MTRecorder.GetMerkleTreeInput
                    {
                        RecorderId = recorderId,
                        LastLeafIndex = receiptCount.Value - 1
                    });
            Logger.Info($"{leaf.Value} {merkleTree.FirstLeafIndex} {merkleTree.LastLeafIndex}");
            long id = 7;
            var merklePath = _merkleTreeGeneratorContract.CallViewMethod<MerklePath>(MerkleTreeGeneratorMethod.GetMerklePath,
                new GetMerklePathInput
                {
                    ReceiptId = id,
                    FirstLeafIndex = 0,
                    LastLeafIndex = receiptCount.Value - 1,
                    ReceiptMaker = maker.ReceiptMakerAddress,
                    RecorderId = recorderId
                });
            
            for (var i = 0; i < receiptCount.Value; i++)
            { 
                var receiptHash = _bridgeContract.CallViewMethod<Hash>(BridgeMethod.GetReceiptHash, new GetReceiptHashInput
                {
                    RecorderId = recorderId,
                    ReceiptId = i
                });  
                Logger.Info(receiptHash.ToHex());
            }
            var firstHash = _bridgeContract.CallViewMethod<Hash>(BridgeMethod.GetReceiptHash, new GetReceiptHashInput
            {
                RecorderId = recorderId,
                ReceiptId = id
            });  
            
            var root = merklePath.ComputeRootWithLeafNode(firstHash);
            root.ShouldBe(merkleTree.MerkleTreeRoot);
            
            var merkleProof =
                _merkleTreeRecorderContract.CallViewMethod<BoolValue>(MerkleTreeRecorderMethod.MerkleProof,
                    new MerkleProofInput
                    {
                        RecorderId = recorderId,
                        MerklePath = merklePath,
                        LeafNode = firstHash,
                        LastLeafIndex = receiptCount.Value - 1
                    });
            merkleProof.Value.ShouldBeTrue();
        }

        #endregion

        [TestMethod]
        public void GetReceiptCount()
        {
            var receiptCount = _bridgeContract.CallViewMethod<Int64Value>(BridgeMethod.GetReceiptCount, new Int64Value{Value = 2});
            var maker = _merkleTreeGeneratorContract.CallViewMethod<GetReceiptMakerOutput>(
                MerkleTreeGeneratorMethod.GetReceiptMaker, _bridgeContract.Contract);
            maker.ReceiptMakerAddress.ShouldBe(_bridgeContract.Contract);
            Logger.Info(receiptCount);
            Logger.Info(maker);
        }

        [TestMethod]
        public void GetSwappedReceiptIdList()
        {
            var sender = "WRRiSjFdJjivFN4ZGcQswAd45foLH4jusNi2HYb35D6CtwGVL";

            var pairId = BSCPairId;
            var pairIdHash = Hash.LoadFromHex(pairId);  
            var list = _bridgeContract.CallViewMethod<ReceiptIdList>(BridgeMethod.GetSwappedReceiptIdList,
                new GetSwappedReceiptIdListInput
                {
                    ReceiverAddress = sender.ConvertAddress(),
                    SwapId = pairIdHash
                });
            Logger.Info(list);
        }

        [TestMethod]
        public void GetSwapInfo()
        {
            var pairId = BSCPairId;
            var swapSymbol = SwapSymbol;
            var pairIdHash = Hash.LoadFromHex(pairId);  
            var info = _bridgeContract.CallViewMethod<SwapInfo>(BridgeMethod.GetSwapInfo, pairIdHash);
            Logger.Info(info);
            var swapPair = _bridgeContract.CallViewMethod<SwapPair>(BridgeMethod.GetSwapPair, new GetSwapPairInput
                {SwapId = pairIdHash, TargetTokenSymbol = swapSymbol});
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
                _tokenContract.TransferBalance(InitAccount, member, 100000000, SwapSymbol);
                _tokenContract.IssueBalance(InitAccount, member, 100000000000, Symbol);
            }
            _tokenContract.IssueBalance(InitAccount, _associationMember.First(), 100000000, Symbol);
            _tokenContract.ApproveToken(_associationMember.First(), _oracleContract.ContractAddress, 100000000, Symbol);
        }

        [TestMethod]
        public void GetRecord()
        {
            var recorderId = 1;
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

        private SwapPair GetSwapPair(Hash pairId, string symbol)
        {
            return _bridgeContract.CallViewMethod<SwapPair>(BridgeMethod.GetSwapPair, new GetSwapPairInput
            {
                SwapId = pairId,
                TargetTokenSymbol = symbol
            });
        }

        [TestMethod]
        public void CreateToken(string token, int decimals, long totalSupply)
        {
            var tokenInfo = _tokenContract.GetTokenInfo(token);
            if (!tokenInfo.Equals(new TokenInfo()))
            {
                Logger.Info($"{token} is already created");
                return;
            }

            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create,
                new CreateInput
                {
                    TokenName = token,
                    Symbol = token,
                    TotalSupply = totalSupply,
                    Issuer = InitAccount.ConvertAddress(),
                    Decimals = decimals,
                    IsBurnable = true
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
    }
}