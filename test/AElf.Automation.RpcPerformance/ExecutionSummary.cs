using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Volo.Abp.Threading;

namespace AElf.Automation.RpcPerformance
{
    public class ExecutionSummary
    {
        private const int Phase = 8;
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private readonly AElfClient _apiService;
        private long _blockHeight;
        private Dictionary<long, BlockDto> _blockMap;
        private readonly long _firstHeight;
        private readonly long _transactionCount;
        private readonly long _threadCount;
        private readonly long _group;

        /// <summary>
        ///     analyze generate blocks summary info
        /// </summary>
        /// <param name="nodeManager"></param>
        /// <param name="fromStart">whether check from height 1</param>
        public ExecutionSummary(INodeManager nodeManager, bool fromStart = false)
        {
            _apiService = nodeManager.ApiClient;
            _blockMap = new Dictionary<long, BlockDto>();
            _blockHeight = fromStart ? 1 : AsyncHelper.RunSync(_apiService.GetBlockHeightAsync);
            _firstHeight = fromStart ? 1 : AsyncHelper.RunSync(_apiService.GetBlockHeightAsync);
            _transactionCount = RpcConfig.ReadInformation.TransactionCount;
            _threadCount = RpcConfig.ReadInformation.GroupCount;
            _group = RpcConfig.ReadInformation.TransactionGroup;
        }

        public void ContinuousCheckTransactionPerformance(CancellationToken ct,
            Dictionary<long, List<long>> txInfos = null)
        {
            if (ct.IsCancellationRequested)
            {
                Logger.Warn("ContinuousCheckTransactionPerformance task was been cancelled.");
                return;
            }

            var height = AsyncHelper.RunSync(_apiService.GetBlockHeightAsync);
            if (height < _blockHeight)
                return;
            for (var i = _blockHeight; i < height; i++)
            {
                var j = i;
                var block = AsyncHelper.RunSync(() => _apiService.GetBlockByHeightAsync(j));
                _blockMap.Add(j, block);
                // if (!_blockMap.Keys.Count.Equals(Phase)) continue;
            }
            SummaryBlockTransactionInPhase(_blockMap.Values.First(), _blockMap.Values.Last(), txInfos);
            _blockHeight = height;
            
            Thread.Sleep(100);
        }

        private void SummaryBlockTransactionInPhase(BlockDto startBlock, BlockDto endBlockDto,
            Dictionary<long, List<long>> txInfos)
        {
            
            var blockHeight = endBlockDto.Header.Height - startBlock.Header.Height;
            var totalTransactions = _blockMap.Values.Sum(o => o.Body.TransactionsCount);
            Logger.Info($"From: {startBlock.Header.Height}, To: {endBlockDto.Header.Height}\n" +
                        $"{blockHeight}, tx:{totalTransactions}");
            var response = totalTransactions - blockHeight - _group * _threadCount - _threadCount;
            var totalTime = GetTotalBlockSeconds(startBlock, endBlockDto);
            if ( blockHeight>=1 && txInfos.Count != 0)
            {
                long allResponseTime = 0;
                foreach (var (key, values) in txInfos)
                {
                    long all = 0;
                    foreach (var value in values)
                    {
                        all += value;
                    }

                    allResponseTime += all / values.Count;
                }

                var totalRequest = txInfos.Keys.Last() * _transactionCount * _threadCount;
                var responseTime = allResponseTime / txInfos.Keys.Last();
                var rate = (double) response/response;
                Logger.Info( $"Request Count: {totalRequest}\n" +
                             $"{(double)response/totalRequest},{responseTime}");
                
                foreach (var (key, value) in _blockMap.TakeLast(100))
                {
                    Logger.Info($"block height:{key},hash:{value.BlockHash}");
                }
                Logger.Info($"\nSummary Information: \n" +
                            $"Block height: {blockHeight} \n" +
                            $"Time: {startBlock.Header.Time:hh:mm:ss}~{endBlockDto.Header.Time:hh:mm:ss} \n" +
                            $"Elapsed time: {(double)totalTime/3600000} hour\n" +
                            $"Response Count: {response}\n" +
                            $"Response Time: {(double)responseTime/_transactionCount}ms\n" +
                            $"Success rate: {rate*100}%");
            }
        }

        private static int GetPerBlockTimeSpan(BlockDto startBlock, BlockDto endBlockDto)
        {
            var timeSpan = new TimeSpan(endBlockDto.Header.Time.Ticks - startBlock.Header.Time.Ticks);
            var hours = timeSpan.Hours;
            var minutes = timeSpan.Minutes;
            var seconds = timeSpan.Seconds;
            var milliseconds = timeSpan.Milliseconds;

            return (hours * 60 * 60 * 1000 + minutes * 60 * 1000 + seconds * 1000 + milliseconds) / Phase;
        }

        private static int GetTotalBlockSeconds(BlockDto startBlock, BlockDto endBlockDto)
        {
            var timeSpan = new TimeSpan(endBlockDto.Header.Time.Ticks - startBlock.Header.Time.Ticks);
            var hours = timeSpan.Hours;
            var minutes = timeSpan.Minutes;
            var seconds = timeSpan.Seconds;
            var mileSeconds = timeSpan.Milliseconds;

            return 1000 * (hours * 60 * 60 + minutes * 60 + seconds) + mileSeconds;
        }
    }
}