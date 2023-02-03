using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Skadi.Entities.ConfigModule;
using Skadi.Interface;
using Sora.Attributes.Command;
using Sora.Entities.Segment;
using Sora.Enumeration.ApiType;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.LightLog;

namespace Skadi.Command;

[CommandSeries]
public class GroupWife
{
    private readonly List<long> _waitingList = new();

    [UsedImplicitly]
    // [SoraCommand(
    //     SourceType = SourceFlag.Group,
    //     CommandExpressions = new[] {"抽老婆"})]
    public async ValueTask RollWife(GroupMessageEventArgs eventArgs)
    {
        eventArgs.IsContinueEventChain = false;
        IStorageService storageService = SkadiApp.GetService<IStorageService>();
        UserConfig config = storageService.GetUserConfig(eventArgs.LoginUid);

        if (config is null || !config.ModuleSwitch.HaveFun)
            return;
        //检查是否已经在抽选
        if (_waitingList.Exists(user => user == eventArgs.Sender))
        {
            await eventArgs.Reply("你已经在抽老婆了真是屑呢");
            return;
        }

        var (apiStatus, memberList) = await eventArgs.SourceGroup.GetGroupMemberList();
        if (apiStatus.RetCode != ApiStatusType.Ok)
        {
            Log.Error("api错误", $"api return {apiStatus}");
            return;
        }

        //删除自身和发送者
        memberList.RemoveAll(i => i.UserId == eventArgs.Sender);
        memberList.RemoveAll(i => i.UserId == eventArgs.LoginUid);

        if (memberList.Count == 0)
        {
            await eventArgs.Reply("群里没人是你的老婆");
            return;
        }

        await
            eventArgs.Reply("10秒后我将at一位幸运群友成为你的老婆\r\n究竟是谁会这么幸运呢");
        _waitingList.Add(eventArgs.Sender);
        await Task.Delay(10000);
        _waitingList.RemoveAll(user => user == eventArgs.Sender);
        var rd = new Random();
        await eventArgs.Reply(SoraSegment.At(memberList[rd.Next(0, memberList.Count - 1)].UserId)
                              + "\r\n恭喜成为"
                              + SoraSegment.At(eventArgs.Sender)
                              + "的老婆 ~");
    }
}