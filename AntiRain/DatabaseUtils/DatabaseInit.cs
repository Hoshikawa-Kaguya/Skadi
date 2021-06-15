using System;
using System.Linq;
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
                //获取所有表格类
                var tables = typeof(Tables).GetNestedTypes().Where(t => t.IsClass).ToList();

                //检查数据库表格
                foreach (var table in tables)
                {
                    Log.Debug("数据库初始化", $"检查表[{table.Name}]");
                    if (table.TableExists(dbClient)) continue;
                    Log.Warning("数据库初始化", $"未找表[{table.Name}],创建新表");
                    table.CreateTable(dbClient);
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