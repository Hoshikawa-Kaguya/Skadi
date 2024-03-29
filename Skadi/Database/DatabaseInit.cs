using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Skadi.Database.SqliteTool;
using Sora.EventArgs.SoraEvent;
using SqlSugar;
using YukariToolBox.LightLog;

namespace Skadi.Database;

internal static class DatabaseInit //数据库初始化类
{
    /// <summary>
    /// 初始化数据库
    /// </summary>
    /// <param name="eventArgs">CQAppEnableEventArgs</param>
    public static void UserDataInit(ConnectEventArgs eventArgs)
    {
        //创建数据库链接
        SqlSugarClient dbClient = SugarUtils.CreateUserDbClient(eventArgs.LoginUid);

        try
        {
            //获取所有表格类
            List<Type> tables = typeof(Tables).GetNestedTypes().Where(t => t.IsClass).ToList();

            //检查数据库表格
            foreach (Type table in tables)
            {
                Log.Debug("数据库初始化", $"检查表[{table.Name}]");
                if (table.TableExists(dbClient))
                    continue;
                Log.Warning("数据库初始化", $"未找表[{table.Name}],创建新表");
                table.CreateTable(dbClient);
            }
        }
        catch (Exception exception)
        {
            Log.Fatal(exception, "Database", "无法初始化数据库");
            Thread.Sleep(5000);
            Environment.Exit(-1);
        }
    }
}