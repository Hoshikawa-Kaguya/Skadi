using System.Text;
using BilibiliApi;
using BilibiliApi.Dynamic;
using BilibiliApi.Dynamic.CardEnum;
using BilibiliApi.Dynamic.DynamicData;
using BilibiliApi.Dynamic.DynamicData.Card;
using com.cbgan.SuiseiBot.Code.IO.Config;
using com.cbgan.SuiseiBot.Code.Tool.Log;
using Native.Sdk.Cqp;
using Newtonsoft.Json.Linq;

namespace com.cbgan.SuiseiBot.Code.TimerEvent.DD
{
    internal class DDHelper
    {
        /// <summary>
        /// 自动获取B站动态
        /// </summary>
        /// <param name="cqApi">CQApi</param>
        public static void TimeToDD(CQApi cqApi)
        {
            //TODO 配置文件读取改到定时器中
            //读取配置文件
            ConfigIO config       = new ConfigIO(cqApi.GetLoginQQ().Id);
            Module   moduleEnable = config.LoadedConfig.ModuleSwitch;
            TimeToDD DDConfig     = config.LoadedConfig.DD_Config;
            //检查模块是否启用或是否有订阅
            if (!moduleEnable.DDHelper || DDConfig.Users.Length == 0) return;

            foreach (long user in DDConfig.Users)
            {
                debug(cqApi, 883740678,user);
            }
        }

        //TODO 修改到公共函数中
        private static void debug(CQApi cqApi, long groupId, long uid)
        {
            string  message;
            Dynamic biliDynamic;
            //获取动态文本
            JObject cardData = NetUtils.GetBiliDynamicJson(uid, out CardType cardType);
            //检查动态类型
            switch (cardType)
            {
                case CardType.PlainText:
                    PlainTextCard plainTextCard = new PlainTextCard(cardData);
                    plainTextCard.ContentType = ContentType.CQCode;
                    message                   = plainTextCard.ToString();
                    biliDynamic               = plainTextCard;
                    break;
                case CardType.TextAndPic:
                    TextAndPicCard textAndPicCard = new TextAndPicCard(cardData);
                    textAndPicCard.ContentType = ContentType.CQCode;
                    message                    = textAndPicCard.ToString();
                    biliDynamic                = textAndPicCard;
                    break;
                default:
                    return;
            }
            //获取用户信息
            UserInfo sender = biliDynamic.GetUserInfo();
            ConsoleLog.Info("动态获取", $"{sender.UserName}的动态获取成功");
            //格式化动态信息
            StringBuilder sendMessageBuilder = new StringBuilder();
            sendMessageBuilder.Append("获取到了来自 ");
            sendMessageBuilder.Append(sender.UserName);
            sendMessageBuilder.Append(" 的动态：\r\n");
            sendMessageBuilder.Append(message);
            sendMessageBuilder.Append("\r\n更新时间：");
            sendMessageBuilder.Append(biliDynamic.UpdateTime);
            sendMessageBuilder.Append("\r\n动态链接：");
            sendMessageBuilder.Append(biliDynamic.GetDynamicUrl());
            cqApi.SendGroupMessage(groupId, sendMessageBuilder.ToString());
        }
    }
}
