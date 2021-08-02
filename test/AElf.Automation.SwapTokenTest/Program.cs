using System;
using AElfChain.Common.Helpers;
using log4net;

namespace AElf.Automation.SwapTokenTest
{
    class Program
    {
        private static ILog Logger = Log4NetHelper.GetLogger();
        static void Main()
        {
            Log4NetHelper.LogInit("TokenSwapTest_");
            var tokenSwap = new TokenSwap();
            tokenSwap.GetSwapInfo();

            var receiptList = ReadReceiptInfo.Config.ReceiptInfos;
            foreach (var receiptInfo in receiptList)
            {
                tokenSwap.SwapToken(receiptInfo);
            }
        }
    }
}