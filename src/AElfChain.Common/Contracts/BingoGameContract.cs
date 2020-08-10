using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{public enum BingoMethod
    {
        //Action
        Play
    }
    public class BingoGameContract : BaseContract<BingoMethod>
    {
        public BingoGameContract(INodeManager nodeManager, string callAddress) :
            base(nodeManager, "AElf.Contracts.BingoContract", callAddress)
        {
        }

        public BingoGameContract(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }
    }
}