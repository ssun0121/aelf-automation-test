using System.Threading.Tasks;
using AElf.Contracts.CentreAssetManagement;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenConverter;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Virgil.Crypto;
using Volo.Abp.Threading;
using Xunit.Sdk;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class CentreAssertContractTests
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }

        private TokenContract _tokenContract;
        private TokenContract _newTokenContracn;
        private TokenConverterContract _tokenConverter;
        private GenesisContract _genesisContract;
        private CentreAssetManagementContract _centreAssetContract;
        private TokenContractContainer.TokenContractStub _tokenContractStub;
        private CentreAssetManagementContainer.CentreAssetManagementStub _centreAssetStub;
        private CentreAssetManagementContainer.CentreAssetManagementStub _adminCentreAssetStub;

        private string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        private string TestAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        private string OtherAccount { get; } = "2REajHMeW2DMrTdQWn89RQ26KQPRg91coCEtPP42EC9Cj7sZ61";

        private static string RpcUrl { get; } = "192.168.197.40:8000";
        private static string NativeSymbol = "ELF";


        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("CentreAssetContractTest_");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env2-main");
            NodeManager = new NodeManager(RpcUrl);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
            _tokenConverter = _genesisContract.GetTokenConverterContract(InitAccount);
            _newTokenContracn = new TokenContract(NodeManager,InitAccount);
            _centreAssetContract = new CentreAssetManagementContract(NodeManager, InitAccount);
            Logger.Info($"CentreAsset contract : {_centreAssetContract}");
            
