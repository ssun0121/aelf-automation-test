using System;
using AElf.Client.Dto;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using Awaken.Contracts.PoolTwoContract;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
{
    public enum FarmTwoMethod
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
        EndBlock,
        Withdraw,
        TotalAllocPoint,
        Reward,
        Set,
        FixEndBlock,
        SetDistributeTokenPerBlock,
        SetHalvingPeriod,
        FarmPoolOne,
        PendingTest
    }

    public class AwakenFarmTwoContract :BaseContract<FarmTwoMethod>
    {

        public AwakenFarmTwoContract(INodeManager nodeManager, string callAddress) : base(nodeManager,
            "Awaken.Contracts.PoolTwo", callAddress)
        {
        }

        public AwakenFarmTwoContract(INodeManager nodeManager, string callAddress, string contractAddress) : base(
            nodeManager,
            contractAddress)
        {
            SetAccount(callAddress);
        }

        public TransactionResultDto Initialize(string tokenSymbol, BigIntValue tokenPerBlock, long halvingPeriod, long startBlock, BigIntValue totalReward, string tokenContract)
        {
            var result = ExecuteMethodWithResult(FarmTwoMethod.Initialize, new InitializeInput
            {
                DistributeToken = tokenSymbol,
                DistributeTokenPerBlock = tokenPerBlock,
                HalvingPeriod = halvingPeriod,
                StartBlock = startBlock,
                TotalReward = totalReward,
                AwakenTokenContract = tokenContract.ConvertAddress()
            });
            return result;
        }

        public UserInfoStruct GetUserInfo(int pid, string user)
        {
            return CallViewMethod<UserInfoStruct>(FarmTwoMethod.UserInfo, new UserInfoInput
            {
                Pid = pid,
                User = user.ConvertAddress()
            });
        }
        
        public PoolInfoStruct GetPoolInfo(Int32 pid)
        {
            return CallViewMethod<PoolInfoStruct>(FarmTwoMethod.PoolInfo, new Int32Value {Value = pid});
        }

        public Int64Value GetPhase(Int64 blockNum)
        {
            return CallViewMethod<Int64Value>(FarmTwoMethod.Phase, new Int64Value {Value = blockNum});
            
        }


        public Int64Value GetStartBlock()
        {
            return CallViewMethod<Int64Value>(FarmTwoMethod.StartBlock, new Empty());
        }

        public Int64Value GetHalvingPeriod()
        {
            return CallViewMethod<Int64Value>(FarmTwoMethod.HalvingPeriod, new Empty());
        }

        public BigIntValue GetTotalReward()
        {
            return CallViewMethod<BigIntValue>(FarmTwoMethod.TotalReward, new Empty());
        }

        public BigIntValue GetDistributeTokenBlockReward()
        {
            return CallViewMethod<BigIntValue>(FarmTwoMethod.DistributeTokenPerBlock, new Empty());
        }

        public TransactionResultDto Add(long allocpoint, string lptoken, bool withupdate)
        {
            var result = ExecuteMethodWithResult(FarmTwoMethod.Add, new AddInput{
                AllocPoint = allocpoint,
                LpToken = lptoken,
                WithUpdate = withupdate
            });
            return result;
        }

        public TransactionResultDto Set(int pid, long allocpoint, BigIntValue newperblock, bool withupdate)
        {
            var result = ExecuteMethodWithResult(FarmTwoMethod.Set, new SetInput
            {
                Pid = pid,
                AllocPoint = allocpoint,
                NewPerBlock = newperblock,
                WithUpdate = withupdate
            });
            return result;
        }

        public TransactionResultDto Deposit(int pid, BigIntValue amount)
        {
            var result = ExecuteMethodWithResult(FarmTwoMethod.Deposit, new DepositInput
            {
                Pid = pid,
                Amount = amount
            });

            return result;
        }

        public TransactionResultDto Redeposit(int amount, Address user)
        {
            var result = ExecuteMethodWithResult(FarmTwoMethod.ReDeposit, new ReDepositInput
            {
                Amount = amount,
                User = user
            });
            return result;
        }

        public BigIntValue IssuedReward()
        {
            return CallViewMethod<BigIntValue>(FarmTwoMethod.IssuedReward, new Empty());
        }
        
        public BigIntValue PendingAmount(int pid, Address user)
        {
            return CallViewMethod<BigIntValue>(FarmTwoMethod.Pending, new PendingInput
            {
                Pid = pid,
                User = user
            });
        }

        public Int64Value EndBlock()
        {
            return CallViewMethod<Int64Value>(FarmTwoMethod.EndBlock, new Empty());
        }

        public BigIntValue TotalReward()
        {
            return CallViewMethod<BigIntValue>(FarmTwoMethod.TotalReward, new Empty());
        }

        public TransactionResultDto Withdraw(int pid, BigIntValue amount)
        {
            var result = ExecuteMethodWithResult(FarmTwoMethod.Withdraw, new WithdrawInput
            {
                Pid = pid,
                Amount = amount
            });
            return result;
        }

        public Int64Value TotalAllocPoint()
        {
            return CallViewMethod<Int64Value>(FarmTwoMethod.TotalAllocPoint, new Empty());
        }

        public BigIntValue Reward(Int64Value block)
        {
            return CallViewMethod<BigIntValue>(FarmTwoMethod.Reward, new Int64Value(block));
        }

        public TransactionResultDto FixEndBlock(bool input)
        {
            return ExecuteMethodWithResult(FarmTwoMethod.FixEndBlock, new BoolValue
            {
                Value = input
            });
        }

        public TransactionResultDto SetDistributeTokenPerBlock(long input)
        {
            return ExecuteMethodWithResult(FarmTwoMethod.SetDistributeTokenPerBlock, new Int64Value
            {
                Value = input
            });
        }
        
        public TransactionResultDto SetHalvingPeriod(long input)
        {
            return ExecuteMethodWithResult(FarmTwoMethod.SetHalvingPeriod, new Int64Value
            {
                Value = input
            });
        }

        public Address FarmPoolOne()
        {
            return CallViewMethod<Address>(FarmTwoMethod.FarmPoolOne, new Empty());
        }

        public TransactionResultDto SetFarmPoolOne(Address input)
        {
            return ExecuteMethodWithResult(FarmTwoMethod.SetFarmPoolOne, new Address(input));
        }

        public PendingOutput GetPendingTest(int pid, string user)
        {
            return CallViewMethod<PendingOutput>(FarmTwoMethod.PendingTest, new PendingInput
            {
                Pid = pid,
                User = user.ConvertAddress()
            });
        }

    }
}