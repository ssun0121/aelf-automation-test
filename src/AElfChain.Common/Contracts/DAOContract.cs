using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum DAOMethod
    { 
        //Action
        ProposeJoin,
        Quit,
        ProposeExpel,
        AdjustProposalReleaseThreshold,
        ProjectPreAudition,
        
        ProposeProjectToDAO,
        ProposeProjectToParliament,
        Invest,
        UpdateInvestmentProject,
        
        ProposeBountyProject,
        ProposeIssueBountyProject,
        ProposeTakeOverBountyProject,
        ProposeDevelopersAudition,
        UpdateBountyProject,
        
        ProposeDeliver,
        AddProject,
        SetReferendumOrganizationAddress,
        
        //View
        GetDAOMemberList,
        GetBudgetPlan,
        GetPreviewProposalId,
        GetProjectInfo,
        CalculateProjectId
    }
    public class DAOContract: BaseContract<DAOMethod>
    {
        public DAOContract(INodeManager nodeManager, string callAddress) :
            base(nodeManager, "AElf.Contracts.DAOContract", callAddress)
        {
        }

        public DAOContract(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }
    }
}