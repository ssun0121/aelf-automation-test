using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs3;
using AElf.Contracts.Association;
using AElf.Contracts.DAOContract;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.Contracts.TokenConverter;
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
using DepositInfo = AElf.Contracts.DAOContract.DepositInfo;
using InitializeInput = AElf.Contracts.DAOContract.InitializeInput;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class DAOContractTests
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }

        private TokenContract _tokenContract;
        private ProfitContract _profitContract;
        private GenesisContract _genesisContract;
        private ParliamentContract _parliament;
        private AssociationContract _association;
        private ReferendumContract _referendum;

        private DAOContract _daoContract;
        private TokenContractContainer.TokenContractStub _tokenContractStub;
        private DAOContractContainer.DAOContractStub _daoContractStub;
        private DAOContractContainer.DAOContractStub _admDaoContractStub;
        private string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        private string TestAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        private string OtherAccount { get; } = "2REajHMeW2DMrTdQWn89RQ26KQPRg91coCEtPP42EC9Cj7sZ61";

        public string ReviewAccount1 { get; } = "h6CRCFAhyozJPwdFRd7i8A5zVAqy171AVty3uMQUQp1MB9AKa";
        public string ReviewAccount2 { get; } = "28qLVdGMokanMAp9GwfEqiWnzzNifh8LS9as6mzJFX1gQBB823";
        public string ReviewAccount3 { get; } = "eFU9Quc8BsztYpEHKzbNtUpu9hGKgwGD2tyL13MqtFkbnAoCZ";

        private static string RpcUrl { get; } = "192.168.197.14:8000";
        private static string NativeSymbol = "ELF";
        private long DepositAmount = 10_00000000;
        private long amount = 1000_00000000;
        private string pullRequestUrl = "A.github.com";
        private string commitId = "http://xxx.com";

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("DAOContractTest_");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env1-main");
            NodeManager = new NodeManager(RpcUrl);
            AuthorityManager = new AuthorityManager(NodeManager);

            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
            _profitContract = _genesisContract.GetProfitContract(InitAccount);
            _parliament = _genesisContract.GetParliamentContract(InitAccount);
            _association = _genesisContract.GetAssociationAuthContract(InitAccount);
            _referendum = _genesisContract.GetReferendumAuthContract(InitAccount);
//            _daoContract = new DAOContract(NodeManager, InitAccount);
//            Logger.Info($"CentreAsset contract : {_daoContract}");

            _daoContract = new DAOContract(NodeManager, InitAccount,
                "2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS");
            _admDaoContractStub =
                _daoContract.GetTestStub<DAOContractContainer.DAOContractStub>(InitAccount);
            _daoContractStub =
                _daoContract.GetTestStub<DAOContractContainer.DAOContractStub>(TestAccount);
            _tokenContractStub = _tokenContract.GetTestStub<TokenContractContainer.TokenContractStub>(InitAccount);
            _tokenContract.TransferBalance(InitAccount, TestAccount, 1000_00000000, "ELF");
