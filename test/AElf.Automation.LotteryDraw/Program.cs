using AElfChain.Common.Helpers;

namespace AElf.Automation.LotteryTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Log4NetHelper.LogInit("LotteryTest");
            var lottery = new Lottery();
            if (lottery.LotteryContract == "")
                lottery.Buy();
            lottery.Draw();
        }
    }
}