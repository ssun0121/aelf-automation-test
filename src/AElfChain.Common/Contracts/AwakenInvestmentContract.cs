using System;
using AElf.Client.Dto;
using AElfChain.Common.Managers;
using AElf.Types;

using Awaken.Contracts.Investment;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
{
    public enum AwakenInvestmentContractMethod
    {
        //Action
        Initialize,
        ReBalance,
        SetTool,
        Harvest,
        Withdraw,
        WithdrawWithReBalance,
        DisableProvider,
        EnableProvider,
        AddProvider,
        ChooseProvider,
        ChangeReservesRatio,
        AddAdmin,
        RemoveAdmin,
        ChangeBeneficiary,
        ChangeRouter,
        Start,
        EmergencePause,
        EmergenceWithdraw,
        AddRouter,

        //View
        ProvidersLength,
        EarnedCurrent,
        Owner,
        IsAdmin,
        Beneficiary,
        Tool,
        Frozen,
        Deposits,
        ReservesRatios,
        ChosenProviders,
        Providers,
        Routers,
        RouterId
    }

    public class AwakenInvestmentContract : BaseContract<AwakenInvestmentContractMethod>
    {
        public AwakenInvestmentContract(INodeManager nodeManager, string callAddress) : base(nodeManager,
            "Awaken.Contracts.Investment", callAddress)
        {
        }
        public AwakenInvestmentContract(INodeManager nodeManager, string callAddress, string contractAddress) : base(
            nodeManager,
            contractAddress)
        {
            SetAccount(callAddress);
        }
        
        
         
        
         /*
         public TransactionResultDto Initialize()
        {
            var result = ExecuteMethodWithResult(AwakenInvestmentContractMethod.Initialize,new Empty());
            
            return result;
        }
        */
        
        
        

        //set
        public TransactionResultDto ReBalance(string tokenSymbol, Address router)
        {
            var result = ExecuteMethodWithResult(AwakenInvestmentContractMethod.ReBalance, new ReBalanceInput
            {
                TokenSymbol = tokenSymbol,
                Router = router
            });
            return result;
        }
        
        public TransactionResultDto SetTool(string InitAccount, Address toolAddress)
        {
            SetAccount(InitAccount);
            return ExecuteMethodWithResult(AwakenInvestmentContractMethod.SetTool, toolAddress);
        }
        public TransactionResultDto Harvest(string tokenSymbol)
        {
            var result = ExecuteMethodWithResult(AwakenInvestmentContractMethod.Harvest, new HarvestInput
            {
                TokenSymbol = tokenSymbol,
            });
            return result;
        }
        
        public TransactionResultDto Withdraw(string tokenSymbol, long amount)
        {
            var result = ExecuteMethodWithResult(AwakenInvestmentContractMethod.Withdraw, new WithdrawInput
            {
                TokenSymbol = tokenSymbol,
                Amount =  amount
            });
            return result;
        }
        
        public TransactionResultDto WithdrawWithReBalance(string tokenSymbol, long amount)
        {
            var result = ExecuteMethodWithResult(AwakenInvestmentContractMethod.WithdrawWithReBalance, new WithdrawInput
            {
                TokenSymbol = tokenSymbol,
                Amount =  amount
            });
            return result;
        }

        public TransactionResultDto DisableProvider(int providerId)
        {
            return ExecuteMethodWithResult(AwakenInvestmentContractMethod.DisableProvider, new Int32Value
            {
                Value = providerId
            });
        }
        
        public TransactionResultDto EnableProvider(int providerId)
        {
            return ExecuteMethodWithResult(AwakenInvestmentContractMethod.EnableProvider, new Int32Value
            {
                Value = providerId
            });
        }
        
        public TransactionResultDto AddProvider(string tokenSymbol, Address vaultAddress)
        {
            var result = ExecuteMethodWithResult(AwakenInvestmentContractMethod.AddProvider, new AddProviderInput
            {
                TokenSymbol = tokenSymbol,
                VaultAddress =  vaultAddress 
            });
            return result;
        }
        
        public TransactionResultDto ChooseProvider(string tokenSymbol, int providerId)
        {
            var result = ExecuteMethodWithResult(AwakenInvestmentContractMethod.ChooseProvider, new ChooseProviderInput
            {
                TokenSymbol = tokenSymbol,
                ProviderId  =  providerId 
            });
            return result;
        }
        
        public TransactionResultDto ChangeReservesRatio(string tokenSymbol, int reservesRatio)
        {
            var result = ExecuteMethodWithResult(AwakenInvestmentContractMethod.ChangeReservesRatio, new ChangeReservesRatioInput
            {
                TokenSymbol = tokenSymbol,
                ReservesRatio  =  reservesRatio 
            });
            return result;
        }
        
        public TransactionResultDto AddAdmin(string InitAccount, Address adminAddress)
        {
            SetAccount(InitAccount);
            return ExecuteMethodWithResult(AwakenInvestmentContractMethod.AddAdmin, adminAddress);
        }
        
        public TransactionResultDto RemoveAdmin(string InitAccount, Address adminAddress)
        {
            SetAccount(InitAccount);
            return ExecuteMethodWithResult(AwakenInvestmentContractMethod.RemoveAdmin, adminAddress);
        }
        
        public TransactionResultDto ChangeBeneficiary(string InitAccount, Address BeneficiaryAddress)
        {
            SetAccount(InitAccount);
            return ExecuteMethodWithResult(AwakenInvestmentContractMethod.ChangeBeneficiary, BeneficiaryAddress);
        }
        
        public TransactionResultDto ChangeRouter(Address oldRouter, Address newRouter)
        {
            var result = ExecuteMethodWithResult(AwakenInvestmentContractMethod.ChangeRouter, new ChangeRouterInput
            {
                OldRouter = oldRouter,
                NewRouter  =  newRouter 
            });
            return result;
        }
        
        public TransactionResultDto Start()
        {
            return ExecuteMethodWithResult(AwakenInvestmentContractMethod.Start, new Empty());
        }
        
        public TransactionResultDto EmergencePause()
        {
            return ExecuteMethodWithResult(AwakenInvestmentContractMethod.EmergencePause, new Empty());
        }
        
        public TransactionResultDto EmergenceWithdraw(string tokenSymbol,int providerId,long amount, Address toAddress)
        {
            var result = ExecuteMethodWithResult(AwakenInvestmentContractMethod.EmergenceWithdraw, new EmergenceWithdrawInput
            {
                TokenSymbol = tokenSymbol,
                ProviderId  =  providerId,
                Amount  =  amount,
                To  =  toAddress 
            });
            return result;
        }
        
        public TransactionResultDto AddRouter(string InitAccount, Address routerAddress)
        {
            SetAccount(InitAccount);
            return ExecuteMethodWithResult(AwakenInvestmentContractMethod.AddRouter, routerAddress);
        }

        

        //view
        public int ProvidersLength()
        {
            return CallViewMethod<Int32Value>(AwakenInvestmentContractMethod.ProvidersLength, new Empty()).Value;
        }
        
        public EarnedCurrentOutput EarnedCurrent(string tokenSymbol)
        {
            return CallViewMethod<EarnedCurrentOutput>(AwakenInvestmentContractMethod.EarnedCurrent, new Token
            {
                TokenSymbol = tokenSymbol
            });

        }

        public Address Owner()
        {
            return CallViewMethod<Address>(AwakenInvestmentContractMethod.Owner, new Empty());
        }

        public bool IsAdmin(Address address)
        {
            return CallViewMethod<BoolValue>(AwakenInvestmentContractMethod.IsAdmin, address).Value;

        }
  
        public Address Beneficiary()
        {
            return CallViewMethod<Address>(AwakenInvestmentContractMethod.Beneficiary, new Empty());
        }
       
        public Address Tool()
        {
            return CallViewMethod<Address>(AwakenInvestmentContractMethod.Tool, new Empty());
        }

        public bool Frozen()
        {
            return CallViewMethod<BoolValue>(AwakenInvestmentContractMethod.Frozen, new Empty()).Value;
        }
        
        public long Deposits(string token,Address router)
        {
            return CallViewMethod<Int64Value>(AwakenInvestmentContractMethod.Deposits, new DepositViewInput{
                Token = token,
                Router = router
                
            }).Value;
        }
        
        public BigIntValue ReservesRatios(string tokenSymbol)
        {
            return CallViewMethod<BigIntValue>(AwakenInvestmentContractMethod.ReservesRatios, new StringValue
            {
                Value = tokenSymbol
            }).Value;
        }
        
        public int  ChosenProviders(string tokenSymbol)
        {
            return CallViewMethod<Int32Value>(AwakenInvestmentContractMethod.ChosenProviders, new StringValue
            {
                Value = tokenSymbol
            }).Value;
        }

        public Provider Providers(int providerId)
        {
            return CallViewMethod<Provider>(AwakenInvestmentContractMethod.Providers, new Int32Value
            {
                Value = providerId,

            });

        }

        public Address Routers(int routersId)
        {
            return CallViewMethod<Address>(AwakenInvestmentContractMethod.Routers,new Int32Value
            {
                Value = routersId
                
            });

        }

        public int RouterId(Address address)
        {
            return CallViewMethod<Int32Value>(AwakenInvestmentContractMethod.RouterId, address).Value;

        }

        
    }
    
} 
