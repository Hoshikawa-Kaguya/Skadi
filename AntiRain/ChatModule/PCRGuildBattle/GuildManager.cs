using System.Formats.Asn1;
using System.Net.Sockets;
using System.Threading.Tasks;
using AntiRain.DatabaseUtils.Helpers.PCRGuildBattleDB;
using AntiRain.Resource.TypeEnum.CommandType;
using Sora.Entities;
using Sora.Entities.Info;
using Sora.Enumeration.ApiEnum;
using Sora.Enumeration.EventParamsType;
using Sora.EventArgs.SoraEvent;
using Sora.Tool;

namespace AntiRain.ChatModule.PcrGuildBattle
{
    internal class GuildManager : BaseManager
    {
        #region 属性
        /// <summary>
        /// 数据库实例
        /// </summary>
        private GuildBattleMgrDBHelper DBHelper { get; set; }
        #endregion

        #region 构造函数
        internal GuildManager(GroupMessageEventArgs messageEventArgs, PCRGuildBattleCommand commandType) : base(messageEventArgs, commandType)
        {
            this.DBHelper    = new GuildBattleMgrDBHelper(messageEventArgs);
        }
        #endregion

        /// <summary>
        /// 公会管理指令响应函数
        /// </summary>
        public async void GuildManagerResponse() //功能响应
        {
            
        }

        #region 私有方法
        #endregion
    }
}
