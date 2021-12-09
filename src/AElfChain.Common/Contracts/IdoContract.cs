using System;
using System.Numerics;
using System.Threading;
using AElf.Client.Dto;
using AElfChain.Common.Managers;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using Gandalf.Contracts.IdoContract;
using Google.Protobuf.WellKnownTypes;
using Org.BouncyCastle.Asn1.X509;
using ProtoBuf.WellKnownTypes;
using Empty = Google.Protobuf.WellKnownTypes.Empty;
using Timestamp = Google.Protobuf.WellKnownTypes.Timestamp;

namespace AElfChain.Common.Contracts
{
    public enum IdoContractMethod
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
        GetPublicOffering,
        GetUserInfo,
        GetPublicOfferingLength
    }

    public class IdoContract : BaseContract<IdoContractMethod>
    {
        public IdoContract(INodeManager nm, string account) :
            base(nm, ContractFileName, account)
        {
            Logger = Log4NetHelper.GetLogger();
        }

        public IdoContract(INodeManager nm, string callAddress, string contractAbi) :
            base(nm, contractAbi)
        {
            SetAccount(callAddress);
            Logger = Log4NetHelper.GetLogger();
        }

        public static string ContractFileName => "Gandalf.Contracts.IdoContract";

        public TransactionResultDto Initialize(string owner)
        {
            return ExecuteMethodWithResult(IdoContractMethod.Initialize, owner.ConvertAddress());
        }

        public TransactionResultDto AddPublicOffering(string offeringTokenSymbol, long offeringTokenAmount,
            string wantTokenSymbol,
            long wantTokenAmount, Timestamp startTime, Timestamp endTime)
        {
            return ExecuteMethodWithResult(IdoContractMethod.AddPublicOffering,
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
            return ExecuteMethodWithResult(IdoContractMethod.ChangeAscription, new ChangeAscriptionInput
            {
                TokenSymbol = tokenSymbol,
                Receiver = receiver.ConvertAddress()
            });
        }

        public TransactionResultDto Withdraw(int publicId)
        {
            return ExecuteMethodWithResult(IdoContractMethod.Withdraw, new Int32Value {Value = publicId});
        }

        public TransactionResultDto Invest(int publicId, long amount, string channel)
        {
            return ExecuteMethodWithResult(IdoContractMethod.Invest, new InvestInput
            {
                PublicId = publicId,
                Amount = amount,
                Channel = channel
            });
        }

        public TransactionResultDto Harvest(int publicId)
        {
            return ExecuteMethodWithResult(IdoContractMethod.Harvest, new Int32Value {Value = publicId});
        }

        public TransactionResultDto ResetTimeSpan(long maxTimespan, long minTimespan)
        {
            return ExecuteMethodWithResult(IdoContractMethod.ResetTimeSpan, new ResetTimeSpanInput
            {
                MaxTimespan = maxTimespan,
                MinTimespan = minTimespan
            });
        }

        public Address GetOwner()
        {
            return CallViewMethod<Address>(IdoContractMethod.GetOwner, new Empty());
        }

        public ResetTimeSpanOutput GetTimespan()
        {
            return CallViewMethod<ResetTimeSpanOutput>(IdoContractMethod.GetTimespan, new Empty());
        }

        public PublicOfferingOutput GetPublicOffering(Int64Value publicId)
        {
            return CallViewMethod<PublicOfferingOutput>(IdoContractMethod.GetPublicOffering, publicId);
        }

        public UserInfo GetUserInfo(int publicId, string user)
        {
            return CallViewMethod<UserInfo>(IdoContractMethod.GetUserInfo, new UserInfoInput
            {
                PublicId = publicId,
                User = user.ConvertAddress()
            });
        }

        public int GetPublicOfferingLength()
        {
            return CallViewMethod<Int32Value>(IdoContractMethod.GetPublicOfferingLength, new Empty()).Value;
        }
    }
}