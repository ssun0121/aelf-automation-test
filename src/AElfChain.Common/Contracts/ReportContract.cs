using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum ReportMethod
    {
        //Action
        Initialize,
        QueryOracle,
        CancelQueryOracle,
        ProposeReport,
        ConfirmReport,
        RejectReport,
        MortgageTokens,
        WithdrawTokens,
        SetSkipMemberList,
        
        // off chain aggregator
        RegisterOffChainAggregation,
        BindOffChainAggregation,
        RemoveOffChainQueryInfo,
        AddOffChainQueryInfo,
        ChangeOffChainQueryInfo,
        AddRegisterWhiteList,
        RemoveFromRegisterWhiteList,
        ChangeOracleContractAddress,
        
        // Observer management.
        ApplyObserver,
        QuitObserver,
        AdjustApplyObserverFee,
        AdjustReportFee,
        
        //View
        GetMerklePath,

        GetReport,
        GetSignature,
        GetSignatureMap,
        GetOffChainAggregationInfo,
        GetReportQueryRecord,
        GetCurrentRoundId,
        
        GenerateRawReport,
        GetRawReport,
        
        IsInRegisterWhiteList,
        IsObserver,
        GetMortgagedTokenAmount,
        GetObserverList,
        GetSkipMemberList,
        GetTokenByChainId
    }

    public class ReportContract : BaseContract<ReportMethod>
    {
        public ReportContract(INodeManager nodeManager, string callAddress) : base(nodeManager,
            "AElf.Contracts.Report", callAddress)
        {
        }

        public ReportContract(INodeManager nodeManager, string callAddress, string contractAddress,string password = "") : base(
            nodeManager, contractAddress)
        {
            SetAccount(callAddress, password);
        }
    }
}