using System.Runtime.CompilerServices;
using AElf.Client.Dto;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using Awaken.Contracts.Farm;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
{
    public enum FarmMethod
    {
        Initialize,
        SetHalvingPeriod,
        SetDistributeTokenPerBlock,
        AddPool,
        SetReDeposit,
        SetTool,
        SetOwner,
        SetAdmin,
        NewReward,

        Deposit,
        Withdraw,
        ReDeposit,
        MassUpdatePools,
        UpdatePool,
        FixEndBlock,

        Pending,
        GetReDepositLimit,
        GetRedepositAmount,
        GetDistributeTokenBlockReward,
        PendingLockDistributeToken,
        GetAdmin,
        GetOwner,
        GetStartBlockOfDistributeToken,
        GetDistributeTokenPerBlockConcentratedMining,
        GetDistributeTokenPerBlockContinuousMining,

        GetUsdtPerBlock,
        GetUsdtStartBlock,
        GetUsdtEndBlock,
        GetCycle,

        GetHalvingPeriod0,
        GetHalvingPeriod1,

        GetTotalReward,
        GetIssuedReward,
        GetEndBlock,
        GetUserInfo,
        GetPoolInfo,
        GetPoolLength,
        GetTotalAllocPoint
    }

    public class AwakenFarmContract : BaseContract<FarmMethod>
    {
        public AwakenFarmContract(INodeManager nodeManager, string callAddress) : base(nodeManager,
            "Awaken.Contracts.Farm", callAddress)
        {
        }

        public AwakenFarmContract(INodeManager nodeManager, string callAddress, string contractAddress) : base(
            nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }

        public TransactionResultDto AddPool(long allocPoint, bool withUpdate, string lpToken)
        {
            var result = ExecuteMethodWithResult(FarmMethod.AddPool, new AddPoolInput
            {
                AllocPoint = allocPoint,
                WithUpdate = withUpdate,
                LpToken = lpToken
            });
            return result;
        }
        
        public TransactionResultDto Deposit(int pid, long amount, string depositAddress)
        {
            SetAccount(depositAddress);
            var result = ExecuteMethodWithResult(FarmMethod.Deposit, new DepositInput
            {
                Pid = pid,
                Amount = amount,
            });
            return result;
        }
        
        public TransactionResultDto Withdraw(int pid, long amount, string depositAddress)
        {
            SetAccount(depositAddress);
            var result = ExecuteMethodWithResult(FarmMethod.Withdraw, new WithdrawInput
            {
                Pid = pid,
                Amount = amount,
            });
            return result;
        }

        #region View

                public Address GetAdmin()
        {
            return CallViewMethod<Address>(FarmMethod.GetAdmin, new Empty());
        }

        public Address GetOwner()
        {
            return CallViewMethod<Address>(FarmMethod.GetOwner, new Empty());
        }

        public long GetCycle()
        {
            var cycle = CallViewMethod<Int64Value>(FarmMethod.GetCycle, new Empty());
            return cycle.Value;
        }

        public long GetHalvingPeriod0()
        {
            var halvingPeriod0 = CallViewMethod<Int64Value>(FarmMethod.GetHalvingPeriod0, new Empty());
            return halvingPeriod0.Value;
        }

        public long GetHalvingPeriod1()
        {
            var halvingPeriod1 = CallViewMethod<Int64Value>(FarmMethod.GetHalvingPeriod1, new Empty());
            return halvingPeriod1.Value;
        }

        public long GetIssuedReward()
        {
            var issuedReward = CallViewMethod<Int64Value>(FarmMethod.GetIssuedReward, new Empty());
            return issuedReward.Value;
        }

        public long GetTotalReward()
        {
            var totalReward = CallViewMethod<Int64Value>(FarmMethod.GetTotalReward, new Empty());
            return totalReward.Value;
        }

        public GetDistributeTokenBlockRewardOutput GetDistributeTokenBlockReward(long lastRewardBlock)
        {
            var distributeTokenBlockReward =
                CallViewMethod<GetDistributeTokenBlockRewardOutput>(FarmMethod.GetDistributeTokenBlockReward,
                    new Int64Value {Value = lastRewardBlock});
            return distributeTokenBlockReward;
        }

        public long GetDistributeTokenPerBlockConcentratedMining()
        {
            var perBlockConcentrateMining =
                CallViewMethod<Int64Value>(FarmMethod.GetDistributeTokenPerBlockConcentratedMining, new Empty());
            return perBlockConcentrateMining.Value;
        }

        public long GetDistributeTokenPerBlockContinuousMining()
        {
            var perBlockContinuousMining =
                CallViewMethod<Int64Value>(FarmMethod.GetDistributeTokenPerBlockContinuousMining, new Empty());
            return perBlockContinuousMining.Value;
        }

        public long GetStartBlockOfDistributeToken()
        {
            var startBlock = CallViewMethod<Int64Value>(FarmMethod.GetStartBlockOfDistributeToken, new Empty());
            return startBlock.Value;
        }

        public long GetEndBlock()
        {
            var endBlock = CallViewMethod<Int64Value>(FarmMethod.GetEndBlock, new Empty());
            return endBlock.Value;
        }

        public PoolInfo GetPoolInfo(int pid)
        {
            var poolInfo =
                CallViewMethod<PoolInfo>(FarmMethod.GetPoolInfo, new Int32Value {Value = pid});
            return poolInfo;
        }

        public long GetPoolLength()
        {
            var poolLength =
                CallViewMethod<Int64Value>(FarmMethod.GetPoolLength, new Empty());
            return poolLength.Value;
        }

        public long GetTotalAllocPoint()
        {
            var totalAllocPoint =
                CallViewMethod<Int64Value>(FarmMethod.GetTotalAllocPoint, new Empty());
            return totalAllocPoint.Value;
        }

        public UserInfo GetUserInfo(int pid, string account = "")
        {
            var user = account == "" ? CallAddress : account;
            var info =
                CallViewMethod<UserInfo>(FarmMethod.GetUserInfo, new GetUserInfoInput
                {
                    Pid = pid,
                    User = user.ConvertAddress()
                });
            Logger.Info($"\nUser {user} \n" +
                        $"Deposit amount: {info.Amount}\n" +
                        $"ClaimedAmount: {info.ClaimedAmount}\n" +
                        $"LockPending: {info.LockPending}\n" +
                        $"LastRewardBlock: {info.LastRewardBlock}\n" +
                        $"RewardUsdtDebt: {info.RewardUsdtDebt}\n" +
                        $"RewardDistributeTokenDebt: {info.RewardDistributeTokenDebt}\n" +
                        $"RewardLockDistributeTokenDebt: {info.RewardLockDistributeTokenDebt}");
            return info;
        }

        public PendingOutput Pending(int pid, string account = "")
        {
            var user = account == "" ? CallAddress : account;
            var pending =
                CallViewMethod<PendingOutput>(FarmMethod.Pending, new PendingInput
                {
                    Pid = pid,
                    User = user.ConvertAddress()
                });
            return pending;
        }

        public long PendingLockDistributeToken(int pid, string account = "")
        {
            var user = account == "" ? CallAddress : account;
            var stillLockReward =
                CallViewMethod<Int64Value>(FarmMethod.PendingLockDistributeToken, new PendingLockDistributeTokenInput
                {
                    Pid = pid,
                    User = user.ConvertAddress()
                });
            return stillLockReward.Value;
        }

        public long GetRedepositAmount(int pid, string account = "")
        {
            var user = account == "" ? CallAddress : account;
            var redepositAmount =
                CallViewMethod<Int64Value>(FarmMethod.GetRedepositAmount, new GetRedepositAmountInput
                {
                    Pid = pid,
                    User = user.ConvertAddress()
                });
            return redepositAmount.Value;
        }
        
        public long GetReDepositLimit(int pid, string account = "")
        {
            var user = account == "" ? CallAddress : account;
            var  limit = 
                CallViewMethod<Int64Value>(FarmMethod.GetReDepositLimit, new GetReDepositLimitInput
                {
                    Pid = pid,
                    User = user.ConvertAddress()
                });
            return limit.Value;
        }

        #endregion
    }
}