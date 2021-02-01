using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using log4net;
using Shouldly;
using Tokenswap;
using Volo.Abp.Threading;

namespace AElf.Automation.TokenSwapTest
{
    public class TokenSwap
    {
        private static ILog Logger { get; set; } = Log4NetHelper.GetLogger();

        public readonly string NativeSymbol;
//        public readonly string Symbol = "LOT";
        public readonly string InitAccount;
        public readonly TokenSwapContract TokenSwapService;
        public readonly TokenSwapContractContainer.TokenSwapContractStub SwapContractStub;
        public readonly TokenContract TokenService;
        public static Hash PairId;

        public TokenSwap(string tokenSwapContract, string pairId = null)
        {
            var contractServices = GetContractServices(tokenSwapContract);
            TokenService = contractServices.TokenService;
            TokenSwapService = contractServices.TokenSwapService;
            InitAccount = contractServices.CallAccount;
            NativeSymbol = TokenService.GetPrimaryTokenSymbol();
            SwapContractStub =
                TokenSwapService.GetTestStub<TokenSwapContractContainer.TokenSwapContractStub>(InitAccount);
//            if (!TokenService.GetTokenInfo(Symbol).Symbol.Equals(Symbol))
//                CreateTokenAndIssue();
            PairId = pairId != null ? Hash.LoadFromHex(pairId) : AsyncHelper.RunSync(CreateSwap);
        }

        private ContractServices GetContractServices(string tokenSwapContract)
        {
            var config = NodeInfoHelper.Config;
            var firstNode = config.Nodes.First();
            var contractService = new ContractServices(firstNode.Endpoint, firstNode.Account, firstNode.Password,
                tokenSwapContract);
            return contractService;
        }

        public async Task AddSwapRound(string root, long i)
        {
//            var result = await SwapContractStub.CreateSwapRound.SendAsync(new CreateSwapRoundInput()
//            {
//                SwapId = PairId,
//                MerkleTreeRoot =
//                    Hash.LoadFromHex(root),
//                RoundId = i
//            });
//            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        public async Task SwapToken(ReceiptInfo receiptInfo, long id)
        {
            var originAmount = receiptInfo.Amount;
            var uniqueId = Hash.LoadFromHex(receiptInfo.UniqueId);

//            var beforeSwapBalance = TokenService.GetUserBalance(TokenSwapService.ContractAddress, Symbol);
//            var swapPair = await SwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
//                {SwapId = PairId, TargetTokenSymbol = Symbol});
//            swapPair.DepositAmount.ShouldBe(beforeSwapBalance);
            var swapElfPair = await SwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                {SwapId = PairId, TargetTokenSymbol = NativeSymbol});
//            swapElfPair.DepositAmount.ShouldBe(beforeSwapElfBalance);
            var beforeSwapElfBalance = swapElfPair.DepositAmount;

            var expectedAmount = originAmount.Length > 10
                ? long.Parse(originAmount.Substring(0, originAmount.Length - 10))
                : 0;

            if (swapElfPair.DepositAmount < expectedAmount)
            {
                await Deposit(expectedAmount);
//                beforeSwapBalance = TokenService.GetUserBalance(TokenSwapService.ContractAddress, Symbol);
                var swapInfo = await SwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                    {SwapId = PairId, TargetTokenSymbol = NativeSymbol});
                beforeSwapElfBalance = swapInfo.DepositAmount;
            }
            
            var receiveAccount = receiptInfo.Receiver;
//            var beforeBalance = TokenService.GetUserBalance(receiveAccount, Symbol);
            var beforeElfBalance = TokenService.GetUserBalance(receiveAccount, NativeSymbol);

            var stringInfo = receiptInfo.MerklePath.Nodes;
            var isLeftInfo = receiptInfo.MerklePath.Positions;
            stringInfo.Count.ShouldBe(receiptInfo.MerklePath.PathLength);

            var merklePathNodes = new List<MerklePathNode>();
            for (var i = 0; i < stringInfo.Count; i++)
            {
                var merkle = new MerklePathNode();
                merkle.Hash = Hash.LoadFromHex(stringInfo[i]);
                merkle.IsLeftChildNode = isLeftInfo[i];
                merklePathNodes.Add(merkle);
            }

            var merklePath = new Types.MerklePath {MerklePathNodes = {merklePathNodes}};

            try
            {
                var receiverStub = TokenSwapService.GetTestStub<TokenSwapContractContainer.TokenSwapContractStub>(receiveAccount);
                var result = await receiverStub.SwapToken.SendAsync(new SwapTokenInput
                {
                    SwapId = PairId,
                    OriginAmount = originAmount,
                    UniqueId = uniqueId,
                    ReceiverAddress = receiveAccount.ConvertAddress(),
                    MerklePath = merklePath,
                });
                if (result.TransactionResult.Status.Equals(TransactionResultStatus.Mined))
                {
                    var tokenTransferredEvent = result.TransactionResult.Logs
                        .First(l => l.Name == nameof(Transferred));
                    var nonIndexed = Transferred.Parser.ParseFrom(tokenTransferredEvent.NonIndexed);
                    nonIndexed.Amount.ShouldBe(expectedAmount);
                }
                else
                    expectedAmount = 0;
            }
            catch (TimeoutException e)
            {
                Console.WriteLine($"Transaction is NotExisted ...\n{e.Message}");
                expectedAmount = 0;
            }

//            var balance = TokenService.GetUserBalance(receiveAccount, Symbol);
            var elfBalance = TokenService.GetUserBalance(receiveAccount, NativeSymbol);
            elfBalance.ShouldBeGreaterThanOrEqualTo(beforeElfBalance + expectedAmount);
//            balance.ShouldBeLessThanOrEqualTo(beforeBalance + expectedAmount);
//
//            var afterSwapBalance = TokenService.GetUserBalance(TokenSwapService.ContractAddress, Symbol);
//            swapPair = await SwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
//                {SwapId = PairId, TargetTokenSymbol = Symbol});
//            swapPair.DepositAmount.ShouldBe(afterSwapBalance);
//            swapPair.DepositAmount.ShouldBe(beforeSwapBalance - expectedAmount);

