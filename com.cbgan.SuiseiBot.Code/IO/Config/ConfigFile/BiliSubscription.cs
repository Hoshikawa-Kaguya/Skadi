using System.Collections.Generic;

namespace com.cbgan.SuiseiBot.Code.IO.Config.ConfigFile
{
    internal class BiliSubscription
    {
        public int                     FlashTime    { set; get; }
        public List<GroupSubscription> GroupsConfig { set; get; }
    }

    internal class GroupSubscription
    {
        public List<long> GroupId          { set; get; }
        public bool       PCR_Subscription { set; get; }
        public List<long> SubscriptionId   { set; get; }
    }
}
