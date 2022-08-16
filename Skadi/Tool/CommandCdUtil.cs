// ReSharper disable RedundantUsingDirective

#if RELEASE
using System;
using System.Collections.Concurrent;
#endif

namespace Skadi.Tool;

internal static class CommandCdUtil
{
    #region 调用记录

    public enum CommandFlag
    {
        PicSearch,
        Setu,
        GroupPoke
    }

#if RELEASE
    public struct UserRecord
    {
        internal long        GroupId     { get; set; }
        internal long        UserId      { get; set; }
        internal CommandFlag CommandName { get; set; }
    }

    private static readonly ConcurrentDictionary<UserRecord, DateTime> _userRecords = new();
#endif

    #endregion

    #region 调用时间检查

    /// <summary>
    /// 检查用户调用时是否在CD中
    /// 对任何可能刷屏的指令都有效
    /// </summary>
    /// <param name="groupId">群号</param>
    /// <param name="userId">用户ID</param>
    /// <param name="cmdFlag">指令类型</param>
    /// <param name="cd">CD时长(s)</param>
    /// <returns>是否在CD中</returns>
    public static bool IsInCD(long groupId, long userId, CommandFlag cmdFlag, int cd = 20)
    {
#if DEBUG
        return false;
#else
        var time = DateTime.Now; //获取当前时间
        var user = new UserRecord
        {
            GroupId = groupId,
            UserId = userId,
            CommandName = cmdFlag
        };
        //尝试从字典中取出上一次调用的时间
        if (_userRecords.TryGetValue(user, out var lastUseTime) &&
            (long) (time - lastUseTime).TotalSeconds < cd)
        {
            //刷新调用时间
            _userRecords.TryUpdate(user, time, lastUseTime);
            return true;
        }

        //写入调用时间
        _userRecords.TryAdd(user, time);
        return false;
#endif
    }

    #endregion
}