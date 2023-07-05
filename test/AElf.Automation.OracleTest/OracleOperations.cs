using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Contracts.MultiToken;
using EBridge.Contracts.Oracle;
using EBridge.Contracts.Report;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Shouldly;
using InitializeInput = EBridge.Contracts.Oracle.InitializeInput;

namespace AElf.Automation.OracleTest
{
    public class OracleOperations
    {
        private static ILog Logger { get; set; } = Log4NetHelper.GetLogger();
        private const string Symbol = "PORT";

        private readonly string _url;
        private readonly string _owner;
        private readonly string _password;
        private readonly Dictionary<Hash, string> _tokenQueryList;
        private List<string> TokenList { get; set; }
        private string _oracleContract;
        private string _reportContract;
        private string _aggregatorContract;
        private List<Info> _queryInfos;
        private long _payAmount;
        private long _reportFee;
        private long _applyObserverFee;
        private string _organization;
        private string _ethereumContractAddress;
        private string _digestStr;

        private EnvironmentInfo EnvironmentInfo { get; set; }
        private ReportInfo ReportInfo { get; set; }
        private readonly OracleContract _oracleService;
        private readonly ReportContract _reportService;
        private readonly TokenContract _tokenService;
        private readonly ParliamentContract _parliamentService;
        private readonly ContractServices _contractServices;

        public bool OnlyOracle;

        public OracleOperations()
        {
            GetConfig();
            _owner = EnvironmentInfo.Owner;
            _password = EnvironmentInfo.Password;
            _url = EnvironmentInfo.Url;
            _contractServices = GetContractServices();
            _tokenService = _contractServices.TokenService;
            _parliamentService = _contractServices.ParliamentContract;
            _oracleService = _contractServices.OracleService;
            _reportService = _contractServices.ReportService;
            _organization =
                _organization == "" ? _parliamentService.ContractAddress : _organization;
            if (_oracleContract == "")
                InitializeContract();
            if (_reportContract == "" && !OnlyOracle)
                InitializeReportContract();
            _tokenQueryList = new Dictionary<Hash, string>();
        }

        public void QueryJob()
        {
            ExecuteStandaloneTask(new Action[] {Query});
        }

        public void ReportQueryJob()
        {
            ExecuteStandaloneTask(new Action[] {QueryIndex});
        }

        private void Query()
        {
            var txIds = new Dictionary<string, string>();
            foreach (var info in _queryInfos)
            {
                var queryInfo = new QueryInfo
                {
                    Title = info.UrlToQuery,
                    Options= {info.AttributesToFetch}
                };
                var id = QueryWithCommitAndReveal(queryInfo);
                txIds[id] = info.Token;
            }

            foreach (var (key, value) in txIds)
            {
                var queryId = CheckTransactionResult(key);
                _tokenQueryList[queryId] = value;
            }

            CheckQueryList();
        }

        public void ApplyObserver()
        {
            var memberList = new List<string>();
            if (_organization == _parliamentService.ContractAddress)
            {
                memberList = _contractServices.AuthorityManager.GetCurrentMiners();
            }
            else
            {
                var association = _contractServices.GenesisService.GetAssociationAuthContract();
                var list = association.GetOrganization(Address.FromBase58(_organization)).OrganizationMemberList
                    .OrganizationMembers;
                memberList.AddRange(list.Select(a => a.ToBase58()));
            }

            IssueToken();
            TransferToken(memberList, Symbol);
            TransferToken(memberList, "ELF");
            foreach (var member in memberList)
            {
                _reportService.SetAccount(member);
                if (_applyObserverFee != 0)
                    _tokenService.ApproveToken(member, _reportService.ContractAddress, _applyObserverFee, Symbol);
                var isObserver =
                    _reportService.CallViewMethod<BoolValue>(ReportMethod.IsObserver, Address.FromBase58(member));
                if (isObserver.Value) continue;
                var result = _reportService.ExecuteMethodWithResult(ReportMethod.ApplyObserver, new Empty());
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }
        }

