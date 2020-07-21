using com.cbgan.SuiseiBot.Code.SqliteTool;
using com.cbgan.SuiseiBot.Code.Tool;
using Native.Sdk.Cqp.EventArgs;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.cbgan.SuiseiBot.Code.Database
{
    class BossDBHelper
    {
        #region 参数
        private long GroupId { set; get; } //群号
        private string[] GuildId { set; get; } //公会信息

        public CQGroupMessageEventArgs EventArgs { private set; get; }
        public object Sender { private set; get; }
        private SqlSugarClient OriginDBClient;
        public readonly static string GuildTableName = "guild";  //公会数据库表名
        public readonly static string MemberTableName = "member"; //成员数据库表名
        private static string DBPath;                    //数据库路径
        #endregion

        #region 构造函数
        public BossDBHelper(object sender, CQGroupMessageEventArgs eventArgs)
        {
            this.Sender = sender;
            this.EventArgs = eventArgs;
            this.GroupId = eventArgs.FromGroup.Id;
            DBPath = SugarUtils.GetDBPath(eventArgs.CQApi);
            OriginDB = SugarUtils.GetLocalClient(@"TargetDBPath");
        }

        #endregion



        #region 操作数据库函数
        public int StartLoadPhase()
        {
            using (SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath))
            {
                if (SugarUtils.TableExists<PhaseInfo>(dbClient,
                                                        $"{SugarTableUtils.GetTableName<PhaseInfo>()}_{GroupId}"))
                {
                    ConsoleLog.Error("会战管理数据库", "Phase信息表已存在，请检查是否结束上一次会战");
                    return -1;
                }
                else
                {
                    SugarUtils.CreateTable<PhaseInfo>(dbClient,
                                                        $"{SugarTableUtils.GetTableName<PhaseInfo>()}_{GroupId}");
                    ConsoleLog.Info("会战管理数据库", "开始加载新一期会战数据");
                    return 0;
                }
            }
        }
        #endregion
    }
}