//            AsyncHelper.RunSync(InitializeDAOContract);
        }

        #region dao 

        [TestMethod]
        public async Task ProposeJoin()
        {
            var result = _tokenContract.ApproveToken(TestAccount, _daoContract.ContractAddress, 10_00000000);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var userBalance = _tokenContract.GetUserBalance(TestAccount);
            var joinResult = AuthorityManager.ExecuteTransactionWithAuthority(_daoContract.ContractAddress,
                nameof(DAOMethod.ProposeJoin), TestAccount.ConvertAddress(), TestAccount);
            joinResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var member = await _daoContractStub.GetDAOMemberList.CallAsync(new Empty());
            member.Value.Contains(TestAccount.ConvertAddress()).ShouldBeTrue();

            var userAfterBalance = _tokenContract.GetUserBalance(TestAccount);
            userAfterBalance.ShouldBeLessThan(userBalance - DepositAmount);
        }

        [TestMethod]
        public async Task Quite()
        {
            var userBalance = _tokenContract.GetUserBalance(InitAccount);
            var quite = await _admDaoContractStub.Quit.SendAsync(new Empty());
            quite.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var member = await _admDaoContractStub.GetDAOMemberList.CallAsync(new Empty());
            member.Value.Contains(InitAccount.ConvertAddress()).ShouldBeFalse();

            var userAfterBalance = _tokenContract.GetUserBalance(InitAccount);
            userAfterBalance.ShouldBe(userBalance + DepositAmount);
        }

        [TestMethod]
        public async Task ProposeExpel()
        {
            var userBalance = _tokenContract.GetUserBalance(TestAccount);
            var expelResult = AuthorityManager.ExecuteTransactionWithAuthority(_daoContract.ContractAddress,
                nameof(DAOMethod.ProposeExpel), TestAccount.ConvertAddress(), InitAccount);
            expelResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var member = await _daoContractStub.GetDAOMemberList.CallAsync(new Empty());
            member.Value.Contains(TestAccount.ConvertAddress()).ShouldBeFalse();

            var userAfterBalance = _tokenContract.GetUserBalance(TestAccount);
            userAfterBalance.ShouldBe(userBalance);
        }

        [TestMethod]
        public async Task AdjustProposalReleaseThreshold()
        {
            var result =
                await _daoContractStub.AdjustProposalReleaseThreshold.SendAsync(new DAOProposalReleaseThreshold
                {
                    MaximalAbstentionThreshold = 1,
                    MinimalApprovalThreshold = 1,
                    MaximalRejectionThreshold = 1,
                    MinimalVoteThreshold = 1
                });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public async Task ProjectPreAudition()
        {
            var referendumOrganization = AuthorityManager.CreateReferendumOrganization();
            var setReferendum = AuthorityManager.ExecuteTransactionWithAuthority(_daoContract.ContractAddress,
                nameof(DAOMethod.SetReferendumOrganizationAddress), referendumOrganization, InitAccount);
            setReferendum.Status.ShouldBe(TransactionResultStatus.Mined);

            var getReferendum = await _daoContractStub.GetReferendumOrganizationAddress.CallAsync(new Empty());
            getReferendum.ShouldBe(referendumOrganization);

            var input = new ProjectPreAuditionInput()
            {
                CommitId = commitId,
                PullRequestUrl = pullRequestUrl
            };
            var proposer = _referendum.GetOrganization(referendumOrganization).ProposerWhiteList.Proposers.First();
            var createProposal = _referendum.CreateProposal(_daoContract.ContractAddress,
                nameof(DAOMethod.ProjectPreAudition),
                input, referendumOrganization, proposer.GetFormatted());
            var approveAmount = _referendum.GetOrganization(referendumOrganization).ProposalReleaseThreshold
                .MinimalVoteThreshold;
            _tokenContract.ApproveToken(proposer.GetFormatted(), _referendum.ContractAddress, approveAmount);
            _referendum.Approve(createProposal, proposer.GetFormatted());
            var release = _referendum.ReleaseProposal(createProposal, proposer.GetFormatted());
            release.Status.ShouldBe(TransactionResultStatus.Mined);

            var projectId = await GetProject();
            var result = await _daoContractStub.GetPreAuditionResult.CallAsync(projectId);
            result.Value.ShouldBeTrue();
        }

        #endregion

        #region developer Invest

        [TestMethod]
        public async Task ProposeProjectToDao()
        {
            var preAuditionHash = Hash.Empty;
            var result = await _daoContractStub.ProposeProjectToDAO.SendAsync(new ProposeProjectInput
            {
                PullRequestUrl = pullRequestUrl,
                CommitId = commitId,
                PreAuditionHash = preAuditionHash
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var proposalId = result.Output;

            var proposalInfo = _association.CheckProposal(proposalId);
            var proposalInput = ProposeProjectInput.Parser.ParseFrom(proposalInfo.Params);
            proposalInput.CommitId.ShouldBe(commitId);

            var members = await _daoContractStub.GetDAOMemberList.CallAsync(new Empty());
            foreach (var member in members.Value)
            {
                if (member.Equals(_daoContract.Contract)) continue;
                _association.ApproveProposal(proposalId, member.GetFormatted());
            }

            var projectId = await GetProject();
            var release = await _daoContractStub.ReleaseProposal.SendAsync(new ReleaseProposalInput
            {
                ProjectId = projectId,
                ProposalId = proposalId,
                OrganizationType = ProposalOrganizationType.DAO
            });
            release.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var projectInfo = await _daoContractStub.GetProjectInfo.CallAsync(projectId);
            projectInfo.Status.ShouldBe(ProjectStatus.Proposed);
            projectInfo.ProjectType.ShouldBe(ProjectType.Grant);
            projectInfo.IsDevelopersAuditionRequired.ShouldBeFalse();
        }

        [TestMethod]
        public async Task ProposeProjectToParliament()
        {
            var projectId = await GetProject();
            var projectInfo = await _daoContractStub.GetProjectInfo.CallAsync(projectId);
            projectInfo.Status.ShouldBe(ProjectStatus.Proposed);

            var result = await _daoContractStub.ProposeProjectToParliament.SendAsync(new ProposeProjectWithBudgetsInput
            {
                ProjectId = projectId,
                BudgetPlans =
                {
                    new BudgetPlan
                    {
                        Index = 0,
                        Amount = amount,
                        IsApprovedByDevelopers = false,
                        Phase = 1,
                        ReceiverAddress = OtherAccount.ConvertAddress(),
                        Symbol = "CPU"
                    },
                    new BudgetPlan
                    {
                        Index = 1,
                        Amount = amount * 2,
                        IsApprovedByDevelopers = false,
                        Phase = 1,
                        ReceiverAddress = OtherAccount.ConvertAddress(),
                        Symbol = NativeSymbol
                    }
                }
            });
            var proposalId = result.Output;
            var miners = AuthorityManager.GetCurrentMiners();
            foreach (var miner in miners)
            {
                _parliament.ApproveProposal(proposalId, miner);
                var proposalInfo = _parliament.CheckProposal(proposalId);
                if (proposalInfo.ToBeReleased) break;
            }

            var release = await _daoContractStub.ReleaseProposal.SendAsync(new ReleaseProposalInput
            {
                ProjectId = projectId,
                ProposalId = proposalId,
                OrganizationType = ProposalOrganizationType.Parliament
            });
            release.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            projectInfo = await _daoContractStub.GetProjectInfo.CallAsync(projectId);
            projectInfo.Status.ShouldBe(ProjectStatus.Approved);
            projectInfo.ProjectType.ShouldBe(ProjectType.Grant);
            projectInfo.ProfitSchemeId.ShouldNotBeNull();
        }

        [TestMethod]
        public async Task Invest()
        {
            var projectId = await GetProject();
            var projectInfo = await _daoContractStub.GetProjectInfo.CallAsync(projectId);
            projectInfo.Status.ShouldBe(ProjectStatus.Approved);
            var converter = _genesisContract.GetTokenConverterStub(InitAccount);
            await converter.Buy.SendAsync(new BuyInput {Symbol = "CPU", Amount = amount * 2});

            _tokenContract.ApproveToken(InitAccount, _daoContract.ContractAddress, amount * 4);
            _tokenContract.ApproveToken(InitAccount, _daoContract.ContractAddress, amount * 4, "CPU");

            var admBalance = _tokenContract.GetUserBalance(InitAccount);
            var admCpuBalance = _tokenContract.GetUserBalance(InitAccount, "CPU");
            var invest1 = await _admDaoContractStub.Invest.SendAsync(new InvestInput
            {
                Amount = amount * 1,
                ProjectId = projectId,
                Symbol = "CPU"
            });
            invest1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            projectInfo = await _daoContractStub.GetProjectInfo.CallAsync(projectId);
            projectInfo.Status.ShouldBe(ProjectStatus.Approved);

            var invest2 = await _admDaoContractStub.Invest.SendAsync(new InvestInput
            {
                Amount = amount * 2,
                ProjectId = projectId,
                Symbol = NativeSymbol
            });
            invest2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var admAfterBalance = _tokenContract.GetUserBalance(InitAccount);
            admAfterBalance.ShouldBe(admBalance - amount * 2);
            admAfterBalance = _tokenContract.GetUserBalance(InitAccount, "CPU");
            admAfterBalance.ShouldBe(admCpuBalance - amount);

            projectInfo = await _daoContractStub.GetProjectInfo.CallAsync(projectId);
            projectInfo.Status.ShouldBe(ProjectStatus.Ready);
        }

        [TestMethod]
        public async Task ProposeDeliver()
        {
            var projectId = await GetProject();
            var projectInfo = await _daoContractStub.GetProjectInfo.CallAsync(projectId);
            projectInfo.Status.ShouldBe(ProjectStatus.Ready);

            var result = await _daoContractStub.ProposeDeliver.SendAsync(new ProposeAuditionInput
            {
                BudgetPlanIndex = 0,
                DeliverCommitId = "http://xxx",
                DeliverPullRequestUrl = "github",
                ProjectId = projectId
            });
            var proposalId = result.Output;

            var members = await _daoContractStub.GetDAOMemberList.CallAsync(new Empty());
            foreach (var member in members.Value)
            {
                if (member.Equals(_daoContract.Contract)) continue;
                _association.ApproveProposal(proposalId, member.GetFormatted());
            }

            var release1 = await _daoContractStub.ReleaseProposal.SendAsync(new ReleaseProposalInput
            {
                ProjectId = projectId,
                ProposalId = proposalId,
                OrganizationType = ProposalOrganizationType.DAO
            });
            release1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var budgetInfo = await _daoContractStub.GetBudgetPlan.CallAsync(new GetBudgetPlanInput
            {
                ProjectId = projectId,
                BudgetPlanIndex = 0
            });

            await Profit(projectId, OtherAccount, budgetInfo.Amount,budgetInfo.Symbol);
        }

        [TestMethod]
        public async Task ProposeRemoveProject()
        {
            var projectId = await GetProject();
            var result = await _daoContractStub.ProposeRemoveProject.SendAsync(projectId);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var proposalId = result.Output;

            var members = await _daoContractStub.GetDAOMemberList.CallAsync(new Empty());
            foreach (var member in members.Value)
            {
                if (member.Equals(_daoContract.Contract)) continue;
                _association.ApproveProposal(proposalId, member.GetFormatted());
            }

            var release = await _daoContractStub.ReleaseProposal.SendAsync(new ReleaseProposalInput
            {
                ProjectId = projectId,
                ProposalId = proposalId,
                OrganizationType = ProposalOrganizationType.DAO
            });
            release.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var projectInfo = await _daoContractStub.GetProjectInfo.CallAsync(projectId);
            projectInfo.ShouldBe(new ProjectInfo());
        }

        #endregion

        #region DAO Bounty

        [TestMethod]
        public async Task ProposeBountyProject()
        {
            var preAuditionHash = Hash.Empty;
            var result = await _admDaoContractStub.ProposeBountyProject.SendAsync(new ProposeProjectInput
            {
                PullRequestUrl = pullRequestUrl,
                CommitId = commitId,
                PreAuditionHash = preAuditionHash,
                IsDevelopersAuditionRequired = true
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var proposalId = result.Output;

            var proposalInfo = _association.CheckProposal(proposalId);
            var proposalInput = ProposeProjectInput.Parser.ParseFrom(proposalInfo.Params);
            proposalInput.CommitId.ShouldBe(commitId);

            var members = await _admDaoContractStub.GetDAOMemberList.CallAsync(new Empty());
            foreach (var member in members.Value)
            {
                if (member.Equals(_daoContract.Contract)) continue;
                _association.ApproveProposal(proposalId, member.GetFormatted());
            }

            var projectId = await GetProject();
            var release = await _admDaoContractStub.ReleaseProposal.SendAsync(new ReleaseProposalInput
            {
                ProjectId = projectId,
                ProposalId = proposalId,
                OrganizationType = ProposalOrganizationType.DAO
            });
            release.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var projectInfo = await _admDaoContractStub.GetProjectInfo.CallAsync(projectId);
            projectInfo.ProjectType.ShouldBe(ProjectType.Bounty);
            projectInfo.Status.ShouldBe(ProjectStatus.Proposed);
            projectInfo.IsDevelopersAuditionRequired.ShouldBeTrue();
        }

        [TestMethod]
        public async Task ProposeIssueBountyProject()
        {
            var projectId = await GetProject();
            var projectInfo = await _daoContractStub.GetProjectInfo.CallAsync(projectId);
            projectInfo.Status.ShouldBe(ProjectStatus.Proposed);

            var receiver = CreateOrganization();
            var result = await _admDaoContractStub.ProposeIssueBountyProject.SendAsync(
                new ProposeProjectWithBudgetsInput
                {
                    ProjectId = projectId,
                    BudgetPlans =
                    {
                        new BudgetPlan
                        {
                            Index = 0,
                            Amount = amount,
                            PaidInAmount = 0,
                            ReceiverAddress = OtherAccount.ConvertAddress(),
                            Symbol = "CPU"
                        },
                        new BudgetPlan
                        {
                            Index = 1,
                            Amount = amount * 2,
                            PaidInAmount = 0,
                            Symbol = NativeSymbol
                        }
                    }
                });
            var proposalId = result.Output;
            var miners = AuthorityManager.GetCurrentMiners();
            foreach (var miner in miners)
            {
                _parliament.ApproveProposal(proposalId, miner);
                var proposalInfo = _parliament.CheckProposal(proposalId);
                if (proposalInfo.ToBeReleased) break;
            }

            var release = await _daoContractStub.ReleaseProposal.SendAsync(new ReleaseProposalInput
            {
                ProjectId = projectId,
                ProposalId = proposalId,
                OrganizationType = ProposalOrganizationType.Parliament
            });
            release.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            projectInfo = await _daoContractStub.GetProjectInfo.CallAsync(projectId);
            projectInfo.Status.ShouldBe(ProjectStatus.Approved);
            projectInfo.ProfitSchemeId.ShouldNotBeNull();
            projectInfo.ProjectType.ShouldBe(ProjectType.Bounty);
            projectInfo.BudgetPlans.First().ReceiverAddress.ShouldBeNull();
        }

        [TestMethod]
        public async Task ProposeTakeOverBountyProject()
        {
            var projectId = await GetProject();
            var projectInfo = await _daoContractStub.GetProjectInfo.CallAsync(projectId);
            projectInfo.Status.ShouldBe(ProjectStatus.Ready);
            projectInfo.ProjectType.ShouldBe(ProjectType.Bounty);

            var result = await _daoContractStub.ProposeTakeOverBountyProject.SendAsync(
                new ProposeTakeOverBountyProjectInput
                {
                    ProjectId = projectId,
                    BudgetPlanIndices = {0,1}
                });
            var proposalId = result.Output;
            var members = await _daoContractStub.GetDAOMemberList.CallAsync(new Empty());
            foreach (var member in members.Value)
            {
                if (member.Equals(_daoContract.Contract)) continue;
                _association.ApproveProposal(proposalId, member.GetFormatted());
            }

            var release = await _daoContractStub.ReleaseProposal.SendAsync(new ReleaseProposalInput
            {
                ProjectId = projectId,
                ProposalId = proposalId,
                OrganizationType = ProposalOrganizationType.DAO
            });
            release.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            projectInfo = await _daoContractStub.GetProjectInfo.CallAsync(projectId);
            projectInfo.Status.ShouldBe(ProjectStatus.Ready);
            projectInfo.ProjectType.ShouldBe(ProjectType.Bounty);

            var budgetPlan = await _daoContractStub.GetBudgetPlan.CallAsync(new GetBudgetPlanInput
            {
                ProjectId = projectId,
                BudgetPlanIndex = 0
            });
            budgetPlan.ReceiverAddress.ShouldBe(TestAccount.ConvertAddress());
        }

        [TestMethod]
        public async Task ProposeTakeOverBountyProjectThroughAssociation()
        {
            var projectId = await GetProject();
            var projectInfo = await _daoContractStub.GetProjectInfo.CallAsync(projectId);
            projectInfo.Status.ShouldBe(ProjectStatus.Ready);
            projectInfo.ProjectType.ShouldBe(ProjectType.Bounty);

            var organization = "2Wfazisu6Lq8XuJUrb7bX19S97KHEzzLWgrfARACYuPrCHWPM2";
            var input = new ProposeTakeOverBountyProjectInput
            {
                ProjectId = projectId,
                BudgetPlanIndices = {0,1}
            };
            var createProposal = _association.CreateProposal(_daoContract.ContractAddress,
                nameof(DAOMethod.ProposeTakeOverBountyProject), input, organization.ConvertAddress(), TestAccount);

            var associationMembers = new[] {ReviewAccount1, ReviewAccount2, ReviewAccount3};
            foreach (var associationMember in associationMembers)
            {
                var balance = _tokenContract.GetUserBalance(associationMember);
                if (balance <= 10_00000000)
                {
                    _tokenContract.TransferBalance(InitAccount, associationMember, 1000_00000000);
                }

                _association.ApproveProposal(createProposal, associationMember);
            }

            var associationRelease = _association.ReleaseProposal(createProposal, TestAccount);
            associationRelease.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var proposalId = ProposalReleased.Parser
                .ParseFrom(ByteString.FromBase64(
                    associationRelease.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed))
                .ProposalId;

            var members = await _daoContractStub.GetDAOMemberList.CallAsync(new Empty());
            foreach (var member in members.Value)
            {
                if (member.Equals(_daoContract.Contract)) continue;
                _association.ApproveProposal(proposalId, member.GetFormatted());
            }

            var release = await _daoContractStub.ReleaseProposal.SendAsync(new ReleaseProposalInput
            {
                ProjectId = projectId,
                ProposalId = proposalId,
                OrganizationType = ProposalOrganizationType.DAO
            });
            release.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            projectInfo = await _daoContractStub.GetProjectInfo.CallAsync(projectId);
            projectInfo.Status.ShouldBe(ProjectStatus.Taken);
            projectInfo.ProjectType.ShouldBe(ProjectType.Bounty);

            var budgetPlan = await _daoContractStub.GetBudgetPlan.CallAsync(new GetBudgetPlanInput
            {
                ProjectId = projectId,
                BudgetPlanIndex = 1
            });
            budgetPlan.ReceiverAddress.ShouldBe(organization.ConvertAddress());
        }

        [TestMethod]
        public async Task ProposeDevelopersAudition()
        {
            var projectId = await GetProject();
            var projectInfo = await _daoContractStub.GetProjectInfo.CallAsync(projectId);
            projectInfo.Status.ShouldBe(ProjectStatus.Taken);

            var result = await _daoContractStub.ProposeDevelopersAudition.SendAsync(new ProposeAuditionInput
            {
                BudgetPlanIndex = 0,
                DeliverCommitId = "http://xxxx",
                DeliverPullRequestUrl = "github",
                ProjectId = projectId
            });
            var proposalId = result.Output;

            _association.ApproveProposal(proposalId, TestAccount);
            var release = await _daoContractStub.ReleaseProposal.SendAsync(new ReleaseProposalInput
            {
                ProjectId = projectId,
                ProposalId = proposalId,
                OrganizationType = ProposalOrganizationType.Developers
            });
            release.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            projectInfo = await _daoContractStub.GetProjectInfo.CallAsync(projectId);
            projectInfo.Status.ShouldBe(ProjectStatus.Taken);
            projectInfo.ProjectType.ShouldBe(ProjectType.Bounty);

            var budgetPlan = await _daoContractStub.GetBudgetPlan.CallAsync(new GetBudgetPlanInput
            {
                ProjectId = projectId,
                BudgetPlanIndex = 1
            });

//            budgetPlan.ReceiverAddress.ShouldBe(TestAccount.ConvertAddress());
            budgetPlan.IsApprovedByDevelopers.ShouldBeTrue();
        }

        [TestMethod]
        public async Task ProposeDeliverBounty()
        {
            var projectId = await GetProject();
            var projectInfo = await _daoContractStub.GetProjectInfo.CallAsync(projectId);
            projectInfo.Status.ShouldBe(ProjectStatus.Taken);
            projectInfo.ProjectType.ShouldBe(ProjectType.Bounty);
//            projectInfo.IsDevelopersAuditionRequired.ShouldBeTrue();
//            projectInfo.BudgetPlans.First().IsApprovedByDevelopers.ShouldBeTrue();

            var result = await _daoContractStub.ProposeDeliver.SendAsync(new ProposeAuditionInput
            {
                BudgetPlanIndex = 0,
                DeliverCommitId = "http://xxxx",
                DeliverPullRequestUrl = "github",
                ProjectId = projectId
            });
            var proposalId = result.Output;

            var members = await _daoContractStub.GetDAOMemberList.CallAsync(new Empty());
            foreach (var member in members.Value)
            {
                if (member.Equals(_daoContract.Contract)) continue;
                _association.ApproveProposal(proposalId, member.GetFormatted());
            }

            var release = await _daoContractStub.ReleaseProposal.SendAsync(new ReleaseProposalInput
            {
                ProjectId = projectId,
                ProposalId = proposalId,
                OrganizationType = ProposalOrganizationType.DAO
            });
            release.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            projectInfo = await _daoContractStub.GetProjectInfo.CallAsync(projectId);
            projectInfo.Status.ShouldBe(ProjectStatus.Taken);
            projectInfo.ProjectType.ShouldBe(ProjectType.Bounty);

            var budgetInfo = await _daoContractStub.GetBudgetPlan.CallAsync(new GetBudgetPlanInput
            {
                ProjectId = projectId,
                BudgetPlanIndex = 0
            });
            
            await Profit(projectId, TestAccount, budgetInfo.Amount,budgetInfo.Symbol);
        }

        [TestMethod]
        public async Task ProposeDeliverBountyThroughAssociation()
        {
            var projectId = await GetProject();
            var projectInfo = await _daoContractStub.GetProjectInfo.CallAsync(projectId);
            projectInfo.Status.ShouldBe(ProjectStatus.Taken);

            var organization = "2Wfazisu6Lq8XuJUrb7bX19S97KHEzzLWgrfARACYuPrCHWPM2";
            var input = new ProposeAuditionInput
            {
                BudgetPlanIndex = 1,
                DeliverCommitId = "http://xxxx",
                DeliverPullRequestUrl = "github",
                ProjectId = projectId
            };

            var createProposal = _association.CreateProposal(_daoContract.ContractAddress,
                nameof(DAOMethod.ProposeDeliver), input, organization.ConvertAddress(), TestAccount);

            var associationMembers = new[] {ReviewAccount1, ReviewAccount2, ReviewAccount3};
            foreach (var associationMember in associationMembers)
            {
                var balance = _tokenContract.GetUserBalance(associationMember);
                if (balance <= 10_00000000)
                {
                    _tokenContract.TransferBalance(InitAccount, associationMember, 1000_00000000);
                }

                _association.ApproveProposal(createProposal, associationMember);
            }

            var associationRelease = _association.ReleaseProposal(createProposal, TestAccount);
            associationRelease.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var proposalId = ProposalReleased.Parser
                .ParseFrom(ByteString.FromBase64(
                    associationRelease.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed))
                .ProposalId;
            var members = await _daoContractStub.GetDAOMemberList.CallAsync(new Empty());
            foreach (var member in members.Value)
            {
                if (member.Equals(_daoContract.Contract)) continue;
                _association.ApproveProposal(proposalId, member.GetFormatted());
            }

            var release = await _daoContractStub.ReleaseProposal.SendAsync(new ReleaseProposalInput
            {
                ProjectId = projectId,
                ProposalId = proposalId,
                OrganizationType = ProposalOrganizationType.DAO
            });
            release.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            projectInfo = await _daoContractStub.GetProjectInfo.CallAsync(projectId);
            projectInfo.ProjectType.ShouldBe(ProjectType.Bounty);
            projectInfo.Status.ShouldBe(ProjectStatus.Delivered);
            
            var budgetInfo = await _daoContractStub.GetBudgetPlan.CallAsync(new GetBudgetPlanInput
            {
                ProjectId = projectId,
                BudgetPlanIndex = 1
            });
            await ProfitThroughAssociation(projectId, organization, budgetInfo.Amount,budgetInfo.Symbol);
        }

        #endregion

        [TestMethod]
        public async Task GetBudgetPlan()
        {
            var projectId = await GetProject();

            var info = await _daoContractStub.GetBudgetPlan.CallAsync(new GetBudgetPlanInput
            {
                ProjectId = projectId,
                BudgetPlanIndex = 1
            });
            var budgetAmount = info.Amount;
            budgetAmount.ShouldBe(amount * 2);
            var paidInAmount = info.PaidInAmount;
            paidInAmount.ShouldBe(amount * 2 - amount / 2);
        }

        private async Task InitializeDAOContract()
        {
            var deposit = new DepositInfo
            {
                Symbol = NativeSymbol,
                Amount = DepositAmount
            };
            var result = await _admDaoContractStub.Initialize.SendAsync(new InitializeInput
            {
                DepositInfo = deposit
//                InitialMemberList = { InitAccount.ConvertAddress(),TestAccount.ConvertAddress(),OtherAccount.ConvertAddress()}
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var info = await _daoContractStub.GetDepositInfo.CallAsync(new Empty());
            info.Amount.ShouldBe(DepositAmount);
            info.Symbol.ShouldBe(NativeSymbol);
        }

        private async Task<Hash> GetProject()
        {
            var preAuditionHash = Hash.Empty;
            var projectId = await _daoContractStub.CalculateProjectId.CallAsync(new ProposeProjectInput
            {
                PullRequestUrl = pullRequestUrl,
                CommitId = commitId
            });

            return projectId;
        }

        private async Task Profit(Hash projectId, string account, long a,string symbol)
        {
            var projectInfo = await _daoContractStub.GetProjectInfo.CallAsync(projectId);
            var profitSchemeId = projectInfo.ProfitSchemeId;

            _tokenContract.TransferBalance(InitAccount, account, 10_00000000);
            var userBalance = _tokenContract.GetUserBalance(account,symbol);
            _profitContract.SetAccount(account);
            var result = _profitContract.ExecuteMethodWithResult(ProfitMethod.ClaimProfits, new ClaimProfitsInput
            {
                SchemeId = profitSchemeId
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var fee = result.GetDefaultTransactionFee();
            var userAfterBalance = _tokenContract.GetUserBalance(account,symbol);
            if(symbol == NativeSymbol)
                userAfterBalance.ShouldBe(userBalance + a - fee);
            else
                userAfterBalance.ShouldBe(userBalance + a);
        }

        private async Task ProfitThroughAssociation(Hash projectId, string account, long a,string symbol)
        {
            var projectInfo = await _daoContractStub.GetProjectInfo.CallAsync(projectId);
            var profitSchemeId = projectInfo.ProfitSchemeId;

            var userBalance = _tokenContract.GetUserBalance(account,symbol);
            var input = new ClaimProfitsInput
            {
                SchemeId = profitSchemeId
            };
            var createProposal = _association.CreateProposal(_profitContract.ContractAddress,
                nameof(ProfitMethod.ClaimProfits), input, account.ConvertAddress(), TestAccount);
            var associationMembers = new[] {ReviewAccount1, ReviewAccount2, ReviewAccount3};
            foreach (var associationMember in associationMembers)
            {
                var balance = _tokenContract.GetUserBalance(associationMember);
                if (balance <= 10_00000000)
                {
                    _tokenContract.TransferBalance(InitAccount, associationMember, 1000_00000000);
                }
                _association.ApproveProposal(createProposal, associationMember);
            }

            var associationRelease = _association.ReleaseProposal(createProposal, TestAccount);
            associationRelease.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var userAfterBalance = _tokenContract.GetUserBalance(account,symbol);
            userAfterBalance.ShouldBe(userBalance + a);
        }

        //2Wfazisu6Lq8XuJUrb7bX19S97KHEzzLWgrfARACYuPrCHWPM2
        public Address CreateOrganization()
        {
            var result = _association.ExecuteMethodWithResult(AssociationMethod.CreateOrganization,
                new CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MaximalAbstentionThreshold = 2,
                        MaximalRejectionThreshold = 1,
                        MinimalApprovalThreshold = 1,
                        MinimalVoteThreshold = 3
                    },
                    OrganizationMemberList = new OrganizationMemberList
                    {
                        OrganizationMembers =
                        {
                            ReviewAccount1.ConvertAddress(), ReviewAccount2.ConvertAddress(),
                            ReviewAccount3.ConvertAddress(), TestAccount.ConvertAddress()
                        }
                    },
                    ProposerWhiteList = new ProposerWhiteList
                    {
                        Proposers = {TestAccount.ConvertAddress()}
                    }
                });
            var organizationAddress =
                Address.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            Logger.Info($"organization address is : {organizationAddress}");
            var fee = result.GetDefaultTransactionFee();
            Logger.Info($"Transaction fee is {fee}");

            return organizationAddress;
        }
    }
}