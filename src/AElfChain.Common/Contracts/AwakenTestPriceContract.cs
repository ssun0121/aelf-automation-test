using AElf.Contracts.Price;
using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts;

public enum PriceMethod
{
    Initialize,
    GetExchangeTokenPriceInfo,
    SetPrice
}

public class AwakenTestPriceContract : BaseContract<PriceMethod>
{
    public AwakenTestPriceContract(INodeManager nodeManager, string callAddress) : base(nodeManager,
        "AElf.Contracts.Price", callAddress)
    {
    }

    public AwakenTestPriceContract(INodeManager nodeManager, string contractAddress, string callAddress) : base(nodeManager,
        contractAddress)
    {
        SetAccount(callAddress);
    }

    public Price GetExchangeTokenPriceInfo(string tokenSymbol)
    {
        return CallViewMethod<Price>(PriceMethod.GetExchangeTokenPriceInfo, new GetExchangeTokenPriceInfoInput
        {
            TokenSymbol = tokenSymbol
        });
    }
}