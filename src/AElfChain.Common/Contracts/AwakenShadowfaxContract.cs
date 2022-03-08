using AElf.Client.Dto;
using AElfChain.Common.Managers;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using Awaken.Contracts.Shadowfax;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
{
    public enum AwakenShadowfaxContractMethod
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
        Owner,
        MaximalTimeSpan,
        MinimalTimespan,
        PublicOfferings,
        UserInfo,
        GetPublicOfferingLength,
        Ascription
    }

    public class AwakenShadowfaxContract : BaseContract<AwakenShadowfaxContractMethod>
    {
        public AwakenShadowfaxContract(INodeManager nm, string account) :
            base(nm, ContractFileName, account)
        {
            Logger = Log4NetHelper.GetLogger();
        }

        public AwakenShadowfaxContract(INodeManager nm, string callAddress, string contractAbi) :
            base(nm, contractAbi)
        {
            SetAccount(callAddress);
            Logger = Log4NetHelper.GetLogger();
        }

        public static string ContractFileName => "Awaken.Contracts.Shadowfax";

        public TransactionResultDto Initialize(string owner)
        {
            return ExecuteMethodWithResult(AwakenShadowfaxContractMethod.Initialize, new InitializeInput
            {
                Owner = owner.ConvertAddress()
            });
        }

        public TransactionResultDto AddPublicOffering(string offeringTokenSymbol, long offeringTokenAmount,
            string wantTokenSymbol,
            long wantTokenAmount, Timestamp startTime, Timestamp endTime)
        {
            return ExecuteMethodWithResult(AwakenShadowfaxContractMethod.AddPublicOffering,
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
            return ExecuteMethodWithResult(AwakenShadowfaxContractMethod.ChangeAscription, new ChangeAscriptionInput
            {
                TokenSymbol = tokenSymbol,
                Receiver = receiver.ConvertAddress()
            });
        }

        public TransactionResultDto Withdraw(int publicId)
        {
            return ExecuteMethodWithResult(AwakenShadowfaxContractMethod.Withdraw, new Int32Value
            {
                Value = publicId
            });
        }

        public TransactionResultDto Invest(int publicId, long amount, string channel)
        {
            return ExecuteMethodWithResult(AwakenShadowfaxContractMethod.Invest, new InvestInput
            {
                PublicId = publicId,
                Amount = amount,
                Channel = channel
            });
        }

        public TransactionResultDto Harvest(int publicId)
        {
            return ExecuteMethodWithResult(AwakenShadowfaxContractMethod.Harvest, new Int32Value {Value = publicId});
        }

        public TransactionResultDto ResetTimeSpan(long maxTimespan, long minTimespan)
        {
            return ExecuteMethodWithResult(AwakenShadowfaxContractMethod.ResetTimeSpan, new ResetTimeSpanInput
            {
                MaxTimespan = maxTimespan,
                MinTimespan = minTimespan
            });
        }

        public Address GetOwner()
        {
            return CallViewMethod<Address>(AwakenShadowfaxContractMethod.Owner, new Empty());
        }

        public long GetMaximalTimeSpan()
        {
            return CallViewMethod<Int64Value>(AwakenShadowfaxContractMethod.MaximalTimeSpan, new Empty()).Value;
        }

        public long GetMinimalTimespan()
        {
            return CallViewMethod<Int64Value>(AwakenShadowfaxContractMethod.MinimalTimespan, new Empty()).Value;
        }

        public PublicOfferingOutput PublicOfferings(Int64Value publicId)
        {
            return CallViewMethod<PublicOfferingOutput>(AwakenShadowfaxContractMethod.PublicOfferings, publicId);
        }

        public UserInfoStruct UserInfo(int publicId, string user)
        {
            return CallViewMethod<UserInfoStruct>(AwakenShadowfaxContractMethod.UserInfo, new UserInfoInput
            {
                PublicId = publicId,
                User = user.ConvertAddress()
            });
        }

        public int GetPublicOfferingLength()
        {
            return CallViewMethod<Int32Value>(AwakenShadowfaxContractMethod.GetPublicOfferingLength, new Empty())
                .Value;
        }

        public Address GetAscription(string symbol)
        {
            return CallViewMethod<Address>(AwakenShadowfaxContractMethod.Ascription,
                new StringValue {Value = symbol});
        }
    }
}