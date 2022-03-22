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

        #region Set
        public TransactionResultDto SetTool(string adminAddress, Address toolAddress)
        {
            SetAccount(adminAddress);
            return ExecuteMethodWithResult(FarmMethod.SetTool, toolAddress);
        }
        
        public TransactionResultDto SetOwner(string ownerAddress, Address newAddress)
        {
            SetAccount(ownerAddress);
            return ExecuteMethodWithResult(FarmMethod.SetOwner, newAddress);
        }
        
        public TransactionResultDto SetAdmin(string adminAddress, Address newAddress)
        {
            SetAccount(adminAddress);
            return ExecuteMethodWithResult(FarmMethod.SetAdmin, newAddress);
        }
        
        public TransactionResultDto SetHalvingPeriod(string adminAddress, long block0, long block1)
        {
            SetAccount(adminAddress);
            return ExecuteMethodWithResult(FarmMethod.SetHalvingPeriod, new SetHalvingPeriodInput
            {
                Block0 = block0,
                Block1 = block1
            });
        }
        
        public TransactionResultDto SetDistributeTokenPerBlock(string adminAddress, long perBlock0, long perBlock1)
        {
            SetAccount(adminAddress);
            return ExecuteMethodWithResult(FarmMethod.SetDistributeTokenPerBlock, new SetDistributeTokenPerBlockInput
            {
                PerBlock0 = perBlock0,
                PerBlock1 = perBlock1
            });
        }
        
        public TransactionResultDto SetReDeposit(string adminAddress, Address swapAddress, Address farmTwoAddress)
        {
            SetAccount(adminAddress);
            var result = ExecuteMethodWithResult(FarmMethod.SetReDeposit, new SetReDepositInput
            {
                Router = swapAddress,
                FarmTwoPool = farmTwoAddress
            });
            return result;
        }

        #endregion
        
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

        public TransactionResultDto UpdatePool(int pid)
        {
            return ExecuteMethodWithResult(FarmMethod.UpdatePool, new Int32Value {Value = pid});
        }
        
        public TransactionResultDto MassUpdatePools(int pd)
        {
            return ExecuteMethodWithResult(FarmMethod.MassUpdatePools, new Empty());
        }

        public TransactionResultDto ReDeposit(int pid, long distributeAmount, long elfAmount, string depositAddress)
        {
            SetAccount(depositAddress);
            var result = ExecuteMethodWithResult(FarmMethod.ReDeposit, new ReDepositInput
            {
                Pid = pid,
                DistributeTokenAmount = distributeAmount,
                ElfAmount = elfAmount,
                Channel = ""
            });
            return result;
        }

        public TransactionResultDto NewReward(string toolAddress, long usdtAmount, long startBlock, long newPerBlock)
        {
            SetAccount(toolAddress);
            var result = ExecuteMethodWithResult(FarmMethod.NewReward, new NewRewardInput
            {
                UsdtAmount = usdtAmount,
                StartBlock = startBlock,
                NewPerBlock = newPerBlock
            });
            return result;
        }

        public TransactionResultDto FixEndBlock(string adminAddress, bool isUpdate)
        {
            SetAccount(adminAddress);
            return ExecuteMethodWithResult(FarmMethod.FixEndBlock, new BoolValue{Value = isUpdate});
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

        public long GetUsdtPerBlock()
        {
            return CallViewMethod<Int64Value>(FarmMethod.GetUsdtPerBlock, new Empty()).Value;
        }
        
        public long GetUsdtStartBlock()
        {
            return CallViewMethod<Int64Value>(FarmMethod.GetUsdtStartBlock, new Empty()).Value;
        }
        
        public long GetUsdtEndBlock()
        {
            return CallViewMethod<Int64Value>(FarmMethod.GetUsdtEndBlock, new Empty()).Value;
        }
        
        #endregion
    }
}