        public void RegisterOffChainAggregation()
        {
            var offChainInfo = GetOffChainInfo();
            var offChainInfoList = GetOffChainQueryInfoList(out var tokenList);
            TokenList = tokenList;
            if (!offChainInfo.Equals(new OffChainAggregationInfo()))
                return;
            var isInWhiteList = _reportService
                .CallViewMethod<BoolValue>(ReportMethod.IsInRegisterWhiteList, Address.FromBase58(_owner)).Value;
            if (!isInWhiteList)
            {
                var result = _contractServices.AuthorityManager.ExecuteTransactionWithAuthority(_reportContract,
                    nameof(ReportMethod.AddRegisterWhiteList), Address.FromBase58(_owner), _owner);
                result.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            _reportService.SetAccount(_owner);
            var setResult = _reportService.ExecuteMethodWithResult(ReportMethod.RegisterOffChainAggregation,
                new RegisterOffChainAggregationInput
                {
                    OffChainQueryInfoList = offChainInfoList,
                    Token = _ethereumContractAddress,
                    AggregateThreshold = 1,
                    AggregatorContractAddress = Address.FromBase58(_aggregatorContract),
                    ConfigDigest = ByteStringHelper.FromHexString(_digestStr),
                });
            setResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        private Hash QueryOracle(int index)
        {
            var allowance = _tokenService.GetAllowance(_owner, _reportService.ContractAddress, Symbol);
            if (allowance < _payAmount + _reportFee)
                _tokenService.ApproveToken(_owner, _reportService.ContractAddress, _payAmount + _reportFee,
                    Symbol);
            allowance = _tokenService.GetAllowance(_owner, _reportService.ContractAddress, Symbol);
            Logger.Info($"Allowance is {allowance}");
            IssueToken();
            var senderBalance = _tokenService.GetUserBalance(_owner, Symbol);
            var reportBalance = _tokenService.GetUserBalance(_reportService.ContractAddress, Symbol);

            _reportService.SetAccount(_owner);
            var result = _reportService.ExecuteMethodWithResult(ReportMethod.QueryOracle, new QueryOracleInput
            {
                AggregateThreshold = 1,
                Payment = _payAmount,
                Token = _ethereumContractAddress,
                NodeIndex = index
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var byteString = result.Logs.First(l => l.Name.Contains(nameof(QueryCreated))).NonIndexed;
            var query = QueryCreated.Parser.ParseFrom(ByteString.FromBase64(byteString));
            Logger.Info($"Index: {index} ==> Query Id : {query.QueryId.ToHex()}");

            var afterSenderBalance = _tokenService.GetUserBalance(_owner, Symbol);
            var afterReportBalance = _tokenService.GetUserBalance(_reportService.ContractAddress, Symbol);
            afterSenderBalance.ShouldBe(senderBalance - (_payAmount + _reportFee));
            afterReportBalance.ShouldBe(reportBalance + _reportFee);

            return query.QueryId;
        }

        private void QueryIndex()
        {
            var indexNode = GetOffChainInfo().OffChainQueryInfoList.Value.Count;
            var roundId = GetCurrentRound();

            Logger.Info($"Round: {roundId}");
            for (var i = 0; i < indexNode; i++)
            {
                Logger.Info($"index: {i}");
                var id = QueryOracle(i);
                _tokenQueryList[id] = TokenList[i];
            }
            
            Thread.Sleep(10000);
            
            CheckQueryList();
            if (roundId -1 < 1) return;
            var report = GetReport(roundId - 1);
            Logger.Info($"Round: {roundId - 1} last query: {report.QueryId.ToHex()}");
            // GetMerklePath();
        }

        #region private

        private OffChainAggregationInfo GetOffChainInfo()
        {
            return _reportService.CallViewMethod<OffChainAggregationInfo>(ReportMethod.GetOffChainAggregationInfo,
                new StringValue {Value = _ethereumContractAddress});
        }

        private OffChainQueryInfoList GetOffChainQueryInfoList(out List<string> tokenList)
        {
            var offChainQueryInfoList = new OffChainQueryInfoList();
            tokenList = new List<string>();
            var list = new List<OffChainQueryInfo>();
            foreach (var query in _queryInfos)
            {
                var offChainQueryInfo = new OffChainQueryInfo
                {
                    Title = query.UrlToQuery,
                    Options = {query.AttributesToFetch}
                };
                list.Add(offChainQueryInfo);
                tokenList.Add(query.Token);
            }

            offChainQueryInfoList.Value.AddRange(list);
            return offChainQueryInfoList;
        }

        private Report GetReport(long roundId)
        {
            return _reportService.CallViewMethod<Report>(ReportMethod.GetReport, new GetReportInput
            {
                Token = _ethereumContractAddress,
                RoundId = roundId
            });
        }

        private long GetCurrentRound()
        {
            return _reportService.CallViewMethod<Int64Value>(ReportMethod.GetCurrentRoundId,
                new StringValue {Value = _ethereumContractAddress}).Value;
        }

        private void GetMerklePath()
        {
            var round = GetCurrentRound() - 1;
            if (round < 1) return;
            var indexNode = GetOffChainInfo().OffChainQueryInfoList.Value.Count;
            var reportInfo = GetReport(round);
            Hash root = null;
            for (var i = 0; i < indexNode; i++)
            {
                var result =
                    _reportService.CallViewMethod<MerklePath>(ReportMethod.GetMerklePath, new GetMerklePathInput
                    {
                        NodeIndex = i,
                        RoundId = round,
                        Token = _ethereumContractAddress
                    });
                var data = reportInfo.Observations.Value[i].Data;
                // var hash = HashHelper.ComputeFrom(data.ToByteArray());
                // if (root == null) root = result.ComputeRootWithLeafNode(hash);
                // var oldRoot = root;
                // root = result.ComputeRootWithLeafNode(hash);
                // oldRoot.ShouldBe(root);
            }

            Logger.Info($"Round: {round} MerkleTreeRoot: {root.ToHex()}");
        }

        #region OracelMethod

        private QueryRecord GetQueryRecord(string hash)
        {
            return _oracleService.CallViewMethod<QueryRecord>(OracleMethod.GetQueryRecord,
                Hash.LoadFromHex(hash));
        }

        private void CheckQueryList()
        {
            foreach (var (key, value) in _tokenQueryList)
            {
                var check = CheckQuery(key, value);
                if (check)
                    _tokenQueryList.Remove(key);
                else
                    Logger.Info($"UnFinished query: {key}");
            }
        }

        private bool CheckQuery(Hash id, string token)
        {
            var finalRecord = GetQueryRecord(id.ToHex());
            if (finalRecord.IsSufficientDataCollected)
            {
                var price = finalRecord.FinalResult;
                Logger.Info($"QueryId:{id.ToHex()} \n" +
                            $"{token} ==> {price}");
                return true;
            }

            return false;
        }

        private string QueryWithCommitAndReveal(QueryInfo queryInfo)
        {
            var allowance = _tokenService.GetAllowance(_owner, _oracleService.ContractAddress, Symbol);
            if (allowance < _payAmount)
                _tokenService.ApproveToken(_owner, _oracleService.ContractAddress, _payAmount, Symbol);
            var txId = _oracleService.ExecuteMethodWithTxId(OracleMethod.Query, new QueryInput
            {
                Payment = _payAmount,
                AggregateThreshold = 1,
                AggregatorContractAddress = _aggregatorContract.ConvertAddress(),
                QueryInfo = queryInfo,
                DesignatedNodeList = new AddressList
                {
                    Value = {Address.FromBase58(_organization)}
                },
                Token = "Test"
            });
            return txId;
        }

        #endregion

        #region Initialize

        private void InitializeContract()
        {
            _oracleService.SetAccount(_owner, _password);
            var result = _oracleService.ExecuteMethodWithResult(OracleMethod.Initialize, new InitializeInput
            {
                MinimumOracleNodesCount = 5,
                DefaultAggregateThreshold = 3,
                DefaultRevealThreshold = 3,
                DefaultExpirationSeconds = 600,
                IsChargeFee = false
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var oracleNodeThreshold =
                _oracleService.CallViewMethod<OracleNodeThreshold>(OracleMethod.GetThreshold, new Empty());
            oracleNodeThreshold.DefaultAggregateThreshold.ShouldBe(3);
            oracleNodeThreshold.MinimumOracleNodesCount.ShouldBe(5);
            oracleNodeThreshold.DefaultRevealThreshold.ShouldBe(3);

            var controller = _oracleService.CallViewMethod<Address>(OracleMethod.GetController, new Empty());
            controller.ShouldBe(_owner.ConvertAddress());
            var tokenSymbol =
                _oracleService.CallViewMethod<StringValue>(OracleMethod.GetOracleTokenSymbol, new Empty());
            tokenSymbol.Value.ShouldBe(Symbol);
        }

        private void InitializeReportContract()
        {
            _reportService.SetAccount(_owner, _password);
            var result = _reportService.ExecuteMethodWithResult(ReportMethod.Initialize,
                new EBridge.Contracts.Report.InitializeInput()
                {
                    OracleContractAddress = _oracleService.Contract,
                    ReportFee = _reportFee,
                    ApplyObserverFee = _applyObserverFee
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var allowance = _tokenService.GetAllowance(_reportService.ContractAddress,
                _oracleService.ContractAddress, Symbol);
            allowance.ShouldBe(long.MaxValue);
            Logger.Info(allowance);
        }

        private ContractServices GetContractServices()
        {
            var contractService =
                new ContractServices(_url, _owner, _password, _oracleContract, _aggregatorContract, _reportContract,
                    OnlyOracle);
            return contractService;
        }

        private void GetConfig()
        {
            var config = OracleConfig.ReadInformation;
            OnlyOracle = config.OnlyOracle;
            _oracleContract = config.OracleContract;
            _aggregatorContract = config.IntegerAggregatorContract;

            var testEnvironment = config.TestEnvironment;
            EnvironmentInfo =
                config.EnvironmentInfos.Find(o => o.Environment.Contains(testEnvironment));
            _queryInfos = config.QueryInfos;
            _payAmount = config.PayAmount;
            _organization = config.Organization;

            if (OnlyOracle) return;
            ReportInfo = config.ReportInfo;
            _digestStr = ReportInfo.DigestStr;
            _reportContract = ReportInfo.ReportContract;
            _applyObserverFee = ReportInfo.ApplyObserverFee;
            _reportFee = ReportInfo.ReportFee;
            _ethereumContractAddress = ReportInfo.EthereumContractAddress;
        }

        #endregion

        private void IssueToken()
        {
            var balance = _tokenService.GetUserBalance(_owner, Symbol);
            if (balance > _payAmount + _reportFee) return;
            var result = _contractServices.AuthorityManager.ExecuteTransactionWithAuthority(
                _tokenService.ContractAddress, nameof(TokenMethod.Issue),
                new IssueInput
                {
                    Symbol = Symbol,
                    Amount = 1000000_00000000,
                    To = Address.FromBase58(_owner)
                }, _owner);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        private void TransferToken(IEnumerable<string> addressList, string symbol)
        {
            foreach (var address in addressList.Where(address => address != _owner))
            {
                var balance = _tokenService.GetUserBalance(address, symbol);
                if (balance > _payAmount) continue;
                _tokenService.TransferBalance(_owner, address, 1000_00000000, symbol);
            }
        }

        private Hash CheckTransactionResult(string id)
        {
            var result = _contractServices.NodeManager.CheckTransactionResult(id);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var nodes = _contractServices.AuthorityManager.GetCurrentMiners().Count;
            var byteString = result.Logs.First(l => l.Name.Contains(nameof(QueryCreated))).NonIndexed;
            var query = QueryCreated.Parser.ParseFrom(ByteString.FromBase64(byteString));
            var oracleNodeThreshold =
                _oracleService.CallViewMethod<OracleNodeThreshold>(OracleMethod.GetThreshold, new Empty());
            var setAggregateThreshold = oracleNodeThreshold.DefaultAggregateThreshold;
            query.Payment.ShouldBe(_payAmount);
            query.QuerySender.ShouldBe(_owner.ConvertAddress());
            query.AggregatorContractAddress.ShouldBe(_aggregatorContract.ConvertAddress());
            query.AggregateThreshold.ShouldBe(Math.Max(Math.Max(nodes.Div(3).Add(1), 1), setAggregateThreshold));
            Logger.Info(query.QueryId.ToHex());
            return query.QueryId;
        }

        private void ExecuteStandaloneTask(IEnumerable<Action> actions, int sleepSeconds = 0,
            bool interrupted = false)
        {
            foreach (var action in actions)
                try
                {
                    action.Invoke();
                }
                catch (Exception e)
                {
                    Logger.Error($"Execute action {action.Method.Name} got exception: {e.Message}", e);
                    if (interrupted)
                        break;
                }

            if (sleepSeconds != 0)
                Thread.Sleep(1000 * sleepSeconds);
        }

        #endregion
    }
}