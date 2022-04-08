using System;
using System.Linq;
using System.Threading;
using AElf.Contracts.QuadraticFunding;
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
using Volo.Abp.Threading;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class QuadraticFundingContractTest
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }

        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;
        private QuadraticFundingContract _quadraticFundingContract;


        private string quadratic = "2WHXRoLRjbUTDQsuqR5CntygVfnDb125qdJkudev4kVNbLhTdG"; //合约地址

        private string RpcUrl { get; } = "192.168.67.166:8000";
        private string InitAccount { get; } = "nn659b9X1BLhnu5RWmEUbuuV7J9QKVVSN54j9UmeCbF3Dve5D";
        private string TestAccount1 { get; } = "2bs2uYMECtHWjB57RqgqQ3X2LrxgptWHtzCqGEU11y45aWimh4";
        private string TestAccount2 { get; } = "tb4qsxbzi4HLwSS4PM19yF89ww4nA1ELJXHP1mXB4ZPnNjCYc";

        private string Voter { get; } = "YvCiXDrwWr3FAqFxCTqdGNF1CitUSLb1ojCpm9Aeo9HTKGAoA";
        private string Voter2 { get; } = "UZd2HWnZKkECcxh9fJYVKHowVtaE4xMi84UZdZYns9zchvKgR";


        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("TokenContractTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-new-env-main");
            // NodeInfoHelper.SetConfig("nodes-online-stage-main");
            NodeManager = new NodeManager(RpcUrl);
            AuthorityManager = new AuthorityManager(NodeManager, InitAccount);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
            _quadraticFundingContract = quadratic.Equals("")
                ? new QuadraticFundingContract(NodeManager, InitAccount)
                : new QuadraticFundingContract(NodeManager, InitAccount, quadratic);
        }

        [TestMethod]
        public void Transfer()
        {
            var account = "RctxRiJUytdyzqNZWqm1PpYGw2eNR83bknn17c1p2bbmVBLQy";
            _tokenContract.TransferBalance(InitAccount, account, 10000_00000000);
        }

        [TestMethod]
        public void InitializeContract()
        {
            var interval =  86400;
            var basicVoting = 100000000;

            var result =
                _quadraticFundingContract.Initialize(InitAccount, interval, basicVoting, "ELF",
                    InitAccount.ConvertAddress());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _quadraticFundingContract.GetInterval().ShouldBe(interval);
            _quadraticFundingContract.GetVotingUnit().ShouldBe(basicVoting);

            Thread.Sleep(10000);
            var resultError =
                _quadraticFundingContract.Initialize(InitAccount, interval * 2, basicVoting * 2, "ELF",
                    InitAccount.ConvertAddress());
            resultError.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            resultError.Error.ShouldContain("Already initialized.");
        }

        #region OnlyOwner

        [TestMethod]
        public void SetConfig()
        {
            var changeInterval = 6000;
            var changeVoting = 200000000;
            var changeTxPoint = 200;
            var resultInterval = _quadraticFundingContract.SetInterval(changeInterval);
            resultInterval.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var resultVoting = _quadraticFundingContract.SetVotingUnit(changeVoting);
            resultVoting.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var resultTxPoint = _quadraticFundingContract.SetTaxPoint(changeTxPoint);
            resultTxPoint.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            _quadraticFundingContract.GetInterval().ShouldBe(changeInterval);
            _quadraticFundingContract.GetVotingUnit().ShouldBe(changeVoting);
            _quadraticFundingContract.GetTaxPoint().ShouldBe(changeTxPoint);
        }

        [TestMethod]
        public void SetConfig_NoPermission()
        {
            var changeInterval = 6000;
            _quadraticFundingContract.SetAccount(TestAccount1);
            var resultInterval = _quadraticFundingContract.SetInterval(changeInterval);
            resultInterval.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            resultInterval.Error.ShouldContain("Assertion failed!");
        }

        [TestMethod]
        public void SetTxPoint_OverFlow()
        {
            var changeTxPoint = 5001;
            var resultTxPoint = _quadraticFundingContract.SetTaxPoint(changeTxPoint);
            resultTxPoint.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            resultTxPoint.Error.ShouldContain("Exceeded max tax point: 5000");
        }

        [TestMethod]
        public void ChangeOwner()
        {
            var result = _quadraticFundingContract.ChangeOwner(TestAccount1);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            Thread.Sleep(10000);
            var resultError = _quadraticFundingContract.ChangeOwner(TestAccount1);
            resultError.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            resultError.Error.ShouldContain("Assertion failed!");

            _quadraticFundingContract.SetAccount(TestAccount1);
            var resultRevert = _quadraticFundingContract.ChangeOwner(InitAccount);
            resultRevert.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void RoundStart()
        {
            var result = _quadraticFundingContract.RoundStart(InitAccount);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var round = _quadraticFundingContract.GetCurrentRound();
            Logger.Info($"Current round: {round}");

            var roundInfo = _quadraticFundingContract.GetRoundInfo(round);

            var height = result.BlockNumber;
            var block = AsyncHelper.RunSync(() => NodeManager.ApiClient.GetBlockByHeightAsync(height));
            var interval = _quadraticFundingContract.GetInterval();
            roundInfo.StartFrom.ShouldBe(Timestamp.FromDateTime(block.Header.Time));
            roundInfo.EndAt.ShouldBe(Timestamp.FromDateTime(block.Header.Time.AddSeconds(interval)));
        }

        [TestMethod]
        public void RoundOver()
        {
            var beforeRound = _quadraticFundingContract.GetCurrentRound();
            Logger.Info($"Current round: {beforeRound}");

            var result = _quadraticFundingContract.RoundOver(InitAccount);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var round = _quadraticFundingContract.GetCurrentRound();
            Logger.Info($"Current round: {round}");
            round.ShouldBe(beforeRound + 1);

            var roundInfo = _quadraticFundingContract.GetRoundInfo(round);
            roundInfo.Support.ShouldBe(0);
            roundInfo.PreTaxSupport.ShouldBe(0);
            roundInfo.StartFrom.ShouldBeNull();
            roundInfo.EndAt.ShouldBeNull();
        }

        [TestMethod]
        public void BanProject()
        {
            var round = _quadraticFundingContract.GetCurrentRound();
            var allProject = _quadraticFundingContract.GetAllProjects(round);

            var roundInfo = _quadraticFundingContract.GetRankingList(round);
            Logger.Info(roundInfo);


            var projectId = allProject.First();
            var isBan = false;
            var projectInfo = _quadraticFundingContract.GetProjectOf(projectId);
            Logger.Info(projectInfo);

            var banProject = _quadraticFundingContract.BanProject(projectId, isBan);
            banProject.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var logEvent = banProject.Logs.First(l => l.Name.Equals("ProjectBanned")).NonIndexed;
            var projectBanned = ProjectBanned.Parser.ParseFrom(ByteString.FromBase64(logEvent));
            projectBanned.Ban.ShouldBe(isBan);
            projectBanned.Project.ShouldBe(projectId);

            var afterRoundInfo = _quadraticFundingContract.GetRankingList(round);
            Logger.Info(afterRoundInfo);

            if (isBan)
            {
                afterRoundInfo.Support.First().ShouldBe(0);
            }
            else
            {
                afterRoundInfo.Support.First().ShouldNotBe(0);
            }
        }

        [TestMethod]
        public void Withdraw()
        {
            var tax = _quadraticFundingContract.GetTax();
            Logger.Info(tax);
            var balance = _tokenContract.GetUserBalance(InitAccount);
            var result = _quadraticFundingContract.Withdraw();
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = _tokenContract.GetUserBalance(InitAccount);
            afterBalance.ShouldBe(balance + tax);
        }

        #endregion

        #region Other

        //UploadProject
        //Donate
        //Vote
        //TakeOutGrants

        //WukLYH288b18RB8meqcsSfRPVf2vr51mHggYF95FZPvJEB2y8
        [TestMethod]
        public void UploadProject()
        {
            var round = _quadraticFundingContract.GetCurrentRound();
            var roundInfo = _quadraticFundingContract.GetRoundInfo(round);
            var uploader = NodeManager.NewAccount();
            var bid = CommonHelper.GenerateRandomNumber(10000, 99999);
            var calculateProjectId = _quadraticFundingContract.CalculateProjectId(bid, uploader);
            var index = long.Parse(calculateProjectId.Value);
            _quadraticFundingContract.SetAccount(uploader);
            if (roundInfo.EndAt == null)
            {
                var result = _quadraticFundingContract.UploadProject(index);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain($"Round {round} not started.");
            }
            else if (roundInfo.EndAt < Timestamp.FromDateTime(DateTime.UtcNow))
            {
                var result = _quadraticFundingContract.UploadProject(index);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
                result.Error.ShouldContain($"Round {round} already ended.");
            }
            else
            {
                var result = _quadraticFundingContract.UploadProject(index);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var logEvent =
                    ProjectUploaded.Parser.ParseFrom(
                        ByteString.FromBase64(result.Logs.First(l => l.Name.Equals("ProjectUploaded")).NonIndexed));
                logEvent.Round.ShouldBe(round);
                logEvent.Uploader.ShouldBe(uploader.ConvertAddress());
                var projectId = _quadraticFundingContract.CalculateProjectId(bid, uploader);
                logEvent.ProjectId.ShouldBe(projectId.Value);
                Logger.Info(projectId);

                var allProject = _quadraticFundingContract.GetAllProjects(round);
                allProject.ShouldContain(projectId.Value);

                var projectOf = _quadraticFundingContract.GetProjectOf(projectId.Value);
                Logger.Info(projectOf);
            }
        }

        [TestMethod]
        public void Donate()
        {
            var round = _quadraticFundingContract.GetCurrentRound();
            Logger.Info($"Current round: {round}");

            var roundInfo = _quadraticFundingContract.GetRoundInfo(round);
            var donateAmount = 10000_00000000;
            var taxPoint = _quadraticFundingContract.GetTaxPoint();
            var fee = donateAmount.Mul(taxPoint).Div(10000);
            _tokenContract.ApproveToken(InitAccount, _quadraticFundingContract.ContractAddress, donateAmount);
            var result = _quadraticFundingContract.Donate(donateAmount);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);


            var afterRoundInfo = _quadraticFundingContract.GetRoundInfo(round);
            afterRoundInfo.Support.ShouldBe(roundInfo.Support + donateAmount - fee);
            afterRoundInfo.PreTaxSupport.ShouldBe(roundInfo.PreTaxSupport + donateAmount);
            Logger.Info(roundInfo.Support);
            Logger.Info(roundInfo.EndAt);
            Logger.Info(roundInfo.StartFrom);
            Logger.Info(roundInfo.PreTaxSupport);
        }

        [TestMethod]
        public void Vote_Error()
        {
            {
                var round = _quadraticFundingContract.GetCurrentRound();
                var allProject = _quadraticFundingContract.GetAllProjects(round - 1);
                var votesAmount = 1;
                var voteResult = _quadraticFundingContract.Vote(allProject.First(), votesAmount);
                voteResult.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                voteResult.Error.ShouldContain($"Project with id {allProject.First()} isn't in current round {round}");
            }
            {
                var round = _quadraticFundingContract.GetCurrentRound();
                var allProject = _quadraticFundingContract.GetAllProjects(round);
                var votesAmount = 1;
                var voteResult = _quadraticFundingContract.Vote(allProject.First(), votesAmount);
                voteResult.Status.ConvertTransactionResultStatus()
                    .ShouldBe(TransactionResultStatus.NodeValidationFailed);
                voteResult.Error.ShouldContain("Assertion failed!");
            }
        }

        //pgFmXyP34Veb28pNYgGpR4tTEZg2V181J8yTRBn5SWQNZekWQ
        //
        [TestMethod]
        public void Vote_First_One_Votes()
        {
            var round = _quadraticFundingContract.GetCurrentRound();
            var allProject = _quadraticFundingContract.GetAllProjects(round);
            var projectId = allProject[1];
            var originProjectOf = _quadraticFundingContract.GetProjectOf(projectId);
            var originTax = _quadraticFundingContract.GetTax();
            var voter = NodeManager.NewAccount("12345678");
            long votesAmount = 2;
            var votingUnit = _quadraticFundingContract.GetVotingUnit();
            var txPoints = _quadraticFundingContract.GetTaxPoint();
            var balance = _tokenContract.GetUserBalance(voter);
            var totalAmount = votingUnit.Mul(votesAmount);
            //cost
            if (balance < totalAmount)
            {
                var result = _tokenContract.TransferBalance(InitAccount, voter, totalAmount * 2);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }

            _quadraticFundingContract.SetAccount(voter);
            var approve = _tokenContract.ApproveToken(voter, _quadraticFundingContract.ContractAddress, totalAmount);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            balance = _tokenContract.GetUserBalance(voter);

            var voteResult = _quadraticFundingContract.Vote(projectId, votesAmount);
            voteResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = _tokenContract.GetUserBalance(voter);
            afterBalance.ShouldBe(balance - totalAmount);

            var voteFee = txPoints.Mul(totalAmount).Div(10000);
            var supportArea = votesAmount.Mul(originProjectOf.TotalVotes - 0);

            var projectOf = _quadraticFundingContract.GetProjectOf(projectId);
            Logger.Info(projectOf);
            projectOf.Round.ShouldBe(round);
            projectOf.TotalVotes.ShouldBe(originProjectOf.TotalVotes + votesAmount);
            projectOf.Grants.ShouldBe(originProjectOf.Grants.Add(totalAmount - voteFee));
            projectOf.SupportArea.ShouldBe(originProjectOf.SupportArea + supportArea);

            var tax = _quadraticFundingContract.GetTax();
            tax.ShouldBe(originTax + voteFee);
        }

        [TestMethod]
        public void Vote()
        {
            var round = _quadraticFundingContract.GetCurrentRound();
            var allProject = _quadraticFundingContract.GetAllProjects(round);
            var projectId = allProject.Last();
            var originProjectOf = _quadraticFundingContract.GetProjectOf(projectId);
            var originTax = _quadraticFundingContract.GetTax();
            var voter = Voter;
            long votesAmount = 1;
            long alreadyVotes = 1;
            var votingUnit = _quadraticFundingContract.GetVotingUnit();
            var txPoints = _quadraticFundingContract.GetTaxPoint();
            var verifyCost = CalculateCost(votesAmount, alreadyVotes, votingUnit);
            var cost = _quadraticFundingContract.GetVotingCost(voter, projectId, votesAmount);
            verifyCost.ShouldBe(cost.Cost);
            Logger.Info(cost.Cost);
            var balance = _tokenContract.GetUserBalance(voter);
            if (balance < cost.Cost)
            {
                var result = _tokenContract.TransferBalance(InitAccount, voter, cost.Cost * 2);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }

            _quadraticFundingContract.SetAccount(voter);
            var approve = _tokenContract.ApproveToken(voter, _quadraticFundingContract.ContractAddress, cost.Cost);
            approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            balance = _tokenContract.GetUserBalance(voter);

            var voteResult = _quadraticFundingContract.Vote(projectId, votesAmount);
            voteResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = _tokenContract.GetUserBalance(voter);
            afterBalance.ShouldBe(balance - cost.Cost);

            var voteFee = txPoints.Mul(cost.Cost).Div(10000);
            var supportArea = votesAmount.Mul(originProjectOf.TotalVotes - alreadyVotes);

            var projectOf = _quadraticFundingContract.GetProjectOf(projectId);
            Logger.Info(projectOf);
            projectOf.Round.ShouldBe(round);
            projectOf.TotalVotes.ShouldBe(originProjectOf.TotalVotes + votesAmount);
            projectOf.Grants.ShouldBe(originProjectOf.Grants.Add(cost.Cost - voteFee));
            projectOf.SupportArea.ShouldBe(originProjectOf.SupportArea + supportArea);

            var tax = _quadraticFundingContract.GetTax();
            tax.ShouldBe(originTax + voteFee);

            var grandsOf = _quadraticFundingContract.GetGrantsOf(projectId);
            Logger.Info(grandsOf);
            grandsOf.Total.ShouldBe(projectOf.Grants);
        }

        [TestMethod]
        public void VoteAllProject()
        {
            var round = _quadraticFundingContract.GetCurrentRound();
            var allProject = _quadraticFundingContract.GetAllProjects(round);
            var i = 1;
            foreach (var projectId in allProject)
            {
                var originProjectOf = _quadraticFundingContract.GetProjectOf(projectId);
                var originTax = _quadraticFundingContract.GetTax();
                var voter = NodeManager.NewAccount("12345678");
                var votesAmount = i;
                var votingUnit = _quadraticFundingContract.GetVotingUnit();
                var txPoints = _quadraticFundingContract.GetTaxPoint();
                var verifyCost = CalculateCost(votesAmount, 0, votingUnit);
                var cost = _quadraticFundingContract.GetVotingCost(voter, projectId, votesAmount);
                verifyCost.ShouldBe(cost.Cost);
                Logger.Info(cost.Cost);
                var balance = _tokenContract.GetUserBalance(voter);
                if (balance < cost.Cost)
                {
                    var result = _tokenContract.TransferBalance(InitAccount, voter, cost.Cost * 2);
                    result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                }

                _quadraticFundingContract.SetAccount(voter);
                var approve = _tokenContract.ApproveToken(voter, _quadraticFundingContract.ContractAddress, cost.Cost);
                approve.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                balance = _tokenContract.GetUserBalance(voter);

                var voteResult = _quadraticFundingContract.Vote(projectId, votesAmount);
                voteResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var afterBalance = _tokenContract.GetUserBalance(voter);
                afterBalance.ShouldBe(balance - cost.Cost);

                var voteFee = txPoints.Mul(cost.Cost).Div(10000);

                var projectOf = _quadraticFundingContract.GetProjectOf(projectId);
                Logger.Info(projectOf);
                projectOf.Round.ShouldBe(round);
                projectOf.TotalVotes.ShouldBe(originProjectOf.TotalVotes + votesAmount);
                projectOf.Grants.ShouldBe(originProjectOf.Grants.Add(cost.Cost - voteFee));

                var tax = _quadraticFundingContract.GetTax();
                tax.ShouldBe(originTax + voteFee);

                var grandsOf = _quadraticFundingContract.GetGrantsOf(projectId);
                Logger.Info(grandsOf);
                grandsOf.Total.ShouldBe(projectOf.Grants);
                i++;
            }
        }

        [TestMethod]
        public void TakeOutGrants()
        {
            var round = _quadraticFundingContract.GetCurrentRound();
            var allProject = _quadraticFundingContract.GetAllProjects(round);
            var projectId = allProject.Last();
            var projectOf = _quadraticFundingContract.GetProjectOf(projectId);
            var grandsOf = _quadraticFundingContract.GetGrantsOf(projectId);
            Logger.Info(grandsOf);

            var uploader = "FC14nceada3uYPLXG2bSrFEDPntkw3rdnWpebzt7Svdvt7J6e";
            var balance = _tokenContract.GetUserBalance(uploader);
            var grants = projectOf.Grants;
            var takeAmount = grants.Div(2);
            _quadraticFundingContract.SetAccount(uploader);
            var result = _quadraticFundingContract.TakeOutGrants(projectId, takeAmount);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var afterBalance = _tokenContract.GetUserBalance(uploader);
            afterBalance.ShouldBe(balance + takeAmount);

            var afterGrandsOf = _quadraticFundingContract.GetGrantsOf(projectId);
            Logger.Info(afterGrandsOf);
            afterGrandsOf.Rest.ShouldBe(grandsOf.Rest - takeAmount);
        }

        [TestMethod]
        public void GetGrandsOf()
        {
            var round = _quadraticFundingContract.GetCurrentRound();
            var allProject = _quadraticFundingContract.GetAllProjects(round);
            var projectId = allProject.First();
            var grandsOf = _quadraticFundingContract.GetGrantsOf(projectId);
            Logger.Info(grandsOf);
        }

        #endregion

        #region View

        [TestMethod]
        public void CheckCurrentRoundInfo()
        {
            var round = _quadraticFundingContract.GetCurrentRound();
            Logger.Info($"Current round: {round}");

            var roundInfo = _quadraticFundingContract.GetRoundInfo(round);
            Logger.Info(roundInfo.Support);
            Logger.Info(roundInfo.EndAt);
            Logger.Info(roundInfo.StartFrom);
            Logger.Info(roundInfo.PreTaxSupport);
        }

        [TestMethod]
        public void CheckProjectOf()
        {
            var round = _quadraticFundingContract.GetCurrentRound();
            var allProject = _quadraticFundingContract.GetAllProjects(round);
            var projectId = allProject.First();
            var projectOf = _quadraticFundingContract.GetProjectOf(projectId);
            Logger.Info(projectOf);
        }

        [TestMethod]
        public void GetVotingCost()
        {
            var voter = Voter;
            var votesAmount = 2;
            var round = _quadraticFundingContract.GetCurrentRound();
            var allProject = _quadraticFundingContract.GetAllProjects(round);
            var projectId = allProject.First();
            var cost = _quadraticFundingContract.GetVotingCost(voter, projectId, votesAmount);
            Logger.Info(cost.Votable);
        }

        #endregion

        private long CalculateCost(long votes, long alreadyVotes, long votingUnit)
        {
            long cost = 0;
            for (var i = alreadyVotes + 1; i < votes + alreadyVotes + 1; i++)
            {
                cost = cost + i.Mul(votingUnit);
            }

            return cost;
        }
    }
}