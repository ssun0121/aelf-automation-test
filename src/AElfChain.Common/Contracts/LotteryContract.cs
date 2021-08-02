using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum LotteryMethod
    {
        //Action
        Initialize,
        Stake,
        Redeem,
        Claim,
        //Admin
        Draw,
        ResetTimestamp,
        
        //View
        GetCurrentPeriodId,
        GetTotalLotteryCount,
        GetLotteryCodeListByUserAddress,
        GetAwardListByUserAddress,
        GetLottery,
        GetStakingAmount,
        GetOwnLottery,
        GetAward,
        GetPeriodAward,
        GetAwardList,
        GetAwardAmountMap,
        
        GetStartTimestamp,
        GetShutdownTimestamp,
        GetRedeemTimestamp
    }

    public class LotteryContract : BaseContract<LotteryMethod>
    {
        public LotteryContract(INodeManager nodeManager, string callAddress) :
            base(nodeManager, "AElf.Contracts.Lottery", callAddress)
        {
        }

        public LotteryContract(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }
    }
}