            swapElfPair = await SwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                {SwapId = PairId, TargetTokenSymbol = NativeSymbol});
            var afterSwapElfBalance = swapElfPair.DepositAmount;
            afterSwapElfBalance.ShouldBe(beforeSwapElfBalance - expectedAmount);
            
            TransferToTokenSwapContract(receiveAccount,elfBalance);
        }

        private async Task Deposit(long depositAmount)
        {
            TokenService.ApproveToken(InitAccount, TokenSwapService.ContractAddress, depositAmount, NativeSymbol);
//            TokenService.ApproveToken(InitAccount, TokenSwapService.ContractAddress, depositAmount, Symbol);

            var swapPairInfo = await SwapContractStub.GetSwapInfo.CallAsync(PairId);
            swapPairInfo.Controller.ShouldBe(InitAccount.ConvertAddress());
            var swapPair = await SwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                {SwapId = PairId, TargetTokenSymbol = NativeSymbol});
            var beforeBalance = swapPair.DepositAmount;

//            var result = await SwapContractStub.Deposit.SendAsync(new DepositInput
//            {
//                SwapId = PairId,
//                TargetTokenSymbol = Symbol,
//                Amount = depositAmount
//            });
//            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var elfResult = await SwapContractStub.Deposit.SendAsync(new DepositInput
            {
                SwapId = PairId,
                TargetTokenSymbol = NativeSymbol,
                Amount = depositAmount
            });
            elfResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            swapPair = await SwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                {SwapId = PairId, TargetTokenSymbol = NativeSymbol});
            var afterBalance = swapPair.DepositAmount;
            afterBalance.ShouldBe(beforeBalance + depositAmount);
        }

        public async Task<long> CheckTree()
        {
            var swapInfo = await SwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                {SwapId = PairId, TargetTokenSymbol = NativeSymbol});
            return swapInfo.DepositAmount;
        }

        private async Task<Hash> CreateSwap()
        {
            var originTokenSizeInByte = 32;
            var swapRatio = new SwapRatio
            {
                OriginShare = 100_00000000,
                TargetShare = 1,
            };
            var depositAmount = 1000_00000000;
//            TokenService.ApproveToken(InitAccount, TokenSwapService.ContractAddress, depositAmount, Symbol);
            TokenService.ApproveToken(InitAccount, TokenSwapService.ContractAddress, depositAmount, NativeSymbol);

            var result = await SwapContractStub.CreateSwap.SendAsync(new CreateSwapInput
            {
                OriginTokenSizeInByte = originTokenSizeInByte,
                OriginTokenNumericBigEndian = true,
                SwapTargetTokenList =
                {
                    new SwapTargetToken
                    {
                        DepositAmount = depositAmount,
                        SwapRatio = swapRatio,
                        TargetTokenSymbol = NativeSymbol
                    }
//                    new SwapTargetToken
//                    {
//                        DepositAmount = depositAmount,
//                        SwapRatio = swapRatio,
//                        TargetTokenSymbol = Symbol
//                    }
                }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var pairId = result.Output;
            var swapId = SwapPairAdded.Parser
                .ParseFrom(result.TransactionResult.Logs.First(l => l.Name.Contains(nameof(SwapPairAdded))).NonIndexed)
                .SwapId;
            pairId.ShouldBe(swapId);
            var swapPair = await SwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
            {
                SwapId = pairId,
                TargetTokenSymbol = NativeSymbol
            });
            swapPair.SwappedAmount.ShouldBe(0);
            swapPair.SwappedTimes.ShouldBe(0);
            swapPair.SwapRatio.ShouldBe(swapRatio);
            swapPair.TargetTokenSymbol.ShouldBe(NativeSymbol);
            swapPair.OriginTokenSizeInByte.ShouldBe(originTokenSizeInByte);
            Logger.Info($"\nSwap id is: {pairId.ToHex()}");
            return pairId;
        }

//        private void CreateTokenAndIssue()
//        {
//            var result = TokenService.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
//            {
//                Symbol = Symbol,
//                TotalSupply = 10_00000000_00000000,
//                Decimals = 8,
//                Issuer = InitAccount.ConvertAddress(),
//                IsBurnable = true,
//                TokenName = "LOT"
//            });
//            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
//            TokenService.IssueBalance(InitAccount, InitAccount, 10_00000000_00000000, Symbol);
//        }

        private void TransferToTokenSwapContract(string receiver, long elfBalance)
        {
            Logger.Info($"Check the balance of receiver account {receiver}, ELF balance is {elfBalance}");
            if (receiver.Equals(InitAccount)) return;
            TokenService.SetAccount(receiver);
//            if (balance > 10000_00000000) 
//                TokenService.TransferBalance(receiver, InitAccount, balance - 10000_00000000, Symbol);
            if (elfBalance > 10000_00000000)
                TokenService.TransferBalance(receiver, InitAccount, elfBalance - 10000_00000000);
        }
    }
}