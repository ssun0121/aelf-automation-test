using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum LotteryMethod
    {
        //Action
        Initialize,
        Buy,
        Draw,
        PrepareDraw,
        TakeReward,
        SetBonusRate,
        SetAdmin,


        //View
        GetPeriods,
        GetLottery,
        GetLotteries,
        GetRewardedLotteries,
        GetLatestCashedLottery,
        GetPeriod,
        GetLatestDrawPeriod,
        GetCurrentPeriodNumber,
        GetCurrentPeriod,
        GetPrice,
        GetTokenSymbol,
        GetCashDuration,
        GetBonusRate,
        GetAdmin,
        GetRewards
    }

    public class LotteryContract : BaseContract<LotteryMethod>
    {
        public LotteryContract(INodeManager nodeManager, string callAddress) :
            base(nodeManager, "AElf.Contracts.LotteryContract", callAddress)
        {
        }

        public LotteryContract(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }
    }
}