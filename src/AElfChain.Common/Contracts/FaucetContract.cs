using AElf.Client.Dto;
using AElf.Contracts.Faucet;
using AElfChain.Common.Managers;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using Google.Protobuf.WellKnownTypes;
using Org.BouncyCastle.Asn1.X509;
using ProtoBuf.WellKnownTypes;
using Timestamp = Google.Protobuf.WellKnownTypes.Timestamp;

namespace AElfChain.Common.Contracts
{
    public enum FaucetContractMethod
    {
        //Action
        Initialize,
        NewFaucet,
        Pour,
        TurnOn,
        TurnOff,
        SetLimit,
        Ban,
        Send,
        Take,
        Return,

        //View
        GetOwner,
        GetFaucetStatus,
        GetLimitAmount,
        GetIntervalMinutes,
        IsBannedByOwner
    }

    public class FaucetContract : BaseContract<FaucetContractMethod>
    {
        public FaucetContract(INodeManager nm, string account) :
            base(nm, ContractFileName, account)
        {
            Logger = Log4NetHelper.GetLogger();
        }

        public FaucetContract(INodeManager nm, string callAddress, string contractAbi) :
            base(nm, contractAbi)
        {
            SetAccount(callAddress);
            Logger = Log4NetHelper.GetLogger();
        }

        public static string ContractFileName => "AElf.Contracts.Faucet";

        public TransactionResultDto Initialize(string admin, long amountLimit,
            long intervalMinutes)
        {
            return ExecuteMethodWithResult(FaucetContractMethod.Initialize, new InitializeInput
            {
                Admin = admin.ConvertAddress(),
                AmountLimit = amountLimit,
                IntervalMinutes = intervalMinutes
            });
        }

        public TransactionResultDto NewFaucet(string symbol, string owner, long amountLimit,
            long intervalMinutes)
        {
            return ExecuteMethodWithResult(FaucetContractMethod.NewFaucet, new NewFaucetInput
            {
                Symbol = symbol,
                Owner = owner.ConvertAddress(),
                AmountLimit = amountLimit,
                IntervalMinutes = intervalMinutes
            });
        }

        public TransactionResultDto Pour(string symbol, long amount)
        {
            return ExecuteMethodWithResult(FaucetContractMethod.Pour, new PourInput
            {
                Symbol = symbol,
                Amount = amount
            });
        }

        public TransactionResultDto TurnOn(string symbol)
        {
            return ExecuteMethodWithResult(FaucetContractMethod.TurnOn, new TurnInput
            {
                Symbol = symbol
            });
        }

        public TransactionResultDto TurnOff(string symbol)
        {
            return ExecuteMethodWithResult(FaucetContractMethod.TurnOff, new TurnInput
            {
                Symbol = symbol
            });
        }

        public TransactionResultDto SetLimit(string symbol, long amountLimit, long intervalMinutes)
        {
            return ExecuteMethodWithResult(FaucetContractMethod.SetLimit, new SetLimitInput
            {
                Symbol = symbol,
                AmountLimit = amountLimit,
                IntervalMinutes = intervalMinutes
            });
        }

        public TransactionResultDto Ban(string symbol, string target, bool isBan)
        {
            return ExecuteMethodWithResult(FaucetContractMethod.Ban, new BanInput
            {
                Symbol = symbol,
                Target = target.ConvertAddress(),
                IsBan = isBan
            });
        }

        public TransactionResultDto Send(string target, string symbol, long amount)
        {
            return ExecuteMethodWithResult(FaucetContractMethod.Send, new SendInput
            {
                Target = target.ConvertAddress(),
                Symbol = symbol,
                Amount = amount
            });
        }

        public TransactionResultDto Take(string symbol, long amount)
        {
            return ExecuteMethodWithResult(FaucetContractMethod.Take, new TakeInput
            {
                Symbol = symbol,
                Amount = amount
            });
        }

        public TransactionResultDto Return(string symbol, long amount)
        {
            return ExecuteMethodWithResult(FaucetContractMethod.Return, new ReturnInput
            {
                Symbol = symbol,
                Amount = amount
            });
        }

        public Address GetOwner(string symbol)
        {
            return CallViewMethod<Address>(FaucetContractMethod.GetOwner, new StringValue {Value = symbol});
        }

        public FaucetStatus GetFaucetStatus(string symbol)
        {
            return CallViewMethod<FaucetStatus>(FaucetContractMethod.GetFaucetStatus, new StringValue {Value = symbol});
        }

        public long GetLimitAmount(string symbol)
        {
            return CallViewMethod<Int64Value>(FaucetContractMethod.GetLimitAmount, new StringValue {Value = symbol})
                .Value;
        }

        public long GetIntervalMinutes(string symbol)
        {
            return CallViewMethod<Int64Value>(FaucetContractMethod.GetIntervalMinutes, new StringValue {Value = symbol})
                .Value;
        }

        public bool IsBannedByOwner(string target, string symbol)
        {
            return CallViewMethod<BoolValue>(FaucetContractMethod.IsBannedByOwner, new IsBannedByOwnerInput
            {
                Target = target.ConvertAddress(),
                Symbol = symbol
            }).Value;
        }
    }
}