using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Sora.Entities;
using Sora.Entities.MessageElement;

namespace AntiRain.ChatModule.PCRGuildBattle
{
    /// <summary>
    /// 上树提示模块
    /// </summary>
    //TODO 等待迁移
    public static class TreeTipManager
    {
        #region 私有字段

        //上树信息
        private struct TreeInfo
        {
            //api接口
            internal Group treeGroup;

            //用户信息
            internal long     uid;
            internal DateTime updateTime;
        }

        //上树计时器
        private static readonly Timer treeTimer =
            new(TreeTimerEvent,
                null,
                new TimeSpan(0),
                new TimeSpan(0, 0, 10, 0));

        //上树列表
        private static readonly List<TreeInfo> treeList = new();

        #endregion

        #region 上树下树事件

        /// <summary>
        /// 成员上树
        /// </summary>
        /// <param name="sourceGroup">源群</param>
        /// <param name="uid">uid</param>
        /// <param name="upTime">上树时间</param>
        internal static void AddTreeMember(Group sourceGroup, long uid, DateTime upTime)
        {
            lock (treeList)
            {
                treeList.Add(new TreeInfo
                {
                    treeGroup  = sourceGroup,
                    uid        = uid,
                    updateTime = upTime
                });
            }
        }

        /// <summary>
        /// 成员下树
        /// </summary>
        /// <param name="uid">uid</param>
        internal static void DelTreeMember(long uid)
        {
            lock (treeList)
            {
                treeList.RemoveAll(member => member.uid == uid);
            }
        }

        #endregion

        #region 计时器事件

        /// <summary>
        /// 上树信息处理
        /// </summary>
        /// <param name="msgObject">null</param>
        private static void TreeTimerEvent(object msgObject)
        {
            lock (treeList)
            {
                Dictionary<Group, MessageBody> messageList = new();
                //生成上树提示信息
                foreach (var info in treeList.Where(info => !((DateTime.Now - info.updateTime).TotalSeconds < 10)))
                {
                    if (messageList.All(group => @group.Key != info.treeGroup))
                        messageList.Add(info.treeGroup, new MessageBody());
                    messageList[info.treeGroup].Add(CQCodes.CQAt(info.uid));
                    messageList[info.treeGroup]
                        .Add(CQCodes.CQText($"已经上树{(DateTime.Now - info.updateTime).TotalMinutes:F0}min了!"));
                    messageList[info.treeGroup].Add(CQCodes.CQText("\r\n"));
                }

                //发送上树提示信息
                foreach (var (key, value) in messageList)
                {
                    value.RemoveAt(value.Count - 1); //去掉最后的换行
                    key.SendGroupMessage(value);
                }
            }
        }

        #endregion
    }
}