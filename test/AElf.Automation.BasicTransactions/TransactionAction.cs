using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using AElf.Client.Dto;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Automation.BasicTransaction
{
    public class TransactionAction: BasicAction
    {
        public TransactionAction()
        {
            GetService();
        }

        public TokenContract DeployTokenContract(TestMode mode)
        {
            if (mode == TestMode.RandomContractTransfer)
                TokenAddress = "";
            
            if (TokenAddress != "")
                return new TokenContract(NodeManager, InitAccount, TokenAddress);
            var tokenAddress = AuthorityManager.DeployContract(InitAccount,
                "AElf.Contracts.MultiToken", Password);
            var token = new TokenContract(NodeManager, InitAccount, tokenAddress.ToBase58());
            Logger.Info($"Token Address: {token.ContractAddress}");
            return token;
        }

        public string CreateAndIssueToken(Address tokenAddress)
        {
            var token = new TokenContract(NodeManager, InitAccount, tokenAddress.ToBase58());
            var symbol = Symbol == "" ? GenerateNotExistTokenSymbol(token) : Symbol;
            var tokenInfo = token.GetTokenInfo(symbol);
            if (!tokenInfo.Equals(new TokenInfo()))
                return symbol;
            var transaction = token.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
            {
                Symbol = symbol,
                TokenName = $"elf token {symbol}",
                TotalSupply = 10_0000_0000_00000000L,
                Decimals = 8,
                Issuer = InitAccount.ConvertAddress(),
                IsBurnable = true
            });
            transaction.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var issueToken = token.IssueBalance(InitAccount, InitAccount, 10_0000_0000_00000000, symbol);
            issueToken.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var balance = token.GetUserBalance(InitAccount, symbol);
            balance.ShouldBe(10_0000_0000_00000000);
            return symbol;
        }
        
        public long TransferFromAccount(TokenContract token, string symbol)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            token.ExecuteMethodWithTxId(TokenMethod.Transfer,new TransferInput
            {
                To = TestAccount.ConvertAddress(),
                Amount = TransferAmount,
                Symbol = symbol,
                Memo = $"T-{Guid.NewGuid().ToString()}"
            });
            stopwatch.Stop();
            var checkTime = stopwatch.ElapsedMilliseconds;
            return checkTime;
        }
        
        public void DoubleTransfer(TokenContract token, string symbol)
        {
            var fromAddress = NodeManager.AccountManager.NewAccount(Password);
            var toAddress = NodeManager.AccountManager.NewAccount(Password);
            token.TransferBalance(InitAccount, fromAddress, 100_00000000, symbol);
            token.TransferBalance(InitAccount, fromAddress, 10_00000000);
            token.TransferBalance(InitAccount, toAddress, 10_00000000);
            var balance = token.GetUserBalance(fromAddress, symbol);
            var toBalance = token.GetUserBalance(toAddress, symbol);
            Logger.Info($"\n{fromAddress} balance : {balance}" +
                        $"\n{toAddress} balance : {toBalance}");
            token.SetAccount(fromAddress);
            var txId1 = token.ExecuteMethodWithTxId(TokenMethod.Transfer,new TransferInput
            {
                To = toAddress.ConvertAddress(),
                Amount = balance,
                Symbol = symbol
            });
            
            var txId2 = token.ExecuteMethodWithTxId(TokenMethod.Transfer,new TransferInput
            {
                To = toAddress.ConvertAddress(),
                Amount = balance,
                Symbol = symbol
            });

            Thread.Sleep(5000);
            var txResult1 = AsyncHelper.RunSync(()=> NodeManager.ApiClient.GetTransactionResultAsync(txId1));
            var txResult2 = AsyncHelper.RunSync(()=> NodeManager.ApiClient.GetTransactionResultAsync(txId2));
            Logger.Info(txResult1.Status);
            var afterBalance = token.GetUserBalance(fromAddress, symbol);
            var afterToBalance = token.GetUserBalance(toAddress, symbol);
            Logger.Info($"\n{fromAddress} {symbol} balance : {afterBalance}" +
                        $"\n{toAddress} {symbol} balance : {afterToBalance}");

            token.SetAccount(toAddress);
            var txId3 = token.ExecuteMethodWithTxId(TokenMethod.Transfer,new TransferInput
            {
                To = fromAddress.ConvertAddress(),
                Amount = afterToBalance,
                Symbol = symbol,
                Memo = "1"
            });
            
            var txId4 = token.ExecuteMethodWithTxId(TokenMethod.Transfer,new TransferInput
            {
                To = fromAddress.ConvertAddress(),
                Amount = afterToBalance,
                Symbol = symbol,
                Memo = "2"
            });
            
            Thread.Sleep(5000);
            var txResult3 = AsyncHelper.RunSync(()=> NodeManager.ApiClient.GetTransactionResultAsync(txId3));
            var txResult4 = AsyncHelper.RunSync(()=> NodeManager.ApiClient.GetTransactionResultAsync(txId4));
            var afterBalance1 = token.GetUserBalance(fromAddress, symbol);
            var afterToBalance2 = token.GetUserBalance(toAddress, symbol);
            Logger.Info($"\n{fromAddress} {symbol} balance : {afterBalance1}" +
                        $"\n{toAddress} {symbol} balance : {afterToBalance2}");

            Logger.Info($"\ntx id: {txResult3.TransactionId}: status: {txResult3.Status}");
            if (txResult3.Error!=null)
                Logger.Info($"\nError:{txResult3.Error}\n");
            Logger.Info($"\ntx id: {txResult4.TransactionId}: status: {txResult4.Status}");
            if (txResult4.Error!=null)
                Logger.Info($"\nError:{txResult4.Error}\n");
        }

        public long CheckTxInfo(TokenContract token, string symbol)
        {
            long all = 0;
            var txIds = new List<string>();
            
            Logger.Info($"send {TestAccountList.Count} transaction");
            foreach (var account in TestAccountList)
            {
                var id = token.ExecuteMethodWithTxId(TokenMethod.Transfer,new TransferInput
                {
                    To = account.ConvertAddress(),
                    Amount = TransferAmount,
                    Symbol = symbol,
                    Memo = $"T-{Guid.NewGuid().ToString()}"
                });
                txIds.Add(id);
            }
            NodeManager.CheckTransactionListResult(txIds);
            
            Logger.Info($"check {txIds.Count} transaction");
            
            foreach (var txId in txIds)
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var txResult = AsyncHelper.RunSync(()=> NodeManager.ApiClient.GetTransactionResultAsync(txId));
                stopwatch.Stop();
                var checkTime = stopwatch.ElapsedMilliseconds;
                all += checkTime;
                Logger.Info($"tx {txId} status is {txResult.Status}");
            }
            Logger.Info($"{txIds.Count} transaction send succeed..");

            return all;
        }
        
        public long CheckAccountBalance(TokenContract token, string symbol)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var balance = token.GetUserBalance(InitAccount,symbol);
            var testBalance = token.GetUserBalance(TestAccount, symbol);
            stopwatch.Stop();
            
            var result = token.TransferBalance(InitAccount, TestAccount, TransferAmount, symbol);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            stopwatch.Restart();
            var afterBalance = token.GetUserBalance(InitAccount,symbol);
            var afterTestBalance = token.GetUserBalance(TestAccount,symbol);
            stopwatch.Stop();

            var checkTime = stopwatch.ElapsedMilliseconds;
            afterBalance.ShouldBe(balance - TransferAmount);
            afterTestBalance.ShouldBe(testBalance + TransferAmount);
            Logger.Info($"Before transfer from account balance is {balance}, to account balance is {testBalance}");
            Logger.Info($"After transfer from account balance is {afterBalance}, to account balance is {afterTestBalance}");
            return checkTime;
        }
        public long CheckBlockHeight(long count)
        {
            long all = 0;
            var height = AsyncHelper.RunSync(() => NodeManager.ApiClient.GetBlockHeightAsync());
            var startHeight = height - count;
            Logger.Info($"Current block height: {height}, check block info from {startHeight} to {height-1}");
            for (var i = startHeight; i < height; i++)
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var info = AsyncHelper.RunSync(() => NodeManager.ApiClient.GetBlockByHeightAsync(i));
                stopwatch.Stop();
                Logger.Info($"block {i},hash : {info.BlockHash}");
                var checkTime = stopwatch.ElapsedMilliseconds;
                all += checkTime;
            }
            return all;
        }
        
        public BlockDto CheckBlockInfo(long height, INodeManager nodeManager)
        {
            Logger.Info($"block height: {height}, check block info");
            var info = AsyncHelper.RunSync(() => nodeManager.ApiClient.GetBlockByHeightAsync(height,true));
            Logger.Info($"\non {nodeManager.GetApiUrl()}:\n" +
                        $"block {height},hash : {info.BlockHash}");
            return info;
        }
        
        public TransactionResultDto CheckTxInfo(string tx, INodeManager nodeManager)
        {
            var txResult = AsyncHelper.RunSync(()=> nodeManager.ApiClient.GetTransactionResultAsync(tx));
            Logger.Info($"\n on {nodeManager.GetApiUrl()}:\n" +
                        $"tx {tx} status is {txResult.Status}\n" +
                        $"tx from: {txResult.Transaction.From}\n" +
                        $"tx to {txResult.Transaction.To}\n" +
                        $"tx on block {txResult.BlockNumber}\n" +
                        $"tx method {txResult.Transaction.MethodName}");
            return txResult;
        }

        public void CheckTransactionAndBlock(TokenContract token, string symbol)
        {
            var fromAddress = NodeManager.AccountManager.NewAccount(Password);
            var toAddress = NodeManager.AccountManager.NewAccount(Password);
            token.TransferBalance(InitAccount, fromAddress, 100_00000000, symbol);
            token.TransferBalance(InitAccount, fromAddress, 10_00000000);
            token.TransferBalance(InitAccount, toAddress, 10_00000000);
            var balance = token.GetUserBalance(fromAddress, symbol);
            var toBalance = token.GetUserBalance(toAddress, symbol);
            Logger.Info($"\n{fromAddress} balance : {balance}" +
                        $"\n{toAddress} balance : {toBalance}");
            token.SetAccount(fromAddress);
            var txResult = token.TransferBalance(fromAddress, toAddress, balance / 2, symbol);
            Logger.Info($"tx in block {txResult.BlockNumber}");
            var info = AsyncHelper.RunSync(() => NodeManager.ApiClient.GetBlockByHeightAsync(txResult.BlockNumber,true));
            Logger.Info($"\nblock {txResult.BlockNumber} include transaction:\n");
            foreach (var tx in info.Body.Transactions)
                Logger.Info($"{tx}");
            Logger.Info($"block signer {info.Header.SignerPubkey}");
        }

        private static readonly ILog Logger = Log4NetHelper.GetLogger();
    }
}