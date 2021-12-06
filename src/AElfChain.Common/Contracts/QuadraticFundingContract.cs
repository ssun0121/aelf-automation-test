using System.Collections.Generic;
using System.Linq;
using AElf.Client.Dto;
using AElf.Contracts.QuadraticFunding;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
{
    public enum QuadraticFundingMethod
    {
        Initialize,

        //OnlyOwner
        RoundOver,
        ChangeOwner,
        BanProject,
        SetTaxPoint,
        SetInterval,
        SetVotingUnit,
        RoundStart,
        Withdraw,

        //Other
        Donate,
        UploadProject,
        Vote,
        TakeOutGrants,

        //View
        GetAllProjects,
        GetRankingList,
        GetPagedRankingList,
        GetRoundInfo,
        GetVotingCost,
        GetGrandsOf,
        GetProjectOf,
        CalculateProjectId,
        GetCurrentRound,
        GetTaxPoint,
        GetTax,
        GetInterval,
        GetVotingUnit
    }

    public class QuadraticFundingContract : BaseContract<QuadraticFundingMethod>
    {
        public QuadraticFundingContract(INodeManager nodeManager, string callAddress)
            : base(nodeManager, "AElf.Contracts.QuadraticFunding", callAddress)
        {
        }

        public QuadraticFundingContract(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }

        public TransactionResultDto Initialize(string sender, long interval, long basicVoting, string symbol,
            Address owner = null)
        {
            SetAccount(sender);
            var result = ExecuteMethodWithResult(QuadraticFundingMethod.Initialize, new InitializeInput
            {
                Interval = interval, // 15min
                BasicVotingUnit = basicVoting,
                VoteSymbol = symbol,
                Owner = owner
            });

            return result;
        }

        public TransactionResultDto BanProject(string projectId, bool isBan)
        {
            var result = ExecuteMethodWithResult(QuadraticFundingMethod.BanProject, new BanProjectInput
            {
                ProjectId = projectId,
                Ban = isBan
            });
            return result;
        }

        public TransactionResultDto RoundOver(string owner)
        {
            SetAccount(owner);
            var result = ExecuteMethodWithResult(QuadraticFundingMethod.RoundOver, new Empty());
            return result;
        }

        public TransactionResultDto RoundStart(string owner)
        {
            SetAccount(owner);
            var result = ExecuteMethodWithResult(QuadraticFundingMethod.RoundStart, new Empty());
            return result;
        }
        // SetConfig
        public TransactionResultDto SetInterval(long interval)
        {
            return ExecuteMethodWithResult(QuadraticFundingMethod.SetInterval, new Int64Value {Value = interval});
        }
        
        public TransactionResultDto SetTaxPoint(long txPoint)
        {
            return ExecuteMethodWithResult(QuadraticFundingMethod.SetTaxPoint, new Int64Value {Value = txPoint});
        }
        
        public TransactionResultDto SetVotingUnit(long votingUnit)
        {
            return ExecuteMethodWithResult(QuadraticFundingMethod.SetVotingUnit, new Int64Value {Value = votingUnit});
        }

        public TransactionResultDto ChangeOwner(string address)
        {
            return ExecuteMethodWithResult(QuadraticFundingMethod.ChangeOwner, address.ConvertAddress());
        }
        
        public TransactionResultDto UploadProject() 
        {
            return ExecuteMethodWithResult(QuadraticFundingMethod.UploadProject, new Empty());
        }

        public TransactionResultDto Donate(long amount)
        {
            return ExecuteMethodWithResult(QuadraticFundingMethod.Donate, new Int64Value{Value = amount});
        }

        public TransactionResultDto Vote(string projectId, long votes)
        {
            return ExecuteMethodWithResult(QuadraticFundingMethod.Vote, new VoteInput
            {
                ProjectId = projectId,
                Votes = votes
            });
        }

        public TransactionResultDto Withdraw()
        {
            return ExecuteMethodWithResult(QuadraticFundingMethod.Withdraw, new Empty());
        }
        
        public TransactionResultDto TakeOutGrants(string projectId, long amount)
        {
            return ExecuteMethodWithResult(QuadraticFundingMethod.TakeOutGrants, new TakeOutGrantsInput
            {
                ProjectId = projectId,
                Amount = amount
            });
        }

        // View
        public VotingCost GetVotingCost(string voter, string projectId, long votes)
        {
            return CallViewMethod<VotingCost>(QuadraticFundingMethod.GetVotingCost, new GetVotingCostInput
            {
                From = voter.ConvertAddress(),
                ProjectId = projectId,
                Votes = votes
            });
        }
        
        public StringValue CalculateProjectId(string address)
        {
            return CallViewMethod<StringValue>(QuadraticFundingMethod.CalculateProjectId, address.ConvertAddress());
        }

        public Project GetProjectOf(string projectId)
        {
            return CallViewMethod<Project>(QuadraticFundingMethod.GetProjectOf, new StringValue{Value = projectId});
        }

        public List<string> GetAllProjects(long round)
        {
            var projectList = CallViewMethod<ProjectList>(QuadraticFundingMethod.GetAllProjects, new Int64Value{Value = round });
            var list = projectList.Value.ToList();
            return list;
        }

        public Grands GetGrandsOf(string projectId)
        {
            return CallViewMethod<Grands>(QuadraticFundingMethod.GetGrandsOf, new StringValue{Value = projectId});
        }

        public RankingList GetRankingList(long round)
        {
            return CallViewMethod<RankingList>(QuadraticFundingMethod.GetRankingList, new Int64Value{Value = round});
        }

        public RoundInfo GetRoundInfo(long round)
        {
            return CallViewMethod<RoundInfo>(QuadraticFundingMethod.GetRoundInfo, new Int64Value {Value = round});
        }

        public long GetCurrentRound()
        {
            var round = CallViewMethod<Int64Value>(QuadraticFundingMethod.GetCurrentRound, new Empty());
            return round.Value;
        }

        public long GetTaxPoint()
        {
            var taxPoint = CallViewMethod<Int64Value>(QuadraticFundingMethod.GetTaxPoint, new Empty());
            return taxPoint.Value;
        }

        public long GetInterval()
        {
            var interval = CallViewMethod<Int64Value>(QuadraticFundingMethod.GetInterval, new Empty());
            return interval.Value;
        }

        public long GetVotingUnit()
        {
            var votingUnit = CallViewMethod<Int64Value>(QuadraticFundingMethod.GetVotingUnit, new Empty());
            return votingUnit.Value;
        }
        
        public long GetTax()
        {
            var tax = CallViewMethod<Int64Value>(QuadraticFundingMethod.GetTax, new Empty());
            return tax.Value;
        }
    }
}