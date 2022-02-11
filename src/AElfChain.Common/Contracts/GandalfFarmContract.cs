
using System;
using AElf.Client.Dto;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using Gandalf.Contracts.PoolTwoContract;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
{
    public enum FarmMethod
    {
        Initialize,
        PoolInfo,
        Phase,
        StartBlock,
        HalvingPeriod,
        TotalReward,
        Add,
        DistributeTokenPerBlock,
        Deposit,
        ReDeposit,
        SetFarmPoolOne,
        PoolLength,
        UserInfo,
        IssuedReward,
        Pending,
        endBlock,
        Withdraw,
        TotalAllocPoint,
        Reward
    }

    public class GandalfFarmContract :BaseContract<FarmMethod>
    {

        public GandalfFarmContract(INodeManager nodeManager, string callAddress) : base(nodeManager,
            "Gandalf.Contracts.PoolTwo", callAddress)
        {
        }

        public GandalfFarmContract(INodeManager nodeManager, string callAddress, string contractAddress) : base(
            nodeManager,
            contractAddress)
        {
            SetAccount(callAddress);
        }

        public TransactionResultDto Initialize(string tokenSymbol, BigIntValue tokenPerBlock, long halvingPeriod, long startBlock, BigIntValue totalReward )
        {
            var result = ExecuteMethodWithResult(FarmMethod.Initialize, new InitializeInput
            {
                DistributeToken = tokenSymbol,
                DistributeTokenPerBlock = tokenPerBlock,
                HalvingPeriod = halvingPeriod,
                StartBlock = startBlock,
                TotalReward = totalReward
            });
            return result;
        }

        public UserInfoStruct GetUserInfo(int pid, string user)
        {
            return CallViewMethod<UserInfoStruct>(FarmMethod.UserInfo, new UserInfoInput
            {
                Pid = pid,
                User = user.ConvertAddress()
            });
        }
        
        public PoolInfoStruct GetPoolInfo(Int32 pid)
        {
            return CallViewMethod<PoolInfoStruct>(FarmMethod.PoolInfo, new Int32Value {Value = pid});
        }

        public Int64Value GetPhase(Int64 blockNum)
        {
            return CallViewMethod<Int64Value>(FarmMethod.Phase, new Int64Value {Value = blockNum});
            
        }


        public Int64Value GetStartBlock()
        {
            return CallViewMethod<Int64Value>(FarmMethod.StartBlock, new Empty());
        }

        public Int64Value GetHalvingPeriod()
        {
            return CallViewMethod<Int64Value>(FarmMethod.HalvingPeriod, new Empty());
        }

        public BigIntValue GetTotalReward()
        {
            return CallViewMethod<BigIntValue>(FarmMethod.TotalReward, new Empty());
        }

        public BigIntValue GetDistributeTokenBlockReward()
        {
            return CallViewMethod<BigIntValue>(FarmMethod.DistributeTokenPerBlock, new Empty());
        }

        public TransactionResultDto Add(long allocpoint, string lptoken, bool withupdate)
        {
            var result = ExecuteMethodWithResult(FarmMethod.Add, new AddInput{
                AllocPoint = allocpoint,
                LpToken = lptoken,
                WithUpdate = withupdate
            });
            return result;
        }

        public TransactionResultDto Deposit(int pid, BigIntValue amount)
        {
            var result = ExecuteMethodWithResult(FarmMethod.Deposit, new DepositInput
            {
                Pid = pid,
                Amount = amount
            });

            return result;
        }

        public TransactionResultDto Redeposit(int amount, Address user)
        {
            var result = ExecuteMethodWithResult(FarmMethod.ReDeposit, new ReDepositInput
            {
                Amount = amount,
                User = user
            });
            return result;
        }

        public BigIntValue IssuedReward()
        {
            return CallViewMethod<BigIntValue>(FarmMethod.IssuedReward, new Empty());
        }
        
        public BigIntValue PendingAmount(int pid, Address user)
        {
            return CallViewMethod<BigIntValue>(FarmMethod.Pending, new PendingInput
            {
                Pid = pid,
                User = user
            });
        }

        public Int64Value EndBlock()
        {
            return CallViewMethod<Int64Value>(FarmMethod.endBlock, new Empty());
        }

        public BigIntValue TotalReward()
        {
            return CallViewMethod<BigIntValue>(FarmMethod.TotalReward, new Empty());
        }

        public TransactionResultDto Withdraw(int pid, BigIntValue amount)
        {
            var result = ExecuteMethodWithResult(FarmMethod.Withdraw, new WithdrawInput
            {
                Pid = pid,
                Amount = amount
            });
            return result;
        }

        public Int64Value TotalAllocPoint()
        {
            return CallViewMethod<Int64Value>(FarmMethod.TotalAllocPoint, new Empty());
        }

        public BigIntValue Reward(Int64Value block)
        {
            return CallViewMethod<BigIntValue>(FarmMethod.Reward, new Int64Value(block));
        }
    }
}