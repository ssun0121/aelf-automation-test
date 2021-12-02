using AElf.Client.Dto;
using AElf.Contracts.Faucet;
using AElfChain.Common.Managers;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using Google.Protobuf.WellKnownTypes;
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
        GetIntervalMinutes
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

        public TransactionResultDto NewFaucet(string symbol, long amount, string owner, long amountLimit,
            long intervalMinutes)
        {
            return ExecuteMethodWithResult(FaucetContractMethod.NewFaucet, new NewFaucetInput
            {
                Symbol = symbol,
                Amount = amount,
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

        public TransactionResultDto TurnOn(string symbol, Timestamp at)
        {
            return ExecuteMethodWithResult(FaucetContractMethod.TurnOn, new TurnInput
            {
                Symbol = symbol,
                At = at
            });
        }

        public TransactionResultDto TurnOff(string symbol, Timestamp at)
        {
            return ExecuteMethodWithResult(FaucetContractMethod.TurnOff, new TurnInput
            {
                Symbol = symbol,
                At = at
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
            return ExecuteMethodWithResult(FaucetContractMethod.Take, new SendInput
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
            return CallViewMethod<Int64Value>(FaucetContractMethod.GetLimitAmount, new StringValue {Value = symbol}).Value;
        }
        
        public long GetIntervalMinutes(string symbol)
        {
            return CallViewMethod<Int64Value>(FaucetContractMethod.GetIntervalMinutes, new StringValue {Value = symbol}).Value;
        }
        
        public static FaucetContract GetFaucetContract(INodeManager nm, string callAddress)
        {
            var genesisContract = nm.GetGenesisContractAddress();

            return new FaucetContract(nm, callAddress, genesisContract);
        }
    }
}