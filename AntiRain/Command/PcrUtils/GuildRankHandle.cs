using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AntiRain.DatabaseUtils.Helpers.PCRGuildBattleDB;
using AntiRain.TypeEnum.CommandType;
using Newtonsoft.Json.Linq;
using PyLibSharp.Requests;
using Sora.Entities;
using Sora.Enumeration.ApiType;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.FormatLog;
using YukariToolBox.Time;

namespace AntiRain.Command.PcrUtils
{
    //TODO 适配Command
    internal class GuildRankHandle
    {
        #region 参数

        public object                Sender       { private set; get; }
        public Group                 QQGroup      { private set; get; }
        public GroupMessageEventArgs eventArgs { private set; get; }

        /// <summary>
        /// 数据库实例
        /// </summary>
        private GuildManagerDBHelper DBHelper { get; set; }

        #endregion

        #region 构造函数

        public GuildRankHandle(object sender, GroupMessageEventArgs e)
        {
            this.eventArgs = e;
            this.Sender       = sender;
            this.QQGroup      = eventArgs.SourceGroup;
            this.DBHelper     = new GuildManagerDBHelper(eventArgs.LoginUid);
        }

        #endregion

        #region 消息响应函数

        /// <summary>
        /// 收到信息的函数
        /// 并匹配相应指令
        /// </summary>
        public async void GetChat(RegexCommand cmdType)
        {
            if (eventArgs == null || Sender == null) return;
            switch (cmdType)
            {
                //查询公会排名
                case RegexCommand.GetGuildRank:
                    

                    break;
            }

            //以群名为查询名
            if (eventArgs.Message.RawText.Length <= 6)
            {
                var groupInfo = await QQGroup.GetGroupInfo();
                if (groupInfo.apiStatus != APIStatusType.OK)
                {
                    await QQGroup.SendGroupMessage("调用onebot API时发生错误");
                    Log.Error("api error", $"调用onebot API时发生错误 Status={groupInfo.apiStatus}");
                    return;
                }

                await KyoukaRank(DBHelper.GetGuildName(eventArgs.SourceGroup));
            }
            else //手动指定
            {
                await KyoukaRank(eventArgs.Message.RawText[6..]);
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 从比利比利源查询排名
        /// </summary>
        private async ValueTask BiliWikiRank(string guildName)
        {
            JArray response;
            //获取响应
            try
            {
                //获取查询结果
                Log.Info("NET", $"尝试查询[{guildName}]会站排名");
                await QQGroup.SendGroupMessage($"正在查询公会[{guildName}]的排名...");
                ReqResponse reqResponse = await Requests.GetAsync("https://tools-wiki.biligame.com/pcr/getTableInfo",
                                                                  new ReqParams
                                                                  {
                                                                      Timeout = 3000,
                                                                      Params = new Dictionary<string, string>
                                                                      {
                                                                          {"type", "search"},
                                                                          {"search", guildName},
                                                                          {"page", "0"}
                                                                      }
                                                                  });
                //判断响应
                if (reqResponse.StatusCode != HttpStatusCode.OK)
                {
                    await
                        QQGroup
                            .SendGroupMessage($"哇哦~发生了网络错误，请联系机器人所在服务器管理员\r\n{reqResponse.StatusCode}({(int) reqResponse.StatusCode})");
                    Log.Error("网络发生错误", $"Code[{(int) reqResponse.StatusCode}]");
                    //阻止下一步处理
                    return;
                }

                //读取返回值
                response = new JArray(reqResponse.Text);
                Log.Info("获取JSON成功", response);
            }
            catch (Exception e)
            {
                await QQGroup.SendGroupMessage("哇哦~发生了网络错误，请联系机器人所在服务器管理员");
                Log.Error("网络发生错误", Log.ErrorLogBuilder(e));
                //阻止下一步处理
                return;
            }

            //JSON数据处理
            try
            {
                //对返回值进行判断
                if (response.Count == 0)
                {
                    await QQGroup.SendGroupMessage("未找到任意公会\n请检查是否查询的错误的公会名或公会排名在70000之后");
                    Log.Info("JSON处理成功", "所查询列表为空");
                    return;
                }

                JArray  dataArray = JArray.Parse(response.First?.ToString() ?? "[]");
                JObject rankData  = dataArray[0].ToObject<JObject>();
                if (dataArray.Count > 1) await QQGroup.SendGroupMessage("查询到多个公会，可能存在重名或关键词错误");
                if (rankData == null || rankData.Count == 0)
                {
                    await QQGroup.SendGroupMessage("发生了未知错误，请请向开发者反馈问题");
                    Log.Error("JSON数据读取错误", "从网络获取的文本为空");
                    return;
                }

                string rank       = rankData["rank"]?.ToString();
                string totalScore = rankData["damage"]?.ToString();
                string leaderName = rankData["leader_name"]?.ToString();
                Log.Info("JSON处理成功", "向用户发送数据");
                await QQGroup.SendGroupMessage("查询成功！\n"             +
                                               $"公会:{guildName}\n"   +
                                               $"排名:{rank}\n"        +
                                               $"总分数:{totalScore}\n" +
                                               $"会长:{leaderName}\n"  +
                                               "如果查询到的信息有误，有可能关键词错误或公会排名在70000之后");
            }
            catch (Exception e)
            {
                await QQGroup.SendGroupMessage("发生了未知错误，请请向开发者反馈问题");
                Log.Error("JSON数据读取错误", $"从网络获取的JSON格式无法解析{Log.ErrorLogBuilder(e)}");
            }
        }

        /// <summary>
        /// 此方法暂时弃用改用比利比利源
        /// </summary>
        private async ValueTask KyoukaRank(string guildName)
        {
            //网络响应
            JToken response;
            //获取网络响应
            try
            {
                //获取查询结果
                Log.Info("NET", $"尝试查询[{guildName}]会站排名");
                await QQGroup.SendGroupMessage($"正在查询公会[{guildName}]的排名...");
                ReqResponse reqResponse =
                    await Requests.PostAsync("https://service-kjcbcnmw-1254119946.gz.apigw.tencentcs.com/name/0",
                                             new ReqParams
                                             {
                                                 Timeout = 3000,
                                                 PostJson = new JObject
                                                 {
                                                     ["clanName"] = guildName,
                                                     ["history"]  = 0
                                                 },
                                                 Header = new Dictionary<HttpRequestHeader, string>
                                                 {
                                                     {HttpRequestHeader.Referer, "https://kengxxiao.github.io/Kyouka/"}
                                                 },
                                                 CustomHeader = new Dictionary<string, string>
                                                 {
                                                     {"Custom-Source", "AntiRainBot"}
                                                 }
                                             });
                //判断响应
                if (reqResponse.StatusCode != HttpStatusCode.OK)
                {
                    await
                        QQGroup
                            .SendGroupMessage($"哇哦~发生了网络错误，请联系机器人所在服务器管理员\r\n{reqResponse.StatusCode}({(int) reqResponse.StatusCode})");
                    Log.Error("网络发生错误", $"Code[{(int) reqResponse.StatusCode}]");
                    //阻止下一步处理
                    return;
                }

                //读取返回值
                response = reqResponse.Json();
                //判断空数据
                if (response == null || !response.Any())
                {
                    await QQGroup.SendGroupMessage("发生了未知错误，请请向开发者反馈问题");
                    Log.Error("JSON数据读取错误", "从网络获取的文本为空");
                    return;
                }

                Log.Info("获取JSON成功", response);
            }
            catch (Exception e)
            {
                await QQGroup.SendGroupMessage($"哇哦~发生了网络错误，请联系机器人所在服务器管理员\n{e.Message}");
                Log.Error("网络发生错误", e);
                //阻止下一步处理
                return;
            }

            //JSON数据处理
            try
            {
                if (response["full"] == null)
                {
                    await QQGroup.SendGroupMessage("发生了未知错误，请请向开发者反馈问题");
                    Log.Error("JSON数据读取错误", "从网络获取的JSON格式可能有问题");
                    return;
                }

                //在有查询结果时查找值
                if (response["full"]?.ToString().Equals("0") ?? false)
                {
                    if (!response["full"]?.ToString().Equals("1") ?? false)
                        await QQGroup.SendGroupMessage("查询到多个公会，可能存在重名或关键词错误");
                    Log.Info("JSON处理成功", "向用户发送数据");
                    long.TryParse(response["ts"]?.ToString() ?? "0", out long updateTimeStamp);
                    await QQGroup.SendGroupMessage("查询成功！\n",
                                                   $"公会:{guildName}\n",
                                                   $"排名:{response["data"]?[0]?["rank"]}\n",
                                                   $"总分数:{response["data"]?[0]?["damage"]}\n",
                                                   $"会长:{response["data"]?[0]?["leader_name"]}\n",
                                                   $"数据更新时间:{updateTimeStamp.ToDateTime():MM-dd HH:mm:ss}\n",
                                                   "如果查询到的信息有误，有可能关键词错误或公会排名在20060之后");
                }
                else
                {
                    await QQGroup.SendGroupMessage("未找到任意公会\n请检查是否查询的错误的公会名或公会排名在20060之后");
                    Log.Info("JSON处理成功", "所查询列表为空");
                }
            }
            catch (Exception e)
            {
                await QQGroup.SendGroupMessage($"在处理数据时发生了错误，请请向开发者反馈问题\n{e.Message}");
                Log.Error("JSON数据读取错误", e);
            }
        }

        #endregion
    }
}