//            _centreAssetContract = new CentreAssetManagementContract(NodeManager, InitAccount,
//                "Xg6cJsRnCuznxHC1JAyB8XSmxfDnCKTQeJN9fP4ca938MBYgU");
//            _newTokenContrac = new TokenContract(NodeManager, InitAccount,
//                "q6B5hzdSMaXZqjrYHVakngmL1xfUoWyfQnDttf4ktxoRSTUC7");
            _adminCentreAssetStub =
                _centreAssetContract.GetTestStub<CentreAssetManagementContainer.CentreAssetManagementStub>(InitAccount);
            _centreAssetStub =
                _centreAssetContract.GetTestStub<CentreAssetManagementContainer.CentreAssetManagementStub>(TestAccount);
            _tokenContractStub = _tokenContract.GetTestStub<TokenContractContainer.TokenContractStub>(InitAccount);
            _tokenContract.TransferBalance(InitAccount, TestAccount, 1000_00000000, "ELF");
            AsyncHelper.RunSync(InitializeCentreAssetContract);
        }

        [TestMethod]
        public async Task Hello()
        {
            var txResult = await _adminCentreAssetStub.Hello.SendAsync(new Empty());
            txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            txResult.Output.Value.ShouldBe("Hello World!");
            var result = await _tokenContractStub.Transfer.SendAsync(new TransferInput
            {
                Symbol = "ELF",
                Amount = 100,
                To = _genesisContract.Contract
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public async Task CreateHolder()
        {
            var result = await _adminCentreAssetStub.CreateHolder.SendAsync(new HolderCreateDto
            {
                OwnerAddress = InitAccount.ConvertAddress(),
                Symbol = NativeSymbol,
                ManagementAddresses =
                {
                    new ManagementAddress
                    {
                        Address = InitAccount.ConvertAddress(),
                        Amount = 10000_00000000,
                        ManagementAddressesInTotal = 1,
                        ManagementAddressesLimitAmount = 1000_00000000
                    }
                },
                ShutdowAddress = InitAccount.ConvertAddress()
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var id = result.Output.Id;
            Logger.Info($"Holder id is {id}");
        }

        [TestMethod]
        public async Task ApproveUpdateHolder()
        {
            var Id = "ddcd6446db4c61570b25e93af0e4ee493667c88ee9a98e9a2f43744abdce8dd5";
            var holderId = Hash.LoadFromHex(Id);
            var result = await _adminCentreAssetStub.RequestUpdateHolder.SendAsync(new HolderUpdateRequestDto
            {
                HolderId = holderId,
                OwnerAddress = InitAccount.ConvertAddress(),
                ManagementAddresses =
                {
                    new ManagementAddress
                    {
                        Address = InitAccount.ConvertAddress(),
                        Amount = 10000_00000000,
                        ManagementAddressesInTotal = 1,
                        ManagementAddressesLimitAmount = 10000_00000000
                    },
                    new ManagementAddress
                    {
                        Address = TestAccount.ConvertAddress(),
                        Amount = 10000_00000000,
                        ManagementAddressesInTotal = 2,
                        ManagementAddressesLimitAmount = 10000_00000000
                    }
                },
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
//            var holderInfo = _centreAssetContract.CallViewMethod<HolderInfo>(CentreAssertMethod.GetHolderInfo, holderId);
//            holderInfo.ManagementAddresses.First().Value.ManagementAddressesInTotal.ShouldBe(1);

            var approve =
                await _adminCentreAssetStub.ApproveUpdateHolder.SendAsync(new HolderUpdateApproveDto
                    {HolderId = holderId});
            approve.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
//            holderInfo = _centreAssetContract.CallViewMethod<HolderInfo>(CentreAssertMethod.GetHolderInfo, holderId);
//            holderInfo.ManagementAddresses.Count.ShouldBe(2);
//            holderInfo.ManagementAddresses.First().Value.ManagementAddressesInTotal.ShouldBe(1);
        }

        [TestMethod]
        public async Task MoveAssetFromToMainAddress()
        {
            var Id = "ddcd6446db4c61570b25e93af0e4ee493667c88ee9a98e9a2f43744abdce8dd5";
            var amount = 10_00000000;
            var holderId =  Hash.LoadFromHex(Id);
            var userBalance = _tokenContract.GetUserBalance(TestAccount);

            AssetMoveDto assetMoveFromVirtualToMainDto1 = new AssetMoveDto()
            {
                Amount = amount,
                UserToken = NativeSymbol,
                HolderId = holderId
            };
            var virtualAddress1 = await _centreAssetStub.GetVirtualAddress.CallAsync(assetMoveFromVirtualToMainDto1);

            _tokenContract.SetAccount(TestAccount);
            var result = _tokenContract.TransferBalance(TestAccount, virtualAddress1.ToBase58(), amount);
            var fee = result.GetDefaultTransactionFee();
            var userAfterBalance = _tokenContract.GetUserBalance(TestAccount);
            userAfterBalance.ShouldBe(userBalance - amount - fee);
            var virtualAddress1Balance = _tokenContract.GetUserBalance(virtualAddress1.ToBase58());
            Logger.Info($"virtualAddress1Balance: {virtualAddress1Balance}");
            var toResult = await _adminCentreAssetStub.MoveAssetToMainAddress.SendAsync(assetMoveFromVirtualToMainDto1);
            toResult.Output.Success.ShouldBeTrue();
            toResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var virtualAfterAddress1Balance = _tokenContract.GetUserBalance(virtualAddress1.ToBase58());
            virtualAfterAddress1Balance.ShouldBe(virtualAddress1Balance-amount);

            AssetMoveDto assetMoveFromMainToVirtualTokenLockDto1 = new AssetMoveDto()
            {
                Amount = amount / 2,
                UserToken = NativeSymbol,
                HolderId = holderId,
                AddressCategoryHash = HashHelper.ComputeFrom("token_lock")
            };
            var userVoteAddress1 =
                await _centreAssetStub.GetVirtualAddress.CallAsync(assetMoveFromMainToVirtualTokenLockDto1);
            var virtualVoteAddressBalance = _tokenContract.GetUserBalance(userVoteAddress1.ToBase58());

            var fromResult =
                await _adminCentreAssetStub.MoveAssetFromMainAddress.SendAsync(assetMoveFromMainToVirtualTokenLockDto1);
            fromResult.Output.Success.ShouldBeTrue();
            fromResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var virtualVoteAfterAddressBalance = _tokenContract.GetUserBalance(userVoteAddress1.ToBase58());
            virtualVoteAfterAddressBalance.ShouldBe(virtualVoteAddressBalance + amount/2);
            
            var transferResult = await _adminCentreAssetStub.SendTransactionByUserVirtualAddress.SendAsync(
                new SendTransactionByUserVirtualAddressDto()
                {
                    Args = new TransferInput()
                    {
                        Amount = 1_00000000,
                        Symbol = "ELF",
                        To = TestAccount.ConvertAddress()
                    }.ToByteString(),
                    MethodName = "Transfer",
                    To = _tokenContract.Contract,
                    HolderId = holderId,
                    UserToken = NativeSymbol,
                    AddressCategoryHash = HashHelper.ComputeFrom("token_lock"),
                });
            transferResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var virtualVoteAfterTransferAddressBalance = _tokenContract.GetUserBalance(userVoteAddress1.ToBase58());
            virtualVoteAfterTransferAddressBalance.ShouldBe(virtualVoteAfterAddressBalance - 1_00000000);
            
            var userAfterTransferBalance = _tokenContract.GetUserBalance(TestAccount);
            userAfterTransferBalance.ShouldBe(userAfterBalance + 1_00000000);
        }

        [TestMethod]
        public async Task SendTransactionByUserVirtualAddress()
        {
            var Id = "ddcd6446db4c61570b25e93af0e4ee493667c88ee9a98e9a2f43744abdce8dd5";
            var amount = 1_00000000;
            var holderId = Hash.LoadFromHex(Id);
            AssetMoveDto assetMoveFromMainToVirtualTokenLockDto1 = new AssetMoveDto()
            {
                Amount = amount / 2,
                UserToken = NativeSymbol,
                HolderId = holderId,
                AddressCategoryHash = HashHelper.ComputeFrom("token_lock")
            };
            var userVoteAddress1 =
                await _centreAssetStub.GetVirtualAddress.CallAsync(assetMoveFromMainToVirtualTokenLockDto1);

            var transferResult = await _adminCentreAssetStub.SendTransactionByUserVirtualAddress.SendAsync(
                new SendTransactionByUserVirtualAddressDto()
                {
                    Args = new BuyInput()
                    {
                        Amount = amount,
                        Symbol = "CPU"
                    }.ToByteString(),
                    MethodName = "Buy",
                    To = _tokenConverter.Contract,
                    HolderId = holderId,
                    UserToken = NativeSymbol,
                    AddressCategoryHash = HashHelper.ComputeFrom("token_lock"),
                });
            transferResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var cpuBalance = _tokenContract.GetUserBalance(userVoteAddress1.ToBase58(), "CPU");
            cpuBalance.ShouldBe(amount);
        }

        [TestMethod]
        public async Task Withdraw()
        {
            var Id = "7488edec73cfea49108e62b1d09d15bdf41246dc6731c8c7189dfab699aafe05";
            var amount = 10_00000000;
            var holderId = Hash.LoadFromHex(Id);
            var userOriginBalance = _tokenContract.GetUserBalance(OtherAccount);
            await TransferToMainAddress(holderId,amount,NativeSymbol);

            var requestWithdraw = await _centreAssetStub.RequestWithdraw.SendAsync(new WithdrawRequestDto
            {
                Address = OtherAccount.ConvertAddress(),
                Amount = amount/2,
                HolderId = holderId
            });
            requestWithdraw.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var withdrawId = requestWithdraw.Output.Id;

            var approveWithdraw = await _centreAssetStub.ApproveWithdraw.SendAsync(new WithdrawApproveDto
            {
                Id = withdrawId,
                Amount = amount/2,
                Address = OtherAccount.ConvertAddress()
            });
            approveWithdraw.Output.Status.ShouldBe(WithdrawApproveReturnDto.Types.Status.Approving);
            var userBalance = _tokenContract.GetUserBalance(OtherAccount);
            userBalance.ShouldBe(userOriginBalance);
            
            var approveWithdraw2 = await _adminCentreAssetStub.ApproveWithdraw.SendAsync(new WithdrawApproveDto
            {
                Id = withdrawId,
                Amount = amount/2,
                Address = OtherAccount.ConvertAddress()
            });
            approveWithdraw2.Output.Status.ShouldBe(WithdrawApproveReturnDto.Types.Status.Approved);
            userBalance = _tokenContract.GetUserBalance(OtherAccount);
            userBalance.ShouldBe(userOriginBalance + amount/2);
        }
        
        [TestMethod]
        public async Task CancelWithdraw()
        {
            var Id = "7488edec73cfea49108e62b1d09d15bdf41246dc6731c8c7189dfab699aafe05";
            var amount = 10_00000000;
            var holderId = Hash.LoadFromHex(Id);
            var userOriginBalance = _tokenContract.GetUserBalance(OtherAccount);
            await TransferToMainAddress(holderId,amount,NativeSymbol);

            var requestWithdraw = await _adminCentreAssetStub.RequestWithdraw.SendAsync(new WithdrawRequestDto
            {
                Address = OtherAccount.ConvertAddress(),
                Amount = amount/2,
                HolderId = holderId
            });
            requestWithdraw.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var withdrawId = requestWithdraw.Output.Id;
            var userBalance = _tokenContract.GetUserBalance(OtherAccount);
            userBalance.ShouldBe(userOriginBalance);

            var result = await _centreAssetStub.CancelWithdraws.SendAsync(new CancelWithdrawsDto
            {
                HolderId = holderId,
                Ids = {withdrawId}
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var approveWithdraw = await _centreAssetStub.ApproveWithdraw.SendAsync(new WithdrawApproveDto
            {
                Id = withdrawId,
                Amount = amount/2,
                Address = OtherAccount.ConvertAddress()
            });
            approveWithdraw.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        }

        [TestMethod]
        public async Task ShutdownHolder()
        {
            var Id = "ddcd6446db4c61570b25e93af0e4ee493667c88ee9a98e9a2f43744abdce8dd5";
            var holderId = Hash.LoadFromHex(Id);
            var failedResult = await _centreAssetStub.ShutdownHolder.SendAsync(new HolderShutdownDto
            {
                HolderId = holderId
            });
            failedResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
            var result = await _adminCentreAssetStub.ShutdownHolder.SendAsync(new HolderShutdownDto
            {
                HolderId = holderId
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public async Task RebootHolder()
        {
            var Id = "ddcd6446db4c61570b25e93af0e4ee493667c88ee9a98e9a2f43744abdce8dd5";
            var holderId = Hash.LoadFromHex(Id);

            var result = await _adminCentreAssetStub.RebootHolder.SendAsync(new HolderRebootDto()
            {
                HolderId = holderId,
                HolderOwner = InitAccount.ConvertAddress()
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        private async Task InitializeCentreAssetContract()
        {
            var result = await _adminCentreAssetStub.Initialize.SendAsync(new InitializeDto
            {
                CategoryToContactCallWhiteListsMap =
                {
                    {
                        "token_lock", new ContractCallWhiteLists()
                        {
                            List =
                            {
                                new ContractCallWhiteList()
                                {
                                    Address = _tokenContract.Contract,
                                    MethodNames = {"Lock", "Unlock", "Transfer"}
                                },
                                new ContractCallWhiteList()
                                {
                                    Address = _newTokenContracn.Contract,
                                    MethodNames = {"Transfer","Approve","TransferFrom"}
                                },
                                new ContractCallWhiteList()
                                {
                                    Address = _tokenConverter.Contract,
                                    MethodNames = {"Buy", "Sell"}
                                }
                            }
                        }
                    }
                },
                Owner = InitAccount.ConvertAddress()
            });
        }

        private async Task TransferToMainAddress(Hash holderId, long amount,string symbol )
        {
            var userBalance = _tokenContract.GetUserBalance(TestAccount);

            AssetMoveDto assetMoveFromVirtualToMainDto1 = new AssetMoveDto()
            {
                Amount = amount,
                UserToken = symbol,
                HolderId = holderId
            };
            var virtualAddress1 = await _centreAssetStub.GetVirtualAddress.CallAsync(assetMoveFromVirtualToMainDto1);

            _tokenContract.SetAccount(TestAccount);
            var result = _tokenContract.TransferBalance(TestAccount, virtualAddress1.ToBase58(), amount*2);
            var fee = result.GetDefaultTransactionFee();
            var userAfterBalance = _tokenContract.GetUserBalance(TestAccount);
            userAfterBalance.ShouldBe(userBalance - amount*2 - fee);
            var virtualAddress1Balance = _tokenContract.GetUserBalance(virtualAddress1.ToBase58());
            Logger.Info($"virtualAddress1Balance: {virtualAddress1Balance}");
            var toResult = await _centreAssetStub.MoveAssetToMainAddress.SendAsync(assetMoveFromVirtualToMainDto1);
            toResult.Output.Success.ShouldBeTrue();
            toResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var virtualAfterAddress1Balance = _tokenContract.GetUserBalance(virtualAddress1.ToBase58());
//            virtualAfterAddress1Balance.ShouldBe(virtualAddress1Balance-amount);
        }
    }
}