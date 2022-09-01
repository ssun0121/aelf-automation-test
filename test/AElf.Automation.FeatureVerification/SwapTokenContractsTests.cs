using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Association;
using AElf.Contracts.Bridge;
using AElf.Contracts.MerkleTreeContract;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ReceiptMakerContract;
using AElf.Contracts.Regiment;
using AElf.CSharp.Core;
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
using Shouldly;
using CreateRegimentInput = AElf.Contracts.Regiment.CreateRegimentInput;

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
        private ReportContract _reportContract;
        private BridgeContract _bridgeContract;
        private MerkleTreeContract _merkleTreeContract;
        private RegimentContract _regimentContract;
        private Address _stringAggregator;
        private Address _defaultParliament;

        private string TestAccount { get; } = "";
        //2qjoi1ZxwmH2XqQFyMhykjW364Ce2cBxxRp47zrvZFT4jqW8Ru
        //ZrAFaqdr79MWYkxA49Hp2LUdSVHdP6fJh3kDw4jmgC7HTgrni
        //2R1rgEKkG84XGtbx6fvxExzChaXEyJrfSnvMpuKCGrUFoR5SKz
        //2ExtaRkjDiFhkGH8hwLZYVpRAnXe7awa25C61KVWy47uwnRw4s
        private string InitAccount { get; } = "";
        private string Admin { get; } = "";

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
        private string _oracleContractAddress = "";
        private string _reportContractAddress = "";
        private string _bridgeContractAddress = "";
        private string _merkleTreeAddress = "";
        private string _regimentContractAddress = "";
        private string _stringAggregatorAddress = "";
        
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
        private string _regimentId = "";
        private long payment = 0;
        private int maximalLeafCount = 1024;
        private string ETHELFSwapId = "";
        private string ETHUSDTSwapId = "";
        private string BSCSwapId = "";
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
            _reportContract = _reportContractAddress == ""
                ? new ReportContract(NodeManager, InitAccount)
                : new ReportContract(NodeManager, InitAccount, _reportContractAddress);
            _bridgeContract = _bridgeContractAddress == ""
                ? new BridgeContract(NodeManager, InitAccount)
                : new BridgeContract(NodeManager, InitAccount, _bridgeContractAddress);
            _merkleTreeContract = _merkleTreeAddress == ""
                ? new MerkleTreeContract(NodeManager, InitAccount)
                : new MerkleTreeContract(NodeManager, InitAccount, _merkleTreeAddress);
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
                    MerkleTreeContractAddress = _merkleTreeContract.Contract,
                    ReportContractAddress = _reportContract.Contract
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
                        $"Regiment Address: {regimentCreated.RegimentAddress}\n" +
                        $"Regiment Id: {regimentCreated.RegimentId}");
            regimentCreated.Manager.ShouldBe(_oracleContract.CallAccount);
            regimentCreated.Manager.ShouldBe(Admin.ConvertAddress());
            var regimentId = HashHelper.ComputeFrom(regimentCreated.RegimentAddress);
            regimentId.ShouldBe(regimentCreated.RegimentId);

        }

        #region bridge

        [TestMethod]
        public void ChangeSwapRatio()
        {
            var swapId = ETHELFSwapId;
            var pairIdHash = Hash.LoadFromHex(swapId);
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
            var symbol = SwapSymbol;
            var fromChainId = "Kovan";
            _tokenContract.TransferBalance(InitAccount, Admin, depositAmount * 2, symbol);
            _tokenContract.ApproveToken(Admin, _bridgeContractAddress, depositAmount, symbol);
            var balance = _tokenContract.GetUserBalance(Admin, symbol);
            _bridgeContract.SetAccount(Admin);
            var result = _bridgeContract.ExecuteMethodWithResult(BridgeMethod.CreateSwap, new CreateSwapInput
            {
                RegimentId = Hash.LoadFromHex(_regimentId),
                MerkleTreeLeafLimit = 1024,
                SwapTargetTokenList =
                {
                    new SwapTargetToken
                    {
                        FromChainId = fromChainId,
                        SwapRatio = swapRatio,
                        Symbol = symbol
                    }
                }
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var swapId = Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            var swapLogs = SwapInfoAdded.Parser
                .ParseFrom(ByteString.FromBase64(result.Logs.First(l => l.Name.Contains(nameof(SwapInfoAdded)))
                    .NonIndexed));
            swapLogs.SwapId.ShouldBe(swapId);
            
            var space = SpaceCreated.Parser.ParseFrom(ByteString.FromBase64(result.Logs
                .First(l => l.Name.Contains(nameof(SpaceCreated)))
                .NonIndexed));
            Logger.Info(space);
            Logger.Info($"{swapId.ToHex()}");
            
            var afterBalance = _tokenContract.GetUserBalance(Admin, symbol);
            afterBalance.ShouldBe(balance - depositAmount);
            
            var swapPair = GetSwapPairInfo(swapId.ToHex(),symbol);
            swapPair.DepositAmount.ShouldBe(depositAmount);
            swapPair.SwappedAmount.ShouldBe(0);
            swapPair.SwappedTimes.ShouldBe(0);
            var swapInfo = GetSwapInfo(swapId.ToHex());
            swapInfo.RegimentId.ShouldBe(Hash.LoadFromHex(_regimentId));
            swapInfo.SwapTargetTokenList.First().Symbol.ShouldBe(symbol);
            swapInfo.SwapTargetTokenList.First().SwapRatio.ShouldBe(swapRatio);
            swapInfo.SwapTargetTokenList.First().FromChainId.ShouldBe(fromChainId);
            Logger.Info(swapPair);
            Logger.Info(swapInfo);
        }

        [TestMethod]
        public void SwapToken()
        {
            var sender = "zptx91dhHVJjJRxf5Wg5KAoMrDrWX6i1H2FAyKAiv2q8VZfbg";
            var receiveAddress = sender;
            var swapId= ETHELFSwapId;
            var swapSymbol = UsdSymbol;
            var originAmount = "80000000";
            var receiptId = "";
            var swapInfo = GetSwapInfo(swapId);
            var chainId = swapInfo.SwapTargetTokenList.First().FromChainId;
            var depositAmount = GetSwapPairInfo(swapId, Symbol).DepositAmount;
            var balance = _tokenContract.GetUserBalance(receiveAddress, swapSymbol);
            // var expectedAmount = long.Parse(originAmount.Substring(0, originAmount.Length - 10));
            // var expectedAmount = Convert.ToInt64(long.Parse(originAmount.Substring(0, originAmount.Length - 10))*1.05);            
            var expectedAmount = Convert.ToInt64(long.Parse(originAmount));
            
            _bridgeContract.SetAccount(receiveAddress);
            var result = _bridgeContract.ExecuteMethodWithResult(BridgeMethod.SwapToken, new SwapTokenInput
            {
                OriginAmount = originAmount,
                ReceiptId = receiptId,
                SwapId = Hash.LoadFromHex(swapId)
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var swapLogs = result.Logs.First(l => l.Name.Equals(nameof(TokenSwapped)));
            var tokenSwapped = TokenSwapped.Parser.ParseFrom(ByteString.FromBase64(swapLogs.NonIndexed));
            tokenSwapped.Address.ShouldBe(receiveAddress.ConvertAddress());
            tokenSwapped.Amount.ShouldBe(expectedAmount);
            tokenSwapped.Symbol.ShouldBe(swapSymbol);
            tokenSwapped.ReceiptId.ShouldBe(receiptId);
            tokenSwapped.FromChainId.ShouldBe(chainId);
            
            var logs = result.Logs.First(l => l.Name.Equals(nameof(Transferred)));
            var amount = Transferred.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed)).Amount;
            amount.ShouldBe(expectedAmount);
            var after = _tokenContract.GetUserBalance(receiveAddress, swapSymbol);
            after.ShouldBe(balance + expectedAmount);
            Logger.Info($"after {swapSymbol} balance is {after}");

            var checkAmount = _bridgeContract.CallViewMethod<SwapAmounts>(BridgeMethod.GetSwapAmounts, new GetSwapAmountsInput
            {
                SwapId = Hash.LoadFromHex(swapId),
                ReceiptId = receiptId
            });
            checkAmount.Receiver.ShouldBe(receiveAddress.ConvertAddress());
            checkAmount.ReceivedAmounts[swapSymbol].ShouldBe(expectedAmount);
            
            var afterDepositAmount = GetSwapPairInfo(swapId, Symbol).DepositAmount;
            afterDepositAmount.ShouldBe(depositAmount.Sub(tokenSwapped.Amount));
            
            var checkSwappedReceiptIdList = _bridgeContract.CallViewMethod<ReceiptIdList>(BridgeMethod.GetSwappedReceiptIdList,
                new GetSwappedReceiptIdListInput
                {
                    ReceiverAddress = receiveAddress.ConvertAddress(),
                    SwapId = Hash.LoadFromHex(swapId)
                });
            checkSwappedReceiptIdList.Value.ShouldContain(receiptId);
            Logger.Info(expectedAmount);
        }

        [TestMethod]
        public void CheckManager()
        {
            var swapId = ETHELFSwapId;
            var swapIdHash = Hash.LoadFromHex(swapId);
            var manager = GetRegimentManger(swapId);
            Logger.Info(manager.ToBase58());
            var swapPair = _bridgeContract.CallViewMethod<SwapPairInfo>(BridgeMethod.GetSwapPairInfo, new GetSwapPairInput
                {SwapId = swapIdHash, Symbol = SwapSymbol});
            Logger.Info(swapPair.DepositAmount);
        }

        [TestMethod]
        public void Deposit()
        {
            var depositAmount = 1000000_00000000;
            var swapId = ETHELFSwapId;
            var token = SwapSymbol;
            var swapIdHash = Hash.LoadFromHex(swapId);     
            // _tokenContract.IssueBalance(InitAccount, Admin, depositAmount, token);
            
            var manager = GetRegimentManger(swapId);
            var swapPair = GetSwapPairInfo(swapId, Symbol);
            _tokenContract.ApproveToken(manager.ToBase58(), _bridgeContractAddress, depositAmount, token);

            _bridgeContract.SetAccount(manager.ToBase58());
            var result = _bridgeContract.ExecuteMethodWithResult(BridgeMethod.Deposit, new DepositInput
            {
                SwapId = swapIdHash,
                TargetTokenSymbol = token,
                Amount = depositAmount
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var afterSwapPair = GetSwapPairInfo(swapId, Symbol);
            afterSwapPair.DepositAmount.ShouldBe(swapPair.DepositAmount + depositAmount);
        }

        [TestMethod]
        public void Withdraw()
        {
            var swapId = ETHELFSwapId;
            var swapIdHash = Hash.LoadFromHex(swapId);
            var symbol = UsdSymbol;
            var swapPairInfo =
                _bridgeContract.CallViewMethod<SwapInfo>(BridgeMethod.GetSwapInfo, swapIdHash);
            var manager = GetRegimentManger(swapId);
            var swapPair = GetSwapPairInfo(swapId, symbol);

            var managerBalance = _tokenContract.GetUserBalance(manager.ToBase58(), symbol);
            var result = _bridgeContract.ExecuteMethodWithResult(BridgeMethod.Withdraw,new WithdrawInput
            {
                SwapId = swapIdHash,
                Amount = swapPair.DepositAmount,
                TargetTokenSymbol = symbol
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var afterSwapPair = GetSwapPairInfo(swapId, symbol);
            afterSwapPair.DepositAmount.ShouldBe(0);

            var afterManagerBalance = _tokenContract.GetUserBalance(manager.ToBase58(), symbol);
            afterManagerBalance.ShouldBe(managerBalance + swapPair.DepositAmount);
        }

        [TestMethod]
        public void GetReceiptIdInfo()
        {
            var receiptId = "";
            var receiptInfo = _bridgeContract.CallViewMethod<ReceiptIdInfo>(BridgeMethod.GetReceiptIdInfo, Hash.LoadFromHex(receiptId));
            Logger.Info(receiptInfo);
        }

        [TestMethod]
        public void GetReceiptHashListInfo()
        {
            var swapId = ETHELFSwapId;
            var receiver = "";
            var receiptIdList =
                _bridgeContract.CallViewMethod<ReceiptIdList>(BridgeMethod.GetSwappedReceiptIdList, 
                    new GetSwappedReceiptIdListInput
                {
                   SwapId = Hash.LoadFromHex(swapId),
                   ReceiverAddress = Address.FromBase58(receiver)
                });
            Logger.Info(receiptIdList);
            
            var receiptInfoList =
                _bridgeContract.CallViewMethod<ReceiptInfoList>(BridgeMethod.GetSwappedReceiptInfoList, 
                    new GetSwappedReceiptInfoListInput
                    {
                        SwapId = Hash.LoadFromHex(swapId),
                        ReceiverAddress = Address.FromBase58(receiver)
                    });
            Logger.Info(receiptInfoList);
            
        }

        #endregion

         #region Merkle

         /*
             ConstructMerkleTree,
             GetMerklePath,
             MerkleProof,
             GetRegimentSpaceCount,
             GetRegimentSpaceIdList,
             GetSpaceInfo,
             GetMerkleTreeByIndex,
             GetMerkleTreeCountBySpace,
             GetLastMerkleTreeIndex,
             GetLastLeafIndex,
             GetFullTreeCount,
             GetLeafLocatedMerkleTree,
             GetRemainLeafCount

          */

         [TestMethod]
         public void GetRegimentInfo()
         {
             var swapId = ETHELFSwapId;
             var regimentId = GetRegimentId(swapId);
             var spaceCount = _merkleTreeContract.CallViewMethod<Int64Value>(MerkleTreeMethod.GetRegimentSpaceCount, regimentId);
             var regimentSpaceIdList = _merkleTreeContract.CallViewMethod<HashList>(MerkleTreeMethod.GetRegimentSpaceIdList, regimentId);
             regimentSpaceIdList.Value.Count.ShouldBe((int)spaceCount.Value);
             var spaceInfo = _merkleTreeContract.CallViewMethod<SpaceInfo>(MerkleTreeMethod.GetRegimentSpaceCount, regimentId);
             spaceInfo.Operators.ShouldBe(regimentId);
         }

         [TestMethod]
         public void GetMerkleTree()
         {
             var swapId = ETHELFSwapId;
             var spaceId = GetSpaceId(swapId);
             
             var leaf = _merkleTreeContract.CallViewMethod<Int64Value>(MerkleTreeMethod.GetLastLeafIndex, 
                 new GetLastLeafIndexInput { SpaceId = spaceId });
             Logger.Info($"Last leaf index: {leaf.Value}");
             var fullTree = _merkleTreeContract.CallViewMethod<Int64Value>(MerkleTreeMethod.GetFullTreeCount, spaceId);
             Logger.Info($"Full Tree count: {fullTree.Value}");

             var localTree = _merkleTreeContract.CallViewMethod<GetLeafLocatedMerkleTreeOutput>(
                 MerkleTreeMethod.GetLeafLocatedMerkleTree, new GetLeafLocatedMerkleTreeInput
                 {
                     LeafIndex = leaf.Value,
                     SpaceId = spaceId
                 });
             Logger.Info(localTree);
             leaf.Value.Div(maximalLeafCount).ShouldBe(localTree.MerkleTreeIndex);
             localTree.FirstLeafIndex.ShouldBe(localTree.MerkleTreeIndex.Mul(maximalLeafCount));
             localTree.LastLeafIndex.ShouldBe(localTree.FirstLeafIndex.Add(maximalLeafCount - 1));
             localTree.SpaceId.ShouldBe(spaceId);

             var lastMerkleTreeIndex =
                 _merkleTreeContract.CallViewMethod<Int64Value>(MerkleTreeMethod.GetLastMerkleTreeIndex, spaceId);
             lastMerkleTreeIndex.Value.ShouldBe(localTree.MerkleTreeIndex);
         }

         [TestMethod]
         public void GetMerkleTreePath()
         {
             var swapId = ETHELFSwapId;
             var spaceId = GetSpaceId(swapId);
             var receiver = "";
             var leaf = _merkleTreeContract.CallViewMethod<Int64Value>(MerkleTreeMethod.GetLastLeafIndex, 
                 new GetLastLeafIndexInput { SpaceId = spaceId });

             var localTree = _merkleTreeContract.CallViewMethod<GetLeafLocatedMerkleTreeOutput>(
                 MerkleTreeMethod.GetLeafLocatedMerkleTree, new GetLeafLocatedMerkleTreeInput
                 {
                     LeafIndex = leaf.Value,
                     SpaceId = spaceId
                 });
             Logger.Info(localTree);
             leaf.Value.Div(maximalLeafCount).ShouldBe(localTree.MerkleTreeIndex);
             localTree.FirstLeafIndex.ShouldBe(localTree.MerkleTreeIndex.Mul(maximalLeafCount));
             localTree.LastLeafIndex.ShouldBe(localTree.FirstLeafIndex.Add(maximalLeafCount - 1));
             localTree.SpaceId.ShouldBe(spaceId);

             var receiptInfos = _bridgeContract.CallViewMethod<ReceiptInfoList>(BridgeMethod.GetSwappedReceiptInfoList,
                 new GetSwappedReceiptInfoListInput
                 {
                     ReceiverAddress = Address.FromBase58(receiver),
                     SwapId = Hash.LoadFromHex(swapId)
                 });
             var receiptId = receiptInfos.Value.First().ReceiptId;
             TryGetReceiptIndex(receiptId, out var receiptIndex);
             
             var merklePath = _merkleTreeContract.CallViewMethod<MerklePath>(MerkleTreeMethod.GetMerklePath,
                 new GetMerklePathInput
                 {
                     SpaceId = spaceId,
                     LeafNodeIndex = receiptIndex - 1,
                     ReceiptMaker = Address.FromBase58(receiver)
                 });
             
             // var firstHash = _bridgeContract.CallViewMethod<Hash>(BridgeMethod.GetReceiptHash, new GetReceiptHashInput
             // {
             //     RecorderId = recorderId,
             //     ReceiptId = id
             // });  
             //
             // var root = merklePath.ComputeRootWithLeafNode(firstHash);
             // root.ShouldBe(localTree.MerkleTreeRoot);
             //
             // var merkleProof =
             //     _merkleTreeContract.CallViewMethod<BoolValue>(MerkleTreeMethod.MerkleProof,
             //         new MerkleProofInput
             //         {
             //             SpaceId = spaceId,
             //             MerklePath = merklePath,
             //             LeafNode = firstHash,
             //             LastLeafIndex = receiptCount.Value - 1
             //         });
             // merkleProof.Value.ShouldBeTrue();
         }

         #endregion
        

        [TestMethod]
        public void GetSwappedReceiptIdList()
        {
            var sender = "WRRiSjFdJjivFN4ZGcQswAd45foLH4jusNi2HYb35D6CtwGVL";

            var swapId = ETHELFSwapId;
            var swapIdHash = Hash.LoadFromHex(swapId);  
            var list = _bridgeContract.CallViewMethod<ReceiptIdList>(BridgeMethod.GetSwappedReceiptIdList,
                new GetSwappedReceiptIdListInput
                {
                    ReceiverAddress = sender.ConvertAddress(),
                    SwapId = swapIdHash
                });
            Logger.Info(list);
        }

        [TestMethod]
        public SwapInfo GetSwapInfo(string swapId)
        {
            var swapSymbol = SwapSymbol;
            var pairIdHash = Hash.LoadFromHex(swapId);  
            var info = _bridgeContract.CallViewMethod<SwapInfo>(BridgeMethod.GetSwapInfo, pairIdHash);
            Logger.Info(info);
            return info;
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

        private Hash GetSpaceId(string swapId)
        {
            var spaceId =
                _bridgeContract.CallViewMethod<Hash>(BridgeMethod.GetSpaceIdBySwapId,Hash.LoadFromHex(swapId));
            Logger.Info(spaceId.ToHex());
            return spaceId;
        }

        private Hash GetRegimentId(string swapId)
        {
            var spaceId =
                _bridgeContract.CallViewMethod<Hash>(BridgeMethod.GetSpaceIdBySwapId, Hash.LoadFromHex(swapId));
            var regimentId = _bridgeContract.CallViewMethod<Hash>(BridgeMethod.GetRegimentIdBySpaceId, spaceId);
            Logger.Info(regimentId.ToHex());
            return regimentId;
        }

        private SwapPairInfo GetSwapPairInfo(string swapId, string symbol)
        {
            return _bridgeContract.CallViewMethod<SwapPairInfo>(BridgeMethod.GetSwapPairInfo, new GetSwapPairInfoInput
            {
                SwapId = Hash.LoadFromHex(swapId),
                Symbol = symbol
            });
        }

        private Address GetRegimentManger(string swapId)
        {
            var swapPairInfo =
                _bridgeContract.CallViewMethod<SwapInfo>(BridgeMethod.GetSwapInfo, Hash.LoadFromHex(swapId));
            var regimentAddress =
                _regimentContract.CallViewMethod<Address>(RegimentMethod.GetRegimentAddress, swapPairInfo.RegimentId);
            var manager =
                _regimentContract.CallViewMethod<RegimentInfo>(RegimentMethod.GetRegimentInfo,
                    regimentAddress).Manager;
            return manager;
        }
        
        private Hash ComputeLeafHash(decimal amount, Address receiverAddress, string receiptId)
        {
            var amountHash = HashHelper.ComputeFrom(amount.ToString());
            var receiptIdHash = HashHelper.ComputeFrom(receiptId);
            var targetAddressHash = HashHelper.ComputeFrom(receiverAddress.ToBase58());
            return HashHelper.ConcatAndCompute(amountHash, targetAddressHash, receiptIdHash);
        }
        private bool TryGetReceiptIndex(string receiptId, out long receiptIndex)
        {
            return long.TryParse(receiptId.Split(".").Last(), out receiptIndex);
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