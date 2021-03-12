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

            var lottery = new Lottery(Rewards, Counts);
            _tester = lottery.GetTestAddress();
            if (lottery.OnlyDraw)
            {
                if (lottery.LotteryContract == "")
                    lottery.Buy();
                lottery.Draw();
            }
            else
            {
                while (true)
                {
                    Logger.Info($"Take {lottery.TestUserCount} tester: ");
                    var testers = lottery.TakeRandomUserAddress(lottery.TestUserCount, _tester);

                    for (int j = 0; j < 25; j++)
                    {
                        taskList.Add(Task.Run(() => { lottery.OnlyBuyJob(testers); }, token));
                        Task.WaitAll(taskList.ToArray<Task>());
                    }

                    if (lottery.OnlyBuy)
                    {
                        break;
                    }
                    lottery.Draw();
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