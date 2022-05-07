
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Awaken.Contracts.Investment;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Shouldly;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class InvestmentTest
    {
        private GenesisContract _genesisContract;
        private TokenContract _tokenContract;
        private AwakenTokenContract _awakenTokenContract;
        private AwakenSwapContract _awakenSwapContract;
        private AwakenInvestmentContract _awakenInvestmentContract;
        private AwakenProviderContract _awakenProviderContract;
        
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private string InitAccount { get; } = "nn659b9X1BLhnu5RWmEUbuuV7J9QKVVSN54j9UmeCbF3Dve5D";
        private string AdminAddress { get; } = "YUW9zH5GhRboT5JK4vXp5BLAfCDv28rRmTQwo418FuaJmkSg8";
        private string UserAddress { get; } = "FHdcx45K5kovWsAKSb3rrdyNPFus8eoJ1XTQE7aXFHTgfpgzN"; 
        private string ToolAddress { get; } = "nn659b9X1BLhnu5RWmEUbuuV7J9QKVVSN54j9UmeCbF3Dve5D"; 
        private string Beneficiary { get; } = "2Pm6opkpmnQedgc5yDUoVmnPpTus5vSjFNDmSbckMzLw22W6Er"; 
        private string Beneficiary1 { get; } = "29qJkBMWU2Sv6mTqocPiN8JTjcsSkBKCC8oUt11fTE7oHmfG3n"; 
        
        private static string RpcUrl { get; } = "http://172.25.127.105:8000";
        //ZHCSr8KsQptzYaodJLReDtn4XYWqLtQA4Bs3zB1KaaP6yCSWM
        private string awakenTokenContract = "";
        private string investmentAddress = "";
        private string providerAddress = "";
        private string providerAddress1 = "";
        private string routerAddress = "";
        private string routerAddress1 = "";

        
        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("InvestmentTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-new-env-main");

            NodeManager = new NodeManager(RpcUrl);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
            
            if (investmentAddress.Equals(""))
                _awakenInvestmentContract = new AwakenInvestmentContract(NodeManager, InitAccount);
            else
                _awakenInvestmentContract = new AwakenInvestmentContract(NodeManager, InitAccount, investmentAddress);
            
            if (providerAddress.Equals(""))
                _awakenProviderContract = new AwakenProviderContract(NodeManager, InitAccount);
            else
                _awakenProviderContract = new AwakenProviderContract(NodeManager, InitAccount, investmentAddress);
            
            if (providerAddress1.Equals(""))
                _awakenProviderContract = new AwakenProviderContract(NodeManager, InitAccount);
            else
                _awakenProviderContract = new AwakenProviderContract(NodeManager, InitAccount, investmentAddress);
            
            _awakenTokenContract = awakenTokenContract == ""
                ? new AwakenTokenContract(NodeManager, InitAccount)
                : new AwakenTokenContract(NodeManager, InitAccount, awakenTokenContract);
            
            _awakenSwapContract = routerAddress == ""
                ? new AwakenSwapContract(NodeManager, InitAccount)
                : new AwakenSwapContract(NodeManager, InitAccount, routerAddress);
            _awakenSwapContract = routerAddress1 == ""
                ? new AwakenSwapContract(NodeManager, InitAccount)
                : new AwakenSwapContract(NodeManager, InitAccount, routerAddress1);
            
            //SetVault();
           // AddSupportToken();
        }
        
        private void SetVault()
        {
            _awakenSwapContract.SetAccount(InitAccount);
            _awakenSwapContract.SetVault(investmentAddress.ConvertAddress());
        }

        private void AddSupportToken()
        {
            var tokenSymbol = "ELF";
            var tokenSymbol1 = "USDT";
            var lend = "".ConvertAddress();
            var lend1 = "".ConvertAddress();
            var lendingLens = "uBvnFUUKG43qfnjPqoXB8S4nHkHaPXYgjMDn5B2CRPigUeM7B".ConvertAddress();
            var profitTokenSymbol = "ELF";  
            
            var result =
                _awakenProviderContract.AddSupportToken(
                    tokenSymbol,
                    lend,
                    lendingLens,
                    profitTokenSymbol
                );
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            _awakenProviderContract.AddSupportToken(
                tokenSymbol1,
                lend1,
                lendingLens,
                profitTokenSymbol
            );
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }


        [TestMethod]
        public void AddProvider()
        {
            var tokenSymbol = "ELF";
            var providerId = 1;
            
            //owner AddProvider success
            _awakenInvestmentContract.SetAccount(InitAccount);
            var result =
                _awakenInvestmentContract.ExecuteMethodWithResult(AwakenInvestmentContractMethod.AddProvider, new AddProviderInput
                {
                    TokenSymbol = tokenSymbol,
                    VaultAddress =  providerAddress.ConvertAddress()
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var providersLength = _awakenInvestmentContract.ProvidersLength();
            Logger.Info($"ProvidersLength is {providersLength}");
            providersLength.ShouldBe(1);
            
            var providers = _awakenInvestmentContract.Providers(providerId);
            Logger.Info($"Providers is { providers}");
            providers.Vault.ShouldBe(providerAddress.ConvertAddress());
            providers.TokenSymbol.ShouldBe(tokenSymbol);
            providers.Enable.ShouldBe(false);
            //providers.AccumulateProfit.ShouldBe();
            
            //owner repeat AddProvider Failed
            var resultFailed = _awakenInvestmentContract.ExecuteMethodWithResult(AwakenInvestmentContractMethod.AddProvider, new AddProviderInput
                {
                    TokenSymbol = tokenSymbol,
                    VaultAddress =  providerAddress.ConvertAddress()
                });
            resultFailed.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
            resultFailed.Error.ShouldContain("");
            
            //ontOwner AddProvider Failed
            _awakenInvestmentContract.SetAccount(AdminAddress);
            var resultAdminAddress =
                _awakenInvestmentContract.ExecuteMethodWithResult(AwakenInvestmentContractMethod.AddProvider, new AddProviderInput
                {
                    TokenSymbol = tokenSymbol,
                    VaultAddress =  providerAddress.ConvertAddress()
                });
            resultAdminAddress.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
            resultAdminAddress.Error.ShouldContain("");
        }

        
        [TestMethod]
        public void ChooseProvider()
        {
            var tokenSymbol = "ELF";
            var providerId = 1;

            //owner AddProvider success
            _awakenInvestmentContract.SetAccount(InitAccount);
            var result =
                _awakenInvestmentContract.ExecuteMethodWithResult(AwakenInvestmentContractMethod.AddProvider, new AddProviderInput
                {
                    TokenSymbol = tokenSymbol,
                    VaultAddress =  providerAddress1.ConvertAddress()
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //InitAccount ChooseProvider
            var resultChooseProvider= 
                _awakenInvestmentContract.ExecuteMethodWithResult(AwakenInvestmentContractMethod.ChooseProvider, new ChooseProviderInput
                {
                    TokenSymbol = tokenSymbol,
                    ProviderId  =  providerId 
                });
            resultChooseProvider.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var chosenProviders = _awakenInvestmentContract.ChosenProviders(tokenSymbol);
            Logger.Info($"chosenProviders is {chosenProviders}");
            chosenProviders.ShouldBe(providerId);
            
            var providers = _awakenInvestmentContract.Providers(providerId);
            Logger.Info($"Providers is { providers}");
            providers.Vault.ShouldBe(providerAddress1.ConvertAddress());
            providers.TokenSymbol.ShouldBe(tokenSymbol);
            providers.Enable.ShouldBe(false);
            //providers.AccumulateProfit.ShouldBe();
            
            //NotInitAccount ChooseProvider
            _awakenInvestmentContract.SetAccount(AdminAddress);
            var resultChooseProvider1= 
                _awakenInvestmentContract.ExecuteMethodWithResult(AwakenInvestmentContractMethod.ChooseProvider, new ChooseProviderInput
            {
                TokenSymbol = tokenSymbol,
                ProviderId  =  providerId 
            });
            resultChooseProvider1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
            resultChooseProvider1.Error.ShouldContain("");
        }
        
        [TestMethod]
        public void DisableProviderAndEnableProvider()
        { 
            var tokenSymbol = "ELF";
            var providerId = 1;
            
            var providers = _awakenInvestmentContract.Providers(providerId);
            Logger.Info($"Providers is { providers}");
            providers.Enable.ShouldBe(false);
            //DisableProvider
            _awakenInvestmentContract.SetAccount(InitAccount);
            var result= 
                _awakenInvestmentContract.ExecuteMethodWithResult(AwakenInvestmentContractMethod.DisableProvider, new Int32Value
                {
                    Value = providerId
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            providers.Enable.ShouldBe(true);
            //provider is true,again DisableProvider
            result= 
                _awakenInvestmentContract.ExecuteMethodWithResult(AwakenInvestmentContractMethod.DisableProvider, new Int32Value
                {
                    Value = providerId
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            providers.Enable.ShouldBe(true);
            
            //EnableProvider
            result= 
                _awakenInvestmentContract.ExecuteMethodWithResult(AwakenInvestmentContractMethod.EnableProvider, new Int32Value
                {
                    Value = providerId
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            providers.Enable.ShouldBe(false);
            
            //provider is false,again EnableProvider
            result= 
                _awakenInvestmentContract.ExecuteMethodWithResult(AwakenInvestmentContractMethod.EnableProvider, new Int32Value
                {
                    Value = providerId
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            providers.Enable.ShouldBe(false);

        }

        [TestMethod]
        public void DisableProviderAndEnableProviderFailed()
        {
            var providerId = 1;

            var providers = _awakenInvestmentContract.Providers(providerId);
            Logger.Info($"Providers is { providers}");
            providers.Enable.ShouldBe(false);
            
            //not InitAccount EnableProvider
            _awakenInvestmentContract.SetAccount(AdminAddress);
            var result= 
                _awakenInvestmentContract.ExecuteMethodWithResult(AwakenInvestmentContractMethod.EnableProvider, new Int32Value
                {
                    Value = providerId
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
            providers.Enable.ShouldBe(false);
            
            //InitAccount EnableProvider
            _awakenInvestmentContract.SetAccount(InitAccount);
             result= 
                _awakenInvestmentContract.ExecuteMethodWithResult(AwakenInvestmentContractMethod.EnableProvider, new Int32Value
                {
                    Value = providerId
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            providers.Enable.ShouldBe(true);
            //not InitAccount DisableProvider
            _awakenInvestmentContract.SetAccount(AdminAddress);
            result= 
                _awakenInvestmentContract.ExecuteMethodWithResult(AwakenInvestmentContractMethod.DisableProvider, new Int32Value
                {
                    Value = providerId
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            result.Error.ShouldContain("");
            providers.Enable.ShouldBe(true);            
            
            //InitAccount DisableProvider
            _awakenInvestmentContract.SetAccount(InitAccount);
            result= 
                _awakenInvestmentContract.ExecuteMethodWithResult(AwakenInvestmentContractMethod.DisableProvider, new Int32Value
                {
                    Value = providerId
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
            result.Error.ShouldContain("");
            providers.Enable.ShouldBe(false);
        }

        [TestMethod]
        public void SetToolAddress()
        {
            var result = _awakenInvestmentContract.SetTool(InitAccount, ToolAddress.ConvertAddress());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var toll = _awakenInvestmentContract.Tool();
            Logger.Info($"Tool is { toll}");
            toll.ShouldBe(ToolAddress.ConvertAddress());
        }

        [TestMethod]
        public void ChangeBeneficiary()
        {
            var beneficiary = _awakenInvestmentContract.Beneficiary();
            Logger.Info($"Beneficiary is { beneficiary}");
            beneficiary.ShouldBe(new Address());
            //非InitAccount执行
            var changeBeneficiary = _awakenInvestmentContract.ChangeBeneficiary(AdminAddress, Beneficiary.ConvertAddress());
            changeBeneficiary.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
            changeBeneficiary.Error.ShouldContain("");
            //InitAccount执行
            changeBeneficiary = _awakenInvestmentContract.ChangeBeneficiary(InitAccount, Beneficiary.ConvertAddress());
            changeBeneficiary.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            beneficiary = _awakenInvestmentContract.Beneficiary();
            Logger.Info($"Beneficiary is { beneficiary}");
            beneficiary.ShouldBe(Beneficiary.ConvertAddress());
            //InitAccount更换Beneficiary地址
            changeBeneficiary = _awakenInvestmentContract.ChangeBeneficiary(InitAccount, Beneficiary1.ConvertAddress());
            changeBeneficiary.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            beneficiary = _awakenInvestmentContract.Beneficiary();
            Logger.Info($"Beneficiary is { beneficiary}");
            beneficiary.ShouldBe(Beneficiary1.ConvertAddress());
        }

        [TestMethod]
        public void AddRouter()
        {
            var id = 1;

            //不是InitAccount添加router
           var addRouter = _awakenInvestmentContract.AddRouter(AdminAddress, routerAddress.ConvertAddress());
           addRouter.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
           addRouter.Error.ShouldContain("");
           //InitAccount添加router
           addRouter = _awakenInvestmentContract.AddRouter(InitAccount, routerAddress.ConvertAddress());
           addRouter.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
           var routers = _awakenInvestmentContract.Routers(id);
           Logger.Info($"Routers is { routers}");
           routers.ShouldBe(routerAddress.ConvertAddress());
           var routerId = _awakenInvestmentContract.RouterId(routerAddress.ConvertAddress());
           Logger.Info($"RouterId is { routerId}");
           routerId.ShouldBe(1);
        }

        [TestMethod]
        public void ChangeRouter()
        {
            var id = 1;

            //不是InitAccount修改router
            var addRouter = _awakenInvestmentContract.ChangeRouter(routerAddress.ConvertAddress(), routerAddress1.ConvertAddress());
            addRouter.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
            addRouter.Error.ShouldContain("");
            //InitAccount修改router
            addRouter = _awakenInvestmentContract.ChangeRouter(routerAddress.ConvertAddress(), routerAddress1.ConvertAddress());
            addRouter.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var routers = _awakenInvestmentContract.Routers(id);
            Logger.Info($"Routers is { routers}");
            routers.ShouldBe(routerAddress1.ConvertAddress());
            var routerId = _awakenInvestmentContract.RouterId(routerAddress1.ConvertAddress());
            Logger.Info($"RouterId is { routerId}");
            routerId.ShouldBe(id);
            //InitAccount修改router，oldRouter=newRouter
            addRouter = _awakenInvestmentContract.ChangeRouter(routerAddress1.ConvertAddress(), routerAddress1.ConvertAddress());
            addRouter.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
           
        }

        [TestMethod]
        public void Start ()
        { 
            //不是InitAccount调用开启
            _awakenInvestmentContract.SetAccount(AdminAddress);
            var start= _awakenInvestmentContract.Start();
           start.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
           start.Error.ShouldContain("");

           var frozen= _awakenInvestmentContract.Frozen();
           Logger.Info($"Frozen is { frozen}");
           frozen.ShouldBe(true);
           //InitAccount调用开启
           _awakenInvestmentContract.SetAccount(InitAccount);
           start= _awakenInvestmentContract.Start();
           start.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
           frozen= _awakenInvestmentContract.Frozen();
           Logger.Info($"Frozen is { frozen}");
           frozen.ShouldBe(false);
           //InitAccount再次调用开启
           start= _awakenInvestmentContract.Start();
           start.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
           frozen= _awakenInvestmentContract.Frozen();
           Logger.Info($"Frozen is { frozen}");
           frozen.ShouldBe(false);

        }

        [TestMethod]
        public void EmergencePause()
        {
            //不是InitAccount关闭
            _awakenInvestmentContract.SetAccount(AdminAddress);
            var emergencePause = _awakenInvestmentContract.EmergencePause();
            emergencePause.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
            emergencePause.Error.ShouldContain("");
            //InitAccount关闭
            _awakenInvestmentContract.SetAccount(InitAccount);
            emergencePause = _awakenInvestmentContract.EmergencePause();
            emergencePause.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var frozen= _awakenInvestmentContract.Frozen();
            Logger.Info($"Frozen is { frozen}");
            frozen.ShouldBe(true);
            //InitAccount再次关闭
            _awakenInvestmentContract.SetAccount(InitAccount);
            emergencePause = _awakenInvestmentContract.EmergencePause();
            emergencePause.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined); 
            frozen= _awakenInvestmentContract.Frozen();
            Logger.Info($"Frozen is { frozen}");
            frozen.ShouldBe(true);
        }

        [TestMethod]
        public void AddAdmin()
        {
            var isAdmin = _awakenInvestmentContract.IsAdmin(AdminAddress.ConvertAddress());
            Logger.Info($"Frozen is { isAdmin}");
            isAdmin.ShouldBe(false);
            //InitAccount添加管理员
            var addAdmin = _awakenInvestmentContract.AddAdmin(InitAccount,AdminAddress.ConvertAddress());
            addAdmin.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined); 
            isAdmin = _awakenInvestmentContract.IsAdmin(AdminAddress.ConvertAddress());
            Logger.Info($"Frozen is { isAdmin}");
            isAdmin.ShouldBe(true);
            //不是InitAccount添加管理员
            addAdmin = _awakenInvestmentContract.AddAdmin(AdminAddress,UserAddress.ConvertAddress());
            addAdmin.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed); 
            addAdmin.Error.ShouldContain("");
        }

        [TestMethod]
        public void RemoveAdmin()
        {
            var isAdmin = _awakenInvestmentContract.IsAdmin(AdminAddress.ConvertAddress());
            Logger.Info($"Frozen is { isAdmin}");
            isAdmin.ShouldBe(false);
            //InitAccount添加管理员
            var addAdmin = _awakenInvestmentContract.RemoveAdmin(InitAccount,AdminAddress.ConvertAddress());
            addAdmin.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined); 
            isAdmin = _awakenInvestmentContract.IsAdmin(AdminAddress.ConvertAddress());
            Logger.Info($"Frozen is { isAdmin}");
            isAdmin.ShouldBe(true);
            //不是InitAccount添加管理员
            addAdmin = _awakenInvestmentContract.AddAdmin(AdminAddress,UserAddress.ConvertAddress());
            addAdmin.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed); 
            addAdmin.Error.ShouldContain("");
        }

        [TestMethod]
        public void ChangeReservesRatio()
        {
            var tokenSymbol = "ELF";
            var reservesRatio = 1000;
            var reservesRatio1 = 10000;
            var reservesRatio2 = 20000;


            //不是InitAccount设置          
            _awakenInvestmentContract.SetAccount(AdminAddress);
            var changeReservesRatio= _awakenInvestmentContract.ChangeReservesRatio(tokenSymbol,reservesRatio);
           changeReservesRatio.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
           changeReservesRatio.Error.ShouldContain("");
            //InitAccount设置 
           _awakenInvestmentContract.SetAccount(InitAccount);
           changeReservesRatio= _awakenInvestmentContract.ChangeReservesRatio(tokenSymbol,reservesRatio1);
           changeReservesRatio.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
           var reservesRatios = _awakenInvestmentContract.ReservesRatios(tokenSymbol);
           Logger.Info($"ReservesRatios is { reservesRatios}");
           reservesRatios.ShouldBe(reservesRatio1);
           //InitAccount修改权重
           _awakenInvestmentContract.SetAccount(InitAccount);
           changeReservesRatio= _awakenInvestmentContract.ChangeReservesRatio(tokenSymbol,reservesRatio);
           changeReservesRatio.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
           reservesRatios = _awakenInvestmentContract.ReservesRatios(tokenSymbol);
           Logger.Info($"ReservesRatios is { reservesRatios}");
           reservesRatios.ShouldBe(reservesRatio);
           //InitAccount修改错误的权重
           _awakenInvestmentContract.SetAccount(InitAccount);
           changeReservesRatio= _awakenInvestmentContract.ChangeReservesRatio(tokenSymbol,reservesRatio2);
           changeReservesRatio.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
           changeReservesRatio.Error.ShouldContain("");
        }
        [TestMethod]
        public void ReBalance()
        {
            var tokenSymbol = "ELF";
            var reservesRatio = 2000;
            //查看当前运行状态
            var frozen= _awakenInvestmentContract.Frozen();
            Logger.Info($"Frozen is { frozen}");
            frozen.ShouldBe(true);
            //例：关闭的状态下，InitAccount调用ReBalance       
            _awakenInvestmentContract.SetAccount(InitAccount);
            var reBalance= _awakenInvestmentContract.ReBalance(tokenSymbol, routerAddress.ConvertAddress());
            reBalance.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
            reBalance.Error.ShouldContain("");
            
            //开启合约
             _awakenInvestmentContract.Start();
             frozen= _awakenInvestmentContract.Frozen();
             frozen.ShouldBe(false);
             //查看比例
             var reservesRatios = _awakenInvestmentContract.ReservesRatios(tokenSymbol);
             reservesRatios.ShouldBe(1000);
             //查看余额
             var deposits = _awakenInvestmentContract.Deposits(tokenSymbol,routerAddress.ConvertAddress());
             Logger.Info($"Providers is {deposits}");
             var getUserBalance =_tokenContract.GetUserBalance(routerAddress, tokenSymbol);
             Logger.Info($"Providers is {getUserBalance}");
             //例：开启状态下，不是InitAccount调用ReBalance 
             _awakenInvestmentContract.SetAccount(AdminAddress);
             reBalance= _awakenInvestmentContract.ReBalance(tokenSymbol, routerAddress.ConvertAddress());
             reBalance.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
             reBalance.Error.ShouldContain("");
             //例：开启状态下，InitAccount调用ReBalance
             _awakenInvestmentContract.SetAccount(InitAccount);
             reBalance= _awakenInvestmentContract.ReBalance(tokenSymbol, routerAddress.ConvertAddress());
             reBalance.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
             
             deposits = _awakenInvestmentContract.Deposits(tokenSymbol,routerAddress.ConvertAddress());
             Logger.Info($"Providers is {deposits}");
             getUserBalance =_tokenContract.GetUserBalance(routerAddress, tokenSymbol);
             Logger.Info($"Providers is {getUserBalance}");
             (deposits /getUserBalance).ShouldBe(9);
             //例：开启状态下，修改权重后，InitAccount调用ReBalance
             _awakenInvestmentContract.SetAccount(InitAccount);
             _awakenInvestmentContract.ChangeReservesRatio(tokenSymbol,reservesRatio);
             reservesRatios = _awakenInvestmentContract.ReservesRatios(tokenSymbol);
             reservesRatios.ShouldBe(reservesRatio);
             reBalance= _awakenInvestmentContract.ReBalance(tokenSymbol, routerAddress.ConvertAddress());
             reBalance.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
             deposits = _awakenInvestmentContract.Deposits(tokenSymbol,routerAddress.ConvertAddress());
             Logger.Info($"Providers is {deposits}");
             getUserBalance =_tokenContract.GetUserBalance(routerAddress, tokenSymbol);
             Logger.Info($"Providers is {getUserBalance}");
             (deposits /getUserBalance).ShouldBe(4);
        }

        [TestMethod]
        public void swap_ReBalance()
        {
            var tokenSymbol = "ELF";
            var tokenSymbol1 = "USDT";
            var amountA = 2000_00000000;
            var amountB = 1000_00000000;
            var removeLpTokenAmount = 10_00000000;
            var pairSymbol = _awakenSwapContract.GetTokenPairSymbol(tokenSymbol, tokenSymbol1);
            
            
            var deposits = _awakenInvestmentContract.Deposits(tokenSymbol,routerAddress.ConvertAddress());
            Logger.Info($"Providers is {deposits}");
            var getUserBalance =_tokenContract.GetUserBalance(routerAddress, tokenSymbol);
            Logger.Info($"Providers is {getUserBalance}");
            //approve
            _tokenContract.SetAccount(InitAccount);
            var approveToken =
                _tokenContract.ApproveToken(InitAccount, _awakenSwapContract.ContractAddress,1000000_00000000 ,tokenSymbol );
            approveToken.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            approveToken =
                _tokenContract.ApproveToken(InitAccount, _awakenSwapContract.ContractAddress,1000000_00000000 ,tokenSymbol1 );
            approveToken.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _awakenTokenContract.ApproveLPToken(_awakenSwapContract.ContractAddress, InitAccount,
                1000000_00000000,
                pairSymbol);
            approveToken.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //AddLiquidity
            var addLiquidity = _awakenSwapContract.AddLiquidity(out _, tokenSymbol, tokenSymbol1, amountA, amountB,
                InitAccount.ConvertAddress());
            addLiquidity.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //例：开启状态下，InitAccount调用ReBalance
            _awakenInvestmentContract.SetAccount(InitAccount);
            var reBalance= _awakenInvestmentContract.ReBalance(tokenSymbol, routerAddress.ConvertAddress());
            reBalance.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            deposits = _awakenInvestmentContract.Deposits(tokenSymbol,routerAddress.ConvertAddress());
            Logger.Info($"Providers is {deposits}");
            getUserBalance =_tokenContract.GetUserBalance(routerAddress, tokenSymbol);
            Logger.Info($"Providers is {getUserBalance}");
            (deposits /getUserBalance).ShouldBe(4);
            
            //RemoveLiquidity
            _awakenSwapContract.RemoveLiquidity(out _, tokenSymbol, tokenSymbol1, removeLpTokenAmount,
                InitAccount.ConvertAddress());
            //例：开启状态下，InitAccount调用ReBalance
            _awakenInvestmentContract.SetAccount(InitAccount);
            reBalance= _awakenInvestmentContract.ReBalance(tokenSymbol, routerAddress.ConvertAddress());
            reBalance.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            deposits = _awakenInvestmentContract.Deposits(tokenSymbol,routerAddress.ConvertAddress());
            Logger.Info($"Providers is {deposits}");
            getUserBalance =_tokenContract.GetUserBalance(routerAddress, tokenSymbol);
            Logger.Info($"Providers is {getUserBalance}");
            (deposits /getUserBalance).ShouldBe(4);
        }

        [TestMethod]
        public void EmergenceWithdraw()
        {
            var tokenSymbol = "ELF";
            var id = 1;
            var amount = 10_00000000;
            var amount1 = 1000000_00000000;
            //平台处于关闭状态
            _awakenInvestmentContract.SetAccount(InitAccount);
            var emergencePause = _awakenInvestmentContract.EmergencePause();
            emergencePause.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined); 
            var frozen= _awakenInvestmentContract.Frozen();
            Logger.Info($"Frozen is { frozen}");
            frozen.ShouldBe(true);
            //例：关闭状态下InitAccount进行紧急提取
            var getUserBalanceBefore =_tokenContract.GetUserBalance(UserAddress, tokenSymbol);
            Logger.Info($"Providers is {getUserBalanceBefore}");
            var depositsBefore = _awakenInvestmentContract.Deposits(tokenSymbol,routerAddress.ConvertAddress());
            Logger.Info($"Providers is {depositsBefore}");
            _awakenInvestmentContract.SetAccount(InitAccount);
            var emergenceWithdraw = _awakenInvestmentContract.EmergenceWithdraw(tokenSymbol, id, amount, UserAddress.ConvertAddress());
            emergenceWithdraw.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var getUserBalanceAfter =_tokenContract.GetUserBalance(UserAddress, tokenSymbol);
            Logger.Info($"Providers is {getUserBalanceAfter}");
            getUserBalanceAfter.ShouldBe(getUserBalanceBefore + amount);
            var depositsAfter = _awakenInvestmentContract.Deposits(tokenSymbol,routerAddress.ConvertAddress());
            Logger.Info($"Providers is {depositsAfter}");
            depositsAfter.ShouldBe(depositsBefore - amount);
            
            //平台处于开启状态
            _awakenInvestmentContract.SetAccount(InitAccount);
            var start= _awakenInvestmentContract.Start();
            start.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            frozen= _awakenInvestmentContract.Frozen();
            Logger.Info($"Frozen is { frozen}");
            frozen.ShouldBe(false);
            //例：开启状态下不是InitAccount进行紧急提取
            _awakenInvestmentContract.SetAccount(AdminAddress);
            emergenceWithdraw = _awakenInvestmentContract.EmergenceWithdraw(tokenSymbol, id, amount, UserAddress.ConvertAddress());
            emergenceWithdraw.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
            emergenceWithdraw.Error.ShouldContain("");
            //例：开启状态下InitAccount进行紧急提取
            var getUserBalanceBefore1 =_tokenContract.GetUserBalance(UserAddress, tokenSymbol);
            Logger.Info($"Providers is {getUserBalanceBefore1}");
            var depositsBefore1 = _awakenInvestmentContract.Deposits(tokenSymbol,routerAddress.ConvertAddress());
            Logger.Info($"Providers is {depositsBefore1}");
            _awakenInvestmentContract.SetAccount(InitAccount);
            var emergenceWithdraw1 = _awakenInvestmentContract.EmergenceWithdraw(tokenSymbol, id, amount, UserAddress.ConvertAddress());
            emergenceWithdraw1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var getUserBalanceAfter1 =_tokenContract.GetUserBalance(UserAddress, tokenSymbol);
            Logger.Info($"Providers is {getUserBalanceAfter1}");
            getUserBalanceAfter1.ShouldBe(getUserBalanceBefore1 + amount);
            var depositsAfter1 = _awakenInvestmentContract.Deposits(tokenSymbol,routerAddress.ConvertAddress());
            Logger.Info($"Providers is {depositsAfter1}");
            depositsAfter1.ShouldBe(depositsBefore1 - amount);
            //例：开启状态下InitAccount进行紧急提取,提取数量大于deposits数量
            var depositsBefore2 = _awakenInvestmentContract.Deposits(tokenSymbol,routerAddress.ConvertAddress());
            Logger.Info($"Providers is {depositsBefore2}");
            _awakenInvestmentContract.SetAccount(InitAccount);
            var emergenceWithdrawFaile = _awakenInvestmentContract.EmergenceWithdraw(tokenSymbol, id, amount1, UserAddress.ConvertAddress());
            emergenceWithdrawFaile.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
            emergenceWithdrawFaile.Error.ShouldContain("");
        }

        [TestMethod]
        public void Withdraw()
        {
            var tokenSymbol = "ELF";
            var amount = 10_00000000;
            var amount1 = 1000000_00000000;
            
            //平台处于关闭状态
            _awakenInvestmentContract.SetAccount(InitAccount);
            var emergencePause = _awakenInvestmentContract.EmergencePause();
            emergencePause.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined); 
            var frozen= _awakenInvestmentContract.Frozen();
            Logger.Info($"Frozen is { frozen}");
            frozen.ShouldBe(true);
            //例：关闭状态下InitAccount提取
            _awakenInvestmentContract.SetAccount(InitAccount);
            var withdraw = _awakenInvestmentContract.Withdraw(tokenSymbol, amount);
            withdraw.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
            withdraw.Error.ShouldContain("");
           
            //平台处于开启状态
            _awakenInvestmentContract.SetAccount(InitAccount);
            var start= _awakenInvestmentContract.Start();
            start.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            frozen= _awakenInvestmentContract.Frozen();
            Logger.Info($"Frozen is { frozen}");
            frozen.ShouldBe(false);
            //例：开启状态下不是InitAccount进行紧急提取
            _awakenInvestmentContract.SetAccount(AdminAddress);
            withdraw = _awakenInvestmentContract.Withdraw(tokenSymbol, amount);
            withdraw.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
            withdraw.Error.ShouldContain("");
            //例：开启状态下InitAccount进行提取
            var getUserBalanceBefore1 =_tokenContract.GetUserBalance(routerAddress, tokenSymbol);
            Logger.Info($"Providers is {getUserBalanceBefore1}");
            var depositsBefore1 = _awakenInvestmentContract.Deposits(tokenSymbol,routerAddress.ConvertAddress());
            Logger.Info($"Providers is {depositsBefore1}");
            _awakenInvestmentContract.SetAccount(InitAccount);
            var withdraw1 = _awakenInvestmentContract.Withdraw(tokenSymbol, amount);
            withdraw1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var getUserBalanceAfter1 =_tokenContract.GetUserBalance(routerAddress, tokenSymbol);
            Logger.Info($"Providers is {getUserBalanceAfter1}");
            getUserBalanceAfter1.ShouldBe(getUserBalanceBefore1 + amount);
            var depositsAfter1 = _awakenInvestmentContract.Deposits(tokenSymbol,routerAddress.ConvertAddress());
            Logger.Info($"Providers is {depositsAfter1}");
            depositsAfter1.ShouldBe(depositsBefore1 - amount);
            //例：开启状态下InitAccount进行提取,提取数量大于deposits数量
            var depositsBefore2 = _awakenInvestmentContract.Deposits(tokenSymbol,routerAddress.ConvertAddress());
            Logger.Info($"Providers is {depositsBefore2}");
            _awakenInvestmentContract.SetAccount(InitAccount);
            var withdrawFaile = _awakenInvestmentContract.Withdraw(tokenSymbol, amount1);
            withdrawFaile.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
            withdrawFaile.Error.ShouldContain("");
        }

        [TestMethod]
        public void WithdrawWithReBalance()
        {
            var tokenSymbol = "ELF";
            var amount = 10_00000000;
            var amount1 = 1000000_00000000;
            
            //平台处于关闭状态
            _awakenInvestmentContract.SetAccount(InitAccount);
            var emergencePause = _awakenInvestmentContract.EmergencePause();
            emergencePause.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined); 
            var frozen= _awakenInvestmentContract.Frozen();
            Logger.Info($"Frozen is { frozen}");
            frozen.ShouldBe(true);
            //例：关闭状态下不是InitAccount提取
            _awakenInvestmentContract.SetAccount(InitAccount);
            var withdrawWithReBalance = _awakenInvestmentContract.WithdrawWithReBalance(tokenSymbol, amount);
            withdrawWithReBalance.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
            withdrawWithReBalance.Error.ShouldContain("");
            
            //平台处于开启状态
            _awakenInvestmentContract.SetAccount(InitAccount);
            var start= _awakenInvestmentContract.Start();
            start.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            frozen= _awakenInvestmentContract.Frozen();
            Logger.Info($"Frozen is { frozen}");
            frozen.ShouldBe(false);
            //例：开启状态下不是InitAccount进行提取
            _awakenInvestmentContract.SetAccount(AdminAddress);
            withdrawWithReBalance = _awakenInvestmentContract.WithdrawWithReBalance(tokenSymbol, amount);
            withdrawWithReBalance.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
            withdrawWithReBalance.Error.ShouldContain("");
            //例：开启状态下InitAccount进行提取
            var getUserBalanceBefore1 =_tokenContract.GetUserBalance(routerAddress, tokenSymbol);
            Logger.Info($"Providers is {getUserBalanceBefore1}");
            var depositsBefore1 = _awakenInvestmentContract.Deposits(tokenSymbol,routerAddress.ConvertAddress());
            Logger.Info($"Providers is {depositsBefore1}");
            _awakenInvestmentContract.SetAccount(InitAccount);
            var withdrawWithReBalance1 = _awakenInvestmentContract.WithdrawWithReBalance(tokenSymbol, amount);
            withdrawWithReBalance1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var getUserBalanceAfter1 =_tokenContract.GetUserBalance(routerAddress, tokenSymbol);
            Logger.Info($"Providers is {getUserBalanceAfter1}");
            var depositsAfter1 = _awakenInvestmentContract.Deposits(tokenSymbol,routerAddress.ConvertAddress());
            Logger.Info($"Providers is {depositsAfter1}");
            //查看比例
            var reservesRatios = _awakenInvestmentContract.ReservesRatios(tokenSymbol);
            reservesRatios.ShouldBe(2000);
            (depositsAfter1 / getUserBalanceAfter1).ShouldBe(4);
            //例：开启状态下InitAccount进行提取,提取数量大于deposits数量
            var depositsBefore2 = _awakenInvestmentContract.Deposits(tokenSymbol,routerAddress.ConvertAddress());
            Logger.Info($"Providers is {depositsBefore2}");
            _awakenInvestmentContract.SetAccount(InitAccount);
            var withdrawWithReBalanceFaile = _awakenInvestmentContract.WithdrawWithReBalance(tokenSymbol, amount1);
            withdrawWithReBalanceFaile.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Failed);
            withdrawWithReBalanceFaile.Error.ShouldContain("");
        }

        [TestMethod]
        public void Harvest()
        {
            
            /*
            var tokenSymbol = "ELF";
            var harvest = _awakenInvestmentContract.Harvest(tokenSymbol);
            harvest.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            */

        }



    }

}