using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum ReportMethod
    {
        //Action
        Initialize,
        // off chain aggregator
        RegisterOffChainAggregation,
        AddRegisterWhiteList,
        QueryOracle,
        CancelQueryOracle,
        
        ApplyObserver,
        QuitObserver,
        MortgageTokens,
        WithdrawTokens,
        ProposeAdjustApplyObserverFee,
        AdjustApplyObserverFee,
        
        ProposeReport,
        ConfirmReport,
        RejectReport,
        
        //View
        GetReport,
        GetSignature,
        GetSignatureMap,
        GetOffChainAggregationInfo,
        GetReportQueryRecord,
        GetMerklePath,
        GetCurrentRoundId,
        
        GenerateEthererumReport,
        GetEthererumReport
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