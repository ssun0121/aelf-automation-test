namespace AElf.Automation.SideChain.Verification.Verify
{
    public class CrossChainTransactionVerifyResult
    {
        public string Result { get; set; }
        public CrossChainTransactionInfo TxInfo { get; set; }
        public int ChainId { get; set; }
        public string TxId { get; set; }

        public CrossChainTransactionVerifyResult(string result, int chainId, string txId)
        {
            Result = result;
            ChainId = chainId;
            TxId = txId;
        }
    }
}