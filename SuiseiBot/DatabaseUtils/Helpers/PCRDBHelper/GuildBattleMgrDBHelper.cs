using System;
using System.Collections.Generic;
using System.Linq;
using Native.Sdk.Cqp.EventArgs;
using SqlSugar;
using SuiseiBot.Code.Resource.TypeEnum;
using SuiseiBot.Code.Resource.TypeEnum.GuildBattleType;
using SuiseiBot.Code.SqliteTool;
using SuiseiBot.Code.Tool;
using SuiseiBot.Code.Tool.LogUtils;

namespace SuiseiBot.Code.DatabaseUtils.Helpers.PCRDBHelper
{
    internal class GuildBattleMgrDBHelper : GuildDBHelper
    {
        #region 属性
        private string BattleTableName { get; set; }
        #endregion

        #region 构造函数
        public GuildBattleMgrDBHelper(CQGroupMessageEventArgs eventArgs)
        {
            GuildEventArgs  = eventArgs;
            DBPath          = SugarUtils.GetDBPath(eventArgs.CQApi.GetLoginQQ().Id.ToString());
            BattleTableName = $"{SugarTableUtils.GetTableName<GuildBattle>()}_{GuildEventArgs.FromGroup.Id}";
        }
        #endregion

        #region 指令
        /// <summary>
        /// 开始会战
        /// </summary>
        /// <returns>0：开始成功 | -1：上次仍未结束或已经开始 | -99:数据库错误</returns>
        public int StartBattle()
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                if (SugarUtils.TableExists<GuildBattle>(dbClient, BattleTableName))
                {
                    ConsoleLog.Error("会战管理数据库", "会战表已经存在，请检查是否未结束上次会战统计");
                    return -1;
                }
                else
                {
                    SugarUtils.CreateTable<GuildBattle>(dbClient, BattleTableName);
                    ConsoleLog.Info("会战管理数据库", "开始新的一期会战统计");
                    return dbClient.Updateable(new GuildInfo {InBattle = true})
                                   .Where(guild => guild.Gid == GuildEventArgs.FromGroup.Id)
                                   .UpdateColumns(i => new {i.InBattle})
                                   .ExecuteCommandHasChange()
                        ? 0
                        : -99;
                }
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database error",ConsoleLog.ErrorLogBuilder(e));
                return -99;
            }
        }

        /// <summary>
        /// 结束会战
        /// </summary>
        /// <returns>0：成功结束 | 1：还未开始会战 | -99:数据库错误</returns>
        public int EndBattle()
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                if (SugarUtils.TableExists<GuildBattle>(dbClient, BattleTableName))
                {
                    ConsoleLog.Warning("会战管理数据库", "结束一期会战，开始输出数据");
                    SugarUtils.DeletTable<GuildBattle>(dbClient, BattleTableName);
                    return dbClient.Updateable(new GuildInfo {InBattle = false})
                                   .Where(guild => guild.Gid == GuildEventArgs.FromGroup.Id)
                                   .UpdateColumns(i => new {i.InBattle})
                                   .ExecuteCommandHasChange()
                        ? 0
                        : -99;
                }
                else
                {
                    ConsoleLog.Info("会战管理数据库", "会战表为空，请确认是否已经开始会战统计");
                    return 1;
                }
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database error",ConsoleLog.ErrorLogBuilder(e));
                return -99;
            }
        }

        /// <summary>
        /// SL命令
        /// </summary>
        /// <param name="uid">成员QQ号</param>
        /// <returns>0：正常 | -1：成员不存在 | -2：当日已用过SL | -3：当前并不在出刀状态中 | -99：数据库出错</returns>
        public int SL(long uid)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            MemberInfo currSL =
                dbClient.Queryable<MemberInfo>()
                        .Where(i => i.Uid == uid && i.Gid == GuildEventArgs.FromGroup.Id)
                        .First();
            if (currSL == null) return -1;
            //检查SL记录的时间
            if (currSL.SL >= Utils.GetUpdateStamp())
            {
                return -2;
            }
            //检查成员状态
            if (currSL.Flag != FlagType.EnGage)
            {
                return -3;
            }

            return dbClient
                   .Updateable(new MemberInfo
                                   {Flag = 0, SL = Utils.GetNowTimeStamp(), Time = Utils.GetNowTimeStamp()})
                   .UpdateColumns(i => new {i.Flag, i.SL})
                   .Where(i => i.Gid == GuildEventArgs.FromGroup.Id && i.Uid == uid)
                   .ExecuteCommandHasChange()
                ? 0
                : -99;
        }

        /// <summary>
        /// 撤销SL命令
        /// </summary>
        /// <param name="uid">成员QQ号</param>
        /// <returns>0：正常 | -1：成员不存在 | -2：今天未使用过SL | -99：数据库出错</returns>
        public int SLUndo(long uid)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            MemberInfo currSL =
                dbClient.Queryable<MemberInfo>()
                        .Where(i => i.Uid == uid && i.Gid == GuildEventArgs.FromGroup.Id)
                        .First();
            if (currSL == null) return -1;
            if (currSL.SL == 0 || currSL.SL < Utils.GetUpdateStamp())
            {
                return -2;
            }

            return dbClient.Updateable(new MemberInfo{SL = 0})
                           .UpdateColumns(i => new {i.SL})
                           .Where(i => i.Uid == uid && i.Gid == GuildEventArgs.FromGroup.Id)
                           .ExecuteCommandHasChange()
                ? 0
                : -99;
        }

        /// <summary>
        /// 撤销出刀
        /// </summary>
        /// <returns>同删刀，但 -4：上一刀为空，不能撤销</returns>
        public int UndoAttack(long uid)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            //查找该成员的上一刀
            GuildBattle lastAttack =
                dbClient.Queryable<GuildBattle>()
                        .AS(BattleTableName)
                        .Where(member => member.Uid == uid)
                        .OrderBy(i => i.Aid, OrderByType.Desc)
                        .First();
            if (lastAttack == null) return -4;
            //删刀
            return DeleteAttack(lastAttack.Aid);
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
                        .AS(BattleTableName)
                        .InSingle(AttackId);
            if (attackInfo == null) return -1;

            GuildInfo guildInfo =
                dbClient.Queryable<GuildInfo>()
                        .InSingle(GuildEventArgs.FromGroup.Id);
            
            if (guildInfo.Round != attackInfo.Round && guildInfo.Order != attackInfo.Order)
            {
                return -2;
            }

            if (attackInfo.Attack != AttackType.Normal)
            {
                return -3;
            }
            bool succDelete = dbClient.Deleteable<GuildBattle>()
                                      .AS(BattleTableName)
                                      .Where(i => i.Aid == AttackId)
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
                        .AS(BattleTableName)
                        .InSingle(AttackId);
            GuildInfo guildInfo =
                dbClient.Queryable<GuildInfo>()
                        .InSingle(GuildEventArgs.FromGroup.Id);
            //判断是否查找到这一刀
            if (attackInfo == null)
            {
                needChangeBoss = false;
                return -1;
            }
            //判断是否在当前boss
            if (guildInfo.Round != attackInfo.Round && guildInfo.Order != attackInfo.Order)
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

            long CurrHP = guildInfo.HP;

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
            bool succModify = dbClient.Updateable(
                                                  new GuildBattle()
                                                  {
                                                      Damage = realDamage,
                                                      Attack = needChangeBoss ? AttackType.Final : AttackType.Normal
                                                  })
                                      .AS(BattleTableName)
                                      .UpdateColumns(i => new {i.Damage, Flag = i.Attack})
                                      .Where(i => i.Aid == AttackId)
                                      .ExecuteCommandHasChange();

            bool succUpdateBoss = true;
            //如果已经击杀
            if (needChangeBoss)
            {
                //全部下树，出刀中取消出刀状态
                dbClient.Updateable(new MemberInfo{Flag = FlagType.IDLE})
                        .Where(i => i.Flag == FlagType.OnTree || i.Flag == FlagType.EnGage)
                        .UpdateColumns(i => new {i.Flag})
                        .ExecuteCommand();
                //切换boss
                int nextOrder = guildInfo.Order;
                int nextRound = guildInfo.Round;
                int nextPhase = guildInfo.BossPhase;
                if (guildInfo.Order != 5)
                {
                    //当前周目下一个怪
                    nextOrder++;
                }
                else
                {
                    //切周目
                    nextOrder = 1;
                    nextRound++;
                    nextPhase = GetNextRoundPhase(guildInfo);
                }

                //查找下一个boss的信息
                var nextBossData = dbClient.Queryable<GuildBattleBoss>()
                                           .Where(i => i.ServerId == guildInfo.ServerId
                                                    && i.Phase    == nextPhase
                                                    && i.Order    == nextOrder)
                                           .First();
                var updateBossData =
                    new GuildInfo()
                    {
                        BossPhase = nextPhase,
                        Order     = nextOrder,
                        Round     = nextRound,
                        HP        = nextBossData.HP,
                        TotalHP   = nextBossData.HP
                    };
                succUpdateBoss = dbClient.Updateable(updateBossData)
                                         .UpdateColumns(i => new {i.Order, i.HP, i.BossPhase, i.Round, i.TotalHP})
                                         .Where(i => i.Gid == GuildEventArgs.FromGroup.Id)
                                         .ExecuteCommandHasChange();
            }


            return (succModify && succUpdateBoss) ? 0 : -99;
        }

        /// <summary>
        /// 显示当前进度（请只在聊天判断中使用，本类中请自行查库，避免不必要的数据库链接）
        /// </summary>
        /// <returns>返回当前进度对象</returns>
        public GuildInfo ShowProgress()
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            GuildInfo bossStatus =
                dbClient.Queryable<GuildInfo>()
                        .InSingle(GuildEventArgs.FromGroup.Id);
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
                           .AS(BattleTableName)
                           .Where(i => i.Time >= Utils.GetUpdateStamp())
                           .OrderBy(i => i.Aid)
                           .ToList();
        }

        /// <summary>
        /// 上树
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="flag"></param>
        /// <returns>0:正常 | -1:刀还没出呢 | -1:你不是在树上了吗,爪巴 | -99:数据库错误</returns>
        public int ClimbTree(long uid, out FlagType flag)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            MemberInfo member =
                dbClient.Queryable<MemberInfo>()
                        .Where(i => i.Uid == uid && i.Gid == GuildEventArgs.FromGroup.Id)
                        .First();
            if (member == null)
            {
                flag = FlagType.UnknownMember;
                return -1;
            }

            //当前成员状态是否能上树
            flag = member.Flag;
            switch (flag)
            {
                case FlagType.EnGage:
                    break;
                //已经上树              并没有在出刀
                case FlagType.OnTree:  case FlagType.IDLE:
                    return -1;
            }
            //修改状态
            member.Flag = FlagType.OnTree;
            member.Info = Utils.GetNowTimeStamp().ToString();

            return dbClient.Updateable(member).ExecuteCommandHasChange()
                ? 0
                : -99;
        }

        /// <summary>
        /// 下树
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="flag"></param>
        /// <returns>0:正常 | -1:你不是在树下面吗？| -99:数据库错误</returns>
        public int LeaveTree(long uid, out FlagType flag)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            MemberInfo member =
                dbClient.Queryable<MemberInfo>()
                        .Where(i => i.Uid == uid && i.Gid == GuildEventArgs.FromGroup.Id)
                        .First();
            if (member == null)
            {
                flag = FlagType.UnknownMember;
                return -1;
            }

            //当前成员状态是否能下树
            flag = member.Flag;
            switch (flag)
            {
                case FlagType.OnTree:
                    break;
                //正在出刀             并没有在出刀
                case FlagType.EnGage: case FlagType.IDLE:
                    return -1;
            }
            //修改状态
            member.Flag = FlagType.IDLE;
            member.Info = null;

            return dbClient.Updateable(member).ExecuteCommandHasChange()
                ? 0
                : -99;
        }

        /// <summary>
        /// 查树
        /// </summary>
        /// <returns>挂树表</returns>
        public List<long> CheckTree()
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            return dbClient.Queryable<MemberInfo>()
                           .Where(member => member.Gid == GuildEventArgs.FromGroup.Id && member.Flag == FlagType.OnTree)
                           .Select(member => member.Uid)
                           .ToList();
        }

        /// <summary>
        /// 查询今日余刀
        /// 用于查刀和催刀
        /// </summary>
        /// <returns>余刀表</returns>
        public Dictionary<long, int> CheckTodayAttacks()
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            var attackTimeList = dbClient.Queryable<GuildBattle>()
                            .AS(BattleTableName)
                            .Where(attack => attack.Time > Utils.GetUpdateStamp())
                            .GroupBy(member => member.Uid)
                            .Select(member => new
                            {
                                member.Uid,
                                times = SqlFunc.AggregateCount(member.Uid)
                            })
                            .ToList();
            return attackTimeList
                   .Where(member => member.times < 3)
                   .ToDictionary(member => member.Uid,
                                 member => member.times);
        }

        /// <summary>
        /// 修改会战进度（无视当前出刀进度和历史出刀）
        /// </summary>
        /// <returns>0:正常 | -1:设定的HP值超出上限 | -99:数据库错误</returns>
        public int ModifyProgress(int round,int bossOrder,long hp)
        {
            //参数检查
            if (round < 0 || bossOrder < 0 || hp < 0 || bossOrder > 5)
            {
                throw new ArgumentOutOfRangeException();
            }

            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);

            //查找公会服务器信息
            Server serverId = dbClient.Queryable<GuildInfo>()
                                      .Where(guild => guild.Gid == GuildEventArgs.FromGroup.Id)
                                      .Select(guild => guild.ServerId)
                                      .First();

            int phase = GetRoundPhase(round);
            long totalHp = dbClient.Queryable<GuildBattleBoss>()
                                  .Where(boss => boss.ServerId == serverId && boss.Order == bossOrder &&
                                                 boss.Phase    == phase)
                                  .Select(boss => boss.HP)
                                  .First();
            if (hp > totalHp) return -1;
            
            //更新数据
            return dbClient.Updateable(new GuildInfo
                           {
                               HP        = hp,
                               TotalHP   = totalHp,
                               Round     = round,
                               Order     = bossOrder,
                               BossPhase = phase,
                           })
                           .Where(guild => guild.Gid == GuildEventArgs.FromGroup.Id)
                           .UpdateColumns(info => new {info.HP, info.TotalHP, info.Round, info.Order, info.BossPhase})
                           .ExecuteCommandHasChange()
                ? 0
                : -99;
        }
        #endregion

        #region 公有方法
        /// <summary>
        /// 检查是否已进入会战
        /// </summary>
        /// <returns>
        /// <para><see langword="1"/> 已经入</para>
        /// <para><see langword="0"/> 未进入</para>
        /// <para><see langword="-1"/> 数据库错误</para>
        /// </returns>
        public int CheckInBattle()
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                return dbClient.Queryable<GuildInfo>().InSingle(GuildEventArgs.FromGroup.Id).InBattle ? 1 : 0;
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database error",ConsoleLog.ErrorLogBuilder(e));
                return -1;
            }
        }

        /// <summary>
        /// 获取最后一次出刀的类型和执行者
        /// </summary>
        /// <param name="uid">uid</param>
        /// <param name="attackType">出刀类型</param>
        /// <returns>
        /// <para>执行者UID</para>
        /// <para><see langword="0"/> 没有出刀记录</para>
        /// <para><see langword="-1"/> 数据库错误</para>
        /// </returns>
        public long GetLastAttack(long uid, out AttackType attackType)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                //获取最后一刀的类型出刀者UID
                var lastAttack =
                    dbClient.Queryable<GuildBattle>()
                            .AS(BattleTableName)
                            .Where(member => member.Uid == uid)
                            .OrderBy(attack => attack.Aid, OrderByType.Desc)
                            .Select(attack => new {lastType = attack.Attack, attack.Uid})
                            .First();
                attackType = lastAttack?.lastType ?? AttackType.Illeage;
                return lastAttack?.Uid ?? 0;
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database error",ConsoleLog.ErrorLogBuilder(e));
                attackType = AttackType.Illeage;
                return -1;
            }
        }

        /// <summary>
        /// 获取今日的出刀数量
        /// </summary>
        /// <param name="uid">执行者UID</param>
        /// <returns>
        /// <para>今日出刀数</para>
        /// <para><see langword="-1"/> 数据库错误</para>
        /// </returns>
        public int GetTodayAttackCount(long uid)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                //出刀数
                return dbClient.Queryable<GuildBattle>()
                               .AS(BattleTableName)
                               //今天5点之后出刀的
                               .Where(i => i.Uid    == uid && i.Time >= Utils.GetUpdateStamp() &&
                                           i.Attack != AttackType.Compensate)
                               //筛选出刀总数
                               .Count();
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database error",ConsoleLog.ErrorLogBuilder(e));
                return -1;
            }
        }

        /// <summary>
        /// 向数据库插入一刀数据
        /// </summary>
        /// <param name="uid">出刀者UID</param>
        /// <param name="guildInfo">公会信息</param>
        /// <param name="dmg">伤害</param>
        /// <param name="attackType">出刀类型</param>
        /// <returns>
        /// 本次出刀刀号
        /// </returns>
        public int NewAttack(long uid,GuildInfo guildInfo,long dmg,AttackType attackType)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                //插入一刀数据
                var insertData = new GuildBattle()
                {
                    Uid    = uid,
                    Time   = Utils.GetNowTimeStamp(),
                    Order  = guildInfo.Order,
                    Round  = guildInfo.Round,
                    Damage = dmg,
                    Attack = attackType
                };
                return dbClient.Insertable(insertData)
                               .AS(BattleTableName)
                               .ExecuteReturnIdentity();
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database error",ConsoleLog.ErrorLogBuilder(e));
                return -1;
            }
        }

        /// <summary>
        /// 成员进入空闲
        /// </summary>
        /// <param name="uid">uid</param>
        /// <returns>
        /// <para><see langword="true"/> 写入成功</para>
        /// <para><see langword="false"/> 数据库错误</para>
        /// </returns>
        public bool MemberIDLE(long uid)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                return dbClient.Updateable(new MemberInfo {Flag = FlagType.IDLE, Info = null})
                               .UpdateColumns(i => new {i.Flag, i.Info})
                               .Where(i => i.Uid == uid && i.Gid == GuildEventArgs.FromGroup.Id)
                               .ExecuteCommandHasChange();
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database error",ConsoleLog.ErrorLogBuilder(e));
                return false;
            }
        }

        /// <summary>
        /// 成员进入实战
        /// </summary>
        /// <param name="uid">uid</param>
        /// <returns>
        /// <para><see langword="true"/> 写入成功</para>
        /// <para><see langword="false"/> 数据库错误</para>
        /// </returns>
        public bool MemberEngage(long uid)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                var bossCode = dbClient.Queryable<GuildInfo>()
                                       .Where(guild => guild.Gid == GuildEventArgs.FromGroup.Id)
                                       .Select(boss => new {boss.Round, boss.Order})
                                       .First();

                return dbClient.Updateable(new MemberInfo
                               {
                                   Flag = FlagType.EnGage,
                                   Info = $"{bossCode.Round}:{bossCode.Order}"
                               })
                               .UpdateColumns(i => new {i.Flag, i.Info})
                               .Where(i => i.Uid == uid && i.Gid == GuildEventArgs.FromGroup.Id)
                               .ExecuteCommandHasChange();
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database error",ConsoleLog.ErrorLogBuilder(e));
                return false;
            }
        }

        /// <summary>
        /// 修改当前boss血量
        /// </summary>
        /// <param name="guildInfo">公会信息</param>
        /// <param name="curBossHP">更新的HP值</param>
        /// <returns>
        /// <para><see langword="true"/> 写入成功</para>
        /// <para><see langword="false"/> 数据库错误</para>
        /// </returns>
        public bool ModifyBossHP(GuildInfo guildInfo, long curBossHP)
        {
            try
            {
                if(curBossHP>guildInfo.TotalHP) throw new ArgumentOutOfRangeException(nameof(curBossHP));
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                guildInfo.HP = curBossHP;
                return dbClient.Updateable(guildInfo)
                               .UpdateColumns(i => new {i.HP})
                               .ExecuteCommandHasChange();
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database error",ConsoleLog.ErrorLogBuilder(e));
                return false;
            }
        }

        /// <summary>
        /// 进入下一个boss
        /// </summary>
        /// <param name="guildInfo">公会信息</param>
        /// <returns>
        /// <para><see langword="true"/> 写入成功</para>
        /// <para><see langword="false"/> 数据库错误</para>
        /// </returns>
        public bool GotoNextBoss(GuildInfo guildInfo)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                //获取下一个boss的信息
                GuildBattleBoss nextBossData = dbClient.Queryable<GuildBattleBoss>()
                                                       .Where(i => i.ServerId == guildInfo.ServerId
                                                                && i.Phase    == guildInfo.BossPhase
                                                                && i.Order    == guildInfo.Order + 1)
                                                       .First();
                GuildInfo updateBossData =
                    new GuildInfo()
                    {
                        Order   = guildInfo.Order + 1,
                        HP      = nextBossData.HP,
                        TotalHP = nextBossData.HP
                    };
                return dbClient.Updateable(updateBossData)
                               .UpdateColumns(i => new {i.Order, i.HP, i.TotalHP})
                               .Where(i => i.Gid == GuildEventArgs.FromGroup.Id)
                               .ExecuteCommandHasChange();
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database error",ConsoleLog.ErrorLogBuilder(e));
                return false;
            }
        }

        /// <summary>
        /// 清空树上成员
        /// </summary>
        /// <param name="guildInfo">公会信息</param>
        /// <returns>
        /// <para><see langword="true"/> 写入成功</para>
        /// <para><see langword="false"/> 数据库错误</para>
        /// </returns>
        public bool CleanTree(GuildInfo guildInfo)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                dbClient.Updateable(new MemberInfo {Flag = FlagType.IDLE, Info = null})
                        .Where(i => i.Flag == FlagType.OnTree || i.Flag == FlagType.EnGage)
                        .UpdateColumns(i => new {i.Flag, i.Info})
                        .ExecuteCommand();
                return true;
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database error",ConsoleLog.ErrorLogBuilder(e));
                return false;
            }
        }

        /// <summary>
        /// 进入下一个周目
        /// </summary>
        /// <param name="guildInfo">公会信息</param>
        /// <returns>
        /// <para><see langword="true"/> 写入成功</para>
        /// <para><see langword="false"/> 数据库错误</para>
        /// </returns>
        public bool GotoNextRound(GuildInfo guildInfo)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                //获取下一个周目boss的信息
                GuildBattleBoss nextBossData = dbClient.Queryable<GuildBattleBoss>()
                                                       .Where(i => i.ServerId == guildInfo.ServerId
                                                                && i.Phase    == GetNextRoundPhase(guildInfo)
                                                                && i.Order    == 1)
                                                       .First();
                GuildInfo updateBossData =
                    new GuildInfo()
                    {
                        BossPhase = nextBossData.Phase,
                        Order     = 1,
                        Round     = guildInfo.Round + 1,
                        HP        = nextBossData.HP,
                        TotalHP   = nextBossData.HP
                    };
                return dbClient.Updateable(updateBossData)
                               .UpdateColumns(i => new {i.Order, i.HP, i.BossPhase, i.Round, i.TotalHP})
                               .Where(i => i.Gid == GuildEventArgs.FromGroup.Id)
                               .ExecuteCommandHasChange();
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database error",ConsoleLog.ErrorLogBuilder(e));
                return false;
            }
        }

        /// <summary>
        /// 更新成员状态
        /// </summary>
        /// <param name="gid">公会群号</param>
        /// <param name="uid">成员UID</param>
        /// <param name="newFlag">新的状态</param>
        /// <param name="newInfo">新的消息</param>
        /// <returns>
        /// <para><see langword="true"/> 写入成功</para>
        /// <para><see langword="false"/> 数据库错误</para>
        /// </returns>
        public bool UpdateMemberStatus(long uid, FlagType newFlag, string newInfo)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                //更新成员信息
                MemberInfo memberInfo = new MemberInfo()
                {
                    Flag = newFlag,
                    Info = newInfo,
                    Time = Utils.GetNowTimeStamp(),
                };
                return dbClient.Updateable(memberInfo)
                               .UpdateColumns(i => new{i.Flag,i.Info,i.Time})
                               .Where(i=>i.Gid == GuildEventArgs.FromGroup.Id && i.Uid == uid)
                               .ExecuteCommandHasChange();
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database error",ConsoleLog.ErrorLogBuilder(e));
                return false;
            }
        }



        #endregion

        #region 私有方法
        /// <summary>
        /// 获取下一个周目的boss对应阶段
        /// </summary>
        /// /// <param name="guildInfo">当前会战进度</param>
        /// <returns>下一周目boss的阶段值</returns>
        private int GetNextRoundPhase(GuildInfo guildInfo)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            //boss的最大阶段
            int maxPhase =
                dbClient.Queryable<GuildBattleBoss>()
                        .Where(boss => boss.Round == -1)
                        .Select(boss => boss.Phase)
                        .First();
            //已到最后一个阶段
            if (guildInfo.BossPhase == maxPhase) return maxPhase;
            //未达到最后一个阶段
            int nextRound = guildInfo.Round + 1;
            int nextPhase = guildInfo.BossPhase;
            //获取除了最后一阶段的所有round值，在获取到相应阶段后终止循环
            for (int i = 1; i < maxPhase; i++)
            {
                nextRound -= dbClient.Queryable<GuildBattleBoss>()
                                     .Where(boss => boss.Phase == i && boss.ServerId == guildInfo.ServerId)
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

        /// <summary>
        /// 获取指定周目的boss对应阶段
        /// </summary>
        /// <param name="Round">指定周目</param>
        /// <returns>下一周目boss的阶段值</returns>
        private int GetRoundPhase(int Round)
        {
            //检查参数合法性
            if(Round <= 0) throw new ArgumentOutOfRangeException(nameof(Round));

            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            //当前所处区服
            Server server =
                dbClient.Queryable<GuildInfo>()
                        .Where(guild => guild.Gid == GuildEventArgs.FromGroup.Id)
                        .Select(guild => guild.ServerId)
                        .First();
            //boss的最大阶段
            int maxPhase =
                dbClient.Queryable<GuildBattleBoss>()
                        .Where(boss => boss.Round == -1)
                        .Select(boss => boss.Phase)
                        .First();
            //未达到最后一个阶段
            int nextPhase = 0;
            //获取除了最后一阶段的所有round值，在获取到相应阶段后终止循环
            for (int i = 1; i < maxPhase; i++)
            {
                Round -= dbClient.Queryable<GuildBattleBoss>()
                                 .Where(boss => boss.Phase == i && boss.ServerId == server)
                                 .Select(boss => boss.Round)
                                 .First();
                if (Round <= 0) //得到下一个周目的阶段终止循环
                {
                    nextPhase = i;
                    break;
                }
            }
            if (Round > 0) nextPhase = maxPhase;
            return nextPhase;
        }
        #endregion
    }
}