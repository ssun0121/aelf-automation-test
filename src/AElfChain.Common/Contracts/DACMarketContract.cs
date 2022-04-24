using AElf.Client.Dto;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using Sinodac.Contracts.DACMarket;

namespace AElfChain.Common.Contracts
{
    public enum DACMarketMethod
    {
        Initialize,

        //Action


        //View
        GetDACSeries,
        GetPublicTime,
        GetOwnBoxIdList,
        GetBoxInfo,
        ConfirmCopyright,
        GetDACCopyright
    }

    public class DacMarketContract : BaseContract<DACMarketMethod>
    {
        public DacMarketContract(INodeManager nodeManager, string callAddress) :
            base(nodeManager, "Sinodac.Contracts.DACMarket", callAddress)
        {
            Logger = Log4NetHelper.GetLogger();
        }

        public DacMarketContract(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
            Logger = Log4NetHelper.GetLogger();
        }

        public TransactionResultDto Initialize(string delegatorContractAddress, string dacContractAddress,
            string adminAddress)
        {
            var result = ExecuteMethodWithResult(DACMarketMethod.Initialize, new InitializeInput
            {
                DelegatorContractAddress = delegatorContractAddress.ConvertAddress(),
                DacContractAddress = dacContractAddress.ConvertAddress(),
                AdminAddress = adminAddress.ConvertAddress()
            });

            return result;
        }

        public DACSeries GetDACSeries(string seriesName)
        {
            return CallViewMethod<DACSeries>(DACMarketMethod.GetDACSeries, new StringValue
            {
                Value = seriesName
            });
        }

        public Timestamp GetPublicTime(string dacName)
        {
            return CallViewMethod<Timestamp>(DACMarketMethod.GetPublicTime, new StringValue
            {
                Value = dacName
            });
        }
    }
}