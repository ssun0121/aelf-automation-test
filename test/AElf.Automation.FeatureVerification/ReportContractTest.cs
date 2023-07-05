using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using AElf.Contracts.MultiToken;
using EBridge.Contracts.Oracle;
using EBridge.Contracts.Report;
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
using Nethereum.Hex.HexConvertors.Extensions;
using Shouldly;
using InitializeInput = EBridge.Contracts.Oracle.InitializeInput;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class ReportContractTest
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }

        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;
        private ParliamentContract _parliamentContract;
        private AssociationContract _associationContract;

        private OracleContract _oracleContract;
        private OracleUserContract _oracleUserContract;
        private ReportContract _reportContract;
        private Address _integerAggregator;
        private OracleContractContainer.OracleContractStub _oracle;

        private string TestAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";

        // private string InitAccount { get; } = "ZrAFaqdr79MWYkxA49Hp2LUdSVHdP6fJh3kDw4jmgC7HTgrni";
        private string InitAccount { get; } = "zptx91dhHVJjJRxf5Wg5KAoMrDrWX6i1H2FAyKAiv2q8VZfbg";
        private string OtherNode { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";

        private readonly List<string> _associationMember = new List<string>
        {
            "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK",
            "28qLVdGMokanMAp9GwfEqiWnzzNifh8LS9as6mzJFX1gQBB823",
            "eFU9Quc8BsztYpEHKzbNtUpu9hGKgwGD2tyL13MqtFkbnAoCZ"
        };

        //2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS
        //2WHXRoLRjbUTDQsuqR5CntygVfnDb125qdJkudev4kVNbLhTdG
        //2RHf2fxsnEaM3wb6N1yGqPupNZbcCY98LgWbGSFWmWzgEs5Sjo
        //2NxwCPAGJr4knVdmwhb1cK7CkZw5sMJkRDLnT7E2GoDP2dy5iZ
        private string _oracleContractAddress = "2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS";
        private string _reportContractAddress = "2WHXRoLRjbUTDQsuqR5CntygVfnDb125qdJkudev4kVNbLhTdG";
        private string _integerAggregatorAddress = "2RHf2fxsnEaM3wb6N1yGqPupNZbcCY98LgWbGSFWmWzgEs5Sjo";
        private string _oracleUserContractAddress = "2NxwCPAGJr4knVdmwhb1cK7CkZw5sMJkRDLnT7E2GoDP2dy5iZ";

        //6Yu5KJprje1EKf78MicuoAL3VsK3DoNoGm1ah1dUR5Y7frPdE
        //2S2Fx7PuK9Us3h7PVUmnsLX7Q3PTsFpTXuW52qdKUBAgJLw5s5
        //2gin3EoK8YvfeNfPjWyuKRaa3GEjF1KY1ZpBQCgPF3mHnQmDAX
        private Address _defaultParliamentOrganization;
        private string _association1 = "6Yu5KJprje1EKf78MicuoAL3VsK3DoNoGm1ah1dUR5Y7frPdE";
        private string _association2 = "2S2Fx7PuK9Us3h7PVUmnsLX7Q3PTsFpTXuW52qdKUBAgJLw5s5";
        private string _association3 = "2gin3EoK8YvfeNfPjWyuKRaa3GEjF1KY1ZpBQCgPF3mHnQmDAX";

        private string Password { get; } = "12345678";

        // private static string RpcUrl { get; } = "13.212.233.221:8000";
        private static string RpcUrl { get; } = "127.0.0.1:8000";
        private string Symbol { get; } = "PORT";
        private readonly bool isNeedInitialize = false;
        private readonly bool isNeedCreateAssociation = false;

        private long payAmount = 1_00000000;
        private long _applyObserverFee = 10_00000000;
        private long _defaultReportFee = 10_0000000;
        // private string eth = "0xc3db2d4548500446aaba5f2c8cc58138e065439e";
        // private string digestStr = "0x644ed201d5cbf63b0ddf13d5ecd0d720";

        private string eth = "0x71ada1f375d7e10d63749765267b942c83416d93";
        private string digestStr = "0x209cf2f3c11496825cad06af2fa88985";

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("ReportContactTest");
            Logger = Log4NetHelper.GetLogger();
            NodeManager = new NodeManager(RpcUrl);
            AuthorityManager = new AuthorityManager(NodeManager, InitAccount, Password);
            Logger.Info(RpcUrl);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount, Password);
            _parliamentContract = _genesisContract.GetParliamentContract(InitAccount, Password);
            _associationContract = _genesisContract.GetAssociationAuthContract(InitAccount, Password);
            _defaultParliamentOrganization = _parliamentContract.GetGenesisOwnerAddress();
            _tokenContract = _genesisContract.GetTokenContract(InitAccount, Password);

            _oracleContract = _oracleContractAddress == ""
                ? new OracleContract(NodeManager, InitAccount)
                : new OracleContract(NodeManager, InitAccount, _oracleContractAddress);
            _reportContract = _reportContractAddress == ""
                ? new ReportContract(NodeManager, InitAccount)
                : new ReportContract(NodeManager, InitAccount, _reportContractAddress);
            _oracle = _oracleContract.GetTestStub<OracleContractContainer.OracleContractStub>(InitAccount);
            _integerAggregator = _integerAggregatorAddress == ""
                ? AuthorityManager.DeployContractWithAuthority(InitAccount, "AElf.Contracts.IntegerAggregator")
                : Address.FromBase58(_integerAggregatorAddress);
            _oracleUserContract = _oracleUserContractAddress == ""
                ? new OracleUserContract(NodeManager, InitAccount)
                : new OracleUserContract(NodeManager, InitAccount, _oracleUserContractAddress);
            
            if (isNeedCreateAssociation)
            {
                _association1 = _association1 == ""
                    ? AuthorityManager.CreateAssociationOrganization(_associationMember).ToBase58()
                    : _association1;
                _association2 = _association2 == ""
                    ? AuthorityManager
                        .CreateAssociationOrganization(new List<string> {InitAccount, TestAccount, OtherNode})
                        .ToBase58()
                    : _association2;
                _association3 = _association3 == ""
                    ? AuthorityManager.CreateAssociationOrganization(new List<string>
                            {_associationMember[0], _associationMember[1], OtherNode})
                        .ToBase58()
                    : _association3;
                Logger.Info($"{_association1},{_association2},{_association3}");
            }

            if (!isNeedInitialize) return;
            InitializeAndCreateToken();
            InitializeReportTest();
            // InitializeTestContract();
        }

        [TestMethod]
        public void InitializeReportTest()
        {
            var result = _reportContract.ExecuteMethodWithResult(ReportMethod.Initialize,
                new EBridge.Contracts.Report.InitializeInput()
                {
                    OracleContractAddress = _oracleContract.Contract,
                    ReportFee = _defaultReportFee,
                    ApplyObserverFee = _applyObserverFee
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var allowance = _tokenContract.GetAllowance(_reportContract.ContractAddress,
                _oracleContract.ContractAddress, Symbol);
            allowance.ShouldBe(long.MaxValue);
            Logger.Info(allowance);
        }

        #region OffChain

        [TestMethod]
        public void AddRegisterWhiteList()
        {
            var isWhiteList = _reportContract.CallViewMethod<BoolValue>(ReportMethod.IsInRegisterWhiteList,
                Address.FromBase58(InitAccount));
            if (isWhiteList.Value.Equals(true)) return;

            var result = AuthorityManager.ExecuteTransactionWithAuthority(_reportContract.ContractAddress,
                nameof(ReportMethod.AddRegisterWhiteList), InitAccount.ConvertAddress(), InitAccount);
            result.Status.ShouldBe(TransactionResultStatus.Mined);

            isWhiteList = _reportContract.CallViewMethod<BoolValue>(ReportMethod.IsInRegisterWhiteList,
                Address.FromBase58(InitAccount));
            isWhiteList.Value.ShouldBeTrue();
        }

        [TestMethod]
        public void RemoveFromRegisterWhiteList()
        {
            var result = AuthorityManager.ExecuteTransactionWithAuthority(_reportContract.ContractAddress,
                nameof(ReportMethod.RemoveFromRegisterWhiteList), InitAccount.ConvertAddress(), InitAccount);
            result.Status.ShouldBe(TransactionResultStatus.Mined);

            var isWhiteList = _reportContract.CallViewMethod<BoolValue>(ReportMethod.IsInRegisterWhiteList,
                Address.FromBase58(InitAccount));
            isWhiteList.Value.ShouldBeFalse();
        }

        [TestMethod]
        public void RegisterOffChainAggregation()
        {
            var result = _reportContract.ExecuteMethodWithResult(ReportMethod.RegisterOffChainAggregation,
                new RegisterOffChainAggregationInput
                {
                    OffChainQueryInfoList = new OffChainQueryInfoList
                    {
                        Value =
                        {
                            new OffChainQueryInfo
                            {
                                Title = 
                                    "https://api.coincap.io/v2/assets/ethereum|" +
                                    "https://api.coingecko.com/api/v3/simple/price?ids=ethereum&vs_currencies=usd",
                                Options =
                                {
                                    "data/priceUsd|" +
                                    "ethereum/usd"
                                }
                            },
                            new OffChainQueryInfo
                            {
                                Title =
                                    "https://api.coincap.io/v2/assets/bitcoin",
                                Options =
                                {
                                    "data/priceUsd"
                                }
                            },
                            new OffChainQueryInfo
                            {
                                Title = 
                                    "https://api.coincap.io/v2/assets/aelf|" +
                                    "https://api.coingecko.com/api/v3/simple/price?ids=aelf&vs_currencies=usd",
                                Options =
                                {
                                    "data/priceUsd|" +
                                    "aelf/usd"
                                }
                            }
                        }
                    },
                    Token = eth,
                    AggregateThreshold = 1,
                    AggregatorContractAddress = _integerAggregator,
                    ConfigDigest = ByteStringHelper.FromHexString(digestStr),
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var log = result.Logs.First(l => l.Name.Equals(nameof(OffChainAggregationRegistered))).NonIndexed;
            var info = OffChainAggregationRegistered.Parser.ParseFrom(ByteString.FromBase64(log));
            // info.AggregatorContractAddress.ShouldBe(_integerAggregator);
            var checkInfo = GetInfo();
            checkInfo.Register.ToBase58().ShouldBe(InitAccount);
            checkInfo.Token.ShouldBe(eth);
            checkInfo.RoundIds.First().ShouldBe(0);
        }

        [TestMethod]
        public void RemoveOffChainQueryInfo()
        {
            var index = 2;
            var result =
                _reportContract.ExecuteMethodWithResult(ReportMethod.RemoveOffChainQueryInfo,
                    new RemoveOffChainQueryInfoInput
                    {
                        RemoveNodeIndex = index,
                        Token = eth
                    });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var info = GetInfo().OffChainQueryInfoList.Value[index].Title;
            info.ShouldBe("invalid");
        }

        [TestMethod]
        public void AddOffChainQueryInfo()
        {
            var result =
                _reportContract.ExecuteMethodWithResult(ReportMethod.AddOffChainQueryInfo,
                    new AddOffChainQueryInfoInput
                    {
                        Token = eth,
                        OffChainQueryInfo = new OffChainQueryInfo
                        {
                            Title = "https://api.coincap.io/v2/assets/ethereum",
                            Options =
                            {
                                "data/priceUsd"
                            }
                        }
                    });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var info = GetInfo().OffChainQueryInfoList.Value.Last().Title;
            info.ShouldBe("https://api.coincap.io/v2/assets/ethereum");
        }

        [TestMethod]
        public void ChangeOffChainQueryInfo()
        {
            var info = GetInfo();
            var result =
                _reportContract.ExecuteMethodWithResult(ReportMethod.ChangeOffChainQueryInfo,
                    new ChangeOffChainQueryInfoInput
                    {
                        Token = eth,
                        NewOffChainQueryInfo = new OffChainQueryInfo
                        {
                            Title = "https://api.coincap.io/v2/assets/bitcoin",
                            Options = {"data/priceUsd"}
                        }
                    });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var newInfo = GetInfo().OffChainQueryInfoList.Value.First();
            info.OffChainQueryInfoList.Value.First().ShouldNotBe(newInfo);
        }

        #endregion

        [TestMethod]
        public void Query()
        {
            var roundId = GetCurrentRound();
            Logger.Info($"Current Round: {roundId}");
            var indexNode = GetInfo().OffChainQueryInfoList.Value;
            for (var i = 0; i < indexNode.Count; i++)
            {
                if (indexNode[i].Title.Equals("invalid")) continue;
                QueryOracle(i);
            }
        }

        private void QueryOracle(int index)
        {
            var allowance = _tokenContract.GetAllowance(InitAccount, _reportContract.ContractAddress, Symbol);
            Logger.Info(allowance);
            if (allowance < payAmount + _defaultReportFee)
                _tokenContract.ApproveToken(InitAccount, _reportContract.ContractAddress, payAmount + _defaultReportFee,
                    Symbol);
            Thread.Sleep(5000);
            allowance = _tokenContract.GetAllowance(InitAccount, _reportContract.ContractAddress, Symbol);
            Logger.Info(allowance);
            if (payAmount + _defaultReportFee > 0)
                _tokenContract.IssueBalance(InitAccount, InitAccount, payAmount + _defaultReportFee, Symbol);
            var senderBalance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            var reportBalance = _tokenContract.GetUserBalance(_reportContract.ContractAddress, Symbol);
            Logger.Info($"index ===> {index}");
            _reportContract.SetAccount(InitAccount);
            var result = _reportContract.ExecuteMethodWithResult(ReportMethod.QueryOracle, new QueryOracleInput
            {
                AggregateThreshold = 1,
                Payment = payAmount,
                Token = eth,
                NodeIndex = index
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var byteString = result.Logs.First(l => l.Name.Contains(nameof(QueryCreated))).NonIndexed;
            var query = QueryCreated.Parser.ParseFrom(ByteString.FromBase64(byteString));
            Logger.Info(query.QueryId.ToHex());
            Logger.Info(query.QueryInfo.Title);
            var queryInfo = _oracleContract.CallViewMethod<QueryRecord>(OracleMethod.GetQueryRecord, query.QueryId);
            var afterSenderBalance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            var afterReportBalance = _tokenContract.GetUserBalance(_reportContract.ContractAddress, Symbol);
            afterSenderBalance.ShouldBe(senderBalance - (payAmount + _defaultReportFee));

            if (queryInfo.IsPaidToOracleContract || queryInfo.IsSufficientCommitmentsCollected)
                afterReportBalance.ShouldBe(reportBalance + _defaultReportFee);
            else
                afterReportBalance.ShouldBe(reportBalance + _defaultReportFee + payAmount);

            foreach (var member in _associationMember)
            {
                var balance = _tokenContract.GetUserBalance(member, Symbol);
                Logger.Info($"{member}: {balance}");
            }
        }

        [TestMethod]
        public void CheckBalance()
        {
            foreach (var member in _associationMember)
            {
                var balance = _tokenContract.GetUserBalance(member, Symbol);
                var mortgageAmount = _reportContract.CallViewMethod<Int64Value>(
                    ReportMethod.GetMortgagedTokenAmount,
                    Address.FromBase58(member));
                Logger.Info($"{member}: {balance} {mortgageAmount.Value}");
            }

            var contractBalance = _tokenContract.GetUserBalance(_reportContract.ContractAddress, Symbol);
            Logger.Info(contractBalance);
        }

        //d4f8923e35c640dfb8c5b5f7adc0cb22ccf85700b9c37c6ae4a3ed980c16963a
        [TestMethod]
        public void CancelQueryReport()
        {
            var id = "49d3affe85aef601fff0f05e3e5f17b3998c46e3b6058359e38b9896ba07ed26";
            var balance = _tokenContract.GetUserBalance(_reportContract.ContractAddress, Symbol);
            var userBalance = _tokenContract.GetUserBalance(_reportContract.CallAddress, Symbol);
            var result = _reportContract.ExecuteMethodWithResult(ReportMethod.CancelQueryOracle, Hash.LoadFromHex(id));
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var afterBalance = _tokenContract.GetUserBalance(_reportContract.ContractAddress, Symbol);
            var afterUserBalance = _tokenContract.GetUserBalance(_reportContract.CallAddress, Symbol);
            afterBalance.ShouldBe(balance  - _defaultReportFee);
            afterUserBalance.ShouldBe(userBalance + _defaultReportFee + payAmount);
        }

        [TestMethod]
        public void ConfirmReport()
        {
            var roundId = 8;
            var report = GetRawReport(8);
            foreach (var member in _associationMember)
            {
                // var member = "eFU9Quc8BsztYpEHKzbNtUpu9hGKgwGD2tyL13MqtFkbnAoCZ";
                //if(member.Equals(_associationMember[0])) continue;
                
                var privateKey = NodeManager.AccountManager.GetPrivateKey(member);
                var generateSignature = CommonHelper.GenerateSignature(report.Value, privateKey.HexToByteArray());
                _reportContract.SetAccount(member);
                var result = _reportContract.ExecuteMethodWithResult(ReportMethod.ConfirmReport, new ConfirmReportInput
                {
                    Signature = generateSignature.RecoverInfo,
                    Token = eth,
                    RoundId = roundId
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var logEvent = result.Logs.First(l => l.Name.Equals(nameof(ReportConfirmed))).NonIndexed;
                var reportConfirmed = ReportConfirmed.Parser.ParseFrom(ByteString.FromBase64(logEvent));
                var signature = reportConfirmed.Signature;
                signature.ShouldBe(generateSignature.RecoverInfo);
                reportConfirmed.Token.ShouldBe(eth);
            }
        }

        [TestMethod]
        public void RejectReport()
        {
            var roundId = 2;
            var node = _associationMember[1];
            var accusingNode = _associationMember[0];
            _reportContract.SetAccount(node);
            var mortgageAmount = _reportContract.CallViewMethod<Int64Value>(ReportMethod.GetMortgagedTokenAmount,
                Address.FromBase58(accusingNode));
            var result = _reportContract.ExecuteMethodWithResult(ReportMethod.RejectReport,
                new RejectReportInput
                {
                    Token = eth,
                    RoundId = roundId,
                    AccusingNodes =
                    {
                        Address.FromBase58(accusingNode)
                    }
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterMortgageAmount = _reportContract.CallViewMethod<Int64Value>(ReportMethod.GetMortgagedTokenAmount,
                Address.FromBase58(accusingNode));
            
            Logger.Info($"{mortgageAmount}, {afterMortgageAmount}");
        }


        [TestMethod]
        public void GetReportQueryRecord()
        {
            // var currentId = GetCurrentRound();
            var offChainInfo = GetInfo();
            var reportInfo = GetReport(7);
            foreach (var observation in reportInfo.Observations.Value)
                Logger.Info($"{observation.Key} ==> {observation.Data}");
            Logger.Info(reportInfo.AggregatedData);
            var info = _reportContract.CallViewMethod<ReportQueryRecord>(ReportMethod.GetReportQueryRecord,
                reportInfo.QueryId);
            info.PaidReportFee.ShouldBe(_defaultReportFee);
            Logger.Info($"ConfirmCount: {info.ConfirmedNodeList}\n" +
                        $"All confirm: {info.IsAllNodeConfirmed}");
        }

        [TestMethod]
        public void GetSignatureMap()
        {
            var result =
                _reportContract.CallViewMethod<SignatureMap>(ReportMethod.GetSignatureMap, new GetSignatureMapInput
                {
                    RoundId = 1,
                    Token = eth
                });
            foreach (var (key, value) in result.Value)
            {
                Logger.Info($"{key}==>{value}");
            }
        }

        [TestMethod]
        public void GetSignature()
        {
            var signature =
                _reportContract.CallViewMethod<StringValue>(ReportMethod.GetSignature,
                    new GetSignatureInput
                    {
                        Address = _associationMember[0].ConvertAddress(),
                        RoundId = 4,
                        Token = eth
                    });
            var r = signature.Value.Substring(0, 64);
            var s = signature.Value.Substring(64, 64);
            var v = signature.Value.Substring(signature.Value.Length - 2, 2);
            Logger.Info($"{signature}==> {r},{s},{v}");
        }

        [TestMethod]
        public void GetAddress()
        {
            foreach (var member in _associationMember)
            {
                var address = CommonHelper.GenerateAddressOnEthereum(NodeManager.GetAccountPublicKey(member));
                Logger.Info($"{member} ==> {address} \n");
            }
        }

        [TestMethod]
        public void GetAddress_Miner()
        {
            var miners = AuthorityManager.GetCurrentMiners();
            foreach (var member in miners)
            {
                var address = CommonHelper.GenerateAddressOnEthereum(NodeManager.GetAccountPublicKey(member));
                Logger.Info($"{member} ==> {address} \n");
            }
        }

        [TestMethod]
        public void GetData()
        {
            var round = 2;
            var reportInfo = GetReport(round);
            var str = reportInfo.AggregatedData;
            var ethStr = "0x323737342e34303236313833313939383835323638";
            var ethRevertData = Encoding.UTF8.GetString(ByteArrayHelper.HexStringToByteArray(ethStr));
            var revertData = Encoding.UTF8.GetString(str.ToByteArray());
            ethRevertData.ShouldBe(revertData);
            Logger.Info($"{revertData} {ethRevertData}");
        }

        [TestMethod]
        public void GetData_MerkleTreeType()
        {
            var index = "0x161215";
            var data = new List<string>
            {
                "0x33363231352e3034303335333331363239373732393600000000000000000000",
                "0x302e323530363931303137393332363436350000000000000000000000000000",
                "0x323530392e313339373533313934343738303234370000000000000000000000"
            };
            var digits = new List<int>();
            var length = index.Length;
            var count = length / 2 - 1;
            for (var i = 0; i < count; i++)
            {
                var b = index.Substring(2 * (i + 1), 2);
                digits.Add(Int32.Parse(b, System.Globalization.NumberStyles.HexNumber));
            }

            for (var i = 0; i < data.Count; i++)
            {
                var actualData = data[i].HexToByteArray().Take(digits[i]).ToArray();
                var revertData = Encoding.UTF8.GetString(actualData);
                Logger.Info(revertData);
            }
        }

        [TestMethod]
        public void CheckRawReport()
        {
            var roundId = 1;
            var getRawReport = GetRawReport(roundId);
            var report = GetReport(roundId);
            var generateRawReport = GenerateRawReport(report);
            getRawReport.ShouldBe(generateRawReport);
        }

        [TestMethod]
        public void GetMerklePath()
        {
            var ethStr = "3d0c16a4c08f96a433d64cfbf0ab628a212a0c30817013d5f955f91ca191f017";
            var round = 7;
            var reportInfo = GetReport(round);
            var str = reportInfo.AggregatedData;
            var exceptRoot = Hash.LoadFromByteArray(str.ToByteArray());
            ethStr.ShouldBe(exceptRoot.ToHex());

            var indexNode = GetInfo().OffChainQueryInfoList.Value.ToList();
            for (var i = 0; i < indexNode.Count; i++)
            {
                if (indexNode[i].Title.Equals("invalid")) continue;
                var data = reportInfo.Observations.Value.FirstOrDefault(o => o.Key == i.ToString()).Data;
                Logger.Info($"{i} ==> {data}");
                var result =
                    _reportContract.CallViewMethod<MerklePath>(ReportMethod.GetMerklePath, new GetMerklePathInput
                    {
                        NodeIndex = i,
                        RoundId = round,
                        Token = eth
                    });
                var hash = HashHelper.ComputeFrom(data);
                var root = result.ComputeRootWithLeafNode(hash);
                root.ShouldBe(exceptRoot);
            }
        }

        #region Obuserver

        [TestMethod]
        public void ApplyObserver()
        {
            foreach (var member in _associationMember)
            {
                // var member = _associationMember[0];
                var isObserver =
                    _reportContract.CallViewMethod<BoolValue>(ReportMethod.IsObserver, Address.FromBase58(member));
                if (isObserver.Value) continue;
                _tokenContract.TransferBalance(InitAccount, member, 1000_00000000, "ELF");
                var mortgageAmount = _reportContract.CallViewMethod<Int64Value>(ReportMethod.GetMortgagedTokenAmount,
                    Address.FromBase58(member));
                if (_applyObserverFee > 0)
                {
                    _tokenContract.IssueBalance(InitAccount, member, _applyObserverFee, Symbol);
                    _tokenContract.ApproveToken(member, _reportContract.ContractAddress, _applyObserverFee, Symbol);
                }

                var balance = _tokenContract.GetUserBalance(member, Symbol);
                _reportContract.SetAccount(member);
                var result = _reportContract.ExecuteMethodWithResult(ReportMethod.ApplyObserver, new Empty());
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                var afterMortgageAmount = _reportContract.CallViewMethod<Int64Value>(
                    ReportMethod.GetMortgagedTokenAmount,
                    Address.FromBase58(member));
                afterMortgageAmount.Value.ShouldBe(mortgageAmount.Value >= _applyObserverFee
                    ? mortgageAmount.Value
                    : _applyObserverFee);

                isObserver =
                    _reportContract.CallViewMethod<BoolValue>(ReportMethod.IsObserver, Address.FromBase58(member));
                isObserver.Value.ShouldBeTrue();
                var afterBalance = _tokenContract.GetUserBalance(member, Symbol);
                afterBalance.ShouldBe(mortgageAmount.Value >= _applyObserverFee
                    ? balance
                    : balance - (_applyObserverFee - mortgageAmount.Value));
                Logger.Info($"{member}: balance ==> {afterBalance}\n mortgage ==> {afterMortgageAmount.Value}");
            }
        }

        [TestMethod]
        public void QuitObserver()
        {
            // foreach (var member in _associationMember)
            // {
            // var isObserver =
            //     _reportContract.CallViewMethod<BoolValue>(ReportMethod.IsObserver, Address.FromBase58(member));
            // if (!isObserver.Value) continue;
            var member = _associationMember[0];

            var mortgageAmount = _reportContract.CallViewMethod<Int64Value>(ReportMethod.GetMortgagedTokenAmount,
                Address.FromBase58(member));

            _reportContract.SetAccount(member);
            var balance = _tokenContract.GetUserBalance(member, Symbol);
            var result = _reportContract.ExecuteMethodWithResult(ReportMethod.QuitObserver, new Empty());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var afterMortgageAmount = _reportContract.CallViewMethod<Int64Value>(
                ReportMethod.GetMortgagedTokenAmount,
                Address.FromBase58(member));
            afterMortgageAmount.Value.ShouldBe(0);

            var afterBalance = _tokenContract.GetUserBalance(member, Symbol);
            afterBalance.ShouldBe(balance + mortgageAmount.Value);

            var isObserver =
                _reportContract.CallViewMethod<BoolValue>(ReportMethod.IsObserver, Address.FromBase58(member));
            isObserver.Value.ShouldBeFalse();
            // }
        }

        [TestMethod]
        public void MortgageTokens()
        {
            var amount = _applyObserverFee;
            foreach (var member in _associationMember)
            {
                var balance = _tokenContract.GetUserBalance(member, Symbol);
                _tokenContract.TransferBalance(InitAccount, member, 100000000000);
                var mortgageAmount = _reportContract.CallViewMethod<Int64Value>(ReportMethod.GetMortgagedTokenAmount,
                    Address.FromBase58(member));
                if (balance < amount)
                {
                    _tokenContract.IssueBalance(InitAccount, member, amount, Symbol);
                    balance = _tokenContract.GetUserBalance(member, Symbol);
                }

                _tokenContract.ApproveToken(member, _reportContract.ContractAddress, amount, Symbol);
                _reportContract.SetAccount(member);
                var result =
                    _reportContract.ExecuteMethodWithResult(ReportMethod.MortgageTokens,
                        new Int64Value {Value = amount});
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var afterBalance = _tokenContract.GetUserBalance(member, Symbol);
                afterBalance.ShouldBe(balance - amount);

                var afterMortgageAmount = _reportContract.CallViewMethod<Int64Value>(
                    ReportMethod.GetMortgagedTokenAmount,
                    Address.FromBase58(member));
                afterMortgageAmount.Value.ShouldBe(mortgageAmount.Value + amount);
            }
        }

        [TestMethod]
        public void WithdrawTokens()
        {
            var amount = 1000000000;
            foreach (var member in _associationMember)
            {
                // var member = _associationMember[1];
                var balance = _tokenContract.GetUserBalance(member, Symbol);
                var mortgageAmount = _reportContract.CallViewMethod<Int64Value>(ReportMethod.GetMortgagedTokenAmount,
                    Address.FromBase58(member));
                _reportContract.SetAccount(member);
                var result =
                    _reportContract.ExecuteMethodWithResult(ReportMethod.WithdrawTokens,
                        new Int64Value {Value = mortgageAmount.Value / 2});
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                var afterBalance = _tokenContract.GetUserBalance(member, Symbol);
                afterBalance.ShouldBe(balance + mortgageAmount.Value / 2);
                var afterMortgageAmount = _reportContract.CallViewMethod<Int64Value>(
                    ReportMethod.GetMortgagedTokenAmount,
                    Address.FromBase58(member));
                afterMortgageAmount.Value.ShouldBe(mortgageAmount.Value / 2);
            }
        }

        [TestMethod]
        public void AdjustApplyObserverFee()
        {
            var result = AuthorityManager.ExecuteTransactionWithAuthority(_reportContract.ContractAddress,
                nameof(ReportMethod.AdjustApplyObserverFee), new Int64Value {Value = 1000000000}, InitAccount);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        #endregion

        #region failed test

        [TestMethod]
        public void RegisterOffChainAggregation_Failed()
        {
            {
                var result = _reportContract.ExecuteMethodWithResult(ReportMethod.RegisterOffChainAggregation,
                    new RegisterOffChainAggregationInput
                    {
                        OffChainQueryInfoList = new OffChainQueryInfoList
                        {
                        },
                        Token = eth,
                        AggregateThreshold = 1,
                        AggregatorContractAddress = _integerAggregator,
                        ConfigDigest = ByteStringHelper.FromHexString(digestStr),
                    });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                result.Error.ShouldContain("At least 1 off-chain info.");
            }
            {
                var result = _reportContract.ExecuteMethodWithResult(ReportMethod.RegisterOffChainAggregation,
                    new RegisterOffChainAggregationInput
                    {
                        OffChainQueryInfoList = new OffChainQueryInfoList
                        {
                            Value =
                            {
                                new OffChainQueryInfo
                                {
                                    Title = "https://api.coincap.io/v2/assets/bitcoin",
                                    Options =
                                    {
                                        "priceUsd"
                                    }
                                },
                                new OffChainQueryInfo
                                {
                                    Title = "https://api.coincap.io/v2/assets/bitcoin",
                                    Options =
                                    {
                                        "priceUsd"
                                    }
                                }
                            }
                        },
                        Token = eth,
                        AggregateThreshold = 1,
                        ConfigDigest = ByteStringHelper.FromHexString(digestStr),
                    });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                result.Error.ShouldContain("Merkle tree style aggregator must set aggregator contract address.");
            }
        }

        #endregion

        #region private

        private void InitializeAndCreateToken()
        {
            var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.InitializeAndCreateToken,
                new InitializeInput
                {
                    MinimumOracleNodesCount = 3,
                    DefaultAggregateThreshold = 1,
                    DefaultRevealThreshold = 1,
                    DefaultExpirationSeconds = 600
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            ChangeTokenIssuer();

            var oracleNodeThreshold =
                _oracleContract.CallViewMethod<OracleNodeThreshold>(OracleMethod.GetThreshold, new Empty());
            oracleNodeThreshold.DefaultAggregateThreshold.ShouldBe(1);
            oracleNodeThreshold.MinimumOracleNodesCount.ShouldBe(3);
            oracleNodeThreshold.DefaultRevealThreshold.ShouldBe(1);

            var controller = _oracleContract.CallViewMethod<Address>(OracleMethod.GetController, new Empty());
            controller.ShouldBe(InitAccount.ConvertAddress());
            var tokenSymbol =
                _oracleContract.CallViewMethod<StringValue>(OracleMethod.GetOracleTokenSymbol, new Empty());
            tokenSymbol.Value.ShouldBe(Symbol);
        }

        private void ChangeTokenIssuer()
        {
            var tokenInfo = _tokenContract.GetTokenInfo(Symbol);
            tokenInfo.ShouldNotBe(new TokenInfo());
            tokenInfo.Issuer.ShouldBe(_defaultParliamentOrganization);

            //change issuer to sender
            var res = AuthorityManager.ExecuteTransactionWithAuthority(_tokenContract.ContractAddress,
                nameof(TokenMethod.ChangeTokenIssuer),
                new ChangeTokenIssuerInput {Symbol = Symbol, NewTokenIssuer = InitAccount.ConvertAddress()},
                InitAccount);
            res.Status.ShouldBe(TransactionResultStatus.Mined);

            tokenInfo = _tokenContract.GetTokenInfo(Symbol);
            tokenInfo.Issuer.ShouldBe(InitAccount.ConvertAddress());
        }

        private StringValue GenerateRawReport(Report report)
        {
            var ethReport = _reportContract.CallViewMethod<StringValue>(ReportMethod.GenerateRawReport,
                new GenerateRawReportInput
                {
                    ConfigDigest = ByteStringHelper.FromHexString(digestStr),
                    Organization = _association1.ConvertAddress(),
                    Report = report
                });
            return ethReport;
        }

        private StringValue GetRawReport(long roundId)
        {
            return _reportContract.CallViewMethod<StringValue>(ReportMethod.GetRawReport,
                new GetRawReportInput
                {
                    RoundId = roundId,
                    Token = eth
                });
        }

        private long GetCurrentRound()
        {
            var round = _reportContract.CallViewMethod<Int64Value>(ReportMethod.GetCurrentRoundId,
                new StringValue {Value = eth});
            return round.Value;
        }

        private Report GetReport(long roundId)
        {
            var reportInfo = _reportContract.CallViewMethod<Report>(ReportMethod.GetReport,
                new GetReportInput
                {
                    RoundId = roundId,
                    Token = eth
                });
            return reportInfo;
        }


        private OffChainAggregationInfo GetInfo()
        {
            var info = _reportContract.CallViewMethod<OffChainAggregationInfo>(ReportMethod.GetOffChainAggregationInfo,
                new StringValue {Value = eth});
            return info;
        }

        private void InitializeTestContract()
        {
            _oracleUserContract.ExecuteMethodWithResult(OracleUserMethod.Initialize, _oracleContract.Contract);
        }
        
        #endregion
    }
}