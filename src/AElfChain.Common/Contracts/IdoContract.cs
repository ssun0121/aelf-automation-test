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
        GetUserInfo
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
            long wantTokenAmount, DateTime startTime, DateTime endTime)
        {
            return ExecuteMethodWithResult(IdoContractMethod.AddPublicOffering,
                new AddPublicOfferingInput
                {
                    OfferingTokenSymbol = offeringTokenSymbol,
                    OfferingTokenAmount = offeringTokenAmount,
                    WantTokenSymbol = wantTokenSymbol,
                    WantTokenAmount = wantTokenAmount,
                    StartTime = Timestamp.FromDateTime(startTime.ToUniversalTime()),
                    EndTime = Timestamp.FromDateTime(endTime.ToUniversalTime())
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

        public TransactionResultDto Withdraw(Int32Value withdrawAmount)
        {
            return ExecuteMethodWithResult(IdoContractMethod.Withdraw, new Int32Value(withdrawAmount));
        }

        public TransactionResultDto Invest(int publicId, long amount)
        {
            return ExecuteMethodWithResult(IdoContractMethod.Invest, new InvestInput
            {
                PublicId = publicId,
                Amount = amount
            });
        }

        public TransactionResultDto Harvest(Int32Value harvestAmount)
        {
            return ExecuteMethodWithResult(IdoContractMethod.Harvest, new Int32Value(harvestAmount));
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

        public PublicOfferingOutput GetPublicOffering(Int32Value publicId)
        {
            return CallViewMethod<PublicOfferingOutput>(IdoContractMethod.GetPublicOffering, new Int32Value(publicId));
        }

        public UserInfo GetUserInfo(int publicId, string user)
        {
            return CallViewMethod<UserInfo>(IdoContractMethod.GetUserInfo, new UserInfoInput
            {
                PublicId = publicId,
                User = user.ConvertAddress()
            });
        }
    }
}