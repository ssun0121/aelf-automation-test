using System;
using AElf.Client.Dto;
using AElf.Contracts.Ido;
using AElf.Contracts.Treasury;
using AElf.Standards.ACS10;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
{
    public enum IdoMethod
    {
        //Action
        Initialize,
        Register,
        AddWhitelists,
        RemoveWhitelists,
        EnableWhitelist,
        DisableWhitelist,
        UpdateAdditionalInfo,
        Cancel,
        NextPeriod,
        Invest,
        UnInvest,
        LockLiquidity,
        Withdraw,
        ClaimLiquidatedDamage,
        Claim,
        SetWhitelistId,
        
        //View


        GetProjectInfo,
        GetWhitelistId,
        GetInvestDetail,
        GetProjectListInfo,
        GetProfitDetail
    }

    public class IdoContract :BaseContract<IdoMethod>
    {

        public IdoContract(INodeManager nodeManager, string callAddress) : base(nodeManager,
            "AElf.Contracts.Ido", callAddress)
        {
        }

        public IdoContract(INodeManager nodeManager, string callAddress, string contractAddress) : base(
            nodeManager,
            contractAddress)
        {
            SetAccount(callAddress);
        }

        public TransactionResultDto Register(RegisterInput registerInput)
        {
            return ExecuteMethodWithResult(IdoMethod.Register, registerInput);
        }

        public TransactionResultDto DisableWhitelist(Hash projectId)
        {
            return ExecuteMethodWithResult(IdoMethod.DisableWhitelist, projectId);
        }

        public TransactionResultDto EnableWhitelist(Hash projectId)
        {
            return ExecuteMethodWithResult(IdoMethod.EnableWhitelist, projectId);
        }

        public TransactionResultDto AddWhitelist(string creator, AddWhitelistsInput addWhitelistsInput)
        {
            var tester = GetNewTester(creator);
            return tester.ExecuteMethodWithResult(IdoMethod.AddWhitelists, addWhitelistsInput);
        }

        public TransactionResultDto RemoveWhitelist(RemoveWhitelistsInput removeWhitelistsInput)
        {
            return ExecuteMethodWithResult(IdoMethod.RemoveWhitelists, removeWhitelistsInput);
        }
        
        public TransactionResultDto Invest(string user, string currency, Int64 investAmount, Hash projectId)
        {
            var tester = GetNewTester(user);
            return tester.ExecuteMethodWithResult(IdoMethod.Invest, new InvestInput
            {
                Currency = currency,
                InvestAmount = investAmount,
                ProjectId = projectId
            });
        }
        
        public TransactionResultDto UnInvest(string user, Hash projectId)
        {
            var tester = GetNewTester(user);
            return tester.ExecuteMethodWithResult(IdoMethod.UnInvest, projectId);
        }



        public TransactionResultDto LockLiquidity(Hash projectId)
        {
            return ExecuteMethodWithResult(IdoMethod.LockLiquidity, projectId);
        }

        public TransactionResultDto Claim(Hash projectId, string user)
        {
            return ExecuteMethodWithResult(IdoMethod.Claim, new ClaimInput
            {
                ProjectId = projectId,
                User = user.ConvertAddress()
            });
        }

        public TransactionResultDto NextPeriod(Hash projectId)
        {
            return ExecuteMethodWithResult(IdoMethod.NextPeriod, projectId);
        }

        public TransactionResultDto UpdateAdditionalInfo(Hash projectId, AdditionalInfo addtionalInfo)
        {
            return ExecuteMethodWithResult(IdoMethod.UpdateAdditionalInfo, new UpdateAdditionalInfoInput
            {
                ProjectId = projectId,
                AdditionalInfo = addtionalInfo
            });
        }

        public TransactionResultDto Withdraw(string creator, Hash projectId)
        {
            var tester = GetNewTester(creator);
            return tester.ExecuteMethodWithResult(IdoMethod.Withdraw, projectId);
        }
        
        public ProjectInfo GetProjectInfo(Hash projectId)
        {
            return CallViewMethod<ProjectInfo>(IdoMethod.GetProjectInfo, projectId);
        }

        public InvestDetail GetInvestDetail(Hash projectId, string user)
        {
            return CallViewMethod<InvestDetail>(IdoMethod.GetInvestDetail, new GetInvestDetailInput
            {
                ProjectId = projectId,
                User = user.ConvertAddress()
            });
        }

        public ProjectListInfo GetProjectListInfo(Hash projectId)
        {
            return CallViewMethod<ProjectListInfo>(IdoMethod.GetProjectListInfo, projectId);
        }

        public ProfitDetail GetProfitDetail(Hash projectId, string user)
        {
            return CallViewMethod<ProfitDetail>(IdoMethod.GetProfitDetail, new GetProfitDetailInput
            {
                ProjectId = projectId,
                User = user.ConvertAddress()

            });
        }

        public Hash GetWhitelistId(Hash projectId)
        {
            return CallViewMethod<Hash>(IdoMethod.GetWhitelistId, projectId);
        }
        
        
        
        
    }
}