using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElfChain.Common.Helpers;
using log4net;

namespace AElf.Automation.OracleTest
{
    class Program
    {
        private static ILog Logger { get; set; } = Log4NetHelper.GetLogger();

        static void Main()
        {
            Log4NetHelper.LogInit("OracleTest");
            var oracle = new OracleOperations();
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            if (oracle.OnlyOracle)
            {
                while (true)
                {
                    var taskList = new List<Task>
                    {
                        Task.Run(() =>
                        {
                            oracle.QueryJob();
                        }, token)
                    };
                    Thread.Sleep(100000);
                    Task.WaitAll(taskList.ToArray<Task>());
                }
            }
            
            oracle.ApplyObserver();
            oracle.RegisterOffChainAggregation();
            while (true)
            {
                var taskList = new List<Task>
                {
                    Task.Run(() =>
                    {
                        oracle.ReportQueryJob();
                    }, token)
                };
                Thread.Sleep(120000);
                Task.WaitAll(taskList.ToArray<Task>());
            }
        }
    }
}