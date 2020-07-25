using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.cbgan.SuiseiBot.Code.SqliteTool;
using com.cbgan.SuiseiBot.Code.Tool;
using Native.Sdk.Cqp.EventArgs;
using SqlSugar;

namespace com.cbgan.SuiseiBot.Code.Database
{
    internal class GuildBattleMgrDBHelper
    {
        private long GroupId { get; set; }
        private string DBPath { get; set; }

        public GuildBattleMgrDBHelper(object sender, CQGroupMessageEventArgs eventArgs)
        {
            GroupId = eventArgs.FromGroup.Id;
            DBPath = SugarUtils.GetDBPath(eventArgs.CQApi);
        }

        public bool GuildExists()
        {
            bool isExists,isExists2;
            using (SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath))
            {
                isExists = dbClient.Queryable<GuildData>().Where(guild => guild.Gid == 883740678).Any();
                isExists2 = dbClient.Queryable<GuildData>().Where(guild => guild.Gid == 1146619912).Any();
            }
            return isExists||isExists2;
        }

        public int StartBattle()
        {
            using (SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath))
            {
                if (SugarUtils.TableExists<GuildBattle>(dbClient,
                                                        $"{SugarTableUtils.GetTableName<GuildBattle>()}_{GroupId}"))
                {
                    ConsoleLog.Error("会战管理数据库","会战表已经存在检查是否未结束上次会战统计");
                    return -1;
                }
                else
                {
                    SugarUtils.CreateTable<GuildBattle>(dbClient,
                                                        $"{SugarTableUtils.GetTableName<GuildBattle>()}_{GroupId}");
                    ConsoleLog.Info("会战管理数据库", "开始新的一期会战统计");
                    return 0;
                }
            }
        }
    }
}
