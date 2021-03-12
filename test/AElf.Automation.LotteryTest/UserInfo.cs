namespace AElf.Automation.LotteryTest
{
    public class UserInfo
    {
        public UserInfo (string user)
        {
            User = user;
            Balance = 0;
            SpentAmount = 0;
            RewardAmount = 0;
        }

        public string User { get; }
        public long Balance { get; set; }
        public long SpentAmount { get; set; }
        public long RewardAmount { get; set; }
    }
}