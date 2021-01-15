using System;
using System.Threading;
using AntiRain.DatabaseUtils.SqliteTool;
using AntiRain.IO;
using Sora.EventArgs.SoraEvent;
using Sora.Tool;
using SqlSugar;

namespace AntiRain.DatabaseUtils
{
    internal static class DatabaseInit//数据库初始化类
    {
        /// <summary>
        /// 初始化数据库
        /// </summary>
        /// <param name="eventArgs">CQAppEnableEventArgs</param>
        public static void UserDataInit(ConnectEventArgs eventArgs)
        {
            string DBPath = SugarUtils.GetDBPath(eventArgs.LoginUid.ToString());
            ConsoleLog.Debug("IO",$"获取用户数据路径{DBPath}");
            //检查文件是否存在
            IOUtils.CheckFileExists(DBPath);
            //创建数据库链接
            SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);

            try
            {
                if (!SugarUtils.TableExists<MemberInfo>(dbClient)) //成员状态表的初始化
                {
                    ConsoleLog.Warning("数据库初始化", "未找到成员状态表 - 创建一个新表");
                    SugarUtils.CreateTable<MemberInfo>(dbClient);
                }
                if (!SugarUtils.TableExists<BiliSubscription>(dbClient)) //动态记录表的初始化
                {
                    ConsoleLog.Warning("数据库初始化", "未找到动态记录表 - 创建一个新表");
                    SugarUtils.CreateTable<BiliSubscription>(dbClient);
                }
                if (!SugarUtils.TableExists<GuildBattleBoss>(dbClient)) //会战数据表的初始化
                {
                    ConsoleLog.Warning("数据库初始化", "未找到会战数据表 - 创建一个新表");
                    SugarUtils.CreateTable<GuildBattleBoss>(dbClient);
                }
                if (!SugarUtils.TableExists<GuildInfo>(dbClient)) //会战状态表的初始化
                {
                    ConsoleLog.Warning("数据库初始化", "未找到会战状态表 - 创建一个新表");
                    SugarUtils.CreateTable<GuildInfo>(dbClient);
                }
            }
            catch (Exception exception)
            {
                ConsoleLog.Fatal("数据库初始化错误",ConsoleLog.ErrorLogBuilder(exception));
                Thread.Sleep(5000);
                Environment.Exit(-1);
            }
        }

        public static void GlobalDataInit()
        {
            //获取数据库路径
            string DBPath = SugarUtils.GetDataDBPath(SugarUtils.GlobalResDBName);
            ConsoleLog.Debug("IO",$"获取数据路径{DBPath}");
            //检擦文件是否存在
            IOUtils.CheckFileExists(DBPath);
            //创建数据库链接
            SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);

            try
            {
                if (!SugarUtils.TableExists<RediveDBVersion>(dbClient))
                {
                    ConsoleLog.Warning("数据库初始化", "未找到版本记录表 - 创建一个新表");
                    SugarUtils.CreateTable<RediveDBVersion>(dbClient);
                }
                if (!SugarUtils.TableExists<PCRChara>(dbClient))
                {
                    ConsoleLog.Warning("数据库初始化", "未找到角色资源表 - 创建一个新表");
                    SugarUtils.CreateTable<PCRChara>(dbClient);
                }
            }
            catch (Exception e)
            {
                ConsoleLog.Fatal("数据库初始化错误",ConsoleLog.ErrorLogBuilder(e));
                Thread.Sleep(5000);
                Environment.Exit(-1);
            }
        }
    }
}
