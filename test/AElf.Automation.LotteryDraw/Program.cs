using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElfChain.Common;
using McMaster.Extensions.CommandLineUtils;
using AElfChain.Common.Helpers;
using log4net;

namespace AElf.Automation.LotteryTest
{
    class Program
    {
        static int Main(string[] args)
        {
            return CommandLineApplication.Execute<Program>(args);
        }
        
        private async Task OnExecute(CommandLineApplication app)
        {
            Log4NetHelper.LogInit("LotteryTest");
            Logger = Log4NetHelper.GetLogger();
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            var taskList = new List<Task>();
            
            var lottery = new Lottery(Rewards,Counts);
            _tester = lottery.GetTestAddress();
            if (lottery.OnlyDraw)
            {
                if (lottery.LotteryContract == "")
                    lottery.Buy();
                lottery.Draw();
            }else if (lottery.OnlyBuy)
            {
                Logger.Info($"Take {lottery.TestUserCount} tester: ");
                var accountLists = new List<List<string>>();
                for (var i = 0; i < 4; i++)
                {
                    var testers = lottery.TakeRandomUserAddress(lottery.TestUserCount, _tester);
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
                        }, token));
                    }
                    Task.WaitAll(taskList.ToArray<Task>());
                }
            }
        }
        private static ILog Logger { get; set; }
        
        [Option("-r|--rewards", Description = "Reward lists")]
        private static string Rewards { get; set; }

        [Option("-c|--counts", Description = "Reward counts")]
        private static string Counts { get; set; }
        private static List<string> _tester;
    }
}