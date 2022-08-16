using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Sora.Attributes.Command;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;

#pragma warning disable CS8509
#pragma warning disable CS8524

namespace Skadi.Command;

/// <summary>
/// 群空调
/// </summary>
// TODO [CommandGroup(GroupName = "ac")]
public class AirCondition
{
    private ConcurrentDictionary<long, AirConditionCof> _conditions = new();

    private enum Mode
    {
        HEAT,
        COOL,
        AUTO
    }

    private record AirConditionCof
    {
        public bool Enable;
        public int  Target  = 25;
        public int  Current = 25;
        public int  Speed   = 1;
        public Mode Mode    = Mode.AUTO;
    }

    [UsedImplicitly]
    [SoraCommand(
        SourceType = SourceFlag.Group,
        CommandExpressions = new[] {"开空调"})]
    public async ValueTask OpenCondition(GroupMessageEventArgs eventArgs)
    {
        eventArgs.IsContinueEventChain = false;
        long gid = eventArgs.SourceGroup;
        if (_conditions.ContainsKey(gid) && _conditions[gid].Enable)
        {
            await eventArgs.Reply("空调开着呢！");
            return;
        }

        if (!_conditions.ContainsKey(gid))
            _conditions.TryAdd(gid, new AirConditionCof());
        _conditions[gid].Enable = true;

        await eventArgs.Reply("哔~");
    }

    [UsedImplicitly]
    [SoraCommand(
        SourceType = SourceFlag.Group,
        CommandExpressions = new[] {"关空调"})]
    public async ValueTask CloseCondition(GroupMessageEventArgs eventArgs)
    {
        eventArgs.IsContinueEventChain = false;
        long gid = eventArgs.SourceGroup;
        if (!_conditions.ContainsKey(gid) || !_conditions[gid].Enable)
        {
            await eventArgs.Reply("空调根本就没开！");
            return;
        }

        _conditions[gid].Enable = false;

        await eventArgs.Reply("💤哔~");
    }

    [UsedImplicitly]
    [SoraCommand(
        SourceType = SourceFlag.Group,
        CommandExpressions = new[] {"^风速[低中高]$"},
        MatchType = MatchType.Regex)]
    public async ValueTask SetSpeed(GroupMessageEventArgs eventArgs)
    {
        eventArgs.IsContinueEventChain = false;
        long gid = eventArgs.SourceGroup;
        if (!_conditions.ContainsKey(gid) || !_conditions[gid].Enable)
        {
            await eventArgs.Reply("空调根本就没开！");
            return;
        }
    }

    [UsedImplicitly]
    [SoraCommand(
        SourceType = SourceFlag.Group,
        CommandExpressions = new[] {"看看空调", "空调状态"})]
    public async ValueTask ConStatus(GroupMessageEventArgs eventArgs)
    {
        eventArgs.IsContinueEventChain = false;
        long gid = eventArgs.SourceGroup;
        if (!_conditions.ContainsKey(gid) || !_conditions[gid].Enable)
        {
            await eventArgs.Reply("空调没开！");
            return;
        }

        AirConditionCof con = _conditions[gid];

        StringBuilder re = new StringBuilder();
        re.AppendLine(con.Mode switch
        {
            Mode.HEAT => "☀制热",
            Mode.COOL => "❄制冷",
            Mode.AUTO => "🌡自动"
        });
        re.AppendLine(con.Speed switch
        {
            1 => "💨",
            2 => "💨💨",
            3 => "💨💨💨"
        });
        re.AppendLine($"当前温度 {con.Current}");
        re.Append($"目标温度 {con.Target}");
        await eventArgs.Reply(re.ToString());
    }
}