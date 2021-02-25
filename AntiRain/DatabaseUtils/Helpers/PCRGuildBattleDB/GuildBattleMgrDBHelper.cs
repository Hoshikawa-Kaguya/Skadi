using System;
using System.Collections.Generic;
using System.Linq;
using AntiRain.DatabaseUtils.SqliteTool;
using AntiRain.TypeEnum;
using AntiRain.TypeEnum.GuildBattleType;
using AntiRain.Tool;
using Sora.EventArgs.SoraEvent;
using SqlSugar;
using YukariToolBox.FormatLog;
using YukariToolBox.Time;

namespace AntiRain.DatabaseUtils.Helpers.PCRGuildBattleDB
{
    internal class GuildBattleMgrDBHelper : BaseGuildBattleDBHelper
    {
        #region 属性

        /// <summary>
        /// 数据库表名
        /// </summary>
        private string BattleTableName { get; set; }

        #endregion

        #region 构造函数

        public GuildBattleMgrDBHelper(GroupMessageEventArgs eventArgs) : base(eventArgs)
        {
            BattleTableName = $"{SugarTableUtils.GetTableName<GuildBattle>()}_{GuildEventArgs.SourceGroup.Id}";
        }

        #endregion

        #region 公有方法

        /// <summary>
        /// 获取今天的出刀列表
        /// </summary>
        /// <returns>
        /// <para>出刀表</para>
        /// <para><see langword="null"/> 数据库错误</para>
        /// </returns>
        public List<GuildBattle> GetTodayAttacks(long? uid = null)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                if (uid == null)
                {
                    //查询所有人的出刀表
                    return dbClient.Queryable<GuildBattle>()
                                   .AS(BattleTableName)
                                   .Where(i => i.Time >= BotUtils.GetUpdateStamp())
                                   .OrderBy(i => i.Aid)
                                   .ToList();
                }
                else
                {
                    //查询单独成员的出刀表
                    return dbClient.Queryable<GuildBattle>()
                                   .AS(BattleTableName)
                                   .Where(i => i.Time >= BotUtils.GetUpdateStamp() && i.Uid == uid)
                                   .OrderBy(i => i.Aid)
                                   .ToList();
                }
            }
            catch (Exception e)
            {
                Log.Error("Database error", Log.ErrorLogBuilder(e));
                return null;
            }
        }

        /// <summary>
        /// 查询今日余刀
        /// 用于查刀和催刀
        /// </summary>
        /// <returns>余刀表</returns>
        public Dictionary<long, int> GetTodayAtkCount()
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                return dbClient.Queryable<GuildBattle>()
                               .AS(BattleTableName)
                               .Where(attack => attack.Time   > BotUtils.GetUpdateStamp() &&
                                                attack.Attack != AttackType.Compensate    &&
                                                attack.Attack != AttackType.CompensateKill)
                               .GroupBy(member => member.Uid)
                               .Select(member => new
                               {
                                   member.Uid,
                                   times = SqlFunc.AggregateCount(member.Uid)
                               })
                               .ToList()
                               .ToDictionary(member => member.Uid,
                                             member => member.times);
            }
            catch (Exception e)
            {
                Log.Error("Database error", Log.ErrorLogBuilder(e));
                return null;
            }
        }

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
                return dbClient.Queryable<GuildInfo>().InSingle(GuildEventArgs.SourceGroup.Id).InBattle ? 1 : 0;
            }
            catch (Exception e)
            {
                Log.Error("Database error", Log.ErrorLogBuilder(e));
                return -1;
            }
        }

        /// <summary>
        /// 开始会战
        /// </summary>
        /// <returns>
        /// <para><see langword="1"/> 成功开始统计</para>
        /// <para><see langword="0"/> 未结束会战</para>
        /// <para><see langword="-1"/> 数据库错误</para>
        /// </returns>
        public int StartBattle(GuildInfo guildInfo)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                if (SugarUtils.TableExists<GuildBattle>(dbClient, BattleTableName))
                {
                    Log.Error("会战管理数据库", "会战表已经存在，请检查是否未结束上次会战统计");
                    return 0;
                }
                else
                {
                    SugarUtils.CreateTable<GuildBattle>(dbClient, BattleTableName);
                    Log.Info("会战管理数据库", "开始新的一期会战统计");
                    dbClient.Updateable(new GuildInfo {InBattle = true})
                            .Where(guild => guild.Gid == GuildEventArgs.SourceGroup.Id)
                            .UpdateColumns(i => new {i.InBattle})
                            .ExecuteCommandHasChange();
                    //获取初始周目boss的信息
                    GuildBattleBoss initBossData = dbClient.Queryable<GuildBattleBoss>()
                                                           .Where(i => i.ServerId == guildInfo.ServerId
                                                                    && i.Phase    == 1
                                                                    && i.Order    == 1)
                                                           .First();
                    GuildInfo updateBossData =
                        new GuildInfo()
                        {
                            BossPhase = initBossData.Phase,
                            Order     = 1,
                            Round     = 1,
                            HP        = initBossData.HP,
                            TotalHP   = initBossData.HP
                        };
                    return dbClient.Updateable(updateBossData)
                                   .UpdateColumns(i => new {i.Order, i.HP, i.BossPhase, i.Round, i.TotalHP})
                                   .Where(i => i.Gid == GuildEventArgs.SourceGroup.Id)
                                   .ExecuteCommandHasChange()
                        ? 1
                        : -1;
                }
            }
            catch (Exception e)
            {
                Log.Error("Database error", Log.ErrorLogBuilder(e));
                return -1;
            }
        }

        /// <summary>
        /// 结束会战
        /// </summary>
        /// <returns>
        /// <para><see langword="1"/> 成功结束统计</para>
        /// <para><see langword="0"/> 未开始会战</para>
        /// <para><see langword="-1"/> 数据库错误</para>
        /// </returns>
        public int EndBattle()
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                if (SugarUtils.TableExists<GuildBattle>(dbClient, BattleTableName))
                {
                    Log.Warning("会战管理数据库", "结束一期会战统计删除旧表");
                    SugarUtils.DeletTable<GuildBattle>(dbClient, BattleTableName);
                    return dbClient.Updateable(new GuildInfo {InBattle = false})
                                   .Where(guild => guild.Gid == GuildEventArgs.SourceGroup.Id)
                                   .UpdateColumns(i => new {i.InBattle})
                                   .ExecuteCommandHasChange()
                        ? 1
                        : -1;
                }
                else
                {
                    Log.Info("会战管理数据库", "会战表为空，请确认是否已经开始会战统计");
                    return 0;
                }
            }
            catch (Exception e)
            {
                Log.Error("Database error", Log.ErrorLogBuilder(e));
                return -1;
            }
        }

        /// <summary>
        /// 获取指定用户最后一次出刀的类型
        /// </summary>
        /// <param name="uid">uid</param>
        /// <param name="attackType">出刀类型</param>
        /// <returns>
        /// <para>刀号</para>
        /// <para><see langword="0"/> 没有出刀记录</para>
        /// <para><see langword="-1"/> 数据库错误</para>
        /// </returns>
        public int GetLastAttack(long uid, out AttackType attackType)
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
                            .Select(attack => new {lastType = attack.Attack, attack.Aid})
                            .First();
                attackType = lastAttack?.lastType ?? AttackType.Illeage;
                return lastAttack?.Aid ?? 0;
            }
            catch (Exception e)
            {
                Log.Error("Database error", Log.ErrorLogBuilder(e));
                attackType = AttackType.Illeage;
                return -1;
            }
        }

        /// <summary>
        /// 由刀号获取出刀信息
        /// </summary>
        /// <param name="aid">刀号</param>
        /// <returns>
        /// <para>出刀信息</para>
        /// <para><see langword="null"/> 数据库错误</para>
        /// </returns>
        public GuildBattle GetAtkByID(int aid)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                return dbClient.Queryable<GuildBattle>()
                               .AS(BattleTableName)
                               .InSingle(aid);
            }
            catch (Exception e)
            {
                Log.Error("Database error", Log.ErrorLogBuilder(e));
                return null;
            }
        }

        /// <summary>
        /// 由刀号获取删除出刀信息
        /// </summary>
        /// <param name="aid">刀号</param>
        /// <returns>
        /// <para><see langword="true"/> 写入成功</para>
        /// <para><see langword="false"/> 数据库错误</para>
        /// </returns>
        public bool DelAtkByID(int aid)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                return dbClient.Deleteable<GuildBattle>()
                               .AS(BattleTableName)
                               .Where(i => i.Aid == aid)
                               .ExecuteCommandHasChange();
            }
            catch (Exception e)
            {
                Log.Error("Database error", Log.ErrorLogBuilder(e));
                return false;
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
                               .Where(i => i.Uid    == uid                   && i.Time   >= BotUtils.GetUpdateStamp() &&
                                           i.Attack != AttackType.Compensate && i.Attack != AttackType.CompensateKill)
                               //筛选出刀总数
                               .Count();
            }
            catch (Exception e)
            {
                Log.Error("Database error", Log.ErrorLogBuilder(e));
                return -1;
            }
        }

        /// <summary>
        /// 获取今日的总出刀数量
        /// </summary>
        /// <returns>
        /// <para>今日出刀数</para>
        /// <para><see langword="-1"/> 数据库错误</para>
        /// </returns>
        public int GetTodayAttackCount()
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                //出刀数
                return dbClient.Queryable<GuildBattle>()
                               .AS(BattleTableName)
                               //今天5点之后出刀的
                               .Where(i => i.Time   >= BotUtils.GetUpdateStamp() &&
                                           i.Attack != AttackType.Compensate && i.Attack != AttackType.CompensateKill)
                               //筛选出刀总数
                               .Count();
            }
            catch (Exception e)
            {
                Log.Error("Database error", Log.ErrorLogBuilder(e));
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
        /// <para>本次出刀刀号</para>
        /// <para><see langword="-1"/> 数据库错误</para>
        /// </returns>
        public int NewAttack(long uid, GuildInfo guildInfo, long dmg, AttackType attackType)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                //插入一刀数据
                var insertData = new GuildBattle()
                {
                    Uid    = uid,
                    Time   = TimeStamp.GetNowTimeStamp(),
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
                Log.Error("Database error", Log.ErrorLogBuilder(e));
                return -1;
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
                if (curBossHP > guildInfo.TotalHP) throw new ArgumentOutOfRangeException(nameof(curBossHP));
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                guildInfo.HP = curBossHP;
                return dbClient.Updateable(guildInfo)
                               .UpdateColumns(i => new {i.HP})
                               .ExecuteCommandHasChange();
            }
            catch (Exception e)
            {
                Log.Error("Database error", Log.ErrorLogBuilder(e));
                return false;
            }
        }

        /// <summary>
        /// 获取指定Boss信息
        /// </summary>
        /// <param name="round">周目</param>
        /// <param name="bossOrder">Boss序号</param>
        /// <param name="server">区服</param>
        /// <returns>
        /// <para>Boss信息</para>
        /// <para><see langword="null"/> 数据库错误</para>
        /// </returns>
        public GuildBattleBoss GetBossInfo(int round, int bossOrder, Server server)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                //获取修订的boss相关信息
                int phase = GetRoundPhase(server, round);
                return dbClient.Queryable<GuildBattleBoss>()
                               .Where(boss => boss.ServerId == server && boss.Order == bossOrder &&
                                              boss.Phase    == phase)
                               .First();
            }
            catch (Exception e)
            {
                Log.Error("Database error", Log.ErrorLogBuilder(e));
                return null;
            }
        }

        /// <summary>
        /// 修改会战进度（无视当前出刀进度和历史出刀）
        /// </summary>
        /// <returns>
        /// <para><see langword="true"/> 修改成功</para>
        /// <para><see langword="false"/> 修改失败</para>
        /// </returns>
        public bool ModifyProgress(int round, int bossOrder, long hp, long totalHP, int phase)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);

                //更新数据
                return dbClient.Updateable(new GuildInfo
                               {
                                   HP        = hp,
                                   TotalHP   = totalHP,
                                   Round     = round,
                                   Order     = bossOrder,
                                   BossPhase = phase,
                               })
                               .Where(guild => guild.Gid == GuildEventArgs.SourceGroup.Id)
                               .UpdateColumns(info => new
                               {
                                   info.HP, info.TotalHP, info.Round, info.Order, info.BossPhase
                               })
                               .ExecuteCommandHasChange();
            }
            catch (Exception e)
            {
                Log.Error("Database error", Log.ErrorLogBuilder(e));
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
                               .Where(i => i.Gid == GuildEventArgs.SourceGroup.Id)
                               .ExecuteCommandHasChange();
            }
            catch (Exception e)
            {
                Log.Error("Database error", Log.ErrorLogBuilder(e));
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

                int nextPhase = GetRoundPhase(guildInfo.ServerId, guildInfo.Round + 1);
                //获取下一个周目boss的信息
                GuildBattleBoss nextBossData = dbClient.Queryable<GuildBattleBoss>()
                                                       .Where(i => i.ServerId == guildInfo.ServerId
                                                                && i.Phase    == nextPhase
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
                               .Where(i => i.Gid == GuildEventArgs.SourceGroup.Id)
                               .ExecuteCommandHasChange();
            }
            catch (Exception e)
            {
                Log.Error("Database error", Log.ErrorLogBuilder(e));
                return false;
            }
        }

        /// <summary>
        /// 清空树上成员
        /// </summary>
        /// <returns>
        /// <para><see langword="true"/> 写入成功</para>
        /// <para><see langword="false"/> 数据库错误</para>
        /// </returns>
        public bool CleanTree()
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                dbClient.Updateable(new MemberInfo {Flag = FlagType.IDLE, Info = null})
                        .Where(i => i.Flag == FlagType.OnTree &&
                                    i.Gid  == GuildEventArgs.SourceGroup.Id)
                        .UpdateColumns(i => new {i.Flag, i.Info})
                        .ExecuteCommand();
                return true;
            }
            catch (Exception e)
            {
                Log.Error("Database error", Log.ErrorLogBuilder(e));
                return false;
            }
        }

        /// <summary>
        /// 清空正在出刀的成员
        /// </summary>
        /// <returns>
        /// <para><see langword="true"/> 写入成功</para>
        /// <para><see langword="false"/> 数据库错误</para>
        /// </returns>
        public bool CleanAtkStatus()
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                dbClient.Updateable(new MemberInfo {Flag = FlagType.IDLE, Info = null})
                        .Where(i => i.Flag == FlagType.EnGage &&
                                    i.Gid  == GuildEventArgs.SourceGroup.Id)
                        .UpdateColumns(i => new {i.Flag, i.Info})
                        .ExecuteCommand();
                return true;
            }
            catch (Exception e)
            {
                Log.Error("Database error", Log.ErrorLogBuilder(e));
                return false;
            }
        }

        /// <summary>
        /// 查树
        /// </summary>
        /// <returns>
        /// <para>挂树表</para>
        /// <para><see langword="null"/> 数据库错误</para>
        /// </returns>
        public List<long> GetTree()
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                return dbClient.Queryable<MemberInfo>()
                               .Where(member => member.Gid  == GuildEventArgs.SourceGroup.Id &&
                                                member.Flag == FlagType.OnTree)
                               .Select(member => member.Uid)
                               .ToList();
            }
            catch (Exception e)
            {
                Log.Error("Database error", Log.ErrorLogBuilder(e));
                return null;
            }
        }

        /// <summary>
        /// 获取正在出刀中的成员列表
        /// </summary>
        /// <returns>
        /// <para>出刀成员列表</para>
        /// <para><see langword="null"/> 数据库错误</para>
        /// </returns>
        public List<long> GetInAtk()
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                return dbClient.Queryable<MemberInfo>()
                               .Where(member => member.Gid  == GuildEventArgs.SourceGroup.Id &&
                                                member.Flag == FlagType.EnGage)
                               .Select(member => member.Uid)
                               .ToList();
            }
            catch (Exception e)
            {
                Log.Error("Database error", Log.ErrorLogBuilder(e));
                return null;
            }
        }

        /// <summary>
        /// 更新成员状态
        /// </summary>
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
                    Time = TimeStamp.GetNowTimeStamp(),
                };
                return dbClient.Updateable(memberInfo)
                               .UpdateColumns(i => new {i.Flag, i.Info, i.Time})
                               .Where(i => i.Gid == GuildEventArgs.SourceGroup.Id && i.Uid == uid)
                               .ExecuteCommandHasChange();
            }
            catch (Exception e)
            {
                Log.Error("Database error", Log.ErrorLogBuilder(e));
                return false;
            }
        }

        /// <summary>
        /// 设置成员SL
        /// 同时自动下树
        /// 如果只清空则不会修改状态
        /// </summary>
        /// <param name="uid">成员UID</param>
        /// <param name="cleanSL">是否清空SL</param>
        /// <returns>
        /// <para><see langword="true"/> 写入成功</para>
        /// <para><see langword="false"/> 数据库错误</para>
        /// </returns>
        public bool SetMemberSL(long uid, bool cleanSL = false)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                if (cleanSL) //清空SL
                {
                    return dbClient
                           .Updateable(new MemberInfo
                                           {SL = 0})
                           .UpdateColumns(i => new {i.SL})
                           .Where(i => i.Gid == GuildEventArgs.SourceGroup.Id && i.Uid == uid)
                           .ExecuteCommandHasChange();
                }
                else //设置新的SL
                {
                    return dbClient
                           .Updateable(new MemberInfo
                           {
                               Flag = FlagType.IDLE, SL                 = TimeStamp.GetNowTimeStamp(),
                               Time = TimeStamp.GetNowTimeStamp(), Info = null
                           })
                           .UpdateColumns(i => new {i.Flag, i.SL, i.Time, i.Info})
                           .Where(i => i.Gid == GuildEventArgs.SourceGroup.Id && i.Uid == uid)
                           .ExecuteCommandHasChange();
                }
            }
            catch (Exception e)
            {
                Log.Error("Database error", Log.ErrorLogBuilder(e));
                return false;
            }
        }

        /// <summary>
        /// 获取指定周目的boss对应阶段
        /// </summary>
        /// <param name="server">区服</param>
        /// <param name="round">指定周目</param>
        /// <returns>
        /// <para>指定周目boss的阶段值</para>
        /// <para>如果没有查询刀则为0</para>
        /// </returns>
        public int GetRoundPhase(Server server, int round)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            //查找阶段数据
            return dbClient.Queryable<GuildBattleBoss>()
                           .Where(area => area.ServerId  == server &&
                                          area.RoundFrom <= round  && area.RoundTo >= round ||
                                          area.RoundFrom <= round && area.RoundTo == -1)
                           .Select(phase => phase.Phase)
                           .First();
        }

        #endregion
    }
}