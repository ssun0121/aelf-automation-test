using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Contracts.Association;
using EBridge.Contracts.Bridge;
using EBridge.Contracts.MerkleTreeContract;
using AElf.Contracts.MultiToken;
using EBridge.Contracts.Oracle;
using AElf.Contracts.ReceiptMakerContract;
using EBridge.Contracts.Regiment;
using EBridge.Contracts.Report;
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
using AddAdminsInput = EBridge.Contracts.Oracle.AddAdminsInput;
using CreateRegimentInput = EBridge.Contracts.Regiment.CreateRegimentInput;
using DeleteRegimentMemberInput = EBridge.Contracts.Oracle.DeleteRegimentMemberInput;
using GetMerklePathInput = EBridge.Contracts.MerkleTreeContract.GetMerklePathInput;
using HashList = EBridge.Contracts.MerkleTreeContract.HashList;
using ReceiptCreated = EBridge.Contracts.Bridge.ReceiptCreated;
using TransferRegimentOwnershipInput = EBridge.Contracts.Regiment.TransferRegimentOwnershipInput;

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

        private string BpAccount { get; } = "2uBC9bMz5DXSbMnFdSQu7RYr8Vu3nKo1xFx6R2KVaf6mHhdD3r";
        private string UsdtAccount { get; } = "eyDPrhJdofZ9f7Qdyi8FtbA8BePubP1M3gwfVMs6MnMHHNwik";
        private string BNBAccount { get; } = "eyDPrhJdofZ9f7Qdyi8FtbA8BePubP1M3gwfVMs6MnMHHNwik";
        private string ETHAccount { get; } = "eyDPrhJdofZ9f7Qdyi8FtbA8BePubP1M3gwfVMs6MnMHHNwik";
        private string InitAccount { get; } = "2uBC9bMz5DXSbMnFdSQu7RYr8Vu3nKo1xFx6R2KVaf6mHhdD3r";
        private string TestAccount { get; } = "6eR4wXd2CmUDKboe7zrka1xAHTPi8xTuXFE1LXKsrpzhEhWQX";

        private string Admin { get; } = "prUs55EDnCWNXWYFgATbWTmgZrBTnbYqGuTUFsLXHprneecZY";

        private string RegimentManager = "prUs55EDnCWNXWYFgATbWTmgZrBTnbYqGuTUFsLXHprneecZY";

        private readonly List<string> _associationMember = new List<string>
        {
            "2n2sxMso8GAC2ZGdNDpQ9tFPJxSGdhN7fft7DegspSABzR4N3Q",
            "hQ4ztC61u9KxdybxHT5qJ15epssyDtH7VNsvhc6iv2ZBpCvkR",
            "2iuJop83hh1DpUntoiN3MnnpRfdGV7Y2vvVpuYQ9GHXn9aa5uK",
            "2LvVpZxdNZtTBYwggw1RNG3PoHEGK7HV7CBH9jPaP3tXdaDH3h",
            "2i8L1kQkjihEXiX5jZMyzsoP2WGfuCyFQzWoKgQgq3uKLQ8fkp"
        };

        private readonly List<string> _stableMember = new List<string>
        {
        };

        private readonly List<string> _newAssociationMember = new List<string>
        {
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
        private string _oracleContractAddressMain = "xwfQDMdE5xCmyKcDDKV8EDTJdmVfSY6zxiojUxjYKvWpkeu65";
        private string _reportContractAddressMain = "2QtXdKR1ap9Sxgvz3ksiozXx88xf12rfQhk7kNGYuamveDh1ZX";
        private string _bridgeContractAddressMain = "foDLAM2Up3xLjg43SvCy5Ed6zaY5CKG8uczj6yUVZUweqQUmz";
        private string _merkleTreeAddressMain = "R9Das6RzsmpGC79fHZCapUR8yimXguvwfvkgq6sKikrtidJqc";
        private string _regimentContractAddressMain = "2hQPHPHUjcQGrdXXYFHJhXQhL5vEw78rTDpigftCUC6b2W8DTL";
        private string _stringAggregatorAddressMain = "xhhLqDthzC4rmyNURwQpASKWWF6o2eJHbGF5j9H1MyxAxNLap";

        //
        //SideChain
        private string _oracleContractAddressSide = "2smYuXnjc6P1DTZmJ59BatuQrwwxAEkRDy6LT6YeXbAKrsW4GX";
        private string _reportContractAddressSide = "G5Fu3sH7YDoVsGja2ruwccNMMPAtsduFS7HZZfiZhMU4fqKPL";
        private string _bridgeContractAddressSide = "JKjoabe2wyrdP1P8TvNyD4GZP6z1PuMvU2y5yJ4JTeBjTMAoX";
        private string _merkleTreeAddressSide = "BG7b87noRQbxqmGuTb8BRrV8yMHMbDQh4ReoHMAnqsbs3FkZB";
        private string _regimentContractAddressSide = "ZwMH9FZwndiJgycM1boWLRS8Fdb33vBPFfnaiM1aisXjUb53S";
        private string _stringAggregatorAddressSide = "ehvZVJPtuCVx9xJYJPCZpS29Baq6mhyefrMWUXAFR6CeS4GMq";

        private string Password { get; } = "12345678";

        private static string RpcUrl { get; set; } = "";

        // private static string MainRpcUrl { get; } = "http://192.168.67.18:8000";
        private static string MainRpcUrl { get; } = "https://aelf-test-node.aelf.io";

        // private static string SideRpcUrl { get; } = "http://192.168.66.106:8000";
        private static string SideRpcUrl { get; } = "https://tdvw-test-node.aelf.io";


        private string Symbol { get; } = "PORT";
        private string SwapSymbol { get; } = "ELF";
        private string UsdSymbol { get; } = "USDT";
        private string EthSymbol { get; } = "ETH";
        private string BnbSymbol { get; } = "BNB";

        private readonly bool isNeedInitialize = false;

        private readonly bool isNeedCreate = false;

        private readonly string _organizationAddress = "veApaXJAt4ZGivkLXE123kJboamzSqbtA2WGy7LWhJYYFXtQu";

        //DKQJtqZDqCfUDFPysHqqDeZNHdzHBmKTZe1bedcRnY5B147Go
        private string _regiment = "";

        private string _regimentId = "";

        //Main
        private string _mainRegiment = "2iL8n3Xg8mMCeNYKF5RFuFPXvsmm6XBPnLPiWLGDFXqg8fYWMb";

        private string _mainRegimentId = "4f8a8a36db81ac379116f893795a193c7368e4cf4008ae6f8a1382d563a07ac3";

        // Side
        private string _sideRegiment = "CtBGxfpG6pmMfybSo3jkvSL1mFLD18hWgFP6w8ygvrJq3NzVN";

        private string _sideRegimentId = "bb228509668b6ad50f590585e15ad3a3009bad456c40e061a0eb402990804c9a";
        //Main
        // private string _mainRegiment = "MPdKzf6878RkopshTn9QvxG4ToKmqDpd42TfT4LVLQ41R7aiy";
        // private string _mainRegimentId = "556d27746f0e7d38946435eea7b864135f52854b1eb389956ece5afce0e47693";
        // Side
        // private string _sideRegiment = "kHy6xRDBHkuZTjw6vBebceotRFsn6jMaNBTKMFNcHgy6b822g";
        // private string _sideRegimentId = "1e7e21d4367a5fb369bbf0e1d524a958b6341e3a2ee76469bd69f6b945643767";

        private long payment = 0;
        private int maximalLeafCount = 8;

        // private string EthSwapId = "";
        // private string UsdtSwapId = "";
        // private string ElfSwapId = "";   


        //Main-Sepolia
        // private string EthSwapId = "3a3146eedf96c99a04bf1415bfd8d83e45dc1993453e217f7c56c53e2001cc0b";
        // private string UsdtSwapId = "5b623d70c262ecef0bb3c8ddd1ad51d9d9933c8fe0dff7d8aea4488e5dfbd817";
        // private string ElfSwapId = "aabec5971a4dd72cd54acaa4fe474c47b45046179234fb1b97a72f30a8084708";

        // //Main-BSC
        // private string ElfSwapId = "9f2b9119c23fbc9924648b572cdea63deec409805d60c9f88d450e7a785abc0a";
        // private string UsdtSwapId = "c3416a26fcc52078c82d62df981b671a5f330bd57fc0808d9b0af6906c5bf301";
        // private string BNBSwapId = "3e5610d43d5a9ce1b1e8d29d7831d158d8f3ac55889b21efa9e04a2f793fe033";


        //Side-Bsc
        private string ElfSwapId = "808b1ad076858d54a31b80b5764245cb5431ab137ceb697b6cd20861ba54321a";
        // private string UsdtSwapId = "b9c65e0a311ba904722ad70717615385c8110f44d3c2ac6f2804a53227bbced1";
        // private string BNBSwapId = "85bf845d747ec40da6b7533fada6050b0816669ba42060c1fc923c72af0d4d64";
        //Side-Sepolia
        // private string EthSwapId = "762e0e000ee2742368a1fce0f0fbe5a2570f8c59a71d621deb63ac5389a20bfe";
        // private string UsdtSwapId = "9ec6fa1542acf976cc8f23eac85317f1784465edd4515fc0b993c032beef4183";
        // private string ElfSwapId = "40de0bdf7f84e577e59079654674f5bf247c6c472ee929523781c47216fbab20";


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

            //Transfer();
            if (!isNeedCreate) return;
            // CreateToken(UsdSymbol,6,10_00000000_000000);
            // CreateToken(EthSymbol,8,10_000000000_00000000);
            // _tokenContract.TransferBalance(BpAccount, InitAccount, 100000_00000000, "ELF");
            // CreateToken(Symbol,8,10_000000000_00000000);
            // var tokenInfo = _tokenContract.GetTokenInfo(Symbol);
            // Logger.Info($"symbol:{tokenInfo.Symbol},decimals:{tokenInfo.Decimals},totalSupply:{tokenInfo.TotalSupply}");
            // CreateToken(BnbSymbol, 8, 1_000000000_00000000);
            // CreateToken(EthSymbol, 8, 1_000000000_00000000);


            // if (!isNeedInitialize) return;
            //InitializeContract();
        }


        [TestMethod]
        public void CreateAssociation()
        {
            var list = new List<Address>();

            _associationMember.ForEach(l => { list.Add(l.ConvertAddress()); });
            var association = _genesisContract.GetAssociationAuthContract(InitAccount);
            var input = new CreateOrganizationInput
            {
                CreationToken = HashHelper.ComputeFrom("ebridge organization"),
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
            // var list = new List<Address>();
            // _associationMember.ForEach(l => { list.Add(l.ConvertAddress()); });
            // list.Add(InitAccount.ConvertAddress());
            // // _tokenContract.TransferBalance(InitAccount, Admin, 100_0000000);
            // Logger.Info("Initialize Oracle: ");
            // var initializeOracle = _oracleContract.ExecuteMethodWithResult(OracleMethod.Initialize,
            //     new AElf.Contracts.Oracle.InitializeInput
            //     {
            //         RegimentContractAddress = _regimentContract.Contract,
            //         MinimumOracleNodesCount = 5,
            //         DefaultRevealThreshold = 3,
            //         DefaultAggregateThreshold = 3,
            //         IsChargeFee = false,
            //         DefaultExpirationSeconds = 3600
            //     });
            // initializeOracle.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //
            // Logger.Info("Initialize Report: ");
            // var initializeReport = _reportContract.ExecuteMethodWithResult(ReportMethod.Initialize,
            //     new AElf.Contracts.Report.InitializeInput
            //     {
            //         OracleContractAddress = _oracleContract.Contract,
            //         ReportFee = 0,
            //         ApplyObserverFee = 0,
            //         RegimentContractAddress = _regimentContract.Contract,
            //         InitialRegisterWhiteList = {list},
            //         OwnerAddress = InitAccount.ConvertAddress()
            //     });
            // initializeReport.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //
            // Logger.Info("Initialize Merkle: ");
            // var initializeMerkle = _merkleTreeContract.ExecuteMethodWithResult(MerkleTreeMethod.Initialize,
            //     new AElf.Contracts.MerkleTreeContract.InitializeInput
            //     {
            //         RegimentContractAddress = _regimentContract.Contract,
            //         Owner = InitAccount.ConvertAddress()
            //     });
            // initializeMerkle.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            Logger.Info("Initialize Bridge: ");
            _bridgeContract.SetAccount(InitAccount);
            var initializeBridge = _bridgeContract.ExecuteMethodWithResult(BridgeMethod.Initialize,
                new EBridge.Contracts.Bridge.InitializeInput
                {
                    OracleContractAddress = _oracleContract.Contract,
                    RegimentContractAddress = _regimentContract.Contract,
                    MerkleTreeContractAddress = _merkleTreeContract.Contract,
                    ReportContractAddress = _reportContract.Contract,
                    Admin = Admin.ConvertAddress(),
                    Controller = InitAccount.ConvertAddress(),
                    OrganizationAddress = _organizationAddress.ConvertAddress(),
                    PauseController = InitAccount.ConvertAddress(),
                    ApproveTransferController = InitAccount.ConvertAddress()
                });
            initializeBridge.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info(_oracleContract.CallViewMethod<Address>(OracleMethod.GetController, new Empty()));
        }

        [TestMethod]
        [DataRow(true)]
        public void CreateRegiment(bool isApproveToJoin)
        {
            _tokenContract.TransferBalance(InitAccount, RegimentManager, 100_0000000);
            var list = new List<Address>();
            _associationMember.ForEach(l => { list.Add(l.ConvertAddress()); });
            _oracleContract.SetAccount(RegimentManager);
            var result =
                _oracleContract.ExecuteMethodWithResult(OracleMethod.CreateRegiment, new CreateRegimentInput
                {
                    IsApproveToJoin = isApproveToJoin,
                    InitialMemberList = { list }
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var logEventDto = result.Logs.First(l => l.Name.Contains(nameof(RegimentCreated))).NonIndexed;
            var regimentCreated = RegimentCreated.Parser.ParseFrom(ByteString.FromBase64(logEventDto));
            Logger.Info($"Manager: {regimentCreated.Manager}\n" +
                        $"Create Time: {regimentCreated.CreateTime}\n" +
                        $"Regiment Address: {regimentCreated.RegimentAddress}\n" +
                        $"Regiment Id: {regimentCreated.RegimentId}");
            regimentCreated.Manager.ShouldBe(_oracleContract.CallAccount);
            regimentCreated.Manager.ShouldBe(RegimentManager.ConvertAddress());
            var regimentId = HashHelper.ComputeFrom(regimentCreated.RegimentAddress);
            regimentId.ShouldBe(regimentCreated.RegimentId);
        }

        [TestMethod]
        public void AddAdmin()
        {
            var regimentInfo =
                _regimentContract.CallViewMethod<RegimentInfo>(RegimentMethod.GetRegimentInfo,
                    _regiment.ConvertAddress());
            _oracleContract.SetAccount(RegimentManager);
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
            var swapId = "424485a0dc459869380f1bc186ebc2da195d51005e62ff67d6fce56fcc829769";
            var pairIdHash = Hash.LoadFromHex(swapId);
            _bridgeContract.SetAccount(Admin);
            var result = _bridgeContract.ExecuteMethodWithResult(BridgeMethod.ChangeSwapRatio, new ChangeSwapRatioInput
            {
                SwapId = pairIdHash,
                SwapRatio = new SwapRatio
                {
                    OriginShare = 10000_00000000,
                    TargetShare = 1
                },
                TargetTokenSymbol = UsdSymbol
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        [DataRow("ELF", "Sepolia", 100_00000000)]
        [DataRow("ETH", "Sepolia", 100_00000000)]
        [DataRow("USDT", "Sepolia", 1)]
        [DataRow("ELF", "BSCTest", 100_00000000)]
        [DataRow("BNB", "BSCTest", 100_00000000)]
        [DataRow("USDT", "BSCTest", 10000_00000000)]
        public void CreateSwap(string symbol, string fromChainId, long originShare)
        {
            var swapRatio = new SwapRatio
            {
                OriginShare = originShare,
                TargetShare = 1
            };
            // _tokenContract.TransferBalance(InitAccount, Admin, depositAmount * 2, symbol);
            // _tokenContract.ApproveToken(Admin, _bridgeContractAddress, depositAmount, symbol);
            _bridgeContract.SetAccount(RegimentManager);
            var result = _bridgeContract.ExecuteMethodWithResult(BridgeMethod.CreateSwap, new CreateSwapInput
            {
                RegimentId = Hash.LoadFromHex(_regimentId),
                MerkleTreeLeafLimit = maximalLeafCount,
                SwapTargetToken = new SwapTargetToken()
                {
                    FromChainId = fromChainId,
                    SwapRatio = swapRatio,
                    Symbol = symbol
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
            swapInfo.SwapTargetToken.Symbol.ShouldBe(symbol);
            swapInfo.SwapTargetToken.SwapRatio.ShouldBe(swapRatio);
            swapInfo.SwapTargetToken.FromChainId.ShouldBe(fromChainId);
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
            var chainId = swapInfo.SwapTargetToken.FromChainId;
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

        // [TestMethod]
        // public void GetSwapIdByToken()
        // {
        //     await _bridgeContract.CallViewMethod<Hash>(BridgeMethod.GetSwapIdByToken, new GetSwapIdByTokenInput
        //     {
        //         Symbol = SwapSymbol,
        //         ChainId = "Goerli"
        //     });
        // }

        [TestMethod]
        [DataRow("a6e1820157855d10a340bf5c81ed9a88b92b1005171dc9f1d9587c641a0ac491")]
        // [DataRow("990512de2bf3418c3054822099154ac602baa164aca2811983d3d401d4b81319")]
        public void Deposit(string swapId)
        {
            var depositAmount = 10_00000000;
            var token = BnbSymbol;
            var swapIdHash = Hash.LoadFromHex(swapId);
            var manager = GetRegimentManger(swapId);
            var swapPair = GetSwapPairInfo(swapId, token);
            var managerBalance = _tokenContract.GetUserBalance(manager.ToBase58(), token);
            if (managerBalance < depositAmount)
                _tokenContract.TransferBalance(BNBAccount, RegimentManager, depositAmount, token);
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
        public void GetBalance()
        {
            var address = UsdtAccount;
            var token = UsdSymbol;
            var balance = _tokenContract.GetUserBalance(address, token);
            Logger.Info($"balance is {balance}");
            // var allowance = _tokenContract.GetAllowance(address, _bridgeContractAddress, token);
            // Logger.Info($"allowance is {allowance}");
        }

        [TestMethod]
        public void GetLastLeafIndex()
        {
            var index = _merkleTreeContract.CallViewMethod<UInt64Value>(MerkleTreeMethod.GetLastLeafIndex,
                new GetLastLeafIndexInput
                {
                    SpaceId = Hash.LoadFromHex("0a59e106470259020b4fdcebfd6840071d90006906e29e7a4c2979dc20ace3fc")
                });
            Logger.Info(index);
        }

        [TestMethod]
        public void SetTokenMaximumAmount()
        {
            _bridgeContract.SetAccount(Admin);
            var result = _bridgeContract.ExecuteMethodWithResult(BridgeMethod.SetTokenMaximumAmount,
                new SetMaximumAmountInput
                {
                    Value =
                    {
                        new TokenMaximumAmount
                        {
                            Symbol = SwapSymbol,
                            MaximumAmount = 100000_00000000
                        },
                        new TokenMaximumAmount
                        {
                            Symbol = UsdSymbol,
                            MaximumAmount = 100000_000000
                        },new TokenMaximumAmount
                        {
                            Symbol = EthSymbol,
                            MaximumAmount = 100000_00000000
                        },new TokenMaximumAmount
                        {
                            Symbol = BnbSymbol,
                            MaximumAmount = 100000_00000000
                        }
                    }
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void GetTokenMaxAmount()
        {
            var amount = _bridgeContract.CallViewMethod<Int64Value>(BridgeMethod.GetTokenMaximumAmount, new StringValue
            {
                Value = SwapSymbol
            });
            Logger.Info($"amount:{amount.Value}");
        }

        [TestMethod]
        public void Approve()
        {
            _bridgeContract.SetAccount(Admin);
            var result = _bridgeContract.ExecuteMethodWithResult(BridgeMethod.ApproveTransfer, new ApproveTransferInput
            {
                ReceiptId = "0xdd6a1d120eac029efb02f6275c42402161276fc3f102263737cfc86d4216a411.2"
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
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

        // [TestMethod]
        // public void GetReceiptHashListInfo()
        // {
        //     var swapId = ElfSwapId;
        //     var receiver = "";
        //     var receiptIdList =
        //         _bridgeContract.CallViewMethod<ReceiptIdList>(BridgeMethod.GetSwappedReceiptIdList,
        //             new GetSwappedReceiptIdListInput
        //             {
        //                 SwapId = Hash.LoadFromHex(swapId),
        //                 ReceiverAddress = Address.FromBase58(receiver)
        //             });
        //     Logger.Info(receiptIdList);
        //
        //     var receiptInfoList =
        //         _bridgeContract.CallViewMethod<ReceiptInfoList>(BridgeMethod.GetSwappedReceiptInfoList,
        //             new GetSwappedReceiptInfoListInput
        //             {
        //                 SwapId = Hash.LoadFromHex(swapId),
        //                 ReceiverAddress = Address.FromBase58(receiver)
        //             });
        //     Logger.Info(receiptInfoList);
        // }

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
            spaceInfo.Operator.ShouldBe(regimentId);
        }

        [TestMethod]
        public void GetMerkleTree()
        {
            var swapId = ElfSwapId;
            var spaceId = Hash.LoadFromHex("e685f6b65566ac3e46b3dd7014288cd45cae585280740a94ed5a7f9932f4849a");

            // var leaf = _merkleTreeContract.CallViewMethod<Int64Value>(MerkleTreeMethod.GetLastLeafIndex,
            //     new GetLastLeafIndexInput {SpaceId = spaceId});
            // Logger.Info($"Last leaf index: {leaf.Value}");
            // var fullTree = _merkleTreeContract.CallViewMethod<Int64Value>(MerkleTreeMethod.GetFullTreeCount, spaceId);
            // Logger.Info($"Full Tree count: {fullTree.Value}");

            var localTree = _merkleTreeContract.CallViewMethod<GetLeafLocatedMerkleTreeOutput>(
                MerkleTreeMethod.GetLeafLocatedMerkleTree, new GetLeafLocatedMerkleTreeInput
                {
                    LeafIndex = 20,
                    SpaceId = spaceId
                });
            Logger.Info(localTree);
            // leaf.Value.Div(maximalLeafCount).ShouldBe(localTree.MerkleTreeIndex);
            localTree.FirstLeafIndex.ShouldBe(localTree.MerkleTreeIndex.Mul(maximalLeafCount));
            // localTree.LastLeafIndex.ShouldBe(localTree.FirstLeafIndex.Add(maximalLeafCount - 1));
            localTree.SpaceId.ShouldBe(spaceId);

            var lastMerkleTreeIndex =
                _merkleTreeContract.CallViewMethod<Int64Value>(MerkleTreeMethod.GetLastMerkleTreeIndex, spaceId);
            lastMerkleTreeIndex.Value.ShouldBe(localTree.MerkleTreeIndex);
        }

        // [TestMethod]
        // public void GetMerkleTreePath()
        // {
        //     var swapId = ElfSwapId;
        //     var spaceId = GetSpaceId(swapId);
        //     var receiver = "";
        //     var leaf = _merkleTreeContract.CallViewMethod<Int64Value>(MerkleTreeMethod.GetLastLeafIndex,
        //         new GetLastLeafIndexInput { SpaceId = spaceId });
        //
        //     var localTree = _merkleTreeContract.CallViewMethod<GetLeafLocatedMerkleTreeOutput>(
        //         MerkleTreeMethod.GetLeafLocatedMerkleTree, new GetLeafLocatedMerkleTreeInput
        //         {
        //             LeafIndex = leaf.Value,
        //             SpaceId = spaceId
        //         });
        //     Logger.Info(localTree);
        //     leaf.Value.Div(maximalLeafCount).ShouldBe(localTree.MerkleTreeIndex);
        //     localTree.FirstLeafIndex.ShouldBe(localTree.MerkleTreeIndex.Mul(maximalLeafCount));
        //     localTree.LastLeafIndex.ShouldBe(localTree.FirstLeafIndex.Add(maximalLeafCount - 1));
        //     localTree.SpaceId.ShouldBe(spaceId);
        //
        //     var receiptInfos = _bridgeContract.CallViewMethod<SwappedReceiptInfo>(BridgeMethod.GetSwappedReceiptInfoList,
        //         new GetSwappedReceiptInfoInput
        //         {
        //             ReceiverAddress = Address.FromBase58(receiver),
        //             SwapId = Hash.LoadFromHex(swapId)
        //         });
        //     var receiptId = receiptInfos.Value.First().ReceiptId;
        //     TryGetReceiptIndex(receiptId, out var receiptIndex);
        //
        //     var merklePath = _merkleTreeContract.CallViewMethod<MerklePath>(MerkleTreeMethod.GetMerklePath,
        //         new GetMerklePathInput
        //         {
        //             SpaceId = spaceId,
        //             LeafNodeIndex = receiptIndex - 1,
        //             ReceiptMaker = Address.FromBase58(receiver)
        //         });
        //
        //     // var firstHash = _bridgeContract.CallViewMethod<Hash>(BridgeMethod.GetReceiptHash, new GetReceiptHashInput
        //     // {
        //     //     RecorderId = recorderId,
        //     //     ReceiptId = id
        //     // });  
        //     //
        //     // var root = merklePath.ComputeRootWithLeafNode(firstHash);
        //     // root.ShouldBe(localTree.MerkleTreeRoot);
        //     //
        //     // var merkleProof =
        //     //     _merkleTreeContract.CallViewMethod<BoolValue>(MerkleTreeMethod.MerkleProof,
        //     //         new MerkleProofInput
        //     //         {
        //     //             SpaceId = spaceId,
        //     //             MerklePath = merklePath,
        //     //             LeafNode = firstHash,
        //     //             LastLeafIndex = receiptCount.Value - 1
        //     //         });
        //     // merkleProof.Value.ShouldBeTrue();
        // }

        #endregion


        // [TestMethod]
        // public void GetSwappedReceiptIdList()
        // {
        //     var sender = "WRRiSjFdJjivFN4ZGcQswAd45foLH4jusNi2HYb35D6CtwGVL";
        //
        //     var swapId = ElfSwapId;
        //     var swapIdHash = Hash.LoadFromHex(swapId);
        //     var list = _bridgeContract.CallViewMethod<ReceiptIdList>(BridgeMethod.GetSwappedReceiptIdList,
        //         new GetSwappedReceiptIdListInput
        //         {
        //             ReceiverAddress = sender.ConvertAddress(),
        //             SwapId = swapIdHash
        //         });
        //     Logger.Info(list);
        // }

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
                // _tokenContract.TransferBalance(BpAccount, InitAccount, 2000_00000000, SwapSymbol);
                // _tokenContract.TransferBalance(BpAccount, Admin, 2000_00000000, SwapSymbol);

                _tokenContract.TransferBalance(BpAccount, member, 1000_00000000, SwapSymbol);
            }

            // _tokenContract.IssueBalance(InitAccount, _associationMember.First(), 100000000, Symbol);
            // _tokenContract.ApproveToken(_associationMember.First(), _oracleContract.ContractAddress, 100000000, Symbol);
        }

        [TestMethod]
        public void TransferTest()
        {
            // var fromAddress = "nn659b9X1BLhnu5RWmEUbuuV7J9QKVVSN54j9UmeCbF3Dve5D";
            foreach (var member in _associationMember)
            {
                var balance = _tokenContract.GetUserBalance(member, SwapSymbol);
                Logger.Info($"Before balance:{balance}");
                _tokenContract.TransferBalance(BpAccount, member, 800_00000000, SwapSymbol);
            }
            // _tokenContract.TransferBalance(fromAddress, InitAccount, 1000_000000, UsdSymbol);
        }

        #region AELF -> other

        [TestMethod]
        public void AddRegisterWhiteList()
        {
            var result = AuthorityManager.ExecuteTransactionWithAuthority(_reportContract.ContractAddress,
                nameof(ReportMethod.AddRegisterWhiteList), RegimentManager.ConvertAddress(), InitAccount);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            
        }
        
        [TestMethod]
        [DataRow("Sepolia", "0x276A12Bd934cb9753AdB89DFe88CA1442c5B1B47")]
        [DataRow("BSCTest", "0x4C6720dec7C7dcdE1c7B5E9dd2b327370AC9F834")]
        public void RegisterOffChainAggregation(string chainId, string token)
        {
            //BridgeOut address on eth
            _reportContract.SetAccount(RegimentManager);
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
        [DataRow("Sepolia", "0x276A12Bd934cb9753AdB89DFe88CA1442c5B1B47")]
        [DataRow("BSCTest", "0x4C6720dec7C7dcdE1c7B5E9dd2b327370AC9F834")]
        public void SetSkipMemberList(string chainId, string token)
        {
            //BridgeOut address on eth
            _reportContract.SetAccount(RegimentManager);
            var result = _reportContract.ExecuteMethodWithResult(ReportMethod.SetSkipMemberList,
                new SetSkipMemberListInput
                {
                    ChainId = chainId,
                    Token = token,
                    Value = new MemberList
                    {
                        Value = { RegimentManager.ConvertAddress() }
                    }
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        [DataRow("Sepolia", "ETH")]
        [DataRow("BSCTest", "BNB")]
        public void AddToken(string chainId, string symbol)
        {
            var adminBalance = _tokenContract.GetUserBalance(Admin, SwapSymbol);
            if (adminBalance <= 0)
                _tokenContract.TransferBalance(InitAccount, Admin, 10_00000000, SwapSymbol);
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
                        Symbol = symbol
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
        [DataRow("Sepolia")]
        [DataRow("BSCTest")]
        public void SetGasLimit(string chainId)
        {
            var gasLimit = (long)((21000 + 68 * 3400) * 1.1);
            //var limit = _bridgeContract.CallViewMethod<Int64Value>(BridgeMethod.GetGasLimit, new StringValue{Value = chainId});
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
        [DataRow("Sepolia")]
        [DataRow("BSCTest")]
        public void SetPriceFluctuationRatio(string chainId)
        {
            _bridgeContract.SetAccount(Admin);
            var result = _bridgeContract.ExecuteMethodWithResult(BridgeMethod.SetPriceFluctuationRatio,
                new SetRatioInput
                {
                    Value =
                    {
                        new Ratio
                        {
                            ChainId = chainId,
                            Ratio_ = 10
                        }
                    }
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"transaction id:{result.TransactionId}");
        }

        [TestMethod]
        public void ChangeRegimentManager()
        {
            _regiment = _sideRegiment;
            _oracleContract.SetAccount(Admin);
            var regimentInfo = _regimentContract.CallViewMethod<RegimentInfo>(RegimentMethod.GetRegimentInfo,
                _regiment.ConvertAddress());
            Logger.Info($"regiment manager:{regimentInfo.Manager}");
            var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.TransferRegimentOwnership,
                new TransferRegimentOwnershipInput
                {
                    RegimentAddress = _regiment.ConvertAddress(),
                    NewManagerAddress = RegimentManager.ConvertAddress()
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"transaction id:{result.TransactionId}");
            _oracleContract.SetAccount(RegimentManager);
            var result1 = _oracleContract.ExecuteMethodWithResult(OracleMethod.DeleteRegimentMember,
                new DeleteRegimentMemberInput
                {
                    RegimentAddress = _regiment.ConvertAddress(),
                    DeleteMemberAddress = Admin.ConvertAddress()
                });
            result1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"transaction id:{result.TransactionId}");
        }

        [TestMethod]
        public void GetRegimentMemberList()
        {
            _regiment = _sideRegiment;
            var memberList = _oracleContract.CallViewMethod<AddressList>(OracleMethod.GetRegimentMemberList,
                _regiment.ConvertAddress());
            foreach (var member in memberList.Value)
            {
                Logger.Info(member.ToBase58());
            }
        }

        [TestMethod]
        public void GetSkipMemberList()
        {
            var memberList = _reportContract.CallViewMethod<MemberList>(ReportMethod.GetSkipMemberList,
                new GetSkipMemberListInput
                {
                    Token = "0x276A12Bd934cb9753AdB89DFe88CA1442c5B1B47",
                    ChainId = "Sepolia"
                });
            foreach (var member in memberList.Value)
            {
                Logger.Info(member.ToBase58());
            }
        }

        [TestMethod]
        public void GetSwapInfo()
        {
            var swapId = Hash.LoadFromHex("3e5610d43d5a9ce1b1e8d29d7831d158d8f3ac55889b21efa9e04a2f793fe033");
            var swapInfo = _bridgeContract.CallViewMethod<SwapInfo>(BridgeMethod.GetSwapInfo,
                swapId);
            Logger.Info(swapInfo.SwapTargetToken.SwapRatio);
        }

        [TestMethod]
        public void ReportConfirm()
        {
            // _regiment = _mainRegiment;
            var memberList = _oracleContract.CallViewMethod<AddressList>(OracleMethod.GetRegimentMemberList,
                _regiment.ConvertAddress());
            foreach (var member in memberList.Value)
            {
                Logger.Info(member.ToBase58());
            }
            //
            // var regimentInfo = _regimentContract.CallViewMethod<RegimentInfo>(RegimentMethod.GetRegimentInfo,
            //     _regiment.ConvertAddress());
            // Logger.Info($"regiment manager:{regimentInfo.Manager}");
        }

        [TestMethod]
        public async Task QueryTransactionResult()
        {
            // var result =
            //     await NodeManager.ApiClient.GetTransactionResultAsync(
            //         "9b5f77fa6e204acedbe0f418d6431c1dbea8d8b33de8387b8b47e1f3aaf1f7cd");
            var logEvent = TransactionFeeCharged.Parser.ParseFrom(ByteString.FromBase64(
                "CgNFTEYQ4Jqa0J0G"));
            Logger.Info($"log event:{logEvent.Amount}");
        }

        [TestMethod]
        public void CreateReceipt()
        {
            _tokenContract.TransferBalance(BNBAccount, TestAccount, 10_00000000, BnbSymbol);
            // _bridgeContract.SetAccount(TestAccount);
            _tokenContract.ApproveToken(TestAccount, _bridgeContract.ContractAddress, 200_00000000, BnbSymbol);
            var result = _bridgeContract.ExecuteMethodWithResult(BridgeMethod.CreateReceipt,
                new CreateReceiptInput
                {
                    Symbol = SwapSymbol,
                    Amount = 2_00000000,
                    TargetAddress = "0xfe88A8E3a7Eac01CF6018c0Ec6Ed114C5892C3a2",
                    TargetChainId = "Sepolia"
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"transaction id:{result.TransactionId}");
            // var txList = new List<string>();
            // for (var j = 0; j < 1; j++)
            // {
            //     for (var i = 1; i <= 3; i++)
            //     {
            //         var random = CommonHelper.GenerateRandomNumber(1, 10);
            //         // long amount = i + j + random;
            //         long amount = (i + j + 1).Mul(100000000);
            //
            //         var result = _bridgeContract.ExecuteMethodWithTxId(BridgeMethod.CreateReceipt,
            //             new CreateReceiptInput
            //             {
            //                 Symbol = "ELF",
            //                 Amount = amount,
            //                 TargetAddress = "0xf01Db78977D025dc9fF4380F631019C09D5EFAcc",
            //                 TargetChainId = "NewGoerli"
            //             });
            //         txList.Add(result);
            //     }
            //
            //     Thread.Sleep(1000);
            // }
            //
            // Thread.Sleep(4000);
            // foreach (var result in txList.Select(tx =>
            //              AsyncHelper.RunSync(() => NodeManager.ApiClient.GetTransactionResultAsync(tx))))
            // {
            //     Logger.Info($"{result.Status.ConvertTransactionResultStatus()} - {result.BlockNumber}");
            // }
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
        public void GetTokenInfo()
        {
            var tokenInfo = _tokenContract.GetTokenInfo("WETH");
            Logger.Info(tokenInfo.Issuer);
        }


        [TestMethod]
        public void IssueTokenFromParliament()
        {
            var symbolList = new List<string> { "WETH", "WBNB" };
            foreach (var symbol in symbolList)
            {
                var totalSupply = _tokenContract.GetTokenInfo(symbol).TotalSupply;
                var input = new IssueInput
                {
                    Amount = totalSupply,
                    Symbol = symbol,
                    To = InitAccount.ConvertAddress()
                };
                AuthorityManager.ExecuteTransactionWithAuthority(_tokenContract.ContractAddress,
                    nameof(TokenMethod.Issue),
                    input, InitAccount, _parliament.GetGenesisOwnerAddress());

                var balance = _tokenContract.GetUserBalance(InitAccount, symbol);
                Logger.Info(balance);
            }
        }

        [TestMethod]
        public void Check()
        {
            var whiteList =
                _bridgeContract.CallViewMethod<TokenSymbolList>(BridgeMethod.GetTokenWhitelist, new StringValue
                {
                    Value = "BSCTest"
                });
            Logger.Info(whiteList.Symbol);

            // var txID = "2adc0140ec070c7129f1e8e6d7e5bc16a827d73047d5c6351c21d8cf3fc0169c";
            // var txResult = AsyncHelper.RunSync(() => NodeManager.ApiClient.GetTransactionResultAsync(txID));
            // var logs = txResult.Logs.First(l => l.Name.Equals("ReportConfirmed")).NonIndexed;
            // var confirmed = ReportConfirmed.Parser.ParseFrom(ByteString.FromBase64(logs));
            // Logger.Info(confirmed);
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
            var chain = "Sepolia";
            var gasLimit =
                _bridgeContract.CallViewMethod<Int64Value>(BridgeMethod.GetGasLimit, new StringValue { Value = chain });
            var gasPrice =
                _bridgeContract.CallViewMethod<Int64Value>(BridgeMethod.GetGasPrice,
                    new StringValue { Value = chain });
            var gasPriceRatio = _bridgeContract.CallViewMethod<Int64Value>(BridgeMethod.GetPriceRatio,
                new StringValue { Value = chain });
            Logger.Info(gasPrice);
            // var floatingRatio = _bridgeContract.CallViewMethod<StringValue>(BridgeMethod.GetFeeFloatingRatio,
            //     new StringValue { Value = chain });
            // if (!decimal.TryParse(floatingRatio.Value, out var floatingRatioDecimal))
            // {
            //     floatingRatioDecimal = 1;
            // }
            // var floatingRatioDecimal = 1;
            // var nativeTokenFee = CalculateTransactionFee(gasLimit.Value, gasPrice.Value,
            //     gasPriceRatio.Value, floatingRatioDecimal);
            // Logger.Info(nativeTokenFee);
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