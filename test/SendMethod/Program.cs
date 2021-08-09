using System;
using AElf.Contracts.Election;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Newtonsoft.Json;
using Shouldly;

namespace SendMethod
{
    class Program
    {
        static void Main()
        {
            Log4NetHelper.LogInit("SendMethod");
            var oldPubkey = ConfigInfo.ReadInformation.OldPubkey;
            var password = ConfigInfo.ReadInformation.Password;
            var admin = ConfigInfo.ReadInformation.Admin;
            var newPubkey = ConfigInfo.ReadInformation.NewPubkdy;
            var url = ConfigInfo.ReadInformation.Url;
            
            var nodeManage = new NodeManager(url);
            var contractManage = new ContractManager(nodeManage,admin, password);
            var election = contractManage.Election;

            Logger.Info("==== replace key ====");
            
            Logger.Info($"{oldPubkey}\n {newPubkey}");
            election.SetAccount(admin, password);
            var replaceResult = election.ExecuteMethodWithResult(ElectionMethod.ReplaceCandidatePubkey, 
                new ReplaceCandidatePubkeyInput
                {
                    OldPubkey = oldPubkey,
                    NewPubkey = newPubkey
                });
            replaceResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var checkKey = election.GetNewestPubkey(oldPubkey);
            checkKey.ShouldBe(newPubkey);
        }

        private static readonly ILog Logger = Log4NetHelper.GetLogger();
    }

    public class ConfigInfo
    {
        [JsonProperty("Url")] public string Url { get; set; }
        [JsonProperty("OldPubkey")] public string OldPubkey { get; set; }
        [JsonProperty("Password")] public string Password { get; set; }
        [JsonProperty("NewPubkdy")] public string NewPubkdy { get; set; }
        [JsonProperty("Admin")] public string Admin { get; set; }

        public static ConfigInfo ReadInformation => 
            ConfigHelper<ConfigInfo>.GetConfigInfo("config.json", false);
    }
}