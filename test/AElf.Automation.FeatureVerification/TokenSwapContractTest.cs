using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Shouldly;
using Tokenswap;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class TokenSwapContractTest
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }

        private TokenContract _tokenContract;
        private GenesisContract _genesisContract;
        private TokenSwapContract _tokenSwapContract;
        private TokenSwapContractContainer.TokenSwapContractStub _tokenSwapContractStub;
        private string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        private string TestAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        private static string RpcUrl { get; } = "192.168.199.205:8000";
        private string Symbol { get; } = "LOT";
        private string[] targetAddress;

        //822c3c33a2c14bf22b300d3efae0054dce1e6f4266350ad923f549f187a5bcd4
        //192b8fddc45c155f444a266c3d9f90c5c90a3351d584fe91714e6fb6c78f4754
        //5404d13ed76614d01b616417064b2b971d0348399fb8773330f4763459b80ae4
        private string PairId { get; } = "1caeff2902a1efd613ad4e176ccad2dc5d34339f4d92051eaeba65ac471b7c69";

        public class TreeInfos
        {
            [JsonProperty("treeInfos")] public List<TreeInfo> Trees { get; set; }
        }

        public class TreeInfo
        {
            [JsonProperty("index")] public int index { get; set; }
            [JsonProperty("root")] public string root { get; set; }
        }

        public class SwapInfo
        {
            private TreeInfos _instance;
            private string _jsonContent;
            private readonly object _lockObj = new object();

            public TreeInfos TreeInfo => GetInfo();

            private TreeInfos GetInfo()
            {
                lock (_lockObj)
                {
                    try
                    {
                        var localPath = CommonHelper.GetDefaultDataDir();
                        var config = Path.Combine(localPath, $@"tokenSwapTest/TreeInfo.json");
                        _jsonContent = File.ReadAllText(config);
                    }
                    catch (FileNotFoundException e)
                    {
                        Console.WriteLine($"Could not find file");
                        return null;
                    }

                    _instance = JsonConvert.DeserializeObject<TreeInfos>(_jsonContent);
                }

                return _instance;
            }
        }


        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("TokenSwap_");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes-env205-main");

            NodeManager = new NodeManager(RpcUrl);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
//            _tokenSwapContract = new TokenSwapContract(NodeManager, InitAccount);
//            Logger.Info($"TokenSwap contract : {_tokenSwapContract}");
            _tokenSwapContract = new TokenSwapContract(NodeManager, InitAccount,
                "2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS");
            _tokenSwapContractStub =
                _tokenSwapContract.GetTestStub<TokenSwapContractContainer.TokenSwapContractStub>(InitAccount);
//            _tokenContract.TransferBalance(InitAccount, TestAccount, 1000000000000);
            if (!_tokenContract.GetTokenInfo(Symbol).Symbol.Equals(Symbol))
                CreateTokenAndIssue();
            targetAddress = new string[]
            {
                "2oSMWm1tjRqVdfmrdL8dgrRvhWu1FP8wcZidjS6wPbuoVtxhEz",
                "WRy3ADLZ4bEQTn86ENi5GXi5J1YyHp9e99pPso84v2NJkfn5k",
                "2frDVeV6VxUozNqcFbgoxruyqCRAuSyXyfCaov6bYWc7Gkxkh2",
                "2ZYyxEH6j8zAyJjef6Spa99Jx2zf5GbFktyAQEBPWLCvuSAn8D",
                "eFU9Quc8BsztYpEHKzbNtUpu9hGKgwGD2tyL13MqtFkbnAoCZ",
                "2V2UjHQGH8WT4TWnzebxnzo9uVboo67ZFbLjzJNTLrervAxnws",
                "EKRtNn3WGvFSTDewFH81S7TisUzs9wPyP4gCwTww32waYWtLB",
                "2LA8PSHTw4uub71jmS52WjydrMez4fGvDmBriWuDmNpZquwkNx",
                "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6",
                "YF8o6ytMB7n5VF9d1RDioDXqyQ9EQjkFK3AwLPCH2b9LxdTEq",
                "h6CRCFAhyozJPwdFRd7i8A5zVAqy171AVty3uMQUQp1MB9AKa",
                "28qLVdGMokanMAp9GwfEqiWnzzNifh8LS9as6mzJFX1gQBB823",
                "2Dyh4ASm6z7CaJ1J1WyvMPe2sJx5TMBW8CMTKeVoTMJ3ugQi3P",
                "2G4L1S7KPfRscRP6zmd7AdVwtptVD3vR8YoF1ZHgPotDNbZnNY",
                "W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo",
                "2REajHMeW2DMrTdQWn89RQ26KQPRg91coCEtPP42EC9Cj7sZ61",
                "2a6MGBRVLPsy6pu4SVMWdQqHS5wvmkZv8oas9srGWHJk7GSJPV",
                "2cv45MBBUHjZqHva2JMfrGWiByyScNbEBjgwKoudWQzp6vX8QX",
                "7BSmhiLtVqHSUVGuYdYbsfaZUGpkL2ingvCmVPx66UR5L5Lbs"
            };
        }

        [TestMethod]
        public void Transfer()
        {
            foreach (var receiver in targetAddress)
            {
                if (receiver.Equals(InitAccount)) continue;
                var elfBalance = _tokenContract.GetUserBalance(receiver);
                var balance = _tokenContract.GetUserBalance(receiver, Symbol);
                Logger.Info(
                    $"Check the balance of receiver account {receiver}, ELF balance is {elfBalance}, {Symbol} balance is {balance}");

                if (elfBalance <= 10000_00000000) continue;
                _tokenContract.SetAccount(receiver);
                _tokenContract.TransferBalance(receiver, InitAccount, elfBalance - 10000_00000000);
                if (balance <= 10000_00000000) continue;
                _tokenContract.SetAccount(receiver);
                _tokenContract.TransferBalance(receiver, InitAccount, balance - 10000_00000000, Symbol);
            }

            var initBalance = _tokenContract.GetUserBalance(InitAccount);
            var initSymbolBalance = _tokenContract.GetUserBalance(InitAccount, Symbol);
            Logger.Info(
                $"Balance of init account {InitAccount}, ELF balance is {initBalance}, {Symbol} balance is {initSymbolBalance}");
        }

        [TestMethod]
        public async Task CreateSwap()
        {
            var originTokenSizeInByte = 32;
            var swapRatio = new SwapRatio
            {
                OriginShare = 100_00000000,
                TargetShare = 1,
            };
            var depositAmount = 1000000_00000000;
            _tokenContract.ApproveToken(InitAccount, _tokenSwapContract.ContractAddress, depositAmount, Symbol);
            _tokenContract.ApproveToken(InitAccount, _tokenSwapContract.ContractAddress, depositAmount, "ELF");

            var result = await _tokenSwapContractStub.CreateSwap.SendAsync(new CreateSwapInput
            {
                OriginTokenSizeInByte = originTokenSizeInByte,
                OriginTokenNumericBigEndian = true,
                SwapTargetTokenList =
                {
                    new SwapTargetToken
                    {
                        DepositAmount = depositAmount,
                        SwapRatio = swapRatio,
                        TargetTokenSymbol = "ELF"
                    },
                    new SwapTargetToken
                    {
                        DepositAmount = depositAmount,
                        SwapRatio = swapRatio,
                        TargetTokenSymbol = Symbol
                    }
                }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var pairId = result.Output;
            var swapid = SwapPairAdded.Parser
                .ParseFrom(result.TransactionResult.Logs.First(l => l.Name.Contains(nameof(SwapPairAdded))).NonIndexed)
                .SwapId;
            Logger.Info($"{pairId}");
            pairId.ShouldBe(swapid);
            var swapPair = await _tokenSwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
            {
                SwapId = pairId,
                TargetTokenSymbol = Symbol
            });
            swapPair.RoundCount.ShouldBe(0);
            swapPair.SwappedAmount.ShouldBe(0);
            swapPair.SwappedTimes.ShouldBe(0);
            swapPair.SwapRatio.ShouldBe(swapRatio);
            swapPair.TargetTokenSymbol.ShouldBe(Symbol);
            swapPair.OriginTokenSizeInByte.ShouldBe(originTokenSizeInByte);
        }

        [TestMethod]
        public async Task AddManyRound()
        {
            var swapInfo = new SwapInfo();
            var treeInfo = swapInfo.TreeInfo;
            foreach (var tree in treeInfo.Trees)
            {
                await AddSwapRound(tree.root, tree.index);
            }
        }

        [TestMethod]
        [DataRow("0x72fd2306d145cbdcc7a295ee2cf36be8da6cb6e3532123a360f994be14abd76d", 0)]
        public async Task AddSwapRound(string root, int id)
        {
            var pId = Hash.LoadFromHex(PairId);
            var result = await _tokenSwapContractStub.CreateSwapRound.SendAsync(new CreateSwapRoundInput()
            {
                SwapId = pId,
                MerkleTreeRoot =
                    Hash.LoadFromHex(root),
                RoundId = id
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public async Task Deposit()
        {
            var depositAmount = 10000000_00000000;
            var pairId = Hash.LoadFromHex(PairId);
            _tokenContract.ApproveToken(InitAccount, _tokenSwapContract.ContractAddress, depositAmount, "ELF");
            _tokenContract.ApproveToken(InitAccount, _tokenSwapContract.ContractAddress, depositAmount, Symbol);

            var beforeBalance = _tokenContract.GetUserBalance(_tokenSwapContract.ContractAddress, Symbol);
            var swapPairInfo = await _tokenSwapContractStub.GetSwapInfo.CallAsync(pairId);
            swapPairInfo.Controller.ShouldBe(InitAccount.ConvertAddress());
            var swapPair = await _tokenSwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                {SwapId = pairId, TargetTokenSymbol = Symbol});
            swapPair.DepositAmount.ShouldBe(beforeBalance);

            var result = await _tokenSwapContractStub.Deposit.SendAsync(new DepositInput
            {
                SwapId = pairId,
                TargetTokenSymbol = Symbol,
                Amount = depositAmount
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var elfResult = await _tokenSwapContractStub.Deposit.SendAsync(new DepositInput
            {
                SwapId = pairId,
                TargetTokenSymbol = "ELF",
                Amount = depositAmount
            });
            elfResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var afterBalance = _tokenContract.GetUserBalance(_tokenSwapContract.ContractAddress, Symbol);
            swapPair = await _tokenSwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                {SwapId = pairId, TargetTokenSymbol = Symbol});
            swapPair.DepositAmount.ShouldBe(afterBalance);
            afterBalance.ShouldBe(beforeBalance + depositAmount);
        }

        [TestMethod]
        public async Task ChangeSwapRatio()
        {
            var pairId = Hash.LoadFromHex(PairId);
            var result = await _tokenSwapContractStub.ChangeSwapRatio.SendAsync(new ChangeSwapRatioInput
            {
                SwapId = pairId,
                TargetTokenSymbol = Symbol,
                SwapRatio = new SwapRatio
                {
                    OriginShare = 100_0000000,
                    TargetShare = 1,
                }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }


        [TestMethod]
        [DataRow("0xf22e7c3926caee87e086e23ddaeecb282b2c702c9b38e0e24c844df3fd67ea49")]
        public async Task SwapToken(string sUniqueId)
        {
            var originAmount = "3380453555694209106933";
            var pairId = Hash.LoadFromHex(PairId);
            var uniqueId = Hash.LoadFromHex(sUniqueId);

            var receiveAccount = "WRy3ADLZ4bEQTn86ENi5GXi5J1YyHp9e99pPso84v2NJkfn5k";
            var beforeBalance = _tokenContract.GetUserBalance(receiveAccount, Symbol);
            var beforeElfBalance = _tokenContract.GetUserBalance(receiveAccount, "ELF");

            var beforeSwapBalance = _tokenContract.GetUserBalance(_tokenSwapContract.ContractAddress, Symbol);
            var swapPair = await _tokenSwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                {SwapId = pairId, TargetTokenSymbol = Symbol});
            swapPair.DepositAmount.ShouldBe(beforeSwapBalance);

            var beforeSwapElfBalance = _tokenContract.GetUserBalance(_tokenSwapContract.ContractAddress, "ELF");
            var swapElfPair = await _tokenSwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                {SwapId = pairId, TargetTokenSymbol = "ELF"});
            swapElfPair.DepositAmount.ShouldBe(beforeSwapElfBalance);

            var stringInfo = "0x39976b7def3e8fe2d8513c82e68db3672631c34ca1029a7a1118fddf5fdaf20f";
            var isLeftInfo = "true";

            var hashList = stringInfo.Split(",").ToList();
            var boolList = isLeftInfo.Split(",").Take(hashList.Count).ToList();
            var merklePathNodes = new List<MerklePathNode>();
            for (int i = 0; i < hashList.Count; i++)
            {
                var merkle = new MerklePathNode();
                merkle.Hash = Hash.LoadFromHex(hashList[i]);
                merkle.IsLeftChildNode = Boolean.Parse(boolList[i]);
                merklePathNodes.Add(merkle);
            }

            var merklePath = new MerklePath
            {
                MerklePathNodes = {merklePathNodes}
            };
            var result = await _tokenSwapContractStub.SwapToken.SendAsync(new SwapTokenInput
            {
                SwapId = pairId,
                OriginAmount = originAmount,
                UniqueId = uniqueId,
                ReceiverAddress = receiveAccount.ConvertAddress(),
                MerklePath = merklePath,
                RoundId = 0
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var tokenTransferredEvent = result.TransactionResult.Logs
                .First(l => l.Name == nameof(Transferred));
            var nonIndexed = Transferred.Parser.ParseFrom(tokenTransferredEvent.NonIndexed);
            var expectedAmount = long.Parse(originAmount.Substring(0, originAmount.Length - 10));
            nonIndexed.Amount.ShouldBe(expectedAmount);

            var balance = _tokenContract.GetUserBalance(receiveAccount, Symbol);
            var elfBalance = _tokenContract.GetUserBalance(receiveAccount, "ELF");
            elfBalance.ShouldBe(beforeElfBalance + expectedAmount);
            balance.ShouldBe(beforeBalance + expectedAmount);

            var afterSwapBalance = _tokenContract.GetUserBalance(_tokenSwapContract.ContractAddress, Symbol);
            swapPair = await _tokenSwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                {SwapId = pairId, TargetTokenSymbol = Symbol});
            swapPair.DepositAmount.ShouldBe(afterSwapBalance);
            swapPair.DepositAmount.ShouldBe(beforeSwapBalance - expectedAmount);

            var afterSwapElfBalance = _tokenContract.GetUserBalance(_tokenSwapContract.ContractAddress, "ELF");
            swapElfPair = await _tokenSwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                {SwapId = pairId, TargetTokenSymbol = "ELF"});
            swapElfPair.DepositAmount.ShouldBe(afterSwapElfBalance);
            swapElfPair.DepositAmount.ShouldBe(beforeSwapElfBalance - expectedAmount);
        }

        [TestMethod]
        public void CheckBalance()
        {
            var balance = _tokenContract.GetUserBalance(TestAccount, Symbol);
            var elfBalance = _tokenContract.GetUserBalance(TestAccount, "ELF");
            Logger.Info($"ELF {elfBalance}; {Symbol} {balance}");

            var swapBalance = _tokenContract.GetUserBalance(_tokenSwapContract.ContractAddress, Symbol);
            var swapElfBalance = _tokenContract.GetUserBalance(_tokenSwapContract.ContractAddress, "ELF");
            Logger.Info($"ELF {swapElfBalance}; {Symbol} {swapBalance}");
        }

        [TestMethod]
        public async Task GetSwapInfo()
        {
            var pairId = Hash.LoadFromHex(PairId);
            var swapPairInfo = await _tokenSwapContractStub.GetSwapInfo.CallAsync(pairId);
            var swapPair = await _tokenSwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                {SwapId = pairId, TargetTokenSymbol = Symbol});
            var swapRound = await _tokenSwapContractStub.GetSwapRound.CallAsync(new GetSwapRoundInput
                {SwapId = pairId, TargetTokenSymbol = Symbol, RoundId = 1});
            var elfSwapPair = await _tokenSwapContractStub.GetSwapPair.CallAsync(new GetSwapPairInput
                {SwapId = pairId, TargetTokenSymbol = "ELF"});
            var elfSwapRound = await _tokenSwapContractStub.GetSwapRound.CallAsync(new GetSwapRoundInput
                {SwapId = pairId, TargetTokenSymbol = "ELF", RoundId = 1});
            swapPairInfo.Controller.ShouldBe(InitAccount.ConvertAddress());
            swapPair.SwapRatio.OriginShare.ShouldBe(100_00000000);
            swapPair.RoundCount.ShouldBe(elfSwapPair.RoundCount);
            swapPair.SwapId.ShouldBe(elfSwapPair.SwapId);
            swapPair.OriginTokenNumericBigEndian.ShouldBe(elfSwapPair.OriginTokenNumericBigEndian);
            swapPair.SwappedTimes.ShouldBe(elfSwapPair.SwappedTimes);

            swapRound.StartTime.ShouldBe(elfSwapRound.StartTime);
            swapRound.SwappedTimes.ShouldBe(elfSwapRound.SwappedTimes);
            swapRound.MerkleTreeRoot.ShouldBe(elfSwapRound.MerkleTreeRoot);

            Logger.Info($"{swapPair.RoundCount}");
            Logger.Info($"All the amount is {swapPair.SwappedAmount}");
            Logger.Info($"times is {swapPair.SwappedTimes}");
            Logger.Info($"Current amount is {swapRound.SwappedAmount}");
            Logger.Info($"Current times is {swapRound.SwappedTimes}");
            Logger.Info($"Merkle root is {swapRound.MerkleTreeRoot}");
        }

        [TestMethod]
        public void CheckTree()
        {
            var merkleNodes =
                "0x327b7ce14a7667bd4d916f58d4520d013f8a5c054e6b169380cd4f9fe583c584,0xdfef8251123d78e6cfbc0ddb176b31ff71ff22cd1cd3ea46545be50710f7cee5,0x041031b64d3d551a3290fbaffe56972c51689fa70dbb666bc03498967ea4ebae,0xd53f6bdd89505012937d242cb12b7cac8436b319fa131b6cf1ee306d8661c704,0x9fe153ac1ba09be2dbf397b8a1eb5e4ec5215ccc1180927979f12ed1f507c92b,0x7aa090a510348c71820a32107dcea21eaa0b5b98a116ded3c298c812b94af694,0x70f28464ed57eb8d12919eb14062cddbe4105a0be67a38928216ddfccbbbdc42,0x25d5742b663f4fbe33a04fed3c611fbee1fc66257889cc9b2faca46f45b615de,0xbc26167f77d18447ad36683f387ab1805dea9978db718cafaf93740424ca8f10,0xe9332adf1208c66c230ffed1388cdc88ff98a6ff5ef07cfb00bac93547b20935,0x61dae27a2ccc576a3f3803058fe14f2f36ad39a3576cce23aeac90f5bc8abf2d,0x8a4ae7d8efcf1ba6b5f3191d5262234c196ef6a4e489dcbfab8fc95f46dc444d,0x29756343b33629db3090094dae0ca0b310eea123ed13ce470bc1bc2aa587a1ae,0x8daaf7319530d77d6b232fade5b1841ddb08128e6dcc2a76b6de9e02f053f2f4,0xe947372cdb7c779507fcee2ed137bef2016540efaabc851cf9eb7430c9a92a4d,0x4bad8660de384fb342b393c3b9b9929ab4eb719aabc3dc69f4e2089ce7c66832,0x1458cbd661f5718e82291692a7b8f6b20ae8110bd70d419a425741146eded597,0x4676384c1fe121672e08099be37dca7d689bfb532ff3a030d902d85749510568,0xd635ed9695fcc291208c9d0f77d20de6627d90bb37fae74d486c6998eaa3e1d7,0x8dd7594467fe3830399b393d5d3732cd36e16041b1964a6c9ca489f7d6d8567d,0xe64c3971332ab6ea08d4eb94f0c88d6221222274df68d8733a1beb4b56e0be5c,0x07d9e34d54b706a45aa36c8e1da898b208778b7543089dc00260deeb1b71d412,0xf646c96d95adee40d0d82498c234729ad965e5eaf7e148165ae2e2b78f7c6abf,0x2ecfebf868a8bfe2117b57d8ca83c98f93c7346a4871f71c39b22f29a72af976,0xb6bf5264b6e93a1526fdc184e4dc6c2d0966f2219bc6ec5db1f509d65cdadf1f,0x3b3e9aefec98858f92f60f9e48c751d1ec1d7b36af11e75b7fef6c594070c88e,0x6541b00f73bc6148857455a5e345dcc1da2098dfe2e5f376585c2fca802ae07a,0xe9907c81b62d3d60f04e64f9b9a72fd9e8574a6836121811ae41d648da99c223,0xa4f82594d28f9f2d61edf1bbd2af909479e28030d2deca1a6b4ef27e1841d790,0xdae0ffaf2d3aae82d18e3dacde66d2b322a174397b2ce3337d6258ce1d5ed69b,0x97f330b436b0fa2e784db000293e51b51c64e9d423bf36be785903266d724430,0xca16956f5aff0897b707ab382031d9d798f18fd75d3b0c16d7a23aeacbb751d4,0xe896c14f654e90959d8b03d0e1ca0ebba970711c964a01cf4f67bbab2b1f24fe,0x9888eb319a04dc0ad8bb4814c577f55e4a5e8fb1f529e4c2a574ec34799b1b25,0xd4afcbcaf5e9574fa5d857d28625a3ac00b3015387f2197e89a195438ad07be2,0x3f4bccdfeee106ceddeb3a21c9e6d17ec5a7761a2b5f7942e5f9c492139cddd2,0xb7efdce413e3391aece95d2047671b1bb6f5e98b07c65c2a3f5a4d26e56cf53a,0x96f0ce12b23f330fba1f7f42efc49944e26b74806597262ce1db6cb8fb2b4589,0x4ee47244af321353ee379f1fe0a603331d868522ac820e0948c02c469f27a997,0x829394cdde8a46311156fe4cf82cc3a637a9f65ad892678ca758ec0d3b1d35f8,0xae8384caa04265746896eb5b68166e2085695f1853e2d9b87780830ef7748705,0xe5813c7caac40d0993987f798e920ded7a664f9fb47801f1cec419fbbdc48937,0x32af09dfe9525db9ecd912d8e0155c250f1156937fa55ed6bfbe5b94663602e4,0x3b26f2b8b24c934b0d9beaf7f48eed26cb5ec4c8442e39f7abaf92d15ebd882f,0x3f0938d0b68c955948b5769280100b8170dcdcecbdb0407e93b5df8cc147f28a,0xd26ab1eff8c8a1a6bf7e4875746179451363f7516855b88e0c1ce73ad315e1bb,0x3cb399d86d81023a42a894cb8dc0530f10829c6f46467450a24ad6c5845cbeb1,0xe53f29989ea94fcd83f080b519fd0a7449dd78fb90ea911f36817680fc1827c4,0xabe55e21777eee685d3e4d13d84adee6d51cee886c5024276364d2cfa6fbabac,0x67924bac1634da753b48c46440896122ed844c2c8558f04671e944f1b114ea84,0xf299944aaac263f1e9ab5c2a184032dd64bf9e8e4d85e4f338d83205536fca05,0x74197ad3d03800cfc1521073167ff8ee65208063be6ce4e15f790de92c7c3ce8,0xbe991f5f0c20b592d84082f5cd51776b136f84ce637281033dead050118aa6fa,0x4e8926acb29325d8bb96744753a3b91d05ac12146bac6945b0b1b88996329dd6,0x5cd5e44b30094655b4e3be2264f27ef7c03aa321a73610ad0df292109cb33860,0x4a847e755fcca145e91217ed0a25b30cc2dbeaeced2871e53796c68f3766eb96,0x48ae954c6b736d9baad6e107a263e3d25fce328e5520012335ee0a7dc7edbbd8,0xcd1cb0b7aae35514fdfc7b9afda2731c8e183fa86aa74ddf092928bb10836ec8,0xe39ca30988b2366073c8e06a8e3d2647428011126a3bd58e63588ffcdb9ad833,0xaa4ad256af0b1da95dba9ddb8701066c3bfc7185b9566d1190275aa4feab3835,0xa7e5e4b0369600ba575b7798c31f2bccbb5af023a9421d7d67abaf7c7126c660,0xc454830f632c2efc7701824b26ed778b2b9a9aba6a14aa2cf61bb0203aa39f51,0x4c5ba7350b704fa0ff33dd8852f89fa981eefaed0eb619ecd4b0b2ff48024628,0x027cfd3963b8e812baf89c9ee2c0994374cbbc54466f6cc236059fb72151c63e,0x5b1794093f0f6c37049f6680943275a0779c564cd1226560873f9022017eecf4";
            var nodeList = merkleNodes.Split(",").ToList();
            if (nodeList.Count % 2 == 1)
                nodeList.Add(nodeList.Last());
            var leftNods = new List<Hash>();
            foreach (var n in nodeList)
            {
                var node = Hash.LoadFromHex(n);
                leftNods.Add(node);
            }

            var nodeToAdd = leftNods.Count / 2;
            var newAdded = 0;
            var i = 0;
            while (i < leftNods.Count - 1)
            {
                var left = leftNods[i++];
                var right = leftNods[i++];
                leftNods.Add(HashHelper.ConcatAndCompute(left, right));
                if (++newAdded != nodeToAdd)
                    continue;

                // complete this row
                if (nodeToAdd % 2 == 1 && nodeToAdd != 1)
                {
                    nodeToAdd++;
                    leftNods.Add(leftNods.Last());
                }

                // start a new row
                nodeToAdd /= 2;
                newAdded = 0;
            }

            leftNods.Last()
                .ShouldBe(Hash.LoadFromHex("0x7bc1c986d4d278ac315c7f0d05939c0e11ae915497f062b754d18fa0986bcf6e"));
            foreach (var node in leftNods)
            {
                Logger.Info(node.ToHex());
            }
        }

        private void CreateTokenAndIssue()
        {
            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
            {
                Symbol = Symbol,
                TotalSupply = 10_00000000_00000000,
                Decimals = 8,
                Issuer = InitAccount.ConvertAddress(),
                IsBurnable = true,
                IsProfitable = true,
                TokenName = "LOT"
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _tokenContract.IssueBalance(InitAccount, InitAccount, 5_00000000_00000000, Symbol);
        }
    }
}