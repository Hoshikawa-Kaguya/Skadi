using System.Collections.Generic;

namespace Skadi.Entities.ConfigModule;

public class BiliSubscription
{
    /// <summary>
    /// 群组订阅数组
    /// </summary>
    public List<GroupSubscription> GroupsConfig { set; get; }
}

/// <summary>
/// 群订阅设置
/// </summary>
public class GroupSubscription
{
    /// <summary>
    /// 群组数组
    /// </summary>
    public List<long> GroupId { set; get; }

    /// <summary>
    /// UID动态订阅
    /// </summary>
    public List<long> SubscriptionId { set; get; }

    /// <summary>
    /// UID直播订阅
    /// </summary>
    public List<long> LiveSubscriptionId { set; get; }
}