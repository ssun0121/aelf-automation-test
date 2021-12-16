using AElf.Client.Dto;
using AElfChain.Common.Managers;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using Gandalf.Contracts.Shadowfax;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
{
    public enum GandalfShadowfaxContractMethod
    {
        //Action
        Initialize,
        AddPublicOffering,
        ChangeAscription,
        Withdraw,
        Invest,
        Harvest,
        ResetTimeSpan,

        //View
        GetOwner,
        GetTimespan,
        PublicOfferings,
        GetUserInfo,
        GetPublicOfferingLength,
        GetTokenOwnership
    }

    public class GandalfShadowfaxContract : BaseContract<GandalfShadowfaxContractMethod>
    {
        public GandalfShadowfaxContract(INodeManager nm, string account) :
            base(nm, ContractFileName, account)
        {
            Logger = Log4NetHelper.GetLogger();
        }

        public GandalfShadowfaxContract(INodeManager nm, string callAddress, string contractAbi) :
            base(nm, contractAbi)
        {
            SetAccount(callAddress);
            Logger = Log4NetHelper.GetLogger();
        }

        public static string ContractFileName => "Gandalf.Contracts.Shadowfax";

        public TransactionResultDto Initialize(string owner)
        {
            return ExecuteMethodWithResult(GandalfShadowfaxContractMethod.Initialize, owner.ConvertAddress());
        }

        public TransactionResultDto AddPublicOffering(string offeringTokenSymbol, long offeringTokenAmount,
            string wantTokenSymbol,
            long wantTokenAmount, Timestamp startTime, Timestamp endTime)
        {
            return ExecuteMethodWithResult(GandalfShadowfaxContractMethod.AddPublicOffering,
                new AddPublicOfferingInput
                {
                    OfferingTokenSymbol = offeringTokenSymbol,
                    OfferingTokenAmount = offeringTokenAmount,
                    WantTokenSymbol = wantTokenSymbol,
                    WantTokenAmount = wantTokenAmount,
                    StartTime = startTime,
                    EndTime = endTime
                });
        }

        public TransactionResultDto ChangeAscription(string tokenSymbol, string receiver)
        {
            return ExecuteMethodWithResult(GandalfShadowfaxContractMethod.ChangeAscription, new ChangeAscriptionInput
            {
                TokenSymbol = tokenSymbol,
                Receiver = receiver.ConvertAddress()
            });
        }

        public TransactionResultDto Withdraw(int publicId)
        {
            return ExecuteMethodWithResult(GandalfShadowfaxContractMethod.Withdraw, new Int32Value
            {
                Value = publicId
            });
        }

        public TransactionResultDto Invest(int publicId, long amount, string channel)
        {
            return ExecuteMethodWithResult(GandalfShadowfaxContractMethod.Invest, new InvestInput
            {
                PublicId = publicId,
                Amount = amount,
                Channel = channel
            });
        }

        public TransactionResultDto Harvest(int publicId)
        {
            return ExecuteMethodWithResult(GandalfShadowfaxContractMethod.Harvest, new Int32Value {Value = publicId});
        }

        public TransactionResultDto ResetTimeSpan(long maxTimespan, long minTimespan)
        {
            return ExecuteMethodWithResult(GandalfShadowfaxContractMethod.ResetTimeSpan, new ResetTimeSpanInput
            {
                MaxTimespan = maxTimespan,
                MinTimespan = minTimespan
            });
        }

        public Address GetOwner()
        {
            return CallViewMethod<Address>(GandalfShadowfaxContractMethod.GetOwner, new Empty());
        }

        public ResetTimeSpanOutput GetTimespan()
        {
            return CallViewMethod<ResetTimeSpanOutput>(GandalfShadowfaxContractMethod.GetTimespan, new Empty());
        }

        public PublicOfferingOutput PublicOfferings(Int64Value publicId)
        {
            return CallViewMethod<PublicOfferingOutput>(GandalfShadowfaxContractMethod.PublicOfferings, publicId);
        }

        public UserInfoStruct GetUserInfo(int publicId, string user)
        {
            return CallViewMethod<UserInfoStruct>(GandalfShadowfaxContractMethod.GetUserInfo, new UserInfoInput
            {
                PublicId = publicId,
                User = user.ConvertAddress()
            });
        }

        public int GetPublicOfferingLength()
        {
            return CallViewMethod<Int32Value>(GandalfShadowfaxContractMethod.GetPublicOfferingLength, new Empty())
                .Value;
        }

        public Address GetTokenOwnership(string symbol)
        {
            return CallViewMethod<Address>(GandalfShadowfaxContractMethod.GetTokenOwnership,
                new StringValue {Value = symbol});
        }
    }
}