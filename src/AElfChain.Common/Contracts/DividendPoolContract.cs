using System;
using AElf.Client.Dto;
using AElf.Contracts.Treasury;
using AElf.Standards.ACS10;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using Awaken.Contracts.PoolTwoContract;
using Gandalf.Contracts.DividendPoolContract;
using Google.Protobuf.WellKnownTypes;
using InitializeInput = Gandalf.Contracts.DividendPoolContract.InitializeInput;
using PendingOutput = Gandalf.Contracts.DividendPoolContract.PendingOutput;
using PoolInfoStruct = Gandalf.Contracts.DividendPoolContract.Pool;
using UserInfoStruct = Gandalf.Contracts.DividendPoolContract.User;


namespace AElfChain.Common.Contracts
{
    public enum DividendPoolMethod
    {
        Initialize,
        StartBlock,
        Add,
        PoolLength,
        PoolInfo,
        AddToken,
        NewReward,
        GetTokenListLength,
        Cycle,
        PerBlock,
        EndBlock,
        AccPerShare,
        Set,
        SetCycle,
        Deposit,
        Withdraw,
        UserInfo,
        TotalAllocPoint,
        Pending,
        RewardDebt,
        TokenList
    }

    public class DividendPoolContract :BaseContract<DividendPoolMethod>
    {

        public DividendPoolContract(INodeManager nodeManager, string callAddress) : base(nodeManager,
            "Gandalf.Contracts.DividendPool", callAddress)
        {
        }

        public DividendPoolContract(INodeManager nodeManager, string callAddress, string contractAddress) : base(
            nodeManager,
            contractAddress)
        {
            SetAccount(callAddress);
        }


        public TransactionResultDto Initialize(int cycle)
        {
            return ExecuteMethodWithResult(DividendPoolMethod.Initialize, new InitializeInput
            {
                Cycle = cycle,
            });
        }

        public TransactionResultDto AddToken(string tokenSymbol)
        {
            return ExecuteMethodWithResult(DividendPoolMethod.AddToken, new Token
            {
                Value = tokenSymbol
            });
        }
        public TransactionResultDto Add(long allocationPoint, string tokenSymbol)
        {
            return ExecuteMethodWithResult(DividendPoolMethod.Add, new AddPoolInput
            {
                AllocationPoint = allocationPoint,
                TokenSymbol = tokenSymbol,
                WithUpdate = true
            });
        }

        public TransactionResultDto NewReward(NewRewardInput newRewardInput)
        {
            return ExecuteMethodWithResult(DividendPoolMethod.NewReward, newRewardInput);
        }

        public TransactionResultDto Set(SetPoolInput setpoolinput)
        {
            return ExecuteMethodWithResult(DividendPoolMethod.Set, setpoolinput);
        }

        public TransactionResultDto Deposit(int pid, BigIntValue amount)
        {
            return ExecuteMethodWithResult(DividendPoolMethod.Deposit, new TokenOptionInput
            {
                Pid = pid,
                Amount = amount
            });
        }

        public TransactionResultDto Withdraw(int pid, BigIntValue amount)
        {
            return ExecuteMethodWithResult(DividendPoolMethod.Withdraw, new TokenOptionInput
            {
                Pid = pid,
                Amount = amount
            });
        }

        public TransactionResultDto SetCycle(int cycle)
        {
            return ExecuteMethodWithResult(DividendPoolMethod.SetCycle, new Int32Value
            {
                Value = cycle
            });
        }
        public Int64Value StartBlock()
        {
            return CallViewMethod<Int64Value>(DividendPoolMethod.StartBlock, new Empty());
        }

        public Int32Value PoolLength()
        {
            return CallViewMethod<Int32Value>(DividendPoolMethod.PoolLength, new Empty());
        }

        public PoolInfoStruct PoolInfo(int pid)
        {
            return CallViewMethod<PoolInfoStruct>(DividendPoolMethod.PoolInfo, new Int32Value
            {
                Value = pid
            });
            
        }

        public UserInfoStruct UserInfo(int pid, string user)
        {
            return CallViewMethod<UserInfoStruct>(DividendPoolMethod.UserInfo,
                new Gandalf.Contracts.DividendPoolContract.UserInfoInput
                {
                    Pid = pid,
                    User = user.ConvertAddress()
                });
        }

        public Int64Value Cycle()
        {
            return CallViewMethod<Int64Value>(DividendPoolMethod.Cycle, new Empty());
        }

        public Int64Value EndBlock()
        {
            return CallViewMethod<Int64Value>(DividendPoolMethod.EndBlock, new Empty());
        }

        public BigIntValue AccPerShare(int pid, string tokensymbol)
        {
            return CallViewMethod<BigIntValue>(DividendPoolMethod.AccPerShare, new AccPerShareInput
            {
                Pid = pid,
                Token = tokensymbol
            });
        }

        public Int64Value TotalAllocPoint()
        {
            return CallViewMethod<Int64Value>(DividendPoolMethod.TotalAllocPoint, new Empty());
        }

        public BigIntValue PerBlock(string token)
        {
            return CallViewMethod<BigIntValue>(DividendPoolMethod.PerBlock, new StringValue
            {
                Value = token
            });
        }

        public PendingOutput Pending(int pid, string user)
        {
            return CallViewMethod<PendingOutput>(DividendPoolMethod.Pending,
                new Gandalf.Contracts.DividendPoolContract.PendingInput
                {
                    Pid = pid,
                    User = user.ConvertAddress()
                });
        }
        
        public BigIntValue RewardDebt(int pid, string user, string token)
        {
            return CallViewMethod<BigIntValue>(DividendPoolMethod.RewardDebt, new RewardDebtInput
            {
                Pid = pid,
                User = user.ConvertAddress(),
                Token = token
            }) ?? new BigIntValue(0);

        }

    }
}