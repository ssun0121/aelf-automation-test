using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.OptionManagers;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.SDK;
using AElfChain.SDK.Models;
using Google.Protobuf;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using ApiMethods = AElf.Automation.Common.Helpers.ApiMethods;

namespace AElf.Automation.Common.Contracts
{
    public class MethodStubFactory : IMethodStubFactory, ITransientDependency
    {
        public Address ContractAddress { get; set; }
        public string SenderAddress { get; set; }
        public Address Sender => AddressHelper.Base58StringToAddress(SenderAddress);
        public IApiHelper ApiHelper { get; }
        public IApiService ApiService => ApiHelper.ApiService;

        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        public MethodStubFactory(IApiHelper apiHelper)
        {
            ApiHelper = apiHelper;
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public IMethodStub<TInput, TOutput> Create<TInput, TOutput>(Method<TInput, TOutput> method)
            where TInput : IMessage<TInput>, new() where TOutput : IMessage<TOutput>, new()
        {
            async Task<IExecutionResult<TOutput>> SendAsync(TInput input)
            {
                var transaction = new Transaction
                {
                    From = Sender,
                    To = ContractAddress,
                    MethodName = method.Name,
                    Params = ByteString.CopyFrom(method.RequestMarshaller.Serializer(input)),
                };
                transaction.AddBlockReference(ApiHelper.GetApiUrl());
                transaction = ApiHelper.TransactionManager.SignTransaction(transaction);

                var transactionOutput = await ApiService.SendTransactionAsync(transaction.ToByteArray().ToHex());

                var checkTimes = 0;
                TransactionResultDto resultDto;
                TransactionResultStatus status;
                while (true)
                {
                    checkTimes++;
                    resultDto = await ApiService.GetTransactionResultAsync(transactionOutput.TransactionId);
                    status = resultDto.Status.ConvertTransactionResultStatus();
                    if (status != TransactionResultStatus.Pending && status != TransactionResultStatus.NotExisted)
                    {
                        if (status == TransactionResultStatus.Mined)
                            Logger.Info($"TransactionId: {resultDto.TransactionId}, Method: {resultDto.Transaction.MethodName}, Status: {status}");
                        else
                            Logger.Error(
                                $"TransactionId: {resultDto.TransactionId}, Status: {status}\r\nDetail message: {JsonConvert.SerializeObject(resultDto)}");

                        break;
                    }

                    if(checkTimes % 20 ==0)
                        $"TransactionId: {resultDto.TransactionId}, Method: {resultDto.Transaction.MethodName}, Status: {status}".WriteWarningLine();
                    
                    if (checkTimes == 360) //max wait time 3 minutes
                        throw new Exception($"Transaction {resultDto.TransactionId} in pending status more than three minutes.");
                    
                    Thread.Sleep(500);
                }

                var transactionResult = resultDto.Logs == null
                    ? new TransactionResult
                    {
                        TransactionId = HashHelper.HexStringToHash(resultDto.TransactionId),
                        BlockHash = resultDto.BlockHash == null
                            ? null
                            : Hash.FromString(resultDto.BlockHash),
                        BlockNumber = resultDto.BlockNumber,
                        Bloom = ByteString.CopyFromUtf8(resultDto.Bloom),
                        Error = resultDto.Error ?? "",
                        Status = status,
                        ReadableReturnValue = resultDto.ReadableReturnValue ?? ""
                    }
                    : new TransactionResult
                    {
                        TransactionId = HashHelper.HexStringToHash(resultDto.TransactionId),
                        BlockHash = resultDto.BlockHash == null
                            ? null
                            : Hash.FromString(resultDto.BlockHash),
                        BlockNumber = resultDto.BlockNumber,
                        Logs =
                        {
                            resultDto.Logs.Select(o => new LogEvent
                            {
                                Address = AddressHelper.Base58StringToAddress(o.Address),
                                Name = o.Name,
                                NonIndexed = ByteString.FromBase64(o.NonIndexed)
                            }).ToArray()
                        },
                        Bloom = ByteString.CopyFromUtf8(resultDto.Bloom),
                        Error = resultDto.Error ?? "",
                        Status = status,
                        ReadableReturnValue = resultDto.ReadableReturnValue ?? ""
                    };

                return new ExecutionResult<TOutput>()
                {
                    Transaction = transaction,
                    TransactionResult = transactionResult
                };
            }

            async Task<TOutput> CallAsync(TInput input)
            {
                var transaction = new Transaction()
                {
                    From = Sender,
                    To = ContractAddress,
                    MethodName = method.Name,
                    Params = ByteString.CopyFrom(method.RequestMarshaller.Serializer(input))
                };
                transaction = ApiHelper.TransactionManager.SignTransaction(transaction);

                var returnValue = await ApiService.ExecuteTransactionAsync(transaction.ToByteArray().ToHex());
                return method.ResponseMarshaller.Deserializer(ByteArrayHelper.HexStringToByteArray(returnValue));
            }

            return new MethodStub<TInput, TOutput>(method, SendAsync, CallAsync);
        }
    }
}