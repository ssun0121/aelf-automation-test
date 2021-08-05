﻿using System;
using System.Linq;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using log4net;
using Shouldly;

namespace AElf.Automation.BasicTransaction
{
    class Program
    {
        static void Main()
        {
            Log4NetHelper.LogInit("BasicTransaction");

            var tx = new TransactionAction();
            Logger.Info($"{tx.NodeManager.GetApiUrl()}");
            var token = tx.DeployTokenContract(TestMode.UserTransfer);
            var symbol = tx.CreateAndIssueToken(token.Contract);
            Logger.Info($"Symbol: {symbol}");

            var execMode = ConfigInfo.ReadInformation.ExecuteMode;
            var times = ConfigInfo.ReadInformation.Times;
            var count = ConfigInfo.ReadInformation.ContractCount;
            
            var tm = (TestMode) execMode;
            long all = 0;
            double req;

            switch (tm)
            {
                case TestMode.UserTransfer:
                    Logger.Info(
                        $"Start basic account transfer, from account: {tx.InitAccount}, to account: {tx.TestAccount}, times: {times}");
                    Logger.Info(DateTime.Now);
                    for (var i = 0; i < times; i++)
                    {
                        var duration = tx.TransferFromAccount(token, symbol);
                        all += duration;
                    }

                    req = (double) times / all * 1000;

                    Logger.Info($"User transfer {times} times use {all}ms, req: {req}/s, time: {all / times}ms");
                    break;
                case TestMode.ContractTransfer:
                    Logger.Info(
                        $"Start contract transfer, from account: {tx.InitAccount}, to account: {tx.TestAccount}， times: {times}");
                    for (var i = 0; i < times; i++)
                    {
                        var duration = tx.TransferFromAccount(token, symbol);
                        all += duration;
                    }

                    req = (double) times / all * 1000;
                    Logger.Info($"Contract transfer {times} times use {all}ms, req: {req}/s, time: {all / times}ms");
                    break;
                case TestMode.RandomContractTransfer:
                    long total = 0;
                    for (var i = 0; i < count; i++)
                    {
                        all = 0;
                        var otherToken = tx.DeployTokenContract(TestMode.RandomContractTransfer);
                        var otherSymbol = tx.CreateAndIssueToken(otherToken.Contract);
                        Logger.Info($"Start random contract transfer, contract: {otherToken.ContractAddress}");
                        for (var j = 0; j < times; j++)
                        {
                            var duration = tx.TransferFromAccount(otherToken, otherSymbol);
                            all += duration;
                        }

                        total += all;
                        req = (double) times / all * 1000;
                        Logger.Info(
                            $"Random contract transfer {times} times use {all}ms, req: {req}/s, time: {all / times}ms");
                    }

                    Logger.Info(
                        $"Random contract transfer {times * count} times use {total}ms, " +
                        $"req: {(double) times * count / total * 1000}/s, " +
                        $"time: {total / (times * count)}ms");
                    
                    break;
                case TestMode.CheckUserBalance:
                    Logger.Info("Start check user balance: ");
                    for (var i = 0; i < times; i++)
                    {
                        var duration = tx.CheckAccountBalance(token, symbol);
                        all += duration;
                    }

                    req = (double) (times * 4) / all * 1000;
                    Logger.Info(
                        $"Check balance {times * 4} times use {all}ms, req: {req}/s, time: {all / (times * 4)}ms");
                    break;
                case TestMode.CheckTxInfo:
                    all = tx.CheckTxInfo(token, symbol);
                    req = (double) times / all * 1000;
                    Logger.Info(
                        $"Check  {times}  use {all}ms, req: {req}/s, time: {all /times}ms");
                    break;
                case TestMode.CheckBlockInfo:
                    Logger.Info("Start check block info:");
                    all = tx.CheckBlockHeight(times);
                    req = (double) times / all * 1000;
                    Logger.Info($"Check block {times} times use {all}ms, req: {req}/s, time: {all / times}ms");
                    break;
                case TestMode.DoubleTransfer:
                    Logger.Info("Start Double Transfer: ");
                    tx.DoubleTransfer(token,symbol);
                    break;
                case TestMode.AllCheck:
                    Logger.Info(
                        $"Start basic account transfer, from account: {tx.InitAccount}, to account: {tx.TestAccount}, times: {times}");
                    tx.CheckTxInfo(token, symbol);
                    Logger.Info("Start check user balance: ");
                    tx.CheckAccountBalance(token, symbol);
                    Logger.Info("Start check block info:");
                    tx.CheckBlockHeight(times);
                    break;
                case TestMode.NewNode:
                    Logger.Info($"base node {tx.NodeManager.GetApiUrl()}");
                    Logger.Info($"new node {tx.NewNodeManager.GetApiUrl()}");
                    Logger.Info("Check Block Info");
                    var blockBase = tx.CheckBlockInfo(10000, tx.NodeManager);
                    var blockNew = tx.CheckBlockInfo(10000, tx.NewNodeManager);

                    var baseTx = blockBase.Body.Transactions;
                    var newTx = blockNew.Body.Transactions;

                    tx.CheckTxInfo(baseTx.First(), tx.NodeManager);
                    tx.CheckTxInfo(newTx.First(), tx.NewNodeManager);

                    var baseBalance = token.GetUserBalance(tx.InitAccount);
                    var newToken = new TokenContract(tx.NewNodeManager, tx.InitAccount, tx.TokenAddress);
                    var newBalance = newToken.GetUserBalance(tx.InitAccount);
                    baseBalance.ShouldBe(newBalance);
                    Logger.Info($"\n on {tx.NodeManager.GetApiUrl()}: balance {baseBalance}\n" +
                                $"on {tx.NewNodeManager.GetApiUrl()}: balance {newBalance}");
                    break;
                case TestMode.CheckTransactionAndBlock:
                    tx.CheckTransactionAndBlock(token,symbol);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static readonly ILog Logger = Log4NetHelper.GetLogger();
    }
}