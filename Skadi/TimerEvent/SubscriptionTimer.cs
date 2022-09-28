using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Skadi.TimerEvent.Event;
using Sora.EventArgs.SoraEvent;
using Sora.EventArgs.WebsocketEvent;
using YukariToolBox.LightLog;

namespace Skadi.TimerEvent;

internal static class SubscriptionTimer
{
    #region 订阅数据

    /// <summary>
    /// 计时器
    /// </summary>
    [UsedImplicitly]
    private static readonly Timer _subTimer = new(
        SubscriptionEvent, //事件处理
        null,              //初始化数据
        new TimeSpan(0),   //即刻执行
        new TimeSpan(0, 0, 1, 0));

    /// <summary>
    /// 订阅列表
    /// </summary>
    private static readonly List<ConnectEventArgs> _subDictionary = new();

    #endregion

    #region 计时器初始化/停止

    /// <summary>
    /// 添加新的订阅
    /// </summary>
    /// <param name="eventArgs">ConnectEventArgs</param>
    internal static void TimerEventAdd(ConnectEventArgs eventArgs)
    {
        Log.Info("SubTimer", "添加订阅");
        //尝试添加订阅
        if (!_subDictionary.Exists(args => args.LoginUid == eventArgs.LoginUid))
        {
            _subDictionary.Add(eventArgs);
        }
        else
        {
            _subDictionary.RemoveAll(arg => arg.LoginUid == eventArgs.LoginUid);
            _subDictionary.Add(eventArgs);
        }
    }

    /// <summary>
    /// 删除计时器
    /// </summary>
    /// <param name="_">没啥用</param>
    /// <param name="eventArgs">ConnectEventArgs</param>
    internal static ValueTask DelTimerEvent(Guid _, ConnectionEventArgs eventArgs)
    {
        //尝试移除订阅
        if (!_subDictionary.Exists(args => args.LoginUid == eventArgs.SelfId))
        {
            Log.Error("SubTimer", "未找到该账号的订阅信息");
            return ValueTask.CompletedTask;
        }

        try
        {
            _subDictionary.RemoveAll(args => args.LoginUid == eventArgs.SelfId);
        }
        catch (Exception e)
        {
            Log.Error("SubTimer", "移除订阅账号失败");
            Log.Error("SubTimer", Log.ErrorLogBuilder(e));
        }

        return ValueTask.CompletedTask;
    }

    #endregion

    #region 更新事件方法

    /// <summary>
    /// 提醒DD动态更新的事件
    /// </summary>
    private static void SubscriptionEvent(object obj)
    {
        foreach (var eventArg in _subDictionary)
            SubscriptionUpdate.BiliUpdateCheck(eventArg);
    }

    #endregion
}