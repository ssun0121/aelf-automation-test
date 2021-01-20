using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElfChain.Common.Helpers;
using log4net;
using Volo.Abp.Threading;

namespace AElf.Automation.LotteryTest
{
    class Program
    {
        private static void Main(string[] args)
        {
            Log4NetHelper.LogInit("LotteryTest");
            var lottery = new Lottery();
            _tester = lottery.GetTestAddress();
            lottery.CheckNativeSymbolBalance(_tester);
            foreach (var tester in _tester)
                lottery.CheckBalance(10_00000000,tester,lottery.Symbol);
            AsyncHelper.RunSync(() => lottery.GetLotteryContractInfo());
            
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            var taskList = new List<Task>
            {
                Task.Run(() =>
                {
                    while (true)
                    {
                        Logger.Info($"Take {lottery.UserTestCount} tester: ");
                        var testers = lottery.TakeRandomUserAddress(lottery.UserTestCount, _tester);
                        lottery.BuyJob(testers);
                    }

                }, token),
                Task.Run(() => 
                {
                    while (true)
                    {
                        Thread.Sleep(600000);
                        lottery.DrawJob(_tester);
                        lottery.CheckBoard();
                        lottery.CalculateRate();
                        lottery.CheckUserRewardRate();
                    }
                },token),
                Task.Run(() =>
                {
                    while (true)
                    {
                        Thread.Sleep(300000);
                        lottery.CheckNativeSymbolBalance(_tester);
                    }
                }, token)
            };

            Task.WaitAll(taskList.ToArray<Task>());
        }
        
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private static List<string> _tester;
    }
}