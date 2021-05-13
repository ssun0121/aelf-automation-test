using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Client.Dto;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Oracle;
using AElf.Contracts.OracleUser;
using AElf.CSharp.Core;
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

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class OracleContractTest
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private INodeManager MainNodeManager { get; set; }
        private GenesisContract _mainGenesisContract;
        private TokenContract _mainTokenContract;

        private AuthorityManager AuthorityManager { get; set; }

        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;
        private ParliamentContract _parliamentContract;
        private AssociationContract _associationContract;

        private OracleContract _oracleContract;
        private OracleUserContract _oracleUserContract;
        private Address _integerAggregator;
        private OracleContractContainer.OracleContractStub _oracle;

        private string TestAccount { get; } = "2RehEQSpXeZ5DUzkjTyhAkr9csu7fWgE5DAuB2RaKQCpdhB8zC";
        private string InitAccount { get; } = "ZrAFaqdr79MWYkxA49Hp2LUdSVHdP6fJh3kDw4jmgC7HTgrni";
        private string OtherNode { get; } = "sjzNpr5bku3ZyvMqQrXeBkXGEvG2CTLA2cuNDfcDMaPTTAqEy";

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
        
        //RXcxgSXuagn8RrvhQAV81Z652EEYSwR6JLnqHYJ5UVpEptW8Y
        //2wRDbyVF28VBQoSPgdSEFaL4x7CaXz8TCBujYhgWc9qTMxBE3n
        //2AsEepqiFCRDnepVheYYN5LK7nvM2kUoXgk2zLKu1Zneh8fwmF
        
        //2F5McxHg7fAqVjDX97v79j4drsMq442rArChpBii8TWuRb8ZnK
        private string _oracleContractAddress = "2YkKkNZKCcsfUsGwCfJ6wyTx5NYLgpCg1stBuRT4z5ep3psXNG";
        private string _oracleUserContractAddress = "2wRDbyVF28VBQoSPgdSEFaL4x7CaXz8TCBujYhgWc9qTMxBE3n";
        private string _integerAggregatorAddress = "2AsEepqiFCRDnepVheYYN5LK7nvM2kUoXgk2zLKu1Zneh8fwmF";

        //6Yu5KJprje1EKf78MicuoAL3VsK3DoNoGm1ah1dUR5Y7frPdE
        //7ePBoE6V98bzWGBfK7pvupAJ4sveytJsZCaq2gaFuW1PSdVBv
        //2gin3EoK8YvfeNfPjWyuKRaa3GEjF1KY1ZpBQCgPF3mHnQmDAX
        private Address _defaultParliamentOrganization;
        private string _association1 = "";
        private string _association2 = "";
        private string _association3 = "";
        private string Password { get; } = "12345678";
        private static string RpcUrl { get; } = "13.212.233.221:8000";
        private static string MainRpcUrl { get; } = "18.166.154.80:8000";
        private string Symbol { get; } = "PORT";
        private readonly bool isNeedInitialize = false;
        private readonly bool isNeedCreateAssociation = false;

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("OracleContactTest");
            Logger = Log4NetHelper.GetLogger();
            NodeManager = new NodeManager(RpcUrl);
            MainNodeManager = new NodeManager(MainRpcUrl);
            Logger.Info(MainRpcUrl,RpcUrl);

            NodeInfoHelper.SetConfig("nodes-online-test-main");
            _mainGenesisContract = GenesisContract.GetGenesisContract(MainNodeManager, InitAccount, Password);
            _mainTokenContract = _mainGenesisContract.GetTokenContract(InitAccount, Password);
            AuthorityManager = new AuthorityManager(NodeManager, InitAccount, Password);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount, Password);
            _parliamentContract = _genesisContract.GetParliamentContract(InitAccount, Password);
            _associationContract = _genesisContract.GetAssociationAuthContract(InitAccount, Password);
            _defaultParliamentOrganization = _parliamentContract.GetGenesisOwnerAddress();
            _tokenContract = _genesisContract.GetTokenContract(InitAccount, Password);
            // CreateToken();

            _oracleContract = _oracleContractAddress == ""
                ? new OracleContract(NodeManager, InitAccount)
                : new OracleContract(NodeManager, InitAccount, _oracleContractAddress);
            _oracleUserContract = _oracleUserContractAddress == ""
                ? new OracleUserContract(NodeManager, InitAccount)
                : new OracleUserContract(NodeManager, InitAccount, _oracleUserContractAddress);
            _oracle = _oracleContract.GetTestStub<OracleContractContainer.OracleContractStub>(InitAccount);

            _integerAggregator = _integerAggregatorAddress == ""
                ? AuthorityManager.DeployContractWithAuthority(InitAccount, "AElf.Contracts.IntegerAggregator")
                : Address.FromBase58(_integerAggregatorAddress);

            if (isNeedCreateAssociation)
            {
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
            }

            if (!isNeedInitialize) return;
            InitializeTestContract();
            // InitializeOracleTest();
        }

        [TestMethod]
        public void InitializeOracleTest()
        {
            var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.Initialize, new InitializeInput
            {
                MinimumOracleNodesCount = 5,
                DefaultAggregateThreshold = 3,
                DefaultRevealThreshold = 3,
                DefaultExpirationSeconds = 600,
                IsChargeFee = false
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            // ChangeTokenIssuer();

            var oracleNodeThreshold =
                _oracleContract.CallViewMethod<OracleNodeThreshold>(OracleMethod.GetThreshold, new Empty());
            oracleNodeThreshold.DefaultAggregateThreshold.ShouldBe(3);
            oracleNodeThreshold.MinimumOracleNodesCount.ShouldBe(5);
            oracleNodeThreshold.DefaultRevealThreshold.ShouldBe(3);

            var controller = _oracleContract.CallViewMethod<Address>(OracleMethod.GetController, new Empty());
            controller.ShouldBe(InitAccount.ConvertAddress());
            var tokenSymbol =
                _oracleContract.CallViewMethod<StringValue>(OracleMethod.GetOracleTokenSymbol, new Empty());
            tokenSymbol.Value.ShouldBe(Symbol);
        }

        [TestMethod]
        public void QueryWithCommitAndReveal()
        {
            var payAmount = 0;
            var allowance = _tokenContract.GetAllowance(InitAccount, _oracleContract.ContractAddress, Symbol);
            Logger.Info(allowance);
            if (allowance < payAmount)
                _tokenContract.ApproveToken(InitAccount, _oracleContract.ContractAddress, payAmount, Symbol);
            _tokenContract.IssueBalance(InitAccount, InitAccount, payAmount, Symbol);
            var nodes = _associationContract.GetOrganization(_association1.ConvertAddress()).OrganizationMemberList
                .OrganizationMembers.Count;

            var balance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            var result = ExecutedQuery(payAmount, _integerAggregator, new QueryInfo
            {
                UrlToQuery = "https://api.coincap.io/v2/assets/aelf",
                AttributesToFetch =
                {
                    "data/priceUsd"
                }
            }, new AddressList
            {
                Value = {_association1.ConvertAddress()}
            }, new CallbackInfo
            {
                ContractAddress = _oracleUserContract.Contract,
                MethodName = "RecordPrice"
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            afterBalance.ShouldBe(balance - payAmount);
            var byteString = result.Logs.First(l => l.Name.Contains(nameof(QueryCreated))).NonIndexed;
            var query = QueryCreated.Parser.ParseFrom(ByteString.FromBase64(byteString));
            query.Payment.ShouldBe(payAmount);
            query.Token.ShouldBe("Test");
            query.QuerySender.ShouldBe(InitAccount.ConvertAddress());
            query.AggregatorContractAddress.ShouldBe(_integerAggregator);
            // query.AggregateThreshold.ShouldBe(Math.Max(nodes.Div(3).Add(1),1));
            Logger.Info(query.QueryId.ToHex());

            var getQuery = GetQueryRecord(query.QueryId.ToHex());
            getQuery.Payment.ShouldBe(query.Payment);
            getQuery.Token.ShouldBe(query.Token);
            getQuery.AggregateThreshold.ShouldBe(Math.Max(nodes.Div(3).Add(1), 1));
            getQuery.CallbackInfo.ShouldBe(query.CallbackInfo);
            getQuery.QueryInfo.ShouldBe(query.QueryInfo);
            getQuery.IsCancelled.ShouldBeFalse();
            getQuery.AggregatorContractAddress.ShouldBe(query.AggregatorContractAddress);
            getQuery.QuerySender.ShouldBe(query.QuerySender);
            getQuery.IsCommitStageFinished.ShouldBeFalse();
            getQuery.IsSufficientDataCollected.ShouldBeFalse();
            getQuery.IsSufficientCommitmentsCollected.ShouldBeFalse();
            //wait node to push message
            Thread.Sleep(130000);
            var finalRecord = GetQueryRecord(query.QueryId.ToHex());
            finalRecord.IsCommitStageFinished.ShouldBeTrue();
            finalRecord.IsSufficientDataCollected.ShouldBeTrue();
            finalRecord.IsSufficientCommitmentsCollected.ShouldBeTrue();
            CheckRevel();
            CheckNodeBalance();
        }
        
        [TestMethod]
        public void QueryWithCommitAndReveal_Parliament()
        {
            var payAmount = 0;
            var allowance = _tokenContract.GetAllowance(InitAccount, _oracleContract.ContractAddress, Symbol);
            Logger.Info(allowance);
            if (allowance < payAmount)
                _tokenContract.ApproveToken(InitAccount, _oracleContract.ContractAddress, payAmount, Symbol);
            // _tokenContract.IssueBalance(InitAccount, InitAccount, payAmount, Symbol);
            var nodes = AuthorityManager.GetCurrentMiners().Count;
            var balance = _tokenContract.GetUserBalance(InitAccount, Symbol);

            var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.Query, new QueryInput
            {
                Payment = payAmount,
                AggregateThreshold = 1,
                AggregatorContractAddress = _integerAggregator,
                QueryInfo = new QueryInfo
                {
                    UrlToQuery = "https://api.coincap.io/v2/assets/aelf",
                    AttributesToFetch =
                    {
                        "data/priceUsd"
                    }
                },
                DesignatedNodeList = new AddressList
                {
                    Value = {_parliamentContract.Contract}
                },
                Token = "Test"
            });
                
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            afterBalance.ShouldBe(balance - payAmount);
            var byteString = result.Logs.First(l => l.Name.Contains(nameof(QueryCreated))).NonIndexed;
            var query = QueryCreated.Parser.ParseFrom(ByteString.FromBase64(byteString));
            var oracleNodeThreshold =
                _oracleContract.CallViewMethod<OracleNodeThreshold>(OracleMethod.GetThreshold, new Empty());
            var setAggregateThreshold = oracleNodeThreshold.DefaultAggregateThreshold;
            query.Payment.ShouldBe(payAmount);
            // query.Token.ShouldBe("Test");
            query.QuerySender.ShouldBe(InitAccount.ConvertAddress());
            query.AggregatorContractAddress.ShouldBe(_integerAggregator); 
            query.AggregateThreshold.ShouldBe(Math.Max(Math.Max(nodes.Div(3).Add(1),1),setAggregateThreshold));
            Logger.Info(query.QueryId.ToHex());

            var getQuery = GetQueryRecord(query.QueryId.ToHex());
            getQuery.Payment.ShouldBe(query.Payment);
            getQuery.Token.ShouldBe(query.Token); 
            getQuery.AggregateThreshold.ShouldBe(Math.Max(Math.Max(nodes.Div(3).Add(1), 1),setAggregateThreshold));
            getQuery.CallbackInfo.ShouldBe(query.CallbackInfo);
            getQuery.QueryInfo.ShouldBe(query.QueryInfo);
            getQuery.IsCancelled.ShouldBeFalse();
            getQuery.AggregatorContractAddress.ShouldBe(query.AggregatorContractAddress);
            getQuery.QuerySender.ShouldBe(query.QuerySender);
            getQuery.IsCommitStageFinished.ShouldBeFalse();
            getQuery.IsSufficientDataCollected.ShouldBeFalse();
            getQuery.IsSufficientCommitmentsCollected.ShouldBeFalse();
            //wait node to push message
            Thread.Sleep(30000);
            var finalRecord = GetQueryRecord(query.QueryId.ToHex());
            finalRecord.IsCommitStageFinished.ShouldBeTrue();
            finalRecord.IsSufficientDataCollected.ShouldBeTrue();
            finalRecord.IsSufficientCommitmentsCollected.ShouldBeTrue();
            var price = StringValue.Parser.ParseFrom(finalRecord.FinalResult);
            Logger.Info($"{price}");
            
            // CheckRevel();
            // CheckNodeBalance();
        }

        [TestMethod]
        public void QueryWithCommitAndReveal_OtherAssociation()
        {
            var payAmount = 100000000;
            var allowance = _tokenContract.GetAllowance(InitAccount, _oracleContract.ContractAddress, Symbol);
            Logger.Info(allowance);
            if (allowance < payAmount)
                _tokenContract.ApproveToken(InitAccount, _oracleContract.ContractAddress, payAmount, Symbol);
            _tokenContract.IssueBalance(InitAccount, InitAccount, payAmount, Symbol);
            var nodes = _associationContract.GetOrganization(_association1.ConvertAddress()).OrganizationMemberList
                .OrganizationMembers.Count;

            var balance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            var result = ExecutedQuery(payAmount, _integerAggregator, new QueryInfo
            {
                UrlToQuery = "http://localhost:7080/price/elf",
                AttributesToFetch =
                {
                    "symbol",
                    "timestamp",
                    "price"
                }
            }, new AddressList
            {
                Value = {_association3.ConvertAddress()}
            }, new CallbackInfo
            {
                ContractAddress = _oracleUserContract.Contract,
                MethodName = "RecordPrice"
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            afterBalance.ShouldBe(balance - payAmount);
            var byteString = result.Logs.First(l => l.Name.Contains(nameof(QueryCreated))).NonIndexed;
            var query = QueryCreated.Parser.ParseFrom(ByteString.FromBase64(byteString));
            query.Payment.ShouldBe(payAmount);
            query.Token.ShouldBe("Test");
            query.QuerySender.ShouldBe(InitAccount.ConvertAddress());
            query.AggregatorContractAddress.ShouldBe(_integerAggregator);
            // query.AggregateThreshold.ShouldBe(Math.Max(nodes.Div(3).Add(1),1));
            Logger.Info(query.QueryId.ToHex());

            var getQuery = GetQueryRecord(query.QueryId.ToHex());
            getQuery.Payment.ShouldBe(query.Payment);
            getQuery.Token.ShouldBe(query.Token);
            getQuery.AggregateThreshold.ShouldBe(Math.Max(nodes.Div(3).Add(1), 1));
            getQuery.CallbackInfo.ShouldBe(query.CallbackInfo);
            getQuery.QueryInfo.ShouldBe(query.QueryInfo);
            getQuery.IsCancelled.ShouldBeFalse();
            getQuery.AggregatorContractAddress.ShouldBe(query.AggregatorContractAddress);
            getQuery.QuerySender.ShouldBe(query.QuerySender);
            getQuery.IsCommitStageFinished.ShouldBeFalse();
            getQuery.IsSufficientDataCollected.ShouldBeFalse();
            getQuery.IsSufficientCommitmentsCollected.ShouldBeFalse();
        }

        [TestMethod]
        public void QueryWithoutAggregator()
        {
            var payAmount = 100000000;
            var allowance = _tokenContract.GetAllowance(InitAccount, _oracleContract.ContractAddress, Symbol);
            Logger.Info(allowance);
            if (allowance < payAmount)
                _tokenContract.ApproveToken(InitAccount, _oracleContract.ContractAddress, payAmount, Symbol);
            _tokenContract.IssueBalance(InitAccount, InitAccount, payAmount, Symbol);
            var nodes = _associationContract.GetOrganization(_association1.ConvertAddress()).OrganizationMemberList
                .OrganizationMembers.Count;

            var balance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            var result = ExecutedQuery(payAmount, null, new QueryInfo
            {
                UrlToQuery = "http://localhost:7080/price/elf",
                AttributesToFetch =
                {
                    "price"
                }
            },new AddressList
            {
                Value = {_association1.ConvertAddress()}
            }, new CallbackInfo
            {
                ContractAddress = _oracleUserContract.Contract,
                MethodName = "RecordPrice"
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            afterBalance.ShouldBe(balance - payAmount);
            var byteString = result.Logs.First(l => l.Name.Contains(nameof(QueryCreated))).NonIndexed;
            var query = QueryCreated.Parser.ParseFrom(ByteString.FromBase64(byteString));
            query.Payment.ShouldBe(payAmount);
            query.Token.ShouldBe("Test");
            query.QuerySender.ShouldBe(InitAccount.ConvertAddress());
            query.AggregatorContractAddress.ShouldBeNull();
            query.AggregateThreshold.ShouldBe(Math.Max(nodes.Div(3).Add(1), 1));
            Logger.Info(query.QueryId.ToHex());

            var getQuery = GetQueryRecord(query.QueryId.ToHex());
            getQuery.Payment.ShouldBe(query.Payment);
            getQuery.Token.ShouldBe(query.Token);
            getQuery.AggregateThreshold.ShouldBe(Math.Max(nodes.Div(3).Add(1), 1));
            getQuery.CallbackInfo.ShouldBe(query.CallbackInfo);
            getQuery.QueryInfo.ShouldBe(query.QueryInfo);
            getQuery.IsCancelled.ShouldBeFalse();
            getQuery.AggregatorContractAddress.ShouldBe(query.AggregatorContractAddress);
            getQuery.QuerySender.ShouldBe(query.QuerySender);
            //wait node to push message
            Thread.Sleep(5000);
            var finalRecord = GetQueryRecord(query.QueryId.ToHex());
            finalRecord.IsCommitStageFinished.ShouldBeTrue();
            finalRecord.IsSufficientDataCollected.ShouldBeTrue();
            finalRecord.IsSufficientCommitmentsCollected.ShouldBeTrue();
            CheckRevel();
            // CheckNodeBalance();
        }

        [TestMethod]
        public void QueryList()
        {
            var payAmount = 100000000;
            var allowance = _tokenContract.GetAllowance(InitAccount, _oracleContract.ContractAddress, Symbol);
            Logger.Info(allowance);
            if (allowance < payAmount)
                _tokenContract.ApproveToken(InitAccount, _oracleContract.ContractAddress, payAmount, Symbol);
            _tokenContract.IssueBalance(InitAccount, InitAccount, payAmount, Symbol);
            var balance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            var result = ExecutedQuery(payAmount,
                _integerAggregator,
                new QueryInfo
                {
                    UrlToQuery = "http://localhost:7080/price/elf",
                    AttributesToFetch =
                    {
                        "price"
                    }
                }, new AddressList
                {
                    Value =
                    {
                        InitAccount.ConvertAddress(),
                        TestAccount.ConvertAddress(),
                        OtherNode.ConvertAddress()
                    }
                }, new CallbackInfo
                {
                    ContractAddress = _oracleUserContract.Contract,
                    MethodName = "RecordPrice"
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            afterBalance.ShouldBe(balance - payAmount);
            var byteString = result.Logs.First(l => l.Name.Contains(nameof(QueryCreated))).NonIndexed;
            var query = QueryCreated.Parser.ParseFrom(ByteString.FromBase64(byteString));
            query.Payment.ShouldBe(payAmount);
            query.Token.ShouldBe("Test");
            query.QuerySender.ShouldBe(InitAccount.ConvertAddress());
            query.AggregatorContractAddress.ShouldBe(_integerAggregator);
            Logger.Info(query.QueryId.ToHex());
        }

        [TestMethod]
        public void Query_Failed()
        {
            var payAmount = 100000000;
            var allowance = _tokenContract.GetAllowance(InitAccount, _oracleContract.ContractAddress, Symbol);
            Logger.Info(allowance);
            if (allowance < payAmount)
                _tokenContract.ApproveToken(InitAccount, _oracleContract.ContractAddress, payAmount, Symbol);
            _tokenContract.IssueBalance(InitAccount, InitAccount, payAmount, Symbol);
            var result = ExecutedQuery(payAmount,
                _integerAggregator,
                new QueryInfo
                {
                    UrlToQuery = "http://localhost:7080/price/elf",
                    AttributesToFetch =
                    {
                        "price"
                    }
                }, new AddressList
                {
                    Value = {_association2.ConvertAddress()}
                }, new CallbackInfo
                {
                    ContractAddress = _oracleUserContract.Contract,
                    MethodName = "RecordPrice"
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
            result.Error.ShouldContain("Invalid designated nodes count, should at least be");
        }

        [TestMethod]
        public void QueryCancel()
        {
            var id = "d4b2c9abb546e2853a40f49e278328ea35d89584b045c1bcdbd9d5f363b0c26b";
            var queryId = Hash.LoadFromHex(id);
            var queryInfo = GetQueryRecord(queryId.ToHex());
            var sender = queryInfo.QuerySender;
            var amount = queryInfo.Payment;
            
            var balance = _tokenContract.GetUserBalance(sender.ToBase58(), Symbol);
            var cancelResult = _oracleContract.ExecuteMethodWithResult(OracleMethod.CancelQuery, queryId);
            cancelResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var getQuery = GetQueryRecord(queryId.ToHex());
            getQuery.IsCancelled.ShouldBeTrue();
            var afterBalance = _tokenContract.GetUserBalance(sender.ToBase58(), Symbol);
            
            afterBalance.ShouldBe(balance + amount);
        }
        
        [TestMethod]
        public void QueryCancel_Failed()
        {
            {
                // query not existed
                var queryId = HashHelper.ComputeFrom("NotExisted");
                var cancelResult = _oracleContract.ExecuteMethodWithResult(OracleMethod.CancelQuery, queryId);
                cancelResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                cancelResult.Error.ShouldContain("Query not exists.");
            }
            {
                // no permission
                var id = "335c5790e8feca8a229737092946f4dee20ba88e44ea6f9aa77ec4c47df31ad4";
                var queryId = Hash.LoadFromHex(id);
                _oracleContract.SetAccount(TestAccount);
                var cancelResult = _oracleContract.ExecuteMethodWithResult(OracleMethod.CancelQuery, queryId);
                cancelResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                cancelResult.Error.ShouldContain("No permission to cancel this query.");
            }
            {
                // already cancel
                var id = "335c5790e8feca8a229737092946f4dee20ba88e44ea6f9aa77ec4c47df31ad4";
                var queryId = Hash.LoadFromHex(id);
                _oracleContract.SetAccount(InitAccount);
                var cancelResult = _oracleContract.ExecuteMethodWithResult(OracleMethod.CancelQuery, queryId);
                cancelResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                cancelResult.Error.ShouldContain("Query already cancelled.");
            }
        }

        [TestMethod]
        public void Commit_Fake()
        {
            _oracleContract.SetAccount(InitAccount);
            {
                // Commit stage of this query is already finished.
                var queryId = "048539fefcb0d207364cbae1a46b030085eeaf88f10485ddd11b881d23e70b72";
                var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.Commit, new CommitInput
                {
                    QueryId = Hash.LoadFromHex(queryId),
                    Commitment = Hash.LoadFromHex("cf47ba005c83cee09af1430d4efb1157f03088093efbb72d2f6e141d39aa327c")
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                result.Error.ShouldContain("Commit stage of this query is already finished.");
            }
        }

        //b2cde62bb42d50349affeefd64aeb231a7bb860d54f402c8ffc5d1de7dc3d07a
        [TestMethod]
        public void Commit_Failed()
        {
            _oracleContract.SetAccount(_associationMember[0]);
            {
                // Commit stage of this query is already finished.
                var queryId = "ff2b18b4389fc991c54c20e1fc4241ccff6f0bbd2c5f5cc89034c1d60145c54a";
                var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.Commit, new CommitInput
                {
                    QueryId = Hash.LoadFromHex(queryId),
                    Commitment = Hash.LoadFromHex("dea54f09d9ec153e3928addd327ba4ea08e2752c5fca1942c967a157fe0c4184")
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                result.Error.ShouldContain("Commit stage of this query is already finished.");
            }
            {
                // Query expired.
                var queryId = "b2cde62bb42d50349affeefd64aeb231a7bb860d54f402c8ffc5d1de7dc3d07a";
                var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.Commit, new CommitInput
                {
                    QueryId = Hash.LoadFromHex(queryId),
                    Commitment = Hash.LoadFromHex("dea54f09d9ec153e3928addd327ba4ea08e2752c5fca1942c967a157fe0c4184")
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                result.Error.ShouldContain("Query expired.");
            }
        }

        [TestMethod]
        public void Reveal_Failed()
        {
            _oracleContract.SetAccount(OtherNode);
            {
                // Commit stage of this query is already finished.
                var queryId = "e37fd49232e37e4c050871645cd8898cff33bdb4ca08e1d6568ec877dff374d6";
                var date = "CiEiRUxGIjsiMjAyMS0wNC0zMCAwODozMTo1NiI7NC4yMzY=";
                var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.Reveal, new RevealInput
                {
                    QueryId = Hash.LoadFromHex(queryId),
                    Data = ByteString.FromBase64(date)
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                result.Error.ShouldContain("This query hasn't collected sufficient commitments.");
            }
            {
                _oracleContract.SetAccount(_associationMember[2]);
                // No permission to reveal for this query. Sender hasn't submit commitment.
                var queryId = "e37fd49232e37e4c050871645cd8898cff33bdb4ca08e1d6568ec877dff374d6";
                var date = "CiEiRUxGIjsiMjAyMS0wNC0zMCAwODozMTo1NiI7NC4yMzY=";
                var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.Reveal, new RevealInput
                {
                    QueryId = Hash.LoadFromHex(queryId),
                    Data = ByteString.FromBase64(date)
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                result.Error.ShouldContain("No permission to reveal for this query. Sender hasn't submit commitment.");
            }
        }

        [TestMethod]
        public void GetQueryRecord()
        {
            var id = "73abfeeb3befe8e88e787d0c12b08916e7b918fbfd42f70806f578732edbb2c5";
            var record = GetQueryRecord(id);
            if (record.IsCancelled)
                Logger.Info($"this query {id} is already cancelled");
            else if (record.IsSufficientDataCollected)
                Logger.Info($"this query {id} is already finished");
            else if (record.IsSufficientCommitmentsCollected)
                Logger.Info($"this query {id} is committed, it is ready to reveal");
            var price = StringValue.Parser.ParseFrom(record.FinalResult);
            Logger.Info(price);
        }

        [TestMethod]
        public void CheckRevel()
        {
            var price = _oracleUserContract.CallViewMethod<PriceRecordList>(OracleUserMethod.GetHistoryPrices,
                new Empty());
            Logger.Info(price.Value.First());
        }

        [TestMethod]
        public void CheckNodeBalance()
        {
            foreach (var member in _associationMember)
            {
                var balance = _tokenContract.GetUserBalance(member, Symbol);
                Logger.Info($"{member}: {balance}");
            }

            var otherBalance = _tokenContract.GetUserBalance(OtherNode, Symbol);
            Logger.Info($"{OtherNode}: {otherBalance}");

            var miners = AuthorityManager.GetCurrentMiners();
            foreach (var miner in miners)
            {
                var balance = _tokenContract.GetUserBalance(miner, Symbol);
                Logger.Info($"{miner}: {balance}");
            }
        }

        [TestMethod]
        public void Initialize_Failed_Test()
        {
            {
                //MinimumOracleNodesCount < RevealThreshold
                var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.Initialize, new InitializeInput
                {
                    MinimumOracleNodesCount = 1,
                    DefaultAggregateThreshold = 1,
                    DefaultRevealThreshold = 2,
                    DefaultExpirationSeconds = 1200
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                result.Error.ShouldContain(
                    "MinimumOracleNodesCount should be greater than or equal to DefaultRevealThreshold.");
            }
            {
                //RevealThreshold < AggregateThreshold 
                var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.Initialize, new InitializeInput
                {
                    MinimumOracleNodesCount = 1,
                    DefaultAggregateThreshold = 2,
                    DefaultRevealThreshold = 1,
                    DefaultExpirationSeconds = 1200
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                result.Error.ShouldContain(
                    "DefaultRevealThreshold should be greater than or equal to DefaultAggregateThreshold.");
            }
            {
                //DefaultAggregateThreshold < 0
                var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.Initialize, new InitializeInput
                {
                    MinimumOracleNodesCount = -1,
                    DefaultAggregateThreshold = -10,
                    DefaultRevealThreshold = -2,
                    DefaultExpirationSeconds = 1200
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
                result.Error.ShouldContain("DefaultAggregateThreshold should be positive.");
            }
            {
                // DefaultAggregateThreshold == 0
                var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.Initialize, new InitializeInput
                {
                    MinimumOracleNodesCount = 0,
                    DefaultRevealThreshold = 0,
                    DefaultAggregateThreshold = 0,
                    DefaultExpirationSeconds = 1200
                });
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var oracleNodeThreshold =
                    _oracleContract.CallViewMethod<OracleNodeThreshold>(OracleMethod.GetThreshold, new Empty());
                oracleNodeThreshold.DefaultAggregateThreshold.ShouldBe(1);
                oracleNodeThreshold.MinimumOracleNodesCount.ShouldBe(3);
                oracleNodeThreshold.DefaultRevealThreshold.ShouldBe(2);
            }
        }

        #region controller

        [TestMethod]
        public void SetThreshold()
        {
            var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.SetThreshold, new OracleNodeThreshold
            {
                DefaultAggregateThreshold = 1,
                DefaultRevealThreshold = 1,
                MinimumOracleNodesCount = 1
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var oracleNodeThreshold =
                _oracleContract.CallViewMethod<OracleNodeThreshold>(OracleMethod.GetThreshold, new Empty());
            oracleNodeThreshold.DefaultAggregateThreshold.ShouldBe(1);
            oracleNodeThreshold.MinimumOracleNodesCount.ShouldBe(1);
            oracleNodeThreshold.DefaultRevealThreshold.ShouldBe(1);
        }

        [TestMethod]
        public void GetThreshold()
        {
            var threshold = _oracleContract.CallViewMethod<OracleNodeThreshold>(OracleMethod.GetThreshold, new Empty());
            Logger.Info($"aggregate: {threshold.DefaultAggregateThreshold}\n" +
                        $"reveal: {threshold.DefaultRevealThreshold} \n" +
                        $"node count: {threshold.MinimumOracleNodesCount}");
        }

        [TestMethod]
        public void SetThreshold_Failed()
        {
            var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.SetThreshold, new OracleNodeThreshold
            {
                DefaultAggregateThreshold = 1,
                DefaultRevealThreshold = 1,
                MinimumOracleNodesCount = 1
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        #endregion
        
        #region association

        [TestMethod]
        public void CreateToken()
        {
            var result = _mainTokenContract.ExecuteMethodWithResult(TokenMethod.Create, 
                new CreateInput
                {
                    TokenName = "Portal Token",
                    Symbol = "PORT",
                    TotalSupply = 100_000_000_00000000,
                    Issuer = _defaultParliamentOrganization,
                    Decimals = 8,
                    IsBurnable = true,
                    IssueChainId = 1866392
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
        
        [TestMethod]
        public void IssueToken()
        {
            var amount = 1000_00000000;
            var tokenInfo = _tokenContract.GetTokenInfo(Symbol);
            tokenInfo.ShouldNotBe(new TokenInfo());
            tokenInfo.Issuer.ShouldBe(_defaultParliamentOrganization);
            var balance = _tokenContract.GetUserBalance(InitAccount, Symbol);

            //change issuer to sender
            var res = AuthorityManager.ExecuteTransactionWithAuthority(_tokenContract.ContractAddress,
                nameof(TokenMethod.Issue),
                new IssueInput {Symbol = Symbol, Amount = amount, To = InitAccount.ConvertAddress()},
                InitAccount);
            res.Status.ShouldBe(TransactionResultStatus.Mined);

            var afterBalance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            balance.ShouldBe(afterBalance - amount);
        }

        [TestMethod]
        public void ChangeTokenIssuer()
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

        [TestMethod]
        public void RemoveMembers()
        {
            CheckMemberBalance();
            _associationContract.SetAccount(_associationMember[0]);
            var input = _associationMember[2].ConvertAddress();
            var createProposal = _associationContract.CreateProposal(_associationContract.ContractAddress,
                nameof(AssociationMethod.RemoveMember), input, _association1.ConvertAddress(), _associationMember[0]);
            var reviewers = _associationContract.GetOrganization(_association1.ConvertAddress());
            foreach (var member in reviewers.OrganizationMemberList.OrganizationMembers)
                _associationContract.ApproveProposal(createProposal, member.ToBase58());
            var release = _associationContract.ReleaseProposal(createProposal, _associationMember[0]);
            release.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterReviewers = _associationContract.GetOrganization(_association1.ConvertAddress());
            afterReviewers.OrganizationMemberList.OrganizationMembers.Count.ShouldBe(2);
        }

        [TestMethod]
        public void AddMembers()
        {
            CheckMemberBalance();
            _associationContract.SetAccount(_associationMember[0]);
            var input = _associationMember[2].ConvertAddress();
            var createProposal = _associationContract.CreateProposal(_associationContract.ContractAddress,
                nameof(AssociationMethod.AddMember), input, _association1.ConvertAddress(), _associationMember[0]);
            var reviewers = _associationContract.GetOrganization(_association1.ConvertAddress());
            foreach (var member in reviewers.OrganizationMemberList.OrganizationMembers)
                _associationContract.ApproveProposal(createProposal, member.ToBase58());
            var release = _associationContract.ReleaseProposal(createProposal, _associationMember[0]);
            release.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterReviewers = _associationContract.GetOrganization(_association1.ConvertAddress());
            afterReviewers.OrganizationMemberList.OrganizationMembers.Count.ShouldBe(3);
        }
        #endregion 
        
        private QueryRecord GetQueryRecord(string hash)
        {
            return _oracleContract.CallViewMethod<QueryRecord>(OracleMethod.GetQueryRecord,
                Hash.LoadFromHex(hash));
        }

        private void InitializeTestContract()
        {
            _oracleUserContract.ExecuteMethodWithResult(OracleUserMethod.Initialize, _oracleContract.Contract);
        }

        private TransactionResultDto ExecutedQuery(long payAmount, Address aggregator, QueryInfo queryInfo,
            AddressList addressList, CallbackInfo callbackInfo = null)
        {
            var result = _oracleContract.ExecuteMethodWithResult(OracleMethod.Query, new QueryInput
            {
                Payment = payAmount,
                AggregateThreshold = 1,
                AggregatorContractAddress = aggregator,
                QueryInfo = queryInfo,
                CallbackInfo = callbackInfo,
                DesignatedNodeList = addressList,
                Token = "Test"
            });
            return result;
        }

        private void CheckMemberBalance()
        {
            foreach (var member in from member in _associationMember
                let balance = _tokenContract.GetUserBalance(member)
                where balance < 100000000
                select member)
            {
                _tokenContract.TransferBalance(InitAccount, member, 100000000000);
            }
        }
    }
}