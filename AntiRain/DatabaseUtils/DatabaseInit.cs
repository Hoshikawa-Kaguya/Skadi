using System;
using System.Threading;
using AntiRain.DatabaseUtils.SqliteTool;
using AntiRain.IO;
using Sora.EventArgs.SoraEvent;
using SqlSugar;
using YukariToolBox.FormatLog;

namespace AntiRain.DatabaseUtils
{
    internal static class DatabaseInit //数据库初始化类
    {
        /// <summary>
        /// 初始化数据库
        /// </summary>
        /// <param name="eventArgs">CQAppEnableEventArgs</param>
        public static void UserDataInit(ConnectEventArgs eventArgs)
        {
            string DBPath = SugarUtils.GetDBPath(eventArgs.LoginUid.ToString());
            Log.Debug("IO", $"获取用户数据路径{DBPath}");
            //检查文件是否存在
            IOUtils.CheckFileExists(DBPath);
            //创建数据库链接
            SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);

            try
            {
                if (!SugarUtils.TableExists<MemberInfo>(dbClient)) //成员状态表的初始化
                {
                    Log.Warning("数据库初始化", "未找到成员状态表 - 创建一个新表");
                    SugarUtils.CreateTable<MemberInfo>(dbClient);
                }

                if (!SugarUtils.TableExists<BiliDynamicSubscription>(dbClient)) //动态记录表的初始化
                {
                    Log.Warning("数据库初始化", "未找到动态订阅表 - 创建一个新表");
                    SugarUtils.CreateTable<BiliDynamicSubscription>(dbClient);
                }

                if (!SugarUtils.TableExists<GuildBattleBoss>(dbClient)) //会战数据表的初始化
                {
                    Log.Warning("数据库初始化", "未找到会战数据表 - 创建一个新表");
                    SugarUtils.CreateTable<GuildBattleBoss>(dbClient);
                }

                if (!SugarUtils.TableExists<BiliLiveSubscription>(dbClient))
                {
                    Log.Warning("数据库初始化", "未找到直播订阅表 - 创建一个新表");
                    SugarUtils.CreateTable<BiliLiveSubscription>(dbClient);
                }

                if (!SugarUtils.TableExists<GuildInfo>(dbClient)) //会战状态表的初始化
                {
                    Log.Warning("数据库初始化", "未找到会战状态表 - 创建一个新表");
                    SugarUtils.CreateTable<GuildInfo>(dbClient);
                }
            }
            catch (Exception exception)
            {
                Log.Fatal("数据库初始化错误", Log.ErrorLogBuilder(exception));
                Thread.Sleep(5000);
                Environment.Exit(-1);
            }
        }
    }
}