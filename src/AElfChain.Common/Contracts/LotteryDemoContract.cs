using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum LotteryDemoMethod
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

    public class LotteryDemoContract : BaseContract<LotteryDemoMethod>
    {
        public LotteryDemoContract(INodeManager nodeManager, string callAddress) :
            base(nodeManager, "AElf.Contracts.LotteryContract", callAddress)
        {
        }

        public LotteryDemoContract(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }
    }
}