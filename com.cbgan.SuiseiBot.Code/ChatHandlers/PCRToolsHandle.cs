using System;
using com.cbgan.SuiseiBot.Code.Network;
using com.cbgan.SuiseiBot.Code.Resource.TypeEnum;
using com.cbgan.SuiseiBot.Code.Resource.TypeEnum.CmdType;
using com.cbgan.SuiseiBot.Code.Tool;
using com.cbgan.SuiseiBot.Code.Tool.Log;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Model;
using Newtonsoft.Json.Linq;

namespace com.cbgan.SuiseiBot.Code.ChatHandlers
{
    internal class PCRToolsHandle
    {
        #region 参数
        public object                  Sender       { private set; get; }
        public Group                   QQGroup      { private set; get; }
        public CQGroupMessageEventArgs PCREventArgs { private set; get; }
        #endregion

        #region 构造函数
        public PCRToolsHandle(object sender, CQGroupMessageEventArgs e)
        {
            this.PCREventArgs = e;
            this.Sender       = sender;
            this.QQGroup      = PCREventArgs.FromGroup;
        }
        #endregion

        #region 消息响应函数
        /// <summary>
        /// 收到信息的函数
        /// 并匹配相应指令
        /// </summary>
        public void GetChat(KeywordCmdType cmdType)
        {
            if (PCREventArgs == null || Sender == null) return;
            string[] commandArgs = PCREventArgs.Message.Text.Split(' ');
            switch (cmdType)
            {
                //查询公会排名
                case KeywordCmdType.PCRTools_GetGuildRank:
                    GetGuildRank(commandArgs);
                    break;
            }
            PCREventArgs.Handler = true;
        }
        #endregion

        #region 私有方法
        private void GetGuildRank(string[] commandArgs)
        {
            //公会名 网络响应
            string guildName, response;
            //检查参数
            switch (Utils.CheckForLength(commandArgs, 1))
            {
                case LenType.Illegal:
                    guildName = QQGroup.GetGroupInfo().Name;
                    break;
                case LenType.Legitimate:
                    guildName = commandArgs[1];
                    break;
                default:
                case LenType.Extra:
                    QQGroup.SendGroupMessage("有多余参数");
                    return;
            }
            //TODO 查询Cache
            //获取网络响应
            try
            {
                //初始化查询JSON
                JObject clanInfoJson = new JObject
                {
                    ["clanName"] = guildName,
                    ["history"] = 0
                };
                ConsoleLog.Info("NET", "尝试查询结果");
                //获取查询结果
                QQGroup.SendGroupMessage("查询中...");
                response =
                    HTTPUtils.PostHttpResponse("https://service-kjcbcnmw-1254119946.gz.apigw.tencentcs.com/name/0",
                                               clanInfoJson,
                                               "Windows", "application/json", "https://kengxxiao.github.io/Kyouka/", 3000, "BOT");
            }
            catch (Exception e)
            {
                QQGroup.SendGroupMessage($"哇哦~发生了网络错误，请联系机器人所在服务器管理员\n{e.Message}");
                ConsoleLog.Error("网络发生错误", e);
                //阻止下一步处理
                return;
            }
            //JSON数据处理
            try
            {
                if (string.IsNullOrEmpty(response))
                {
                    QQGroup.SendGroupMessage("发生了未知错误，请请向开发者反馈问题");
                    ConsoleLog.Error("JSON数据读取错误", "从网络获取的文本为空");
                    return;
                }
                ConsoleLog.Info("获取JSON成功", response);
                JObject responseJObject = JObject.Parse(response);
                if (responseJObject["full"] == null)
                {
                    QQGroup.SendGroupMessage("发生了未知错误，请请向开发者反馈问题");
                    ConsoleLog.Error("JSON数据读取错误", "从网络获取的JSON格式可能有问题");
                    return;
                }
                //在有查询结果时查找值
                if (!responseJObject["full"].ToString().Equals("0"))
                {
                    if (!responseJObject["full"].ToString().Equals("1")) QQGroup.SendGroupMessage("查询到多个公会，可能存在重名或关键词错误");
                    string rank = responseJObject["data"]?[0]?["rank"]?.ToString();
                    string totalScore = responseJObject["data"]?[0]?["damage"]?.ToString();
                    string leaderName = responseJObject["data"]?[0]?["leader_name"]?.ToString();
                    ConsoleLog.Info("JSON处理成功", "向用户发送数据");
                    QQGroup.SendGroupMessage("查询成功！\n" +
                                             $"公会  |{guildName}\n" +
                                             $"排名  |{rank}\n" +
                                             $"总分数|{totalScore}\n" +
                                             $"会长  |{leaderName}\n" +
                                             "如果查询到的信息有误，有可能关键词错误或公会排名在20060之后");
                }
                else
                {
                    QQGroup.SendGroupMessage("未找到任意公会\n请检查是否查询的错误的公会名或公会排名在20060之后");
                    ConsoleLog.Info("JSON处理成功", "所查询列表为空");
                }
            }
            catch (Exception e)
            {
                QQGroup.SendGroupMessage($"在处理数据时发生了错误，请请向开发者反馈问题\n{e.Message}");
                ConsoleLog.Error("JSON数据读取错误", e);
            }
        }
        #endregion
    }
}
