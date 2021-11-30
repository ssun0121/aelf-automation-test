using System.Collections.Generic;
using System.Linq;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using Shouldly;

namespace AElfChain.Common.Contracts
{
    public enum TxFeesMethod
    {
        InitializeFeesContract,
        ReadCpuCountTest,
        WriteRamCountTest,
        NoReadWriteCountTest,
        ComplexCountTest,
        
        SetMethodFee,
        GetContractName,
        QueryContractResource,
        GetMethodFeeController,
        GetMethodFee
    }

    public class TransactionFeesContract : BaseContract<TxFeesMethod>
    {
        public TransactionFeesContract(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }

        public TransactionFeesContract(INodeManager nodeManager, string callAddress)
            : base(nodeManager, ContractFileName, callAddress)
        {
        }

        public static string ContractFileName => "AElf.Contracts.TestContract.TransactionFees";

        public void InitializeTxFees(Address address)
        {
            var initializeResult = ExecuteMethodWithResult(TxFeesMethod.InitializeFeesContract, address);
            initializeResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        public static TransactionFeesContract GetOrDeployTxFeesContract(INodeManager nodeManager, string callAddress)
        {
            var genesis = nodeManager.GetGenesisContract();
            var addressList = genesis.QueryCustomContractByMethodName("InitializeFeesContract");
            if (addressList.Count == 0)
            {
                var contract = new TransactionFeesContract(nodeManager, callAddress);
                return contract;
            }

            return new TransactionFeesContract(nodeManager, callAddress, addressList.First().ToBase58());
        }
    }
}