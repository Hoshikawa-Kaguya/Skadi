using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuiseiBot.Code.Resource.TypeEnum.GuildBattleType
{
    internal enum AttackType
    {
        /// <summary>
        /// 非法刀
        /// </summary>
        Illeage = -1,
        /// <summary>
        /// 普通刀
        /// </summary>
        Normal = 0,
        /// <summary>
        /// 尾刀
        /// </summary>
        Final = 1,
        /// <summary>
        /// 过度伤害
        /// </summary>
        FinalOutOfRange = 2,
        /// <summary>
        /// 补时刀
        /// </summary>
        Compensate = 3,
        /// <summary>
        /// 掉刀
        /// </summary>
        Offline = 4,
        /// <summary>
        /// 补时刀击杀
        /// </summary>
        CompensateKill = 5
    }
}
