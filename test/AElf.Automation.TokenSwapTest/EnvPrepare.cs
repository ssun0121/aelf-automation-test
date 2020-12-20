using System.Collections.Generic;
using System.Linq;

namespace AElf.Automation.TokenSwapTest
{
    public class EnvPrepare
    {
        private static readonly EnvPrepare Instance = new EnvPrepare();
        public static readonly Dictionary<long, TreeInfo> TreeInfos = new Dictionary<long, TreeInfo>();
        
        public static EnvPrepare GetDefaultEnv()
        {
            return Instance;
        }
        public Dictionary<long, TreeInfo> GetCurrentTreeInfo(long index)
        {
            for (long i = index; i < 1024; i++)
            {
                if (TreeInfos.Keys.Contains(i)) continue;
                var swapInfo = new SwapInfo(i);
                var treeInfo = swapInfo.TreeInfo;
                if (treeInfo == null)
                    break;
                TreeInfos.Add(i,treeInfo);
            }

            return TreeInfos;
        }
    }
}