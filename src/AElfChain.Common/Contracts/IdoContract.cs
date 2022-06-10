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
        GetProjectListInfo
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

        public TransactionResultDto AddWhitelist(AddWhitelistsInput addWhitelistsInput)
        {
            return ExecuteMethodWithResult(IdoMethod.AddWhitelists, addWhitelistsInput);
        }

        public TransactionResultDto Invest(string currency, Int64 investAmount, Hash projectId)
        {
            return ExecuteMethodWithResult(IdoMethod.Invest, new InvestInput
            {
                Currency = currency,
                InvestAmount = investAmount,
                ProjectId = projectId
            });
        }

        public TransactionResultDto LockLiquidity(Hash projectId)
        {
            return ExecuteMethodWithResult(IdoMethod.LockLiquidity, projectId);
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
        
        
    }
}