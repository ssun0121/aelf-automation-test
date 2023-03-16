using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Contracts.Association;
using AElf.Contracts.Bridge;
using AElf.Contracts.MerkleTreeContract;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ReceiptMakerContract;
using AElf.Contracts.Regiment;
using AElf.Contracts.Report;
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
using Volo.Abp.Threading;
using AddAdminsInput = AElf.Contracts.Oracle.AddAdminsInput;
using CreateRegimentInput = AElf.Contracts.Regiment.CreateRegimentInput;
using GetMerklePathInput = AElf.Contracts.MerkleTreeContract.GetMerklePathInput;
using HashList = AElf.Contracts.MerkleTreeContract.HashList;

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

        //2qjoi1ZxwmH2XqQFyMhykjW364Ce2cBxxRp47zrvZFT4jqW8Ru
        //ZrAFaqdr79MWYkxA49Hp2LUdSVHdP6fJh3kDw4jmgC7HTgrni
        //2R1rgEKkG84XGtbx6fvxExzChaXEyJrfSnvMpuKCGrUFoR5SKz
        //2ExtaRkjDiFhkGH8hwLZYVpRAnXe7awa25C61KVWy47uwnRw4s

        private string BpAccount { get; } = "";
        private string InitAccount { get; } = "";
        private string TestAccount { get; } = "";

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
        private string mode = "main";

        //
        private string _oracleContractAddress = "";
        private string _reportContractAddress = "";
        private string _bridgeContractAddress = "";
        private string _merkleTreeAddress = "";
        private string _regimentContractAddress = "";
        private string _stringAggregatorAddress = "";

        //MainChain
        private string _oracleContractAddressMain = "2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS";
        private string _reportContractAddressMain = "2WHXRoLRjbUTDQsuqR5CntygVfnDb125qdJkudev4kVNbLhTdG";
        private string _bridgeContractAddressMain = "2RHf2fxsnEaM3wb6N1yGqPupNZbcCY98LgWbGSFWmWzgEs5Sjo";
        private string _merkleTreeAddressMain = "2NxwCPAGJr4knVdmwhb1cK7CkZw5sMJkRDLnT7E2GoDP2dy5iZ";
        private string _regimentContractAddressMain = "sr4zX6E7yVVL7HevExVcWv2ru3HSZakhsJMXfzxzfpnXofnZw";

        private string _stringAggregatorAddressMain = "xsnQafDAhNTeYcooptETqWnYBksFGGXxfcQyJJ5tmu6Ak9ZZt";

        //
        //SideChain
        private string _oracleContractAddressSide = "RXcxgSXuagn8RrvhQAV81Z652EEYSwR6JLnqHYJ5UVpEptW8Y";
        private string _reportContractAddressSide = "2wRDbyVF28VBQoSPgdSEFaL4x7CaXz8TCBujYhgWc9qTMxBE3n";
        private string _bridgeContractAddressSide = "2AsEepqiFCRDnepVheYYN5LK7nvM2kUoXgk2zLKu1Zneh8fwmF";
        private string _merkleTreeAddressSide = "2YkKkNZKCcsfUsGwCfJ6wyTx5NYLgpCg1stBuRT4z5ep3psXNG";
        private string _regimentContractAddressSide = "2onFLTnPEiZrXGomzJ8g74cBre2cJuHrn1yBJF3P6Xu9K5Gbth";

        private string _stringAggregatorAddressSide = "2F5McxHg7fAqVjDX97v79j4drsMq442rArChpBii8TWuRb8ZnK";

        private string Password { get; } = "12345678";
        private static string RpcUrl { get; set; } = "";
        private static string MainRpcUrl { get; } = "http://192.168.67.166:8000";

        private static string SideRpcUrl { get; } = "http://192.168.66.225:8000";

        private string Symbol { get; } = "PORT";
        private string SwapSymbol { get; } = "ELF";
        private string UsdSymbol { get; } = "USDT";
        private string EthSymbol { get; } = "WETH";
        private string BnbSymbol { get; } = "WBNB";

        private readonly bool isNeedInitialize = false;

        private readonly bool isNeedCreate = false;

        //DKQJtqZDqCfUDFPysHqqDeZNHdzHBmKTZe1bedcRnY5B147Go
        private string _regiment = "";

        private string _regimentId = "";

        //Main
        private string _mainRegiment = "RdJeA3VdAzrD12AsBNFzbdUwAqs5DzkDK4N7QLT6pc55SdrNy";

        private string _mainRegimentId = "3954383990de6d045daa3268fe36170cc37edc0af4224f71ad2f3e3bce3c4011";

        // Side
        private string _sideRegiment = "2QXz8kd6hGf8PxDzia3tD7PzXboajkSvn2GE9G2XctMi3sdtJT";

        private string _sideRegimentId = "c0c410f87981b414e699423870bca3881d565c254ec4606d14a1115c466b3c92";
        //Main
        // private string _mainRegiment = "tW5oUAyCJhZxhzihYvqR4VbNprEziTDakiETBsfBSKCn8Agqy";
        // private string _mainRegimentId = "edda07204203e273376b8c39559c98c48f80800cab811eeb91c01c85254c0da1";
        // Side
        // private string _sideRegiment = "kHy6xRDBHkuZTjw6vBebceotRFsn6jMaNBTKMFNcHgy6b822g";
        // private string _sideRegimentId = "1e7e21d4367a5fb369bbf0e1d524a958b6341e3a2ee76469bd69f6b945643767";

        private long payment = 0;
        private int maximalLeafCount = 128;

        // private string EthSwapId = "";
        // private string UsdtSwapId = "";
        // private string ElfSwapId = "";   

        //Main-Kovan
        // private string EthSwapId = "701ddb40246506fd48cc7a28ba453303b8e43f7d140bcf7d9cbea14fe05e528d";
        // private string UsdtSwapId = "0d0a485dc471662372bda4944dd444004db1598b3031546039a353259df8cff2";
        // private string ElfSwapId = "c70ded97ae810984aa4dbd317e5574fc12b6ff4cae4b7052757d8a71f0aae523";

        //Main-Goerli
        // private string EthSwapId = "417e250a684b7d7534db11d70715ca11da0f1dc1d041e2d14f84acd9291edb15";
        // private string UsdtSwapId = "cd969eacb6ae3f8bc8da043e9f400669a9550c5af08f38cce4998244c46db455";
        // private string ElfSwapId = "d9af6a30b1d529f054703970ac6f2a4eaf784aece277c535bd11b640157ddad1";

        //Main-New-Goerli
        private string EthSwapId = "4a5509601e1f6050eace6bb27fbf322a04c7010c73199d97482866d4e4a36868";
        private string UsdtSwapId = "71b11d6da149cdc9bec52240b59f8bb790fc8320529e8f0c1b584fe4166bbfa2";
        private string ElfSwapId = "80f51802f1f864ba85e53ee0343dc1de4afe011922bc19556bfe4ab6f7c03e4b";

        // //Main-BSC
        // private string ElfSwapId = "18b2e4ce0647537f51a8150565dc6afe14bdfa5ac180653525160e487276ae33";
        // private string UsdtSwapId = "331204906fa76d769ca27b4c0e8ee495f0b52f1d716d741da0e3adae6a271f44";
        // private string BNBSwapId = "20fd718d863b8f4d64f2c4814d8c975176894886412314b0510f7e221eefd4d8";

        //Side-Kovan
        // private string EthSwapId = "22601b16cf12a88b1a228b2051ba28a4e7ec7772c398a81d051d801711f1bc3c";
        // private string UsdtSwapId = "c46c672d57b708a50d82d18be3c3cbdfe6fdf417ef13d95fbdeb804f30fc478a";
        // private string ElfSwapId = "1db5c7ac518f8856dc782fbda38e8df2be710fc9430519f306b8466c50d1b978";
        //Side-Bsc
        // private string ElfSwapId = "4edbba95f9e7d01106fa6e465e370cc44a99ea8bfad29390f10e3590cfa053b5";
        // private string UsdtSwapId = "b0a9d9dcde9921d132b55c3a868daa3dde57b6d62b3f85cb7d0c321b5bb0bf27";
        // private string BNBSwapId = "7d3185839761715d3a8034775172652405d6c676a41a46f4dca24d84ccf95ade";
        //Side-Goerli
        // private string EthSwapId = "762e0e000ee2742368a1fce0f0fbe5a2570f8c59a71d621deb63ac5389a20bfe";
        // private string UsdtSwapId = "9ec6fa1542acf976cc8f23eac85317f1784465edd4515fc0b993c032beef4183";
        // private string ElfSwapId = "40de0bdf7f84e577e59079654674f5bf247c6c472ee929523781c47216fbab20";
        //Side-New-Goerli
        // private string EthSwapId = "5d645081b36fc03b95d7fef377acfdadbed3bd21d8a7c1dcffea6650022f2fcd";
        // private string UsdtSwapId = "81918b54157c1211dce31a0a3291826b9d99fc7ea9272b3f1de164565bc88e21";
        // private string ElfSwapId = "289a8ec7403eeb9f73e7f0663b472e8488454a494bdee0e3a148722b2b226cd4";

        //bb16f381b0f2e795a988285dec3a68affacdccd7d3ac2e74edc808c102efcd95
        //caaa8140bb484e1074872350687df0b1262436cdec3042539e78eb615b376d5e
        //03ccf1c18a7a82391f936ce58db7ee4b6fd9eca4a1ae7ee930f4a750a0a5653a

        //cf25e2d376b20131622ae1b8c8db72d040efe8cf5ba1fc5316873cff2a8ebebe --USD
        //03ccf1c18a7a82391f936ce58db7ee4b6fd9eca4a1ae7ee930f4a750a0a5653a --ELF
        //vfACnL3CsejPVxCjjensoQBNvDMPtid9cSGZVqjMAXKLtZKgo
        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("SwapTest");
            Logger = Log4NetHelper.GetLogger();

            NodeInfoHelper.SetConfig("nodes");

            if (mode == "side")
            {
                RpcUrl = SideRpcUrl;
                NodeManager = new NodeManager(RpcUrl);
                AuthorityManager = new AuthorityManager(NodeManager, InitAccount, Password);
                _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount, Password);
                _tokenContract = _genesisContract.GetTokenContract(InitAccount, Password);
                _parliament = _genesisContract.GetParliamentContract(InitAccount, Password);
                var cross = _genesisContract.GetCrossChainContract(InitAccount);

                _oracleContractAddress = _oracleContractAddressSide;
                _reportContractAddress = _reportContractAddressSide;
                _bridgeContractAddress = _bridgeContractAddressSide;
                _merkleTreeAddress = _merkleTreeAddressSide;
                _regimentContractAddress = _regimentContractAddressSide;
                _stringAggregatorAddress = _stringAggregatorAddressSide;
                _regiment = _sideRegiment;
                _regimentId = _sideRegimentId;
            }
            else
            {
                RpcUrl = MainRpcUrl;
                NodeManager = new NodeManager(RpcUrl);
                AuthorityManager = new AuthorityManager(NodeManager, InitAccount, Password);
                _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount, Password);
                _tokenContract = _genesisContract.GetTokenContract(InitAccount, Password);
                _parliament = _genesisContract.GetParliamentContract(InitAccount, Password);

                _oracleContractAddress = _oracleContractAddressMain;
                _reportContractAddress = _reportContractAddressMain;
                _bridgeContractAddress = _bridgeContractAddressMain;
                _merkleTreeAddress = _merkleTreeAddressMain;
                _regimentContractAddress = _regimentContractAddressMain;
                _stringAggregatorAddress = _stringAggregatorAddressMain;

                _regiment = _mainRegiment;
                _regimentId = _mainRegimentId;
            }

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
            Logger.Info(
                $"\nOracle: {_oracleContract.ContractAddress}" +
                $"\nReport: {_reportContract.ContractAddress}" +
                $"\nBridge: {_bridgeContract.ContractAddress}" +
                $"\nMerkle: {_merkleTreeContract.ContractAddress}" +
                $"\nRegiment: {_regimentContract.ContractAddress}" +
                $"\nStringAggregator: {_stringAggregator.ToBase58()}");

            // Transfer();
            if (!isNeedCreate) return;
            // CreateToken(UsdSymbol,6,10_00000000_000000);
            // CreateToken(EthSymbol,8,10_000000000_00000000);
            // CreateToken(Symbol,8,10_000000000_00000000);
            CreateToken(BnbSymbol, 8, 1_000000000_00000000);
            CreateToken(EthSymbol, 8, 1_000000000_00000000);


            // if (!isNeedInitialize) return;
            // InitializeContract();
        }

        [TestMethod]
        public void CreateAssociation()
        {
            var list = new List<Address>();

            _associationMember.ForEach(l => { list.Add(l.ConvertAddress()); });
            var association = _genesisContract.GetAssociationAuthContract(InitAccount);
            var input = new CreateOrganizationInput
            {
                CreationToken = HashHelper.ComputeFrom("organization"),
                OrganizationMemberList = new OrganizationMemberList
                {
                    OrganizationMembers = { list }
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
                    Proposers = { list }
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
            var list = new List<Address>();
            _associationMember.ForEach(l => { list.Add(l.ConvertAddress()); });
            list.Add(InitAccount.ConvertAddress());
            // _tokenContract.TransferBalance(InitAccount, Admin, 1000_0000000);
            Logger.Info("Initialize Oracle: ");
            var initializeOracle = _oracleContract.ExecuteMethodWithResult(OracleMethod.Initialize,
                new AElf.Contracts.Oracle.InitializeInput
                {
                    RegimentContractAddress = _regimentContract.Contract,
                    MinimumOracleNodesCount = 5,
                    DefaultRevealThreshold = 3,
                    DefaultAggregateThreshold = 3,
                    IsChargeFee = false,
                    DefaultExpirationSeconds = 3600
                });
            initializeOracle.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            Logger.Info("Initialize Report: ");
            var initializeReport = _reportContract.ExecuteMethodWithResult(ReportMethod.Initialize,
                new AElf.Contracts.Report.InitializeInput
                {
                    OracleContractAddress = _oracleContract.Contract,
                    ReportFee = 0,
                    ApplyObserverFee = 0,
                    RegimentContractAddress = _regimentContract.Contract,
                    InitialRegisterWhiteList = { list }
                });
            initializeReport.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            Logger.Info("Initialize Merkle: ");
            var initializeMerkle = _merkleTreeContract.ExecuteMethodWithResult(MerkleTreeMethod.Initialize,
                new AElf.Contracts.MerkleTreeContract.InitializeInput
                {
                    RegimentContractAddress = _regimentContract.Contract,
                    Owner = _bridgeContract.Contract
                });
            initializeMerkle.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            Logger.Info("Initialize Bridge: ");
            _bridgeContract.SetAccount(InitAccount);
            var initializeBridge = _bridgeContract.ExecuteMethodWithResult(BridgeMethod.Initialize,
                new AElf.Contracts.Bridge.InitializeInput
                {
                    OracleContractAddress = _oracleContract.Contract,
                    RegimentContractAddress = _regimentContract.Contract,
                    MerkleTreeContractAddress = _merkleTreeContract.Contract,
                    ReportContractAddress = _reportContract.Contract,
                    Admin = Admin.ConvertAddress(),
                    Controller = InitAccount.ConvertAddress()
                });
            initializeBridge.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info(_oracleContract.CallViewMethod<Address>(OracleMethod.GetController, new Empty()));
        }

        [TestMethod]
        [DataRow(true)]
        public void CreateRegiment(bool isApproveToJoin)
        {
            var list = new List<Address>();
            _associationMember.ForEach(l => { list.Add(l.ConvertAddress()); });
            _oracleContract.SetAccount(Admin);
            var result =
                _oracleContract.ExecuteMethodWithResult(OracleMethod.CreateRegiment, new CreateRegimentInput
                {
                    IsApproveToJoin = isApproveToJoin,
                    InitialMemberList = { list },
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

        [TestMethod]
        public void AddAdmin()
        {
            var regimentInfo =
                _regimentContract.CallViewMethod<RegimentInfo>(RegimentMethod.GetRegimentInfo,
                    _regiment.ConvertAddress());
            _oracleContract.SetAccount(Admin);
            var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.AddAdmins, new AddAdminsInput
            {
                RegimentAddress = _regiment.ConvertAddress(),
                NewAdmins = { _bridgeContract.Contract }
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        #region bridge

        [TestMethod]
        public void ChangeSwapRatio()
        {
            var swapId = ElfSwapId;
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
            var symbol = BnbSymbol;
            var fromChainId = "BSCTest";
            // _tokenContract.TransferBalance(InitAccount, Admin, depositAmount * 2, symbol);
            // _tokenContract.ApproveToken(Admin, _bridgeContractAddress, depositAmount, symbol);
            var balance = _tokenContract.GetUserBalance(Admin, symbol);
            _bridgeContract.SetAccount(Admin);
            var result = _bridgeContract.ExecuteMethodWithResult(BridgeMethod.CreateSwap, new CreateSwapInput
            {
                RegimentId = Hash.LoadFromHex(_regimentId),
                MerkleTreeLeafLimit = maximalLeafCount,
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

            var swapPair = GetSwapPairInfo(swapId.ToHex(), symbol);
            swapPair.DepositAmount.ShouldBe(0);
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
            var swapId = ElfSwapId;
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

            var checkAmount = _bridgeContract.CallViewMethod<SwapAmounts>(BridgeMethod.GetSwapAmounts,
                new GetSwapAmountsInput
                {
                    SwapId = Hash.LoadFromHex(swapId),
                    ReceiptId = receiptId
                });
            checkAmount.Receiver.ShouldBe(receiveAddress.ConvertAddress());
            checkAmount.ReceivedAmounts[swapSymbol].ShouldBe(expectedAmount);

            var afterDepositAmount = GetSwapPairInfo(swapId, Symbol).DepositAmount;
            afterDepositAmount.ShouldBe(depositAmount.Sub(tokenSwapped.Amount));

            var checkSwappedReceiptIdList = _bridgeContract.CallViewMethod<ReceiptIdList>(
                BridgeMethod.GetSwappedReceiptIdList,
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
            var swapId = ElfSwapId;
            var swapIdHash = Hash.LoadFromHex(swapId);
            var manager = GetRegimentManger(swapId);
            Logger.Info(manager.ToBase58());
            var swapPair = _bridgeContract.CallViewMethod<SwapPairInfo>(BridgeMethod.GetSwapPairInfo,
                new GetSwapPairInfoInput
                    { SwapId = swapIdHash, Symbol = SwapSymbol });
            Logger.Info(swapPair.DepositAmount);
        }

        [TestMethod]
        public void Deposit()
        {
            var depositAmount = 100000_000000;
            var swapId = UsdtSwapId;
            var token = UsdSymbol;
            var swapIdHash = Hash.LoadFromHex(swapId);
            var manager = GetRegimentManger(swapId);
            var swapPair = GetSwapPairInfo(swapId, token);
            var managerBalance = _tokenContract.GetUserBalance(manager.ToBase58(), token);
            if (managerBalance < depositAmount)
                _tokenContract.TransferBalance(InitAccount, Admin, depositAmount, token);
            _tokenContract.ApproveToken(manager.ToBase58(), _bridgeContractAddress, depositAmount, token);
            _bridgeContract.SetAccount(manager.ToBase58());
            var result = _bridgeContract.ExecuteMethodWithResult(BridgeMethod.Deposit, new DepositInput
            {
                SwapId = swapIdHash,
                TargetTokenSymbol = token,
                Amount = depositAmount
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var afterSwapPair = GetSwapPairInfo(swapId, token);
            afterSwapPair.DepositAmount.ShouldBe(swapPair.DepositAmount + depositAmount);
        }

        [TestMethod]
        public void Withdraw()
        {
            var swapId = ElfSwapId;
            var swapIdHash = Hash.LoadFromHex(swapId);
            var symbol = UsdSymbol;
            var swapPairInfo =
                _bridgeContract.CallViewMethod<SwapInfo>(BridgeMethod.GetSwapInfo, swapIdHash);
            var manager = GetRegimentManger(swapId);
            var swapPair = GetSwapPairInfo(swapId, symbol);

            var managerBalance = _tokenContract.GetUserBalance(manager.ToBase58(), symbol);
            var result = _bridgeContract.ExecuteMethodWithResult(BridgeMethod.Withdraw, new WithdrawInput
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
            var receiptInfo =
                _bridgeContract.CallViewMethod<ReceiptIdInfo>(BridgeMethod.GetReceiptIdInfo,
                    Hash.LoadFromHex(receiptId));
            Logger.Info(receiptInfo);
        }

        [TestMethod]
        public void GetReceiptHashListInfo()
        {
            var swapId = ElfSwapId;
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
            var swapId = ElfSwapId;
            var regimentId = GetRegimentId(swapId);
            var spaceCount =
                _merkleTreeContract.CallViewMethod<Int64Value>(MerkleTreeMethod.GetRegimentSpaceCount, regimentId);
            var regimentSpaceIdList =
                _merkleTreeContract.CallViewMethod<HashList>(MerkleTreeMethod.GetRegimentSpaceIdList, regimentId);
            regimentSpaceIdList.Value.Count.ShouldBe((int)spaceCount.Value);
            var spaceInfo =
                _merkleTreeContract.CallViewMethod<SpaceInfo>(MerkleTreeMethod.GetRegimentSpaceCount, regimentId);
            spaceInfo.Operators.ShouldBe(regimentId);
        }

        [TestMethod]
        public void GetMerkleTree()
        {
            var swapId = UsdtSwapId;
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
            // localTree.LastLeafIndex.ShouldBe(localTree.FirstLeafIndex.Add(maximalLeafCount - 1));
            localTree.SpaceId.ShouldBe(spaceId);

            var lastMerkleTreeIndex =
                _merkleTreeContract.CallViewMethod<Int64Value>(MerkleTreeMethod.GetLastMerkleTreeIndex, spaceId);
            lastMerkleTreeIndex.Value.ShouldBe(localTree.MerkleTreeIndex);
        }

        [TestMethod]
        public void GetMerkleTreePath()
        {
            var swapId = ElfSwapId;
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

            var swapId = ElfSwapId;
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
                nameof(ChangeMaximalLeafCount), new Int32Value { Value = 1024 }, InitAccount);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void Transfer()
        {
            foreach (var member in _associationMember)
            {
                _tokenContract.TransferBalance(BpAccount, InitAccount, 2000_00000000, SwapSymbol);
                _tokenContract.TransferBalance(BpAccount, Admin, 2000_00000000, SwapSymbol);

                _tokenContract.IssueBalance(InitAccount, member, 100000000000, Symbol);
            }

            _tokenContract.IssueBalance(InitAccount, _associationMember.First(), 100000000, Symbol);
            _tokenContract.ApproveToken(_associationMember.First(), _oracleContract.ContractAddress, 100000000, Symbol);
        }

        #region AELF -> other

        [TestMethod]
        [DataRow("Goerli", "0x7e71ec21264eAD35C88b38C2Db49e3e78FF0e663")]
        [DataRow("BSCTest", "0x98aC5Ea75F72dE8b8DBB9F3fA1fc369dC3829288")]
        public void RegisterOffChainAggregation(string chainId, string token)
        {
            //BridgeOut address on eth
            var result = _reportContract.ExecuteMethodWithResult(ReportMethod.RegisterOffChainAggregation,
                new RegisterOffChainAggregationInput
                {
                    Token = token,
                    RegimentId = Hash.LoadFromHex(_regimentId),
                    ChainId = chainId
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        [DataRow("Goerli", "0x7e71ec21264eAD35C88b38C2Db49e3e78FF0e663")]
        [DataRow("BSCTest", "0x98aC5Ea75F72dE8b8DBB9F3fA1fc369dC3829288")]
        public void SetSkipMemberList(string chainId, string token)
        {
            //BridgeOut address on eth
            _reportContract.SetAccount(Admin);
            var result = _reportContract.ExecuteMethodWithResult(ReportMethod.SetSkipMemberList,
                new SetSkipMemberListInput
                {
                    ChainId = chainId,
                    Token = token,
                    Value = new MemberList
                    {
                        Value = { _bridgeContract.Contract }
                    }
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void AddToken()
        {
            var chainId = "Goerli";
            _bridgeContract.SetAccount(Admin);
            var result = _bridgeContract.ExecuteMethodWithResult(BridgeMethod.AddToken, new AddTokenInput
            {
                Value =
                {
                    new ChainToken
                    {
                        ChainId = chainId,
                        Symbol = SwapSymbol
                    },
                    new ChainToken
                    {
                        ChainId = chainId,
                        Symbol = EthSymbol
                    },
                    new ChainToken
                    {
                        ChainId = chainId,
                        Symbol = UsdSymbol
                    }
                }
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        [DataRow("Goerli")]
        [DataRow("BSCTest")]
        public void SetGasLimit(string chainId)
        {
            var gasLimit = (long)((21000 + 68 * 3400) * 1.1);
            var result = _bridgeContract.ExecuteMethodWithResult(BridgeMethod.SetGasLimit, new SetGasLimitInput
            {
                GasLimitList =
                {
                    new GasLimit
                    {
                        ChainId = chainId,
                        GasLimit_ = gasLimit
                    }
                }
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void CreateReceipt()
        {
            _bridgeContract.SetAccount(TestAccount);
            _tokenContract.ApproveToken(TestAccount, _bridgeContract.ContractAddress, 1000000_00000000, "ELF");
            var txList = new List<string>();
            for (var j = 0; j < 1; j++)
            {
                for (var i = 1; i <= 3; i++)
                {
                    var random = CommonHelper.GenerateRandomNumber(1, 10);
                    // long amount = i + j + random;
                    long amount = (i + j + 1).Mul(100000000);

                    var result = _bridgeContract.ExecuteMethodWithTxId(BridgeMethod.CreateReceipt,
                        new CreateReceiptInput
                        {
                            Symbol = "ELF",
                            Amount = amount,
                            TargetAddress = "0xf01Db78977D025dc9fF4380F631019C09D5EFAcc",
                            TargetChainId = "NewGoerli"
                        });
                    txList.Add(result);
                }

                Thread.Sleep(1000);
            }

            Thread.Sleep(4000);
            foreach (var result in txList.Select(tx =>
                         AsyncHelper.RunSync(() => NodeManager.ApiClient.GetTransactionResultAsync(tx))))
            {
                Logger.Info($"{result.Status.ConvertTransactionResultStatus()} - {result.BlockNumber}");
            }
        }

        [TestMethod]
        public void CheckReceiptHashList()
        {
            var result =
                _bridgeContract.CallViewMethod<GetReceiptHashListOutput>(BridgeMethod.GetReceiptHashList,
                    new GetReceiptHashListInput
                    {
                        SpaceId = Hash.LoadFromHex("cd5e329f4431fe9b9fb3c9b169263046899375c431c2dbc54887046eea895647"),
                        FirstLeafIndex = 0,
                        LastLeafIndex = 127
                    });
            Logger.Info(result);
        }

        //ClearMerkleTree
        [TestMethod]
        public void FixTree()
        {
            // var result = _merkleTreeContract.ExecuteMethodWithResult
            //     (MerkleTreeMethod.ClearMerkleTree,Hash.LoadFromHex("cd5e329f4431fe9b9fb3c9b169263046899375c431c2dbc54887046eea895647"));

            var receiptHash =
                _bridgeContract.CallViewMethod<GetReceiptHashListOutput>(BridgeMethod.GetReceiptHashList,
                    new GetReceiptHashListInput
                    {
                        SpaceId = Hash.LoadFromHex("cd5e329f4431fe9b9fb3c9b169263046899375c431c2dbc54887046eea895647"),
                        FirstLeafIndex = 0,
                        LastLeafIndex = 135
                    });
            _merkleTreeContract.SetAccount(Admin);
            var result = _merkleTreeContract.ExecuteMethodWithResult(MerkleTreeMethod.RecordMerkleTree,
                new RecordMerkleTreeInput
                {
                    SpaceId = Hash.LoadFromHex("cd5e329f4431fe9b9fb3c9b169263046899375c431c2dbc54887046eea895647"),
                    LeafNodeHash =
                    {
                        receiptHash.ReceiptHashList
                    }
                });
        }

        [TestMethod]
        public void CheckSwapInfo()
        {
            var info = GetSwapInfo(ElfSwapId);
            Logger.Info(info);
        }

        #endregion

        private Hash GetSpaceId(string swapId)
        {
            var spaceId =
                _bridgeContract.CallViewMethod<Hash>(BridgeMethod.GetSpaceIdBySwapId, Hash.LoadFromHex(swapId));
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

            var genesisOwnerAddress = _parliament.GetGenesisOwnerAddress();

            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create,
                new CreateInput
                {
                    TokenName = token,
                    Symbol = token,
                    TotalSupply = totalSupply,
                    Issuer = genesisOwnerAddress,
                    Decimals = decimals,
                    IsBurnable = true
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
        
        [TestMethod]
        public void Check()
        {
            var whiteList =
                _bridgeContract.CallViewMethod<TokenSymbolList>(BridgeMethod.GetTokenWhitelist, new StringValue
                {
                    Value = "Kovan"
                });
            Logger.Info(whiteList.Symbol);

            var txID = "2adc0140ec070c7129f1e8e6d7e5bc16a827d73047d5c6351c21d8cf3fc0169c";
            var txResult = AsyncHelper.RunSync(() => NodeManager.ApiClient.GetTransactionResultAsync(txID));
            var logs = txResult.Logs.First(l => l.Name.Equals("ReportConfirmed")).NonIndexed;
            var confirmed = ReportConfirmed.Parser.ParseFrom(ByteString.FromBase64(logs));
            Logger.Info(confirmed);
        }

        [TestMethod]
        public void GetContractController()
        {
            var controller = _bridgeContract.CallViewMethod<Address>(BridgeMethod.GetContractController, new Empty());
            Logger.Info(controller.ToBase58());
        }

        [TestMethod]
        public void CheckLogs()
        {
            var txId = "ba8c34d4ff749f53530943fbb5f7b837dceb006a90ad981d1a4b84c9b88c8df2";
            var txResult = AsyncHelper.RunSync(() => NodeManager.ApiClient.GetTransactionResultAsync(txId));
            var logs = txResult.Logs.First(l => l.Name.Equals(nameof(CrossChainTransferred))).NonIndexed;
            var transferred = CrossChainTransferred.Parser.ParseFrom(ByteString.FromBase64(logs));
            Logger.Info(transferred);
        }

        [TestMethod]
        public void CheckPrice()
        {
            var chain = "NewGoerli";
            var gasLimit =
                _bridgeContract.CallViewMethod<Int64Value>(BridgeMethod.GetGasLimit, new StringValue { Value = chain });
            var gasPrice =
                _bridgeContract.CallViewMethod<Int64Value>(BridgeMethod.GetGasPrice,
                    new StringValue { Value = chain });
            var gasPriceRatio = _bridgeContract.CallViewMethod<Int64Value>(BridgeMethod.GetPriceRatio,
                new StringValue { Value = chain });
            // var floatingRatio = _bridgeContract.CallViewMethod<StringValue>(BridgeMethod.GetFeeFloatingRatio,
            //     new StringValue { Value = chain });
            // if (!decimal.TryParse(floatingRatio.Value, out var floatingRatioDecimal))
            // {
            //     floatingRatioDecimal = 1;
            // }
            var floatingRatioDecimal = 1;
            var nativeTokenFee = CalculateTransactionFee(gasLimit.Value, gasPrice.Value,
                gasPriceRatio.Value, floatingRatioDecimal);
            Logger.Info(nativeTokenFee);
        }

        private long CalculateTransactionFee(long gasFee, long gasPrice, long priceRatio, decimal feeRatio)
        {
            var gasPriceDecimal = (decimal)gasPrice / 1000000000;
            var transactionFee = gasFee * gasPriceDecimal;
            var priceRatioDecimal = (decimal)priceRatio / 100000000;
            var fee = decimal.Round((transactionFee / 1000000000) * priceRatioDecimal * feeRatio, 8);
            return (long)decimal.Ceiling(fee) * 100000000;
        }
    }
}