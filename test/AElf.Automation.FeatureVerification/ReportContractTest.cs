using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Oracle;
using AElf.Contracts.Report;
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
using InitializeInput = AElf.Contracts.Oracle.InitializeInput;
using Org.BouncyCastle.Crypto.Digests;

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
        private string _oracleUserContractAddress = "2RHf2fxsnEaM3wb6N1yGqPupNZbcCY98LgWbGSFWmWzgEs5Sjo";
        private string _integerAggregatorAddress = "2NxwCPAGJr4knVdmwhb1cK7CkZw5sMJkRDLnT7E2GoDP2dy5iZ";

        //6Yu5KJprje1EKf78MicuoAL3VsK3DoNoGm1ah1dUR5Y7frPdE
        //7ePBoE6V98bzWGBfK7pvupAJ4sveytJsZCaq2gaFuW1PSdVBv
        //2gin3EoK8YvfeNfPjWyuKRaa3GEjF1KY1ZpBQCgPF3mHnQmDAX
        private Address _defaultParliamentOrganization;
        private string _association1 = "6Yu5KJprje1EKf78MicuoAL3VsK3DoNoGm1ah1dUR5Y7frPdE";
        private string _association2 = "7ePBoE6V98bzWGBfK7pvupAJ4sveytJsZCaq2gaFuW1PSdVBv";
        private string _association3 = "2gin3EoK8YvfeNfPjWyuKRaa3GEjF1KY1ZpBQCgPF3mHnQmDAX";
        private string Password { get; } = "12345678";
        private static string RpcUrl { get; } = "127.0.0.1:8000";
        private string Symbol { get; } = "PORT";
        private readonly bool isNeedInitialize = false;
        private long payAmount = 100000000;
        private long _applyObserverFee = 100000000;
        private long _defaultReportFee = 100000000;
        private string eth = "0x483cd9d0bedca44a8724f98a7915f0e04dbc1a55";
        private string digestStr = "0x6aac9e49f09712cec6175c5335682308";

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
            _oracleUserContract = _oracleUserContractAddress == ""
                ? new OracleUserContract(NodeManager, InitAccount)
                : new OracleUserContract(NodeManager, InitAccount, _oracleUserContractAddress);
            _integerAggregator = _integerAggregatorAddress == ""
                ? AuthorityManager.DeployContractWithAuthority(InitAccount, "AElf.Contracts.IntegerAggregator")
                : Address.FromBase58(_integerAggregatorAddress);

            _association1 = _association1 == ""
                ? AuthorityManager.CreateAssociationOrganization(_associationMember).ToBase58()
                : _association1;
            _association2 = _association2 == ""
                ? AuthorityManager.CreateAssociationOrganization(new List<string> {InitAccount, TestAccount, OtherNode})
                    .ToBase58()
                : _association2;
            _association3 = _association3 == ""
                ? AuthorityManager.CreateAssociationOrganization(new List<string>
                        {_associationMember[0], _associationMember[1], OtherNode})
                    .ToBase58()
                : _association3;
            Logger.Info($"{_association1},{_association2},{_association3}");
            if (!isNeedInitialize) return;
            InitializeAndCreateToken();
            InitializeReportTest();
            InitializeTestContract();
        }

        [TestMethod]
        public void InitializeReportTest()
        {
            var result = _reportContract.ExecuteMethodWithResult(ReportMethod.Initialize,
                new AElf.Contracts.Report.InitializeInput()
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

        [TestMethod]
        public void RegisterOffChainAggregation()
        {
            var result = _reportContract.ExecuteMethodWithResult(ReportMethod.RegisterOffChainAggregation,
                new RegisterOffChainAggregationInput
                {
                    ObserverList = new ObserverList {Value = {_association1.ConvertAddress()}},

                    OffChainQueryInfoList = new OffChainQueryInfoList
                    {
                        Value =
                        {
                            new OffChainQueryInfo
                            {
                                UrlToQuery = "https://api.coincap.io/v2/assets/bitcoin",
                                AttributesToFetch =
                                {
                                    "data/priceUsd"
                                }
                            },
                            new OffChainQueryInfo
                            {
                                UrlToQuery = "https://api.coincap.io/v2/assets/aelf",
                                AttributesToFetch =
                                {
                                    "data/priceUsd"
                                }
                            },
                            new OffChainQueryInfo
                            {
                                UrlToQuery = "https://api.coincap.io/v2/assets/ethereum",
                                AttributesToFetch =
                                {
                                    "data/priceUsd"
                                }
                            }
                            // new OffChainQueryInfo
                            // {
                            //     UrlToQuery = "http://localhost:7080/price/elf",
                            //     AttributesToFetch =
                            //     {
                            //         "price"
                            //     }
                            // },
                            // new OffChainQueryInfo
                            // {
                            //     UrlToQuery = "http://localhost:7080/price/btc",
                            //     AttributesToFetch =
                            //     {
                            //         "price"
                            //     }
                            // }
                        }
                    },
                    EthereumContractAddress = eth,
                    AggregateThreshold = 1,
                    AggregatorContractAddress = _integerAggregator,
                    ConfigDigest = ByteStringHelper.FromHexString(digestStr),
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var log = result.Logs.First(l => l.Name.Equals(nameof(OffChainAggregationRegistered))).NonIndexed;
            var info = OffChainAggregationRegistered.Parser.ParseFrom(ByteString.FromBase64(log));
            info.AggregatorContractAddress.ShouldBe(_integerAggregator);
        }

        [TestMethod]
        public void AddRegisterWhiteList()
        {
            var result = AuthorityManager.ExecuteTransactionWithAuthority(_reportContract.ContractAddress,
                nameof(ReportMethod.AddRegisterWhiteList), InitAccount.ConvertAddress(), InitAccount);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        
        private void QueryOracle(int index)
        {
            var allowance = _tokenContract.GetAllowance(InitAccount, _reportContract.ContractAddress, Symbol);
            Logger.Info(allowance);
            if (allowance < payAmount + _defaultReportFee)
                _tokenContract.ApproveToken(InitAccount, _reportContract.ContractAddress, payAmount + _defaultReportFee,
                    Symbol);
            _tokenContract.IssueBalance(InitAccount, InitAccount, payAmount + _defaultReportFee, Symbol);
            var senderBalance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            var reportBalance = _tokenContract.GetUserBalance(_reportContract.ContractAddress, Symbol);
            var result = _reportContract.ExecuteMethodWithResult(ReportMethod.QueryOracle, new QueryOracleInput
            {
                AggregateThreshold = 1,
                AggregatorContractAddress = _integerAggregator,
                Payment = payAmount,
                EthereumContractAddress = eth,
                NodeIndex = index
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var byteString = result.Logs.First(l => l.Name.Contains(nameof(QueryCreated))).NonIndexed;
            var query = QueryCreated.Parser.ParseFrom(ByteString.FromBase64(byteString));
            Logger.Info(query.QueryId.ToHex());
            
            var afterSenderBalance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            var afterReportBalance = _tokenContract.GetUserBalance(_reportContract.ContractAddress, Symbol);
            afterSenderBalance.ShouldBe(senderBalance - (payAmount + _defaultReportFee));
            afterReportBalance.ShouldBe(reportBalance + _defaultReportFee);
        }
        
        [TestMethod]
        public void Query()
        {
            var indexNode = 3;
            for (var i = 0; i < indexNode; i++)
            {
                QueryOracle(i);
            }
        }

        //0cd7984799c89efb287a12b7343aadd08096156182ebfe219a5f7b9f5990c226
        //bedfb96d80bb65a9c6412877792fb93ffe2f8901022c3dfc5427ad966e80c088
        //f095efa96ca5fd592e38ef7f9577f4302181b491c2f0b162bf093a403db6b074
        [TestMethod]
        public void CancelQueryReport()
        {
            var id = "35650108d68053ab8604210f2fc51281a6b17f06c2a366e571688d2f79b91fd9";
            var balance = _tokenContract.GetUserBalance(_reportContract.ContractAddress, Symbol);
            var userBalance = _tokenContract.GetUserBalance(_reportContract.CallAddress, Symbol);
            var result = _reportContract.ExecuteMethodWithResult(ReportMethod.CancelQueryOracle, Hash.LoadFromHex(id));
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var afterBalance = _tokenContract.GetUserBalance(_reportContract.ContractAddress, Symbol);
            var afterUserBalance = _tokenContract.GetUserBalance(_reportContract.CallAddress, Symbol);
            afterBalance.ShouldBe(balance - payAmount);
            afterUserBalance.ShouldBe(userBalance + payAmount);
        }

        [TestMethod]
        public void ConfirmReport()
        {
            _reportContract.SetAccount(_associationMember[0]);
            var result = _reportContract.ExecuteMethodWithResult(ReportMethod.ConfirmReport, new ConfirmReportInput
            {
                Signature =
                    "",
                EthereumContractAddress = eth,
                RoundId = 1
            });
        }

        [TestMethod]
        public void GetReportQueryRecord()
        {
        }

        [TestMethod]
        public void GetSignatureMap()
        {
            var result =
                _reportContract.CallViewMethod<SignatureMap>(ReportMethod.GetSignatureMap, new GetSignatureMapInput
                {
                    RoundId = 1,
                    EthereumContractAddress = eth
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
                        RoundId = 3,
                        EthereumContractAddress = eth
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
                var address = GenerateAddressOnEthereum(NodeManager.GetAccountPublicKey(member));
                Logger.Info($"{member} ==> {address} \n");
            }
        }

        [TestMethod]
        public void GetData()
        {
            var data = "0x0a05342e313732";
            var revertData = StringValue.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(data));
            Logger.Info(revertData);
        }
        
        [TestMethod]
        public void GetData_MerkleTreeType()
        {
            var hash = "0xc954ed0830c7cdbe9ab9ca4c86d901a0a600be9c2754e56d48ee07c8696412ef";
            var index = "0x181417";
            var data = new List<string>
            {
                "0x0a1635373233372e343233303439393031313131333639330000000000000000",
                "0x0a12302e34353035373439313636383738353436000000000000000000000000",
                "0x0a15343331382e35373032333230353137393339343137000000000000000000"
            };
            var digits = new List<int>();
            var length = index.Length;
            var count = length / 2 - 1;
            for (var i = 0; i < count; i++)
            {
                var b= index.Substring(2*(i+1), 2);
                digits.Add(Int32.Parse(b, System.Globalization.NumberStyles.HexNumber));
            }
            
            for (var i = 0; i < data.Count; i++)
            {
                var actualData = data[i].HexToByteArray().Take(digits[i]).ToArray();
                var revertData = StringValue.Parser.ParseFrom(actualData);
                Logger.Info(revertData);
            }
        }

        [TestMethod]
        public void GetEthererumReport()
        {
        }

        [TestMethod]
        public void GenerateEthererumReport()
        {
        }

        [TestMethod]
        public void GetMerklePath()
        {
            var exceptRoot = "0xc954ed0830c7cdbe9ab9ca4c86d901a0a600be9c2754e56d48ee07c8696412ef";
            var round = GetCurrentRound() - 1;
            var indexNode = GetInfo().OffChainQueryInfoList.Value.Count;
            var reportInfo = GetReport(round);
            var nodeList = new List<Hash>();
            for (var i = 0; i < indexNode; i++)
            {
                var result =
                    _reportContract.CallViewMethod<MerklePath>(ReportMethod.GetMerklePath, new GetMerklePathInput
                    {
                        NodeIndex = i,
                        RoundId = round,
                        EthereumContractAddress = eth
                    });
                var data = reportInfo.Observations.Value[i].Data;
                var stringValue = StringValue.Parser.ParseFrom(data);
                var hash = HashHelper.ComputeFrom(stringValue.ToByteArray());
                var root = result.ComputeRootWithLeafNode(hash);
                root.ShouldBe(Hash.LoadFromHex(exceptRoot));
            }
        }

        #region Obuserver

        [TestMethod]
        public void ApplyObserver()
        {
            foreach (var association in _associationMember)
            {
                _tokenContract.IssueBalance(InitAccount, association, 2000_00000000, Symbol);
                _tokenContract.TransferBalance(InitAccount, association, 1000_00000000, "ELF");
                var balance = _tokenContract.GetUserBalance(association, Symbol);
                _reportContract.SetAccount(association);
                _tokenContract.ApproveToken(association, _reportContract.ContractAddress, _applyObserverFee, Symbol);
                var result = _reportContract.ExecuteMethodWithResult(ReportMethod.ApplyObserver, new Empty());
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                var afterBalance = _tokenContract.GetUserBalance(association, Symbol);
                afterBalance.ShouldBe(balance - _applyObserverFee);
            }
        }

        [TestMethod]
        public void QuitObserver()
        {
            foreach (var association in _associationMember)
            {
                _reportContract.SetAccount(association);
                var balance = _tokenContract.GetUserBalance(association, Symbol);
                var result = _reportContract.ExecuteMethodWithResult(ReportMethod.QuitObserver, new Empty());
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var afterBalance = _tokenContract.GetUserBalance(association, Symbol);
                afterBalance.ShouldBe(balance + _applyObserverFee);
            }
        }

        [TestMethod]
        public void MortgageTokens()
        {
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
                        ObserverList = new ObserverList {Value = {_association1.ConvertAddress()}},

                        OffChainQueryInfoList = new OffChainQueryInfoList
                        {
                        },
                        EthereumContractAddress = eth,
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
                        ObserverList = new ObserverList {Value = {_association1.ConvertAddress()}},

                        OffChainQueryInfoList = new OffChainQueryInfoList
                        {
                            Value =
                            {
                                new OffChainQueryInfo
                                {
                                    UrlToQuery = "https://api.coincap.io/v2/assets/bitcoin",
                                    AttributesToFetch =
                                    {
                                        "priceUsd"
                                    }
                                },
                                new OffChainQueryInfo
                                {
                                    UrlToQuery = "https://api.coincap.io/v2/assets/bitcoin",
                                    AttributesToFetch =
                                    {
                                        "priceUsd"
                                    }
                                }
                            }
                        },
                        EthereumContractAddress = eth,
                        AggregateThreshold = 1,
                        ConfigDigest = ByteStringHelper.FromHexString(digestStr),
                    });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                result.Error.ShouldContain("Merkle tree style aggregator must set aggregator contract address.");
            }
            {
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

        private void InitializeTestContract()
        {
            _oracleUserContract.ExecuteMethodWithResult(OracleUserMethod.Initialize, _oracleContract.Contract);
        }

        private void GeneratedEthReport(Report report)
        {
            var ethReport = _reportContract.CallViewMethod<StringValue>(ReportMethod.GenerateEthererumReport,
                new GenerateEthererumReportInput
                {
                    ConfigDigest = ByteStringHelper.FromHexString(digestStr),
                    Organization = _association1.ConvertAddress(),
                    Report = report
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
                    EthereumContractAddress = eth
                });
            return reportInfo;
        }

        private OffChainAggregationInfo GetInfo()
        {
            var info = _reportContract.CallViewMethod<OffChainAggregationInfo>(ReportMethod.GetOffChainAggregationInfo,
                new StringValue{Value = eth});
            return info;
        }

        private string GenerateAddressOnEthereum(string publicKey)
        {
            if (publicKey.StartsWith("0x"))
            {
                publicKey = publicKey.Substring(2, publicKey.Length - 2);
            }

            publicKey = publicKey.Substring(2, publicKey.Length - 2);
            publicKey = GetKeccak256(publicKey);
            var address = "0x" + publicKey.Substring(publicKey.Length - 40, 40);
            return address;
        }

        private static string GetKeccak256(string hexMsg)
        {
            var offset = hexMsg.StartsWith("0x") ? 2 : 0;

            var txByte = Enumerable.Range(offset, hexMsg.Length - offset)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hexMsg.Substring(x, 2), 16))
                .ToArray();

            //Note: Not intended for intensive use so we create a new Digest.
            //if digest reuse, prevent concurrent access + call Reset before BlockUpdate
            var digest = new KeccakDigest(256);

            digest.BlockUpdate(txByte, 0, txByte.Length);
            var calculatedHash = new byte[digest.GetByteLength()];
            digest.DoFinal(calculatedHash, 0);

            var transactionHash = BitConverter.ToString(calculatedHash, 0, 32).Replace("-", "").ToLower();

            return transactionHash;
        }

        #endregion
    }
}