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
            var taskList = new List<Task>();
            if (lottery.OnlyDraw)
            {
                taskList = new List<Task>
                {
                    Task.Run(() => 
                    {
                        while (true)
                        {
                            var second = DateTime.Now.Second;
                            var minute = DateTime.Now.Minute;
                            if (minute % 10 == 0 && second == 0)
                            {
                                lottery.DrawJob();
                            }
                        }
                    },token)
                };
            }
            else if (lottery.OnlyBuy)
            {
                Logger.Info($"Take {lottery.UserTestCount} tester: ");
                var accountLists = new List<List<string>>();
                for (var i = 0; i < 4; i++)
                {
                    var testers = lottery.TakeRandomUserAddress(lottery.UserTestCount, _tester);
                    accountLists.Add(testers);
                }
                
                while (true)
                {
                    for (var i = 0; i < 4; i++)
                    {
                        var i1 = i;
                        taskList.Add(Task.Run(() =>  
                        { 
                            lottery.OnlyBuyJob(accountLists[i1]);
                            lottery.CheckNativeSymbolBalance(_tester);
                        }, token));
                    }
                    Task.WaitAll(taskList.ToArray<Task>());
                }
            }
            else
            {
                taskList = new List<Task>
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
                            Thread.Sleep(900000);
                            lottery.TakeRewardJob(_tester);
                            lottery.CheckBoard();
                        }
                    },token),
                    Task.Run(() =>
                    {
                        while (true)
                        {
                            Thread.Sleep(300000);
                            lottery.CheckNativeSymbolBalance(_tester);
                            lottery.CalculateRate();
                            lottery.CheckUserRewardRate();
                        }
                    }, token)
                };
            }
           

            Task.WaitAll(taskList.ToArray<Task>());
        }
        
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private static List<string> _tester;
    }
}