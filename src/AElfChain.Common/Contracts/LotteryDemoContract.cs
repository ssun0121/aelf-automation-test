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
        AddRewardList,
        SetRewardListForOnePeriod,
        Suspend,
        Recover,
        
        //View
        GetRewardResult,
        GetLotteries
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