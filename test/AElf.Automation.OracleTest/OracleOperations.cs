using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Contracts.Oracle;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Shouldly;

namespace AElf.Automation.OracleTest
{
    public class OracleOperations
    {
        private static ILog Logger { get; set; } = Log4NetHelper.GetLogger();
        public string Symbol = "PORT";

        public readonly string Url;
        public readonly string Owner;
        public readonly string Password;
        public string OracleContract;
        public string AggregatorContract;
        public List<Info> QueryInfos;
        public long PayAmount;
        public Dictionary<Hash, string> TokenQueryList;

        public EnvironmentInfo EnvironmentInfo { get; set; }
        public readonly OracleContract OracleService;
        public readonly TokenContract TokenService;
        public readonly ParliamentContract ParliamentService;
        public readonly ContractServices ContractServices;

        public OracleOperations()
        {
            GetConfig();
            Owner = EnvironmentInfo.Owner;
            Password = EnvironmentInfo.Password;
            Url = EnvironmentInfo.Url;
            ContractServices = GetContractServices();
            TokenService = ContractServices.TokenService;
            ParliamentService = ContractServices.ParliamentContract;
            OracleService = ContractServices.OracleService;
            if (OracleContract == "")
                InitializeContract();
            TokenQueryList = new Dictionary<Hash, string>();
        }

        public void QueryJob()
        {
            ExecuteStandaloneTask(new Action[] {Query});
        }

        public void Query()
        {
            var txIds = new Dictionary<string,string>();
            foreach (var info in QueryInfos)
            {
                var queryInfo = new QueryInfo
                {
                    UrlToQuery = info.UrlToQuery,
                    AttributesToFetch = {info.AttributesToFetch}
                };
                var id = QueryWithCommitAndReveal_Parliament(queryInfo);
                txIds[id] = info.Token;
            }

            foreach (var (key,value) in txIds)
            {
                var queryId = CheckTransactionResult(key);
                TokenQueryList[queryId] = value;
            }
            CheckQueryList();
        }


        private void CheckQueryList()
        {
            foreach (var (key, value) in TokenQueryList)
            {
                var check = CheckQuery(key, value);
                if (check)
                {
                    TokenQueryList.Remove(key);
                }
            }
        }

        private string QueryWithCommitAndReveal_Parliament(QueryInfo queryInfo)
        {
            var allowance = TokenService.GetAllowance(Owner, OracleService.ContractAddress, Symbol);
            if (allowance < PayAmount)
                TokenService.ApproveToken(Owner, OracleService.ContractAddress, PayAmount, Symbol);
            var txId = OracleService.ExecuteMethodWithTxId(OracleMethod.Query, new QueryInput
            {
                Payment = PayAmount,
                AggregateThreshold = 1,
                AggregatorContractAddress = AggregatorContract.ConvertAddress(),
                QueryInfo = queryInfo,
                DesignatedNodeList = new AddressList
                {
                    Value = {ParliamentService.Contract}
                },
                Token = "Test"
            });
            return txId;
        }

        private Hash CheckTransactionResult(string id)
        {
            var result = ContractServices.NodeManager.CheckTransactionResult(id);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var nodes = ContractServices.AuthorityManager.GetCurrentMiners().Count;
            var byteString = result.Logs.First(l => l.Name.Contains(nameof(QueryCreated))).NonIndexed;
            var query = QueryCreated.Parser.ParseFrom(ByteString.FromBase64(byteString));
            var oracleNodeThreshold =
                OracleService.CallViewMethod<OracleNodeThreshold>(OracleMethod.GetThreshold, new Empty());
            var setAggregateThreshold = oracleNodeThreshold.DefaultAggregateThreshold;
            query.Payment.ShouldBe(PayAmount);
            query.QuerySender.ShouldBe(Owner.ConvertAddress());
            query.AggregatorContractAddress.ShouldBe(AggregatorContract.ConvertAddress());
            query.AggregateThreshold.ShouldBe(Math.Max(Math.Max(nodes.Div(3).Add(1), 1), setAggregateThreshold));
            Logger.Info(query.QueryId.ToHex());
            return query.QueryId;
        }


        #region private

        private bool CheckQuery(Hash id, string token)
        {
            var finalRecord = GetQueryRecord(id.ToHex());
            if (finalRecord.IsSufficientDataCollected)
            {
                var price = StringValue.Parser.ParseFrom(finalRecord.FinalResult);
                Logger.Info($"QueryId:{id.ToHex()} \n" +
                            $"{token} ==> {price}");

                return true;
            }

            return false;
        }

        private QueryRecord GetQueryRecord(string hash)
        {
            return OracleService.CallViewMethod<QueryRecord>(OracleMethod.GetQueryRecord,
                Hash.LoadFromHex(hash));
        }

        private ContractServices GetContractServices()
        {
            var contractService =
                new ContractServices(Url, Owner, Password, OracleContract, AggregatorContract);
            return contractService;
        }

        private void GetConfig()
        {
            var config = OracleConfig.ReadInformation;
            OracleContract = config.OracleContract;
            AggregatorContract = config.IntegerAggregatorContract;

            var testEnvironment = config.TestEnvironment;
            EnvironmentInfo =
                config.EnvironmentInfos.Find(o => o.Environment.Contains(testEnvironment));
            QueryInfos = config.QueryInfos;
            PayAmount = config.PayAmount;
        }

        private void InitializeContract()
        {
            OracleService.SetAccount(Owner, Password);
            var result = OracleService.ExecuteMethodWithResult(OracleMethod.Initialize, new InitializeInput
            {
                MinimumOracleNodesCount = 5,
                DefaultAggregateThreshold = 3,
                DefaultRevealThreshold = 3,
                DefaultExpirationSeconds = 600,
                IsChargeFee = false
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var oracleNodeThreshold =
                OracleService.CallViewMethod<OracleNodeThreshold>(OracleMethod.GetThreshold, new Empty());
            oracleNodeThreshold.DefaultAggregateThreshold.ShouldBe(3);
            oracleNodeThreshold.MinimumOracleNodesCount.ShouldBe(5);
            oracleNodeThreshold.DefaultRevealThreshold.ShouldBe(3);

            var controller = OracleService.CallViewMethod<Address>(OracleMethod.GetController, new Empty());
            controller.ShouldBe(Owner.ConvertAddress());
            var tokenSymbol =
                OracleService.CallViewMethod<StringValue>(OracleMethod.GetOracleTokenSymbol, new Empty());
            tokenSymbol.Value.ShouldBe(Symbol);
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