using System;
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

            var lottery = new Lottery(Rewards,Counts);
            if (lottery.LotteryContract == "")
                lottery.Buy();
            lottery.Draw();
            
            Console.ReadLine();
        }
        private static ILog Logger { get; set; }
        
        [Option("-r|--rewards", Description = "Reward lists")]
        private static string Rewards { get; set; }

        [Option("-c|--counts", Description = "Reward counts")]
        private static string Counts { get; set; }
    }
}