using Native.Sdk.Cqp.EventArgs;
using SqlSugar;
using SuiseiBot.Code.Resource.TypeEnum;
using SuiseiBot.Code.SqliteTool;
using SuiseiBot.Code.Tool;
using SuiseiBot.Code.Tool.LogUtils;
using System.Collections.Generic;
using System.Linq;
using SuiseiBot.Code.Resource.TypeEnum.GuildBattleType;

namespace SuiseiBot.Code.DatabaseUtils.Helpers
{
    //TODO 指令需要检查是否存在公会
    //TODO 上树查树下树
    //TODO 余刀催刀
    //TODO 会战进度修正
    internal class GuildBattleMgrDBHelper
    {
        private long   GroupId { get; set; }
        private string DBPath  { get; set; }

        private string TableName { get; set; }

        public GuildBattleMgrDBHelper(object sender, CQGroupMessageEventArgs eventArgs)
        {
            GroupId   = eventArgs.FromGroup.Id;
            DBPath    = SugarUtils.GetDBPath(eventArgs.CQApi.GetLoginQQ().Id.ToString());
            TableName = $"{SugarTableUtils.GetTableName<GuildBattle>()}_{GroupId}";
        }

        /// <summary>
        /// 检查公会是否存在
        /// </summary>
        public bool GuildExists()
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            return dbClient.Queryable<GuildData>().Where(guild => guild.Gid == GroupId).Any();
        }

        /// <summary>
        /// 开始会战
        /// </summary>
        /// <returns>0：开始成功 | -1：上次仍未结束或已经开始</returns>
        public int StartBattle()
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            if (SugarUtils.TableExists<GuildBattle>(dbClient, TableName))
            {
                ConsoleLog.Error("会战管理数据库", "会战表已经存在，请检查是否未结束上次会战统计");
                return -1;
            }
            else
            {
                SugarUtils.CreateTable<GuildBattle>(dbClient, TableName);
                ConsoleLog.Info("会战管理数据库", "开始新的一期会战统计");
                return 0;
            }
        }

        /// <summary>
        /// 结束会战
        /// </summary>
        /// <returns>0：成功结束 | 1：还未开始会战</returns>
        public int EndBattle()
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            if (SugarUtils.TableExists<GuildBattle>(dbClient, TableName))
            {
                ConsoleLog.Warning("会战管理数据库", "结束一期会战，开始输出数据");
                //TODO: EXCEL导出公会战数据
                return 0;
            }
            else
            {
                ConsoleLog.Info("会战管理数据库", "会战表为空，请确认是否已经开始会战统计");
                return -1;
            }
        }

        /// <summary>
        /// 出刀命令
        /// </summary>
        /// <param name="uid">用户QQ号</param>
        /// <param name="dmg">当前刀伤害</param>
        /// <param name="attackType">当前刀类型（0=通常刀 1=尾刀 2=补偿刀 3=掉刀）</param>
        /// <param name="status">0：无异常 | 1：乱报尾刀警告 | 2：过度虐杀警告</param>
        /// <param name="lostAttack">是否掉刀</param>
        /// <returns>0：正常 | -1：该成员不存在 | -2：需要先下树 | -3：未开始出刀 | -6：会战未开始 | -99：数据库出错</returns>
        public int Attack(int uid, long dmg,out AttackType attackType, out int status)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            var statusData = dbClient.Queryable<MemberStatus>()
                               .Where(i => i.Uid == uid && i.Gid == GroupId)
                               .First();
            //检查是否查找到此成员
            if (statusData == null)
            {
                attackType = AttackType.Illeage;
                status     = 0;
                return -1;
            }
            attackType = AttackType.Normal;
            //成员状态检查
            switch (statusData.Flag)
            {
                //当前并未开始出刀，请先申请出刀=>返回
                case FlagType.IDLE:
                    attackType = AttackType.Illeage;
                    status     = 0;
                    return -3;
                //进入出刀判断
                case FlagType.EnGage:
                    break;
                //需要下树才能报刀
                case FlagType.OnTree:
                    attackType = AttackType.Illeage;
                    status     = 0;
                    return -2;
            }

            //当前BOSS数据
            GuildBattleStatus bossStatus =
                dbClient.Queryable<GuildBattleStatus>()
                        .InSingle(GroupId); //单主键查询
            //检查公会是否进入会战
            if (!bossStatus.InBattle)
            {
                status     = 0;
                attackType = AttackType.Illeage;
                return -6;
            }

            #region 出刀类型判断
            long CurrHP     = bossStatus.HP;
            long realDamage = dmg; //实际计量伤害

            //获取最后一刀的类型
            var lastAttack =
                dbClient.Queryable<GuildBattle>()
                        .OrderBy(attack => attack.Bid, OrderByType.Desc)
                        .Select(attack => new {Flag = attack.Attack, attack.Uid})
                        .First();
            //出刀类型判断
            //判断顺序: 补时刀->尾刀->通常刀
            if (lastAttack != null && uid == lastAttack.Uid && (AttackType)lastAttack.Flag == AttackType.Final) //补时
            {
                status = 0;
                attackType = dmg >= CurrHP
                    ? AttackType.Normal //当补时刀的伤害也超过了boss血量,判定为普通刀（你开挂！
                    : AttackType.Compensate;
            }
            else
            {
                status     = 0;
                attackType = AttackType.Normal; //普通刀
                //尾刀判断
                if (dmg >= CurrHP)
                {
                    status     = dmg > CurrHP ? 2 : 0;
                    realDamage = CurrHP;
                    attackType = AttackType.Final;
                }
                //掉刀判断
                if (dmg == 0)
                    attackType = AttackType.Offline;
            }
            #endregion
            
            //储存请求的时间
            long requestTime = statusData.Time;

            //插入一刀数据
            var insertData = new GuildBattle()
            {
                Uid    = uid,
                Time   = requestTime,
                BossID = GetCurrentBossID(bossStatus),
                Damage = realDamage,
                Attack = attackType
            };
            bool succInsert = dbClient.Insertable<GuildBattle>(insertData)
                                      .AS(TableName)
                                      .ExecuteCommand() > 0;
            bool succUpdateBoss = true;

            //如果是尾刀
            if (attackType == AttackType.Final)
            {
                //全部下树，出刀中取消出刀状态
                dbClient.Updateable(new MemberStatus() {Flag = 0})
                        .Where(i => i.Flag == FlagType.OnTree || i.Flag == FlagType.EnGage)
                        .UpdateColumns(i => new {i.Flag})
                        .ExecuteCommand();
                //切换boss
                int nextOrder = bossStatus.Order;
                int nextRound = bossStatus.Round;
                int nextPhase = bossStatus.BossPhase;
                if (bossStatus.Order != 5)
                {
                    //当前周目下一个boss
                    nextOrder++;
                }
                else
                {
                    //切周目
                    nextOrder = 1;
                    nextRound++;
                    nextPhase = GetNextRoundPhase(bossStatus);
                }

                var nextBossData = dbClient.Queryable<GuildBattleBoss>()
                                           .Where(i => i.ServerId == Server.CN
                                                    && i.Phase    == nextPhase
                                                    && i.Order    == nextOrder)
                                           .First();
                var updateBossData =
                    new GuildBattleStatus()
                    {
                        BossPhase = nextPhase,
                        Order     = nextOrder,
                        Round     = nextRound,
                        HP        = nextBossData.HP,
                        TotalHP   = nextBossData.HP
                    };
                succUpdateBoss = dbClient.Updateable<GuildBattleStatus>(updateBossData)
                                         .UpdateColumns(i => new {i.Order, i.HP, i.BossPhase, i.Round, i.TotalHP})
                                         .Where(i => i.Gid == GroupId)
                                         .ExecuteCommandHasChange();
            }

            //更新成员信息，报刀后变空闲
            var memberStatus = new MemberStatus()
            {
                Flag = 0,
                Info = "",
                Time = Utils.GetNowTimeStamp(),
            };
            bool succUpdate = dbClient.Updateable(memberStatus)
                                      .ExecuteCommandHasChange();
            return (succUpdateBoss && succUpdate && succInsert) ? 0 : -99;
        }

        /// <summary>
        /// SL命令
        /// </summary>
        /// <param name="uid">成员QQ号</param>
        /// <returns>0：正常 | -1：成员不存在 | -2：当日已用过SL | -3：当前并不在出刀状态中 | -99：数据库出错</returns>
        public int SL(int uid)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            MemberStatus currSL =
                dbClient.Queryable<MemberStatus>()
                        .Where(i => i.Uid == uid && i.Gid == GroupId)
                        .First();
            if (currSL == null) return -1;
            //检查SL记录的时间
            if (currSL.SL >= Utils.GetUpdateStamp())
            {
                return -2;
            }

            if (currSL.Flag != FlagType.EnGage)
            {
                return -3;
            }

            return dbClient
                   .Updateable(new MemberStatus{Flag = 0, SL = Utils.GetNowTimeStamp(),Time = Utils.GetNowTimeStamp()})
                   .UpdateColumns(i => new {i.Flag, i.SL})
                   .ExecuteCommandHasChange()
                ? 0
                : -99;
        }

        /// <summary>
        /// 撤销SL命令
        /// </summary>
        /// <param name="uid">成员QQ号</param>
        /// <returns>0：正常 | -1：成员不存在 | -2：今天未使用过SL | -99：数据库出错</returns>
        public int SLUndo(int uid)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            MemberStatus currSL =
                dbClient.Queryable<MemberStatus>()
                        .Where(i => i.Uid == uid && i.Gid == GroupId)
                        .First();
            if (currSL == null) return -1;
            if (currSL.SL == 0 || currSL.SL <= Utils.GetUpdateStamp())
            {
                return -2;
            }

            return dbClient.Updateable(new MemberStatus() {SL = 0})
                           .UpdateColumns(i => new {i.SL})
                           .ExecuteCommandHasChange()
                ? 0
                : -99;
        }

        /// <summary>
        /// 申请出刀
        /// </summary>
        /// <param name="uid">成员QQ号（请填写真实造成伤害的成员的QQ号）</param>
        /// <param name="flag">当前成员状态的Flag</param>
        /// <returns>0：正常 | -1：成员不存在 | -2：宁不是搁着树上爬吗，出个🔨的刀 | -3：已出满3刀 | -4：已经出刀，请不要重复出刀 | -5:补时刀前不允许出刀 | -99：数据库出错</returns>
        public int RequestAttack(int uid, out FlagType flag)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            MemberStatus member =
                dbClient.Queryable<MemberStatus>()
                        .Where(i => i.Uid == uid && i.Gid == GroupId)
                        .First();
            if (member == null)
            {
                flag = FlagType.UnknownMember;
                return -1;
            }
            //获取最后一刀的类型
            var lastAttack =
                dbClient.Queryable<GuildBattle>()
                        .AS(TableName)
                        .OrderBy(attack => attack.Bid, OrderByType.Desc)
                        .Select(attack => new {Flag = attack.Attack, attack.Uid})
                        .First();
            if (lastAttack != null && (AttackType) lastAttack.Flag == AttackType.Final && uid != lastAttack.Uid)
            {
                flag = 0;
                return -5;
            }

            //当前成员状态是否能出刀
            flag = member.Flag;
            switch (member.Flag)
            {
                //空闲可以出刀
                case FlagType.IDLE:
                    break;
                //重复出刀
                case FlagType.EnGage:
                    return -4;
                //挂树不允许出刀
                case FlagType.OnTree:
                    return -2;
            }

            //出刀数判断
            var AttackHistory =
                dbClient.Queryable<GuildBattle>()
                        .AS(TableName)
                        //今天5点之后出刀的
                        .Where(i => i.Uid == uid && i.Time > Utils.GetUpdateStamp())
                        .GroupBy(i => i.Uid)
                        //筛选出刀总数
                        .Select(i => new {id = i.Uid, times = SqlFunc.AggregateCount(i.Uid)}).ToList();
            //一天只能3刀
            if (AttackHistory.Any() && AttackHistory.FirstOrDefault()?.times >= 3)
            {
                return -3;
            }

            //修改出刀成员状态
            return dbClient.Updateable(new MemberStatus()
                           {
                               Flag = FlagType.EnGage,
                               Info = GetCurrentBossID(dbClient.Queryable<GuildBattleStatus>()
                                                               .InSingle(GroupId))
                           })
                           .UpdateColumns(i => new {i.Flag, i.Info})
                           .Where(i => i.Uid == uid && i.Gid == GroupId)
                           .ExecuteCommandHasChange()
                ? 0
                : -99;
        }

        /// <summary>
        /// 撤销出刀申请
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="flag"></param>
        /// <returns>0：正常 | -1：成员不存在 | -2：宁不是搁着树上爬吗，找管理下来罢 | -3：这不是没有出刀吗，你取消申请个锤子 | -98：不可能遇到的错误 | -99：数据库出错</returns>
        public int UndoRequest(int uid, out FlagType flag)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            MemberStatus member =
                dbClient.Queryable<MemberStatus>()
                        .Where(i => i.Uid == uid && i.Gid == GroupId)
                        .First();
            if (member == null)
            {
                flag = FlagType.UnknownMember;
                return -1;
            }
            flag = member.Flag;
            return member.Flag switch
            {
                FlagType.IDLE => -3,
                FlagType.EnGage => dbClient.Updateable(new MemberStatus() {Flag = FlagType.IDLE, Info = null})
                                           .UpdateColumns(i => new {i.Flag, i.Info})
                                           .Where(i => i.Uid == uid && i.Gid == GroupId)
                                           .ExecuteCommandHasChange()
                    ? 0
                    : -99,
                FlagType.OnTree => -2,
                //如果返回-98了，我完蛋了
                _ => -98
            };
        }

        /// <summary>
        /// 撤销出刀
        /// </summary>
        /// <returns>同删刀</returns>
        public int UndoAttack(long uid)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            //查找该成员的上一刀
            GuildBattle lastAttack =
                dbClient.Queryable<GuildBattle>()
                        .Where(member => member.Uid == uid)
                        .OrderBy(i => i.Bid, OrderByType.Desc)
                        .First();
            if (lastAttack == null) return -1;
            //删刀
            return DeleteAttack(lastAttack.Bid);
        }
        /// <summary>
        /// 删刀
        /// </summary>
        /// <param name="AttackId">出刀编号</param>
        /// <returns>0：正常 | -1：未找到该出刀编号 | -2：禁止删除非当前BOSS的刀 | -3：只能删通常刀 | -99：数据库出错</returns>
        public int DeleteAttack(int AttackId)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            GuildBattle attackInfo =
                dbClient.Queryable<GuildBattle>()
                        .AS(TableName)
                        .InSingle(AttackId);
            GuildBattleStatus bossStatus =
                dbClient.Queryable<GuildBattleStatus>()
                        .InSingle(GroupId);
            if (attackInfo == null) return -1;
            if (bossStatus.Round != Utils.GetFirstIntFromString(attackInfo.BossID))
            {
                return -2;
            }

            if (attackInfo.Attack != AttackType.Normal)
            {
                return -3;
            }
            bool succDelete = dbClient.Deleteable<GuildBattle>()
                                      .AS(TableName)
                                      .Where(i => i.Bid == AttackId)
                                      .ExecuteCommandHasChange();

            return succDelete ? 0 : -99;
        }

        /// <summary>
        /// 改刀（尾刀不能修改）
        /// </summary>
        /// <param name="AttackId">出刀编号</param>
        /// <param name="toValue">要修改为的目标伤害</param>
        /// <param name="needChangeBoss">是否切换BOSS</param>
        /// <returns>0：正常 | -1：未找到该出刀编号 | -2：禁止修改非当前BOSS的刀 | -3：禁止修改尾刀 | -99：数据库出错</returns>
        public int ModifyAttack(int AttackId, long toValue, out bool needChangeBoss)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            GuildBattle attackInfo =
                dbClient.Queryable<GuildBattle>()
                        .AS(TableName)
                        .InSingle(AttackId);
            GuildBattleStatus bossStatus =
                dbClient.Queryable<GuildBattleStatus>()
                        .InSingle(GroupId);
            //判断是否查找到这一刀
            if (attackInfo == null)
            {
                needChangeBoss = false;
                return -1;
            }
            //判断是否在当前boss
            if (bossStatus.Round != Utils.GetFirstIntFromString(attackInfo.BossID))
            {
                needChangeBoss = false;
                return -2;
            }
            //修改尾刀可能导致数据发生大范围回滚，禁止修改
            if (attackInfo.Attack == AttackType.Normal)
            {
                needChangeBoss = false;
                return -3;
            }

            long CurrHP = bossStatus.HP;

            long realDamage = toValue;
            //是否需要切换boss
            needChangeBoss = false;
            //修改后是否已经击杀Boss
            if (toValue >= CurrHP)
            {
                realDamage     = CurrHP;
                needChangeBoss = true;
            }

            //修改一刀数据
            bool succModify = dbClient.Updateable<GuildBattle>(
                                                               new GuildBattle()
                                                               {
                                                                   Damage = realDamage,
                                                                   Attack   = needChangeBoss ? AttackType.Final : AttackType.Normal
                                                               })
                                      .AS(TableName)
                                      .UpdateColumns(i => new {i.Damage, Flag = i.Attack})
                                      .Where(i => i.Bid == AttackId)
                                      .ExecuteCommandHasChange();

            bool succUpdateBoss = true;
            //如果已经击杀
            if (needChangeBoss)
            {
                //全部下树，出刀中取消出刀状态
                dbClient.Updateable(new MemberStatus() {Flag = 0})
                        .Where(i => i.Flag == FlagType.OnTree || i.Flag == FlagType.EnGage)
                        .UpdateColumns(i => new {i.Flag})
                        .ExecuteCommand();
                //切换boss
                int nextOrder = bossStatus.Order;
                int nextRound = bossStatus.Round;
                int nextPhase = bossStatus.BossPhase;
                if (bossStatus.Order != 5)
                {
                    //当前周目下一个怪
                    nextOrder++;
                }
                else
                {
                    //切周目
                    nextOrder = 1;
                    nextRound++;
                    nextPhase = GetNextRoundPhase(bossStatus);
                }

                var nextBossData = dbClient.Queryable<GuildBattleBoss>()
                                           .Where(i => i.ServerId == Server.CN
                                                    && i.Phase    == nextPhase
                                                    && i.Order    == nextOrder)
                                           .First();
                var updateBossData =
                    new GuildBattleStatus()
                    {
                        BossPhase = nextPhase,
                        Order     = nextOrder,
                        Round     = nextRound,
                        HP        = nextBossData.HP,
                        TotalHP   = nextBossData.HP
                    };
                succUpdateBoss = dbClient.Updateable<GuildBattleStatus>(updateBossData)
                                         .UpdateColumns(i => new {i.Order, i.HP, i.BossPhase, i.Round, i.TotalHP})
                                         .Where(i => i.Gid == GroupId)
                                         .ExecuteCommandHasChange();
            }


            return (succModify && succUpdateBoss) ? 0 : -99;
        }

        /// <summary>
        /// 显示当前进度（请只在聊天判断中使用，本类中请自行查库，避免不必要的数据库链接）
        /// </summary>
        /// <returns>返回当前进度对象</returns>
        public GuildBattleStatus ShowProgress()
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            GuildBattleStatus bossStatus =
                dbClient.Queryable<GuildBattleStatus>()
                        .InSingle(GroupId);
            return bossStatus;
        }

        /// <summary>
        /// 获取今天的出刀列表
        /// </summary>
        /// <returns>出刀List</returns>
        public List<GuildBattle> GetTodayAttacks()
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            return dbClient.Queryable<GuildBattle>()
                           .Where(i => i.Time > Utils.GetUpdateStamp())
                           .OrderBy(i => i.Bid)
                           .ToList();
        }

        /// <summary>	
        /// 获取当前公会所在boss的代号
        /// <param name="status">当前会战进度</param>
        /// </summary>	
        public string GetCurrentBossID(GuildBattleStatus status)
        {
            const string BOSS_NUM = "abcde";
            return $"{status.Round}{BOSS_NUM[status.Order]}";
        }

        /// <summary>
        /// 获取下一个周目的boss对应阶段
        /// </summary>
        /// /// <param name="status">当前会战进度</param>
        /// <returns>下一周目boss的阶段值</returns>
        public int GetNextRoundPhase(GuildBattleStatus status)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            //当前所处区服
            Server server =
                dbClient.Queryable<GuildData>()
                        .Where(guild => guild.Gid == GroupId)
                        .Select(guild => guild.ServerArea)
                        .First();
            //boss的最大阶段
            int maxPhase =
                dbClient.Queryable<GuildBattleBoss>()
                        .Where(boss => boss.Round == -1)
                        .Select(boss => boss.Phase)
                        .First();
            //已到最后一个阶段
            if (status.BossPhase == maxPhase) return maxPhase;
            //未达到最后一个阶段
            int nextRound = status.Round + 1;
            int nextPhase = status.BossPhase;
            //获取除了最后一阶段的所有round值，在获取到相应阶段后终止循环
            for (int i = 1; i < maxPhase; i++)
            {
                nextRound -= dbClient.Queryable<GuildBattleBoss>()
                                     .Where(boss => boss.Phase == i && boss.ServerId == server)
                                     .Select(boss => boss.Round)
                                     .First();
                if (nextRound <= 0) //得到下一个周目的阶段终止循环
                {
                    nextPhase = i;
                    break;
                }
            }

            if (nextRound > 0) nextPhase = maxPhase;
            return nextPhase;
        }
    }
}