using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Bridge;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Regiment;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using Google.Protobuf;
using log4net;
using Shouldly;

namespace AElf.Automation.SwapTokenTest
{
    public class TokenSwap
    {
        private static ILog Logger { get; set; } = Log4NetHelper.GetLogger();

        public readonly string InitAccount;
        public readonly BridgeContract Bridge;
        public readonly TokenContract TokenService;
        public readonly RegimentContract Regiment;
        public static Hash PairId;
        public string SwapSymbol;
        public List<string> Receivers;

        public TokenSwap()
        {
            var contractServices = GetContractServices();
            TokenService = contractServices.TokenService;
            Bridge = contractServices.BridgeService;
            Regiment = contractServices.RegimentService;
            InitAccount = contractServices.CallAccount;
        }

        private ContractServices GetContractServices()
        {
            var swapInfo = SwapConfig.ReadInformation;
            var environment = swapInfo.EnvironmentInfo;
            NodeInfoHelper.SetConfig(environment.ConfigFile);

            var contractService = new ContractServices(environment.Url, environment.Owner, environment.Password,
                swapInfo.Bridge, swapInfo.MerkleTreeRecorder, swapInfo.MerkleTreeGenerator, swapInfo.Regiment);
            PairId = Hash.LoadFromHex(swapInfo.PairId);
            Receivers = swapInfo.ReceiveAccounts;
            return contractService;
        }

        public void GetSwapInfo()
        {
            var swapInfo = Bridge.CallViewMethod<SwapInfo>(BridgeMethod.GetSwapInfo, PairId);
            SwapSymbol = swapInfo.SwapTargetToken.Symbol;
            Logger.Info($"SwapId: {swapInfo.SwapId.ToHex()}\n" +
                        $"RecorderId: {swapInfo.SpaceId.ToHex()}\n" +
                        $"RegimentAddress: {swapInfo.RegimentId.ToHex()}\n" +
                        $"SwapToken: {swapInfo.SwapTargetToken.Symbol}");
        }

        public void SwapToken(ReceiptInfo receiptInfo)
        {
            var receiveAddress = receiptInfo.Receiver;
            var originAmount = receiptInfo.Amount;
            var receiptId = receiptInfo.ReceiptId;
            Logger.Info($"{receiptId}: {receiveAddress}  {originAmount}");
            // var list = CheckSwappedReceiptIdList(receiveAddress);
            // if (list.Contains(receiptId))
            // {
            //     Logger.Info($"{receiveAddress} already claim receipt: {receiptId}");
            //     return;
            // }

            if (!Receivers.Contains(receiveAddress))
                return;

            var expectedAmount = long.Parse(originAmount.Substring(0, originAmount.Length - 10));
            var swapPair = Bridge.CallViewMethod<SwapPairInfo>(BridgeMethod.GetSwapPairInfo, new GetSwapPairInfoInput
                {SwapId = PairId, Symbol = SwapSymbol});
            if (swapPair.DepositAmount < expectedAmount)
            {
                Deposit(expectedAmount);
                swapPair = Bridge.CallViewMethod<SwapPairInfo>(BridgeMethod.GetSwapPairInfo, new GetSwapPairInfoInput
                    {SwapId = PairId, Symbol = SwapSymbol});
            }

            var balance = TokenService.GetUserBalance(receiveAddress, SwapSymbol);
            Bridge.SetAccount(receiveAddress);
            var result = Bridge.ExecuteMethodWithResult(BridgeMethod.SwapToken, new SwapTokenInput
            {
                OriginAmount = originAmount,
                ReceiptId = receiptId,
                SwapId = PairId
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logs = result.Logs.First(l => l.Name.Equals(nameof(Transferred)));
            var amount = Transferred.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed)).Amount;
            amount.ShouldBe(expectedAmount);
            var after = TokenService.GetUserBalance(receiveAddress, SwapSymbol);
            after.ShouldBe(balance + expectedAmount);
            var afterSwapPair = Bridge.CallViewMethod<SwapPairInfo>(BridgeMethod.GetSwapPairInfo, new GetSwapPairInfoInput
                {SwapId = PairId, Symbol = SwapSymbol});
            afterSwapPair.DepositAmount.ShouldBe(swapPair.DepositAmount - expectedAmount);

            var checkAmount = Bridge.CallViewMethod<SwapAmounts>(BridgeMethod.GetSwapAmounts, new GetSwapAmountsInput
            {
                SwapId = PairId,
                ReceiptId = receiptId
            });
            checkAmount.Receiver.ShouldBe(receiveAddress.ConvertAddress());
            checkAmount.ReceivedAmounts[SwapSymbol].ShouldBe(expectedAmount);
            // var afterList = CheckSwappedReceiptIdList(receiveAddress);
            // afterList.ShouldContain(receiptId);
        }

        // private List<string> CheckSwappedReceiptIdList(string receiveAddress)
        // {
        //     var checkSwappedReceiptIdList = Bridge.CallViewMethod<ReceiptIdList>(BridgeMethod.GetSwappedReceiptIdList,
        //         new GetSwappedReceiptIdListInput
        //         {
        //             ReceiverAddress = receiveAddress.ConvertAddress(),
        //             SwapId = PairId
        //         });
        //
        //     return checkSwappedReceiptIdList.Value.ToList();
        // }

        private void Deposit(long depositAmount)
        {
            TokenService.ApproveToken(InitAccount, Bridge.ContractAddress, depositAmount, SwapSymbol);
            var swapPairInfo =
                Bridge.CallViewMethod<SwapInfo>(BridgeMethod.GetSwapInfo, PairId);
            var manager =
                Regiment.CallViewMethod<RegimentInfo>(RegimentMethod.GetRegimentInfo,
                    swapPairInfo.RegimentId).Manager;
            var swapPair = Bridge.CallViewMethod<SwapPairInfo>(BridgeMethod.GetSwapPairInfo, new GetSwapPairInfoInput
                {SwapId = PairId, Symbol = SwapSymbol});

            Bridge.SetAccount(manager.ToBase58());
            var result = Bridge.ExecuteMethodWithResult(BridgeMethod.Deposit, new DepositInput
            {
                SwapId = PairId,
                TargetTokenSymbol = SwapSymbol,
                Amount = depositAmount
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var afterSwapPair = Bridge.CallViewMethod<SwapPairInfo>(BridgeMethod.GetSwapPairInfo, new GetSwapPairInfoInput
                {SwapId = PairId, Symbol = SwapSymbol});
            afterSwapPair.DepositAmount.ShouldBe(swapPair.DepositAmount + depositAmount);
        }
    }
}