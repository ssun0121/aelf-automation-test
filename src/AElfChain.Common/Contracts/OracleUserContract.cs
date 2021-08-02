using AElfChain.Common.Contracts;
using AElfChain.Common.Managers;

namespace AElfChain.Common
{
    public enum OracleUserMethod
    {
        //Action
        RecordTemperature,
        RecordPrice,
        Initialize,

        //View
        GetHistoryPrices,
        GetHistoryTemperatures,
        GetHelpfulNodeList
    }

    public class OracleUserContract : BaseContract<OracleUserMethod>
    {
        public OracleUserContract(INodeManager nodeManager, string callAddress) : base(nodeManager,
            "AElf.Contracts.OracleUser", callAddress)
        {
        }

        public OracleUserContract(INodeManager nodeManager, string callAddress, string contractAddress,string password = "") : base(
            nodeManager, contractAddress)
        {
            SetAccount(callAddress,password);
        }
    }
}