using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AElf.Automation.CheckBranch
{
    [DataContract]
    public class Branch
    {
        public Branch(long height, string blockHash)
        {
            Height = height;
            BlockHash = blockHash;
        }
        [DataMember]
        public long Height { get; set; }
        [DataMember]
        public string BlockHash { get; set; }
    }
    
    [DataContract]
    public class ForkBranch
    {
       
        public ForkBranch(long height, List<Branch> branches)
        {
            
            LIBHeight = height;
            Branches = branches;
        }
        [DataMember]
        public long LIBHeight { get; set; }
        [DataMember]
        public List<Branch> Branches { get; set; }
    }
}