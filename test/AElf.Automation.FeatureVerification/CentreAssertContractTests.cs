using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class CentreAssertContractTests
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }

        private TokenContract _tokenContract;
        private TokenContract _newTokenContract;
        private TokenConverterContract _tokenConverter;
        private GenesisContract _genesisContract;
        private CentreAssetManagementContract _centreAssetContract;
        private TokenContractContainer.TokenContractStub _tokenContractStub;
        private CentreAssetManagementContainer.CentreAssetManagementStub _centreAssetStub;
        private CentreAssetManagementContainer.CentreAssetManagementStub _adminCentreAssetStub;
        private CentreAssetManagementContainer.CentreAssetManagementStub _ownerStub;


        private string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        private string TestAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        private string OwnerAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        private string OtherAccount { get; } = "EKRtNn3WGvFSTDewFH81S7TisUzs9wPyP4gCwTww32waYWtLB";

        private static string RpcUrl { get; } = "192.168.197.40:8000";
        private static string NativeSymbol = "ELF";
        private static string OtherSymbol = "TEST";

        private static string InitAccountToken = "InitAccount";
        private static string TestAccountToken = "TestAccount";
        private static string OtherAccountToken = "OtherAccount";
        private static string HolderId = "4c1b7a3ff558b558d495592d3ba5fe9ff47f7fcaa122fd80e41d11cefdb9f2ba";
        private static string MainAddress = "2oEgh4REs84B7AcCwyAzXpvVSYtKUavTEPfKRPxeMDLV6k5tqr";

        private static string TestHolderId = "f23d2912bfb94ef97293ea64dc36a58b9e89c370f8e83d1687f00457e5fb76b3";
        private static string TestMainAddress = "2WNjhewCSDZPXR6hSxkUwb1uUHr7CqJMvzujmGLxaMdjcC2Jwa";
        
        //on side chain contracts: 2wRDbyVF28VBQoSPgdSEFaL4x7CaXz8TCBujYhgWc9qTMxBE3n
        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("CentreAssetContractTest_");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env2-main");
            NodeManager = new NodeManager(RpcUrl);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
//            _tokenConverter = _genesisContract.GetTokenConverterContract(InitAccount);
//            _newTokenContract = new TokenContract(NodeManager,InitAccount);
            _centreAssetContract = new CentreAssetManagementContract(NodeManager, InitAccount,
                "2YkKkNZKCcsfUsGwCfJ6wyTx5NYLgpCg1stBuRT4z5ep3psXNG");
            Logger.Info($"CentreAsset contract : {_centreAssetContract.Contract}");

            _adminCentreAssetStub =
                _centreAssetContract.GetTestStub<CentreAssetManagementContainer.CentreAssetManagementStub>(InitAccount);
//            _centreAssetStub =
//                _centreAssetContract.GetTestStub<CentreAssetManagementContainer.CentreAssetManagementStub>(TestAccount);
//            _ownerStub =
//                _centreAssetContract
//                    .GetTestStub<CentreAssetManagementContainer.CentreAssetManagementStub>(OwnerAccount);
//            _centreAssetStub =
//                _centreAssetContract.GetTestStub<CentreAssetManagementContainer.CentreAssetManagementStub>(TestAccount);
            _tokenContractStub = _tokenContract.GetTestStub<TokenContractContainer.TokenContractStub>(InitAccount);
//            _tokenContract.TransferBalance(InitAccount, OtherAccount, 100000_00000000, "ELF");
//            _tokenContract.TransferBalance(InitAccount, TestAccount, 100000_00000000, "ELF");
//            _tokenContract.TransferBalance(InitAccount, OwnerAccount, 100000_00000000, "ELF");
//            AsyncHelper.RunSync(() => InitializeCentreAssetContract(InitAccount));
        }

        [TestMethod]
        [DataRow("")]
        public async Task InitializeCentreAssetContract(string owner)
        {
            var result = await _adminCentreAssetStub.Initialize.SendAsync(new InitializeDto
            {
                CategoryToContactCallWhiteListsMap =
                {
                    {
                        "token_transfer", new ContractCallWhiteLists()
                        {
                            List =
                            {
                                new ContractCallWhiteList()
                                {
                                    Address = _tokenContract.Contract,
                                    MethodNames = {"Transfer"}
                                }
                            }
                        }
                    }
                },
                Owner = owner.ConvertAddress()
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            await GetCentreAssetManagementInfo();
            await GetCategoryToContractCall();
        }

        [TestMethod]
        public async Task AddCategoryToContractCallWhiteLists()
        {
            var addResult = await _adminCentreAssetStub.AddCategoryToContractCallWhiteLists.SendAsync(
                new CategoryToContractCallWhiteListsDto
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
                                    }
                                }
                            }
                        },
                        {
                            "resource_buy", new ContractCallWhiteLists()
                            {
                                List =
                                {
                                    new ContractCallWhiteList()
                                    {
                                        Address = _tokenConverter.Contract,
                                        MethodNames = {"Buy", "Sell"}
                                    }
                                }
                            }
                        }
                    }
                });
            addResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var result = await _adminCentreAssetStub.GetCategoryContractCallAllowance.CallAsync(new CategoryDto
            {
                Category = "token_lock"
            });
            Logger.Info(result);
            
            await GetCentreAssetManagementInfo();
            await GetCategoryToContractCall();
        }

        [TestMethod]
        public async Task ChangeContractOwner()
        {
            var result = await _adminCentreAssetStub.GetCentreAssetManagementInfo.CallAsync(new Empty());
            var owner = result.Owner;
            var stub = _centreAssetContract
                .GetTestStub<CentreAssetManagementContainer.CentreAssetManagementStub>(owner.ToBase58());

            var newAddress = InitAccount.ConvertAddress();
            var change = await stub.ChangeContractOwner.SendAsync(newAddress);
            change.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var info = await _adminCentreAssetStub.GetCentreAssetManagementInfo.CallAsync(new Empty());
            var newOwner = info.Owner;
            newOwner.ShouldBe(newAddress);

            var newChange = await stub.ChangeContractOwner.SendAsync(owner);
            newChange.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            newChange.TransactionResult.Error.ShouldContain("No permission.");
        }

        [TestMethod]
        public async Task CreateHolder()
        {
            var result = await _adminCentreAssetStub.CreateHolder.SendAsync(new HolderCreateDto
            {
                OwnerAddress = OwnerAccount.ConvertAddress(),
                Symbol = NativeSymbol,
                ManagementAddresses =
                {
                    new ManagementAddress
                    {
                        Address = InitAccount.ConvertAddress(),
                        Amount = 1000_00000000,
                        ManagementAddressesInTotal = 1,
                        ManagementAddressesLimitAmount = 1000_00000000
                    },
                    new ManagementAddress
                    {
                        Address = TestAccount.ConvertAddress(),
                        Amount = 100_00000000,
                        ManagementAddressesInTotal = 0,
                        ManagementAddressesLimitAmount = 100_00000000
                    }
                },
                ShutdownAddress = InitAccount.ConvertAddress(),
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var id = result.Output.Id;
            Logger.Info($"Holder id is {id}");
            var tokenHolderInfo = result.Output.Info;
            Logger.Info($"Holder info is {tokenHolderInfo}");

            var logEvent = HolderCreated.Parser.ParseFrom(result.TransactionResult.Logs
                .First(l => l.Name.Contains(nameof(HolderCreated))).NonIndexed);
            logEvent.Symbol.ShouldBe(tokenHolderInfo.Symbol);
            logEvent.HolderId.ShouldBe(id);
            logEvent.OwnerAddress.ShouldBe(OwnerAccount.ConvertAddress());
        }
        
        [TestMethod]
        public async Task ApproveUpdateHolder()
        {
            var holderId = Hash.LoadFromHex(HolderId);
            var holderOriginInfo =
                _centreAssetContract.CallViewMethod<HolderInfo>(CentreAssertMethod.GetHolderInfo, holderId);
            var input = new HolderUpdateRequestDto
            {
                HolderId = holderId,
                OwnerAddress = OwnerAccount.ConvertAddress(),
                ShutdownAddress = InitAccount.ConvertAddress(),
                SettingsEffectiveTime = 0,
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
                        Amount = 100_00000000,
                        ManagementAddressesInTotal = 2,
                        ManagementAddressesLimitAmount = 100_00000000
                    },
                    new ManagementAddress
                    {
                        Address = OwnerAccount.ConvertAddress(),
                        Amount = 1000_00000000,
                        ManagementAddressesInTotal = 0,
                        ManagementAddressesLimitAmount = 1000_00000000
                    }
                }
            };
            var result = await _ownerStub.RequestUpdateHolder.SendAsync(input);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var logEvent = HolderUpdateRequested.Parser.ParseFrom(result.TransactionResult.Logs
                .First(l => l.Name.Contains(nameof(HolderUpdateRequested))).NonIndexed);
//            var id = result.TransactionResult.Logs
//                .First(l => l.Name.Contains(nameof(HolderUpdateRequested))).Indexed;
//            id.First().ShouldBe(holderId);
            logEvent.OwnerAddress.ShouldBe(input.OwnerAddress);
            logEvent.ShutdownAddress.ShouldBe(input.ShutdownAddress);
            logEvent.SettingsEffectiveTime.ShouldBe(input.SettingsEffectiveTime);

            var holderInfo =
                _centreAssetContract.CallViewMethod<HolderInfo>(CentreAssertMethod.GetHolderInfo, holderId);
            holderInfo.Symbol.ShouldBe(holderOriginInfo.Symbol);
            holderInfo.ShutdownAddress.ShouldBe(holderOriginInfo.ShutdownAddress);
            holderInfo.OwnerAddress.ShouldBe(holderOriginInfo.OwnerAddress);
            holderInfo.ManagementAddresses.ShouldBe(holderOriginInfo.ManagementAddresses);
            holderInfo.MainAddress.ShouldBe(holderOriginInfo.MainAddress);
            holderInfo.UpdatingInfo.ShouldBeNull();

            var second = holderInfo.SettingsEffectiveTime;
            Thread.Sleep(second * 1000);

            var approve =
                await _ownerStub.ApproveUpdateHolder.SendAsync(new HolderUpdateApproveDto
                    {HolderId = holderId});
            approve.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var approveLogEvent = HolderUpdateApproved.Parser.ParseFrom(approve.TransactionResult.Logs
                .First(l => l.Name.Contains(nameof(HolderUpdateApproved))).NonIndexed);
//            var approveId =  HolderUpdateApproved.Parser.ParseFrom(approve.TransactionResult.Logs
//                .First(l => l.Name.Contains(nameof(HolderUpdateApproved))).Indexed.First()).HolderId;
//            approveId.ShouldBe(holderId);
            approveLogEvent.OwnerAddress.ShouldBe(input.OwnerAddress);
            approveLogEvent.ShutdownAddress.ShouldBe(input.ShutdownAddress);
            approveLogEvent.SettingsEffectiveTime.ShouldBe(input.SettingsEffectiveTime);

            holderInfo = _centreAssetContract.CallViewMethod<HolderInfo>(CentreAssertMethod.GetHolderInfo, holderId);
            holderInfo.ShouldNotBe(holderOriginInfo);
            holderInfo.MainAddress.ShouldBe(holderOriginInfo.MainAddress);
            Logger.Info(holderInfo);
        }

        [TestMethod]
        public async Task ShutdownHolder()
        {
            var holderId = Hash.LoadFromHex(HolderId);
            var holderOriginInfo =
                _centreAssetContract.CallViewMethod<HolderInfo>(CentreAssertMethod.GetHolderInfo, holderId);

            var result = await _ownerStub.ShutdownHolder.SendAsync(new HolderShutdownDto
            {
                HolderId = holderId
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var holderInfo =
                _centreAssetContract.CallViewMethod<HolderInfo>(CentreAssertMethod.GetHolderInfo, holderId);
            holderInfo.IsShutdown.ShouldBeTrue();
            holderInfo.UpdatingInfo.ShouldBeNull();
            holderInfo.MainAddress.ShouldBe(holderOriginInfo.MainAddress);
            holderInfo.ManagementAddresses.ShouldBe(holderOriginInfo.ManagementAddresses);
            holderInfo.Symbol.ShouldBe(holderOriginInfo.Symbol);
        }

        [TestMethod]
        public async Task RebootHolder()
        {
            var holderId = Hash.LoadFromHex(HolderId);
            var holderOriginInfo =
                _centreAssetContract.CallViewMethod<HolderInfo>(CentreAssertMethod.GetHolderInfo, holderId);
            holderOriginInfo.IsShutdown.ShouldBeTrue();
            holderOriginInfo.OwnerAddress.ShouldBe(InitAccount.ConvertAddress());

            var result = await _adminCentreAssetStub.RebootHolder.SendAsync(new HolderRebootDto()
            {
                HolderId = holderId,
                HolderOwner = OwnerAccount.ConvertAddress()
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var holderInfo =
                _centreAssetContract.CallViewMethod<HolderInfo>(CentreAssertMethod.GetHolderInfo, holderId);
            holderInfo.IsShutdown.ShouldBeFalse();
            holderInfo.UpdatingInfo.ShouldBeNull();
            holderInfo.OwnerAddress.ShouldBe(OwnerAccount.ConvertAddress());
            holderInfo.ManagementAddresses.ShouldBe(new MapField<string, ManagementAddress>());
        }

        //User charge token to VirtualAddress
        [TestMethod]
        public async Task TransferToUserVirtualAddress()
        {
            var holderId = Hash.LoadFromHex(HolderId);
            var amount = 100_00000000;
            var account = InitAccount;
            var userToken = TestAccountToken;
            var token = NativeSymbol;
            var addressCategoryHash = await GetCategoryHash("token_lock");

            VirtualAddressCalculationDto assetMoveFromMainToVirtualTokenLockDto1 = new VirtualAddressCalculationDto()
            {
                UserToken = userToken,
                HolderId = holderId,
                AddressCategoryHash = addressCategoryHash
            };
            var userVirtualAddress =
                await _centreAssetStub.GetVirtualAddress.CallAsync(assetMoveFromMainToVirtualTokenLockDto1);
            var virtualBalance = _tokenContract.GetUserBalance(userVirtualAddress.ToBase58(),token);

            _tokenContract.SetAccount(account);
            var result =
                _tokenContract.TransferBalance(account, userVirtualAddress.ToBase58(), amount, token);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var virtualAfterBalance = _tokenContract.GetUserBalance(userVirtualAddress.ToBase58(),token);
            virtualAfterBalance.ShouldBe(virtualBalance + amount);
            Logger.Info($"{userVirtualAddress} => {token} {virtualAfterBalance}");
        }


        // User virtualAddress to MainAddress
        [TestMethod]
        public async Task MoveAssetToMainAddress()
        {
            var holderId = Hash.LoadFromHex(HolderId);
            var amount = 100_00000000;
            var account = TestAccount;
            var userToken = TestAccountToken;
            var addressCategoryHash = await GetCategoryHash("token_lock");

            VirtualAddressCalculationDto virtualAddressCalculationDto = new VirtualAddressCalculationDto
            {
                UserToken = userToken,
                HolderId = holderId,
                AddressCategoryHash = addressCategoryHash
            };

            AssetMoveDto assetMoveFromVirtualToMainDto = new AssetMoveDto()
            {
                Amount = amount,
                UserToken = userToken,
                HolderId = holderId,
                AddressCategoryHash = addressCategoryHash
            };
            var userVirtualAddress =
                await _centreAssetStub.GetVirtualAddress.CallAsync(virtualAddressCalculationDto);
            var virtualBalance = _tokenContract.GetUserBalance(userVirtualAddress.ToBase58());
            var mainBalance = _tokenContract.GetUserBalance(MainAddress);

            var toResult = await _adminCentreAssetStub.MoveAssetToMainAddress.SendAsync(assetMoveFromVirtualToMainDto);
            toResult.Output.Success.ShouldBeTrue();
            toResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var virtualAfterBalance = _tokenContract.GetUserBalance(userVirtualAddress.ToBase58());
            virtualAfterBalance.ShouldBe(virtualBalance - amount);

            var afterMainBalance = _tokenContract.GetUserBalance(MainAddress);
            afterMainBalance.ShouldBe(mainBalance + amount);
        }
        
        // User virtualAddress to MainAddress
        [TestMethod]
        public async Task MoveAssetToMainAddress_OtherSymbol()
        {
            var testHolderId = Hash.LoadFromHex(TestHolderId);
            var amount = 100_00000000;
            var account = TestAccount;
            var userToken = TestAccountToken;
            var token = OtherSymbol;
            
            var addressCategoryHash = await GetCategoryHash("resource_buy");

            VirtualAddressCalculationDto virtualAddressCalculationDto = new VirtualAddressCalculationDto
            {
                UserToken = userToken,
                HolderId = testHolderId,
                AddressCategoryHash = addressCategoryHash
            };

            AssetMoveDto assetMoveFromVirtualToMainDto = new AssetMoveDto()
            {
                Amount = amount,
                UserToken = userToken,
                HolderId = testHolderId,
                AddressCategoryHash = addressCategoryHash
            };
            var userVirtualAddress =
                await _centreAssetStub.GetVirtualAddress.CallAsync(virtualAddressCalculationDto);
            var virtualBalance = _tokenContract.GetUserBalance(userVirtualAddress.ToBase58(),token);
            var mainBalance = _tokenContract.GetUserBalance(TestMainAddress,token);

            var stub =
                _centreAssetContract
                    .GetTestStub<CentreAssetManagementContainer.CentreAssetManagementStub>(OtherAccount);
            var toResult = await stub.MoveAssetToMainAddress.SendAsync(assetMoveFromVirtualToMainDto);
            toResult.Output.Success.ShouldBeTrue();
            toResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var virtualAfterBalance = _tokenContract.GetUserBalance(userVirtualAddress.ToBase58(),token);
            virtualAfterBalance.ShouldBe(virtualBalance - amount);

            var afterMainBalance = _tokenContract.GetUserBalance(TestMainAddress,token);
            afterMainBalance.ShouldBe(mainBalance + amount);
        }

        //User take token from virtualAddress
        //1. ManagementAddress RequestWithdraw
        //2. ManagementAddress ApproveWithdraw

        [TestMethod]
        public async Task Withdraw()
        {
            var amount = 10_00000000;
            var withdrawAmount = amount;
            var holderId = Hash.LoadFromHex(HolderId);
            var withdrawAccount = TestAccount;
            await TransferToMainAddress(withdrawAccount, holderId, amount, TestAccountToken, "token_lock");
            var userAfterTransferBalance = _tokenContract.GetUserBalance(withdrawAccount);
            var mainAddressAfterMoveAsset = _tokenContract.GetUserBalance(MainAddress);

            var holderInfo =
                _centreAssetContract.CallViewMethod<HolderInfo>(CentreAssertMethod.GetHolderInfo, holderId);
            var managementAddresses = holderInfo.ManagementAddresses;
            var managementAddress = holderInfo.ManagementAddresses.Values.First(m => m.ManagementAddressesInTotal == 0);
            var senderStub =
                _centreAssetContract.GetTestStub<CentreAssetManagementContainer.CentreAssetManagementStub>(
                    managementAddress.Address.ToBase58());

            var requestWithdraw = await senderStub.RequestWithdraw.SendAsync(new WithdrawRequestDto
            {
                Address = withdrawAccount.ConvertAddress(),
                Amount = withdrawAmount,
                HolderId = holderId
            });
            requestWithdraw.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var withdrawId = requestWithdraw.Output.Id;
            Logger.Info($"withdraw id : {withdrawId}");

            var byteString = ByteString.FromBase64(requestWithdraw.TransactionResult.Logs
                .First(l => l.Name.Contains(nameof(WithdrawRequested))).NonIndexed.ToBase64());
            var logEvent = WithdrawRequested.Parser.ParseFrom(byteString);

            logEvent.Amount.ShouldBe(withdrawAmount);
            logEvent.RequestAddress.ShouldBe(managementAddress.Address);
            logEvent.WithdrawId.ShouldBe(withdrawId);
            logEvent.WithdrawAddress.ShouldBe(withdrawAccount.ConvertAddress());

            foreach (var management in managementAddresses)
            {
                var stub =
                    _centreAssetContract.GetTestStub<CentreAssetManagementContainer.CentreAssetManagementStub>(
                        management.Value.Address.ToBase58());
                var approveWithdraw = await stub.ApproveWithdraw.SendAsync(new WithdrawApproveDto
                {
                    Id = withdrawId,
                    Amount = withdrawAmount,
                    Address = withdrawAccount.ConvertAddress()
                });
                approveWithdraw.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                if (approveWithdraw.Output.ApprovedAddresses == approveWithdraw.Output.TotalRequired)
                {
                    approveWithdraw.Output.Status.ShouldBe(WithdrawApproveReturnDto.Types.Status.Approved);
                    var withdrawLogEvent = WithdrawReleased.Parser.ParseFrom(approveWithdraw.TransactionResult.Logs
                        .First(l => l.Name.Contains(nameof(WithdrawReleased))).NonIndexed);
                    withdrawLogEvent.Amount.ShouldBe(withdrawAmount);
                    withdrawLogEvent.WithdrawAddress.ShouldBe(withdrawAccount.ConvertAddress());
                    break;
                }

                approveWithdraw.Output.Status.ShouldBe(WithdrawApproveReturnDto.Types.Status.Approving);
                approveWithdraw.Output.TotalRequired.ShouldBeGreaterThan(approveWithdraw.Output.ApprovedAddresses);
            }

            var userBalance = _tokenContract.GetUserBalance(withdrawAccount);
            userBalance.ShouldBeLessThanOrEqualTo(userAfterTransferBalance + withdrawAmount);

            var mainAddressBalance = _tokenContract.GetUserBalance(MainAddress);
            mainAddressBalance.ShouldBe(mainAddressAfterMoveAsset - withdrawAmount);
        }

        [TestMethod]
        public async Task Withdraw_Expire()
        {
            var holderId = Hash.LoadFromHex(HolderId);
            var amount = 10_00000000;
            var withdrawAmount = amount;
            var withdrawAccount = TestAccount;
            var withdrawId = Hash.LoadFromHex("cc66f36f00d31be763a913e318b6283df37f09395b4165c99f4ff49b299bb96d");
            var userBalance = _tokenContract.GetUserBalance(withdrawAccount);
            var mainAddressBalance= _tokenContract.GetUserBalance(MainAddress);
            
            var approveWithdraw = await _centreAssetStub.ApproveWithdraw.SendAsync(new WithdrawApproveDto
            {
                Id = withdrawId,
                Amount = withdrawAmount,
                Address = withdrawAccount.ConvertAddress()
            });
            approveWithdraw.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            approveWithdraw.Output.Status.ShouldBe(WithdrawApproveReturnDto.Types.Status.Expired);
            var logEvent = approveWithdraw.TransactionResult.Logs.First(l => l.Name.Contains(nameof(WithdrawExpired)))
                .NonIndexed;
            var withdrawInfo = WithdrawExpired.Parser.ParseFrom(logEvent);
            withdrawInfo.Amount.ShouldBe(withdrawAmount);
            withdrawInfo.WithdrawAddress.ShouldBe(withdrawAccount.ConvertAddress());
            
            var userAfterBalance = _tokenContract.GetUserBalance(withdrawAccount);
            var mainAddressAfterBalance = _tokenContract.GetUserBalance(MainAddress);
//            userAfterBalance.ShouldBe(userBalance);
            mainAddressAfterBalance.ShouldBe(mainAddressBalance);
            
            var approveWithdraw2 = await _adminCentreAssetStub.ApproveWithdraw.SendAsync(new WithdrawApproveDto
            {
                Id = withdrawId,
                Amount = withdrawAmount,
                Address = withdrawAccount.ConvertAddress()
            });
            approveWithdraw2.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            approveWithdraw2.TransactionResult.Error.ShouldContain("Withdraw not exists.");
            
            var cancelWithdraw = await _adminCentreAssetStub.CancelWithdraws.SendAsync(new CancelWithdrawsDto()
            {
                HolderId = holderId,
                Ids = { withdrawId}
            });
            cancelWithdraw.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
        }

        [TestMethod]
        public async Task CancelWithdraw()
        {
            var amount = 100_00000000;
            var withdrawAmount = amount;
            var holderId = Hash.LoadFromHex(HolderId);
            var withdrawAccount = TestAccount;
            
            var userOriginBalance = _tokenContract.GetUserBalance(withdrawAccount);
            await TransferToMainAddress(InitAccount, holderId, amount, InitAccountToken, "token_lock");

            var requestWithdraw = await _adminCentreAssetStub.RequestWithdraw.SendAsync(new WithdrawRequestDto
            {
                Address = withdrawAccount.ConvertAddress(),
                Amount = withdrawAmount,
                HolderId = holderId
            });
            requestWithdraw.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var withdrawId = requestWithdraw.Output.Id;
            var userBalance = _tokenContract.GetUserBalance(withdrawAccount);
            userBalance.ShouldBe(userOriginBalance);

            var result = await _adminCentreAssetStub.CancelWithdraws.SendAsync(new CancelWithdrawsDto
            {
                HolderId = holderId,
                Ids = {withdrawId}
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var logEvent = WithdrawCanceled.Parser.ParseFrom(result.TransactionResult.Logs
                .First(l => l.Name.Contains(nameof(WithdrawCanceled))).NonIndexed);
            logEvent.Amount.ShouldBe(withdrawAmount);
            logEvent.WithdrawAddress.ShouldBe(withdrawAccount.ConvertAddress());

            var approveWithdraw = await _adminCentreAssetStub.ApproveWithdraw.SendAsync(new WithdrawApproveDto
            {
                Id = withdrawId,
                Amount = withdrawAmount,
                Address = withdrawAccount.ConvertAddress()
            });
            approveWithdraw.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            approveWithdraw.TransactionResult.Error.ShouldContain("Withdraw not exists.");
        }
        
        [TestMethod]
        public async Task CancelMoreWithdrawId()
        {
            var amount = 10_00000000;
            var withdrawAmount = amount;
            var holderId = Hash.LoadFromHex(HolderId);
            var withdrawAccounts = new List<string>{InitAccount,TestAccount,OtherAccount};

            var withdrawIds = new List<Hash>();
            foreach (var withdrawAccount in withdrawAccounts)
            {
                var holderInfo =
                    _centreAssetContract.CallViewMethod<HolderInfo>(CentreAssertMethod.GetHolderInfo, holderId);
                var managementAddresses = holderInfo.ManagementAddresses;
                var managementAddress = holderInfo.ManagementAddresses.Values.First(m => m.ManagementAddressesInTotal == 2);
                var senderStub =
                    _centreAssetContract.GetTestStub<CentreAssetManagementContainer.CentreAssetManagementStub>(
                        managementAddress.Address.ToBase58());

                var requestWithdraw = await senderStub.RequestWithdraw.SendAsync(new WithdrawRequestDto
                {
                    Address = withdrawAccount.ConvertAddress(),
                    Amount = withdrawAmount,
                    HolderId = holderId
                });
                requestWithdraw.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                var withdrawId = requestWithdraw.Output.Id;
                Logger.Info($"withdraw id : {withdrawId}");
                withdrawIds.Add(withdrawId);
            }

            var result = _centreAssetContract.ExecuteMethodWithResult(CentreAssertMethod.CancelWithdraws,new CancelWithdrawsDto
            {
                HolderId = holderId,
                Ids = {withdrawIds}
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var logEvents = result.Logs.Where(l => l.Name.Contains(nameof(WithdrawCanceled))).ToList();
            foreach (var logEvent in logEvents)
            {
                var id = WithdrawCanceled.Parser.ParseFrom(ByteString.FromBase64(logEvent.Indexed.First())).HolderId;
                id.ShouldBe(holderId);
                var withdrawId = WithdrawCanceled.Parser.ParseFrom(ByteString.FromBase64(logEvent.Indexed.Last())).WithdrawId;
                withdrawIds.ShouldContain(withdrawId);
            }
        }

        [TestMethod]
        public async Task MoveAssetFromToMainAddress()
        {
            var amount = 0;
            var holderId = Hash.LoadFromHex(HolderId);
            var token = NativeSymbol;
            var addressCategoryHash = await GetCategoryHash("token_lock");
            
            VirtualAddressCalculationDto virtualAddressCalculationDto = new VirtualAddressCalculationDto()
            {
                UserToken = TestAccountToken,
                HolderId = holderId,
                AddressCategoryHash = addressCategoryHash
            };
            var virtualAddress = await _centreAssetStub.GetVirtualAddress.CallAsync(virtualAddressCalculationDto);
            var virtualBalance = _tokenContract.GetUserBalance(virtualAddress.ToBase58(),token);
            var mainAddressBalance = _tokenContract.GetUserBalance(MainAddress,token);

            AssetMoveDto assetMoveFromMainToVirtualTokenLockDto = new AssetMoveDto()
            {
                Amount = amount,
                UserToken = TestAccountToken,
                HolderId = holderId,
                AddressCategoryHash = addressCategoryHash
            };
            
            var fromResult =
                await _adminCentreAssetStub.MoveAssetFromMainAddress.SendAsync(assetMoveFromMainToVirtualTokenLockDto);
            fromResult.Output.Success.ShouldBeTrue();
            fromResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var virtualAddressBalance = _tokenContract.GetUserBalance(virtualAddress.ToBase58(),token);
            virtualAddressBalance.ShouldBe(virtualBalance + amount);
            var mainAddressAfterBalance = _tokenContract.GetUserBalance(MainAddress,token);
            mainAddressAfterBalance.ShouldBe(mainAddressBalance - amount);
        }

        [TestMethod]
        public async Task SendTransactionByUserVirtualAddress()
        {
            var amount = 1_00000000;
            var elfAmount = 100_00000000;
            var holderId = Hash.LoadFromHex(HolderId);
            var addressCategoryHash = await GetCategoryHash("resource_buy");
            
            VirtualAddressCalculationDto virtualAddressCalculationDto = new VirtualAddressCalculationDto()
            {
                UserToken = TestAccountToken,
                HolderId = holderId,
                AddressCategoryHash = addressCategoryHash
            };
            var userVirtualAddress = 
                await _centreAssetStub.GetVirtualAddress.CallAsync(virtualAddressCalculationDto);
            _tokenContract.SetAccount(TestAccount);
            var result =
                _tokenContract.TransferBalance(TestAccount, userVirtualAddress.ToBase58(), elfAmount);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var userVirtualBalance = _tokenContract.GetUserBalance(userVirtualAddress.ToBase58());
            
            var sendTransactionResult = await _adminCentreAssetStub.SendTransactionByUserVirtualAddress.SendAsync(
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
                    UserToken = TestAccountToken,
                    AddressCategoryHash = addressCategoryHash,
                });
            sendTransactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var elfBalance = _tokenContract.GetUserBalance(userVirtualAddress.ToBase58());
            elfBalance.ShouldBeLessThan(userVirtualBalance);
            var cpuBalance = _tokenContract.GetUserBalance(userVirtualAddress.ToBase58(), "CPU");
//            cpuBalance.ShouldBe(amount);

            var addressCategoryHash2 = await GetCategoryHash("token_lock");

            VirtualAddressCalculationDto virtualAddressCalculationDto2 = new VirtualAddressCalculationDto()
            {
                UserToken = TestAccountToken,
                HolderId = holderId,
                AddressCategoryHash = addressCategoryHash2
            };
            var userVirtualAddress2 = 
                await _centreAssetStub.GetVirtualAddress.CallAsync(virtualAddressCalculationDto);
            _tokenContract.SetAccount(TestAccount);
            _tokenContract.TransferBalance(TestAccount, userVirtualAddress2.ToBase58(), elfAmount);
            var userVirtualBalance2 = _tokenContract.GetUserBalance(userVirtualAddress.ToBase58());

            var sendTransactionResult2 = await _adminCentreAssetStub.SendTransactionByUserVirtualAddress.SendAsync(
                new SendTransactionByUserVirtualAddressDto()
                {
                    Args = new TransferInput()
                    {
                        Amount = amount,
                        Symbol = "ELF",
                        To = MainAddress.ConvertAddress()
                    }.ToByteString(),
                    MethodName = "Transfer",
                    To = _tokenContract.Contract,
                    HolderId = holderId,
                    UserToken = TestAccountToken,
                    AddressCategoryHash = addressCategoryHash2,
                });
            sendTransactionResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        }

        [TestMethod]
        public async Task GetHolderInfo()
        {
            var holderId = Hash.LoadFromHex(HolderId);
            var info = await _adminCentreAssetStub.GetHolderInfo.CallAsync(holderId);
            Logger.Info(info);
        }

//        GetCategoryToContractCall
//        GetCategoryHash
//        GetHolderInfo
//        GetCentreAssetManagementInfo

        [TestMethod]
        public async Task GetCategoryContractCallAllowance()
        {
            var result = await _adminCentreAssetStub.GetCategoryContractCallAllowance.CallAsync(new CategoryDto
            {
                Category = "token_lock"
            });
            Logger.Info(result);
        }

        [TestMethod]
        public async Task GetCategoryToContractCall()
        {
            var result = await _adminCentreAssetStub.GetCategoryToContractCall.CallAsync(new Empty());
            Logger.Info(result);
        }

        [TestMethod]
        public async Task GetCentreAssetManagementInfo()
        {
            var result = await _adminCentreAssetStub.GetCentreAssetManagementInfo.CallAsync(new Empty());
            Logger.Info(result);
        }


        #region Failed case

        [TestMethod]
        public async Task Initialize_Failed_Case()
        {
            // Null Owner
            var result1 = await _adminCentreAssetStub.Initialize.SendAsync(new InitializeDto
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
                                }
                            }
                        }
                    },
                    {
                        "resource_buy", new ContractCallWhiteLists()
                        {
                            List =
                            {
                                new ContractCallWhiteList()
                                {
                                    Address = _tokenConverter.Contract,
                                    MethodNames = {"Buy", "Sell"}
                                }
                            }
                        }
                    }
                }
            });
            result1.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result1.TransactionResult.Error.ShouldContain("Contract owner cannot be null.");

            // Already Initialize 
            var result2 = await _adminCentreAssetStub.Initialize.SendAsync(new InitializeDto
            {
                Owner = InitAccount.ConvertAddress()
            });
            result2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var result3 = await _adminCentreAssetStub.Initialize.SendAsync(new InitializeDto
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
                                }
                            }
                        }
                    },
                    {
                        "resource_buy", new ContractCallWhiteLists()
                        {
                            List =
                            {
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

            result3.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result3.TransactionResult.Error.ShouldContain("Already initialized.");
        }

        [TestMethod]
        public async Task CreateHolder_Failed_Case()
        {
            var result1 = await _adminCentreAssetStub.CreateHolder.SendAsync(new HolderCreateDto
            {
                OwnerAddress = InitAccount.ConvertAddress(),
                ShutdownAddress = InitAccount.ConvertAddress()
            });
            result1.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result1.TransactionResult.Error.ShouldContain("Symbol cannot be null or white space.");

            var result2 = await _adminCentreAssetStub.CreateHolder.SendAsync(new HolderCreateDto
            {
                Symbol = OtherSymbol
            });
            result2.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result2.TransactionResult.Error.ShouldContain("Symbol is not registered in token contract.");

            var result3 = await _adminCentreAssetStub.CreateHolder.SendAsync(new HolderCreateDto
            {
                Symbol = NativeSymbol,
                ManagementAddresses =
                {
                    new ManagementAddress
                    {
                        Address = InitAccount.ConvertAddress(),
                        Amount = 1000_00000000,
                        ManagementAddressesInTotal = 1,
                        ManagementAddressesLimitAmount = 1000_00000000
                    },
                    new ManagementAddress
                    {
                        Address = InitAccount.ConvertAddress(),
                        Amount = 100_00000000,
                        ManagementAddressesInTotal = 2,
                        ManagementAddressesLimitAmount = 100_00000000
                    }
                }
            });
            result3.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result3.TransactionResult.Error.ShouldContain("The same management address exists.");

            var result4 = await _adminCentreAssetStub.CreateHolder.SendAsync(new HolderCreateDto
            {
                Symbol = NativeSymbol,
                ManagementAddresses =
                {
                    new ManagementAddress
                    {
                        Address = InitAccount.ConvertAddress(),
                        Amount = 1000_00000000,
                        ManagementAddressesInTotal = 1,
                        ManagementAddressesLimitAmount = 1000_00000000
                    },
                    new ManagementAddress
                    {
                        Address = TestAccount.ConvertAddress(),
                        Amount = 100_00000000,
                        ManagementAddressesInTotal = 2,
                        ManagementAddressesLimitAmount = 100_00000000
                    }
                }
            });
            result4.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result4.TransactionResult.Error.ShouldContain("Owner address cannot be null.");

            var result5 = await _adminCentreAssetStub.CreateHolder.SendAsync(new HolderCreateDto
            {
                Symbol = NativeSymbol,
                ManagementAddresses =
                {
                    new ManagementAddress
                    {
                        Address = InitAccount.ConvertAddress(),
                        Amount = 1000_00000000,
                        ManagementAddressesInTotal = 1,
                        ManagementAddressesLimitAmount = 1000_00000000
                    },
                    new ManagementAddress
                    {
                        Address = TestAccount.ConvertAddress(),
                        Amount = 100_00000000,
                        ManagementAddressesInTotal = 2,
                        ManagementAddressesLimitAmount = 1000_00000000
                    }
                },
                OwnerAddress = InitAccount.ConvertAddress(),
                ShutdownAddress = InitAccount.ConvertAddress()
            });
            result5.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result5.TransactionResult.Error.ShouldContain("Invalid management address.");
        }

        [TestMethod]
        public async Task ApproveUpdateHolder_Failed_Case()
        {
            var holderId = Hash.LoadFromHex(HolderId);
            var errorId = HashHelper.ComputeFrom("Test");
            var result1 = await _ownerStub.RequestUpdateHolder.SendAsync(new HolderUpdateRequestDto
            {
                HolderId = errorId,
                OwnerAddress = OwnerAccount.ConvertAddress(),
                ShutdownAddress = InitAccount.ConvertAddress(),
                ManagementAddresses =
                {
                    new ManagementAddress
                    {
                        Address = InitAccount.ConvertAddress(),
                        Amount = 10000_00000000,
                        ManagementAddressesInTotal = 1,
                        ManagementAddressesLimitAmount = 10000_00000000
                    }
                }
            });
            result1.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result1.TransactionResult.Error.ShouldContain("Holder is not initialized.");


            var result2 = await _ownerStub.RequestUpdateHolder.SendAsync(new HolderUpdateRequestDto
            {
                OwnerAddress = OwnerAccount.ConvertAddress(),
                ShutdownAddress = InitAccount.ConvertAddress(),
                ManagementAddresses =
                {
                    new ManagementAddress
                    {
                        Address = InitAccount.ConvertAddress(),
                        Amount = 10000_00000000,
                        ManagementAddressesInTotal = 1,
                        ManagementAddressesLimitAmount = 10000_00000000
                    }
                }
            });
            result2.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result2.TransactionResult.Error.ShouldContain("Holder id required.");

            var holderInfo =
                _centreAssetContract.CallViewMethod<HolderInfo>(CentreAssertMethod.GetHolderInfo, holderId);
            holderInfo.UpdatingInfo.ShouldBeNull();
            var approve =
                await _ownerStub.ApproveUpdateHolder.SendAsync(new HolderUpdateApproveDto
                    {HolderId = holderId});
            approve.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            approve.TransactionResult.Error.ShouldContain("Updating info not found.");
        }
        
        [TestMethod]
        public async Task MoveAssetToMainAddress_Failed_Case()
        {
            var holderId = Hash.LoadFromHex(HolderId);
            var errorId = HashHelper.ComputeFrom("Test");
            var amount = 100_00000000;
            var account = TestAccount;
            var userToken = TestAccountToken;
            var addressCategoryHash = await GetCategoryHash("token_lock");
            var errorAddressCategoryHash = HashHelper.ComputeFrom("error_category");

            AssetMoveDto assetMoveFromVirtualToMainDto = new AssetMoveDto()
            {
                Amount = amount,
                UserToken = userToken,
                HolderId = errorId,
                AddressCategoryHash = addressCategoryHash
            };
            var toResult = await _adminCentreAssetStub.MoveAssetToMainAddress.SendAsync(assetMoveFromVirtualToMainDto);
            toResult.Output.Success.ShouldBeFalse();
            toResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            toResult.TransactionResult.Error.ShouldContain("Holder is not initialized");
            

            AssetMoveDto assetMoveFromVirtualToMainDto1 = new AssetMoveDto()
            {
                Amount = amount,
                UserToken = userToken,
                HolderId = holderId,
                AddressCategoryHash = errorAddressCategoryHash
            };
            var toResult1 = await _adminCentreAssetStub.MoveAssetToMainAddress.SendAsync(assetMoveFromVirtualToMainDto1);
            toResult1.Output.Success.ShouldBeFalse();
            toResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            toResult1.TransactionResult.Error.ShouldContain("No contract call list for this category, maybe not initialized.");
            Logger.Info(toResult1.TransactionResult.Error);
        }

        [TestMethod]
        public async Task Withdraw_Failed_Case()
        {
            var amount = 100_00000000;
            var withdrawAmount = amount;
            var holderId = Hash.LoadFromHex(HolderId);
            var withdrawAccount = TestAccount;
            
            var requestWithdraw1 = await _centreAssetStub.RequestWithdraw.SendAsync(new WithdrawRequestDto
            {
                Amount = withdrawAmount,
                HolderId = holderId
            });
            requestWithdraw1.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            requestWithdraw1.TransactionResult.Error.ShouldContain("Address required.");
            
            var requestWithdraw2 = await _centreAssetStub.RequestWithdraw.SendAsync(new WithdrawRequestDto
            {
                Address = withdrawAccount.ConvertAddress(),
                Amount = 0,
                HolderId = holderId
            });
            requestWithdraw2.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            requestWithdraw2.TransactionResult.Error.ShouldContain("Amount required.");
            
            
            var holderInfo =
                _centreAssetContract.CallViewMethod<HolderInfo>(CentreAssertMethod.GetHolderInfo, holderId);
            var managementAddress = holderInfo.ManagementAddresses.Values.First(m => m.ManagementAddressesInTotal == 0);
            var senderStub =
                _centreAssetContract.GetTestStub<CentreAssetManagementContainer.CentreAssetManagementStub>(
                    managementAddress.Address.ToBase58());
            var requestWithdraw3 = await senderStub.RequestWithdraw.SendAsync(new WithdrawRequestDto
            {
                Address = withdrawAccount.ConvertAddress(),
                Amount = withdrawAmount,
                HolderId = holderId
            });
            requestWithdraw3.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            requestWithdraw3.TransactionResult.Error.ShouldContain("Current key cannot make withdraw request.");

            var approveWithdraw = await _centreAssetStub.ApproveWithdraw.SendAsync(new WithdrawApproveDto
            {
                Id = HashHelper.ComputeFrom("Error"),
                Amount = withdrawAmount,
                Address = withdrawAccount.ConvertAddress()
            });
            approveWithdraw.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            approveWithdraw.TransactionResult.Error.ShouldContain("Withdraw not exists.");
            
            
            var requestWithdraw4 = await _adminCentreAssetStub.RequestWithdraw.SendAsync(new WithdrawRequestDto
            {
                Address = withdrawAccount.ConvertAddress(),
                Amount = withdrawAmount,
                HolderId = holderId
            });
            requestWithdraw4.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var withdrawId = requestWithdraw4.Output.Id;
            
            var approveWithdraw1 = await _centreAssetStub.ApproveWithdraw.SendAsync(new WithdrawApproveDto
            {
                Id = withdrawId,
                Amount = withdrawAmount - 100000000,
                Address = withdrawAccount.ConvertAddress()
            });
            approveWithdraw1.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            approveWithdraw1.TransactionResult.Error.ShouldContain("Withdraw data not matched.");
            
            var approveWithdraw2 = await _centreAssetStub.ApproveWithdraw.SendAsync(new WithdrawApproveDto
            {
                Id = withdrawId,
                Amount = withdrawAmount,
                Address = withdrawAccount.ConvertAddress()
            });
            approveWithdraw2.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            approveWithdraw2.TransactionResult.Error.ShouldContain("Current management address cannot approve, amount limited");
        }
        
         [TestMethod]
        public async Task SendTransactionByUserVirtualAddress_Tailed_Case()
        {
            var amount = 1_00000000;
            var holderId = Hash.LoadFromHex(HolderId);
            var addressCategoryHash = await GetCategoryHash("resource_buy");
            
            VirtualAddressCalculationDto virtualAddressCalculationDto = new VirtualAddressCalculationDto()
            {
                UserToken = TestAccountToken,
                HolderId = holderId,
                AddressCategoryHash = addressCategoryHash
            };
            var sendTransactionResult = await _ownerStub.SendTransactionByUserVirtualAddress.SendAsync(
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
                    UserToken = TestAccountToken,
                    AddressCategoryHash = addressCategoryHash,
                });
            sendTransactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            sendTransactionResult.TransactionResult.Error.ShouldContain("Sender is not registered as management address in the holder.");
        }


        #endregion

        private async Task<Hash> GetCategoryHash(string category)
        {
            return await _adminCentreAssetStub.GetCategoryHash.CallAsync(new StringValue {Value = category});
        }

        private async Task TransferToMainAddress(string userAccount, Hash holderId, long amount, string userToken,
            string category)
        {
            var userBalance = _tokenContract.GetUserBalance(userAccount);

            AssetMoveDto assetMoveFromVirtualToMainDto = new AssetMoveDto()
            {
                Amount = amount,
                UserToken = userToken,
                HolderId = holderId,
                AddressCategoryHash = HashHelper.ComputeFrom(category)
            };

            VirtualAddressCalculationDto virtualAddressCalculationDto = new VirtualAddressCalculationDto()
            {
                UserToken = userToken,
                HolderId = holderId,
                AddressCategoryHash = HashHelper.ComputeFrom(category)
            };

            var virtualAddress = await _centreAssetStub.GetVirtualAddress.CallAsync(virtualAddressCalculationDto);

            _tokenContract.SetAccount(userAccount);
            var result = _tokenContract.TransferBalance(userAccount, virtualAddress.ToBase58(), amount);
            var fee = result.GetDefaultTransactionFee();
            var userAfterBalance = _tokenContract.GetUserBalance(userAccount);
            userAfterBalance.ShouldBe(userBalance - amount - fee);

            var virtualAddressBalance = _tokenContract.GetUserBalance(virtualAddress.ToBase58());
            Logger.Info($"user {userAccount} => virtualAddressBalance: {virtualAddressBalance}");

            var mainAddress = _tokenContract.GetUserBalance(MainAddress);

            var toResult = await _adminCentreAssetStub.MoveAssetToMainAddress.SendAsync(assetMoveFromVirtualToMainDto);
            toResult.Output.Success.ShouldBeTrue();
            toResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var afterMainAddress = _tokenContract.GetUserBalance(MainAddress);
            afterMainAddress.ShouldBe(mainAddress + amount);
            var virtualAfterAddressBalance = _tokenContract.GetUserBalance(virtualAddress.ToBase58());
            virtualAfterAddressBalance.ShouldBe(virtualAddressBalance - amount);
        }
    }
}