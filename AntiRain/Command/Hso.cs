using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AntiRain.Config;
using AntiRain.Config.ConfigModule;
using AntiRain.DatabaseUtils.Helpers;
using AntiRain.IO;
using AntiRain.Tool;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using PyLibSharp.Requests;
using Sora.Attributes.Command;
using Sora.Entities;
using Sora.Entities.MessageElement;
using Sora.Enumeration.ApiType;
using Sora.Enumeration.EventParamsType;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.FormatLog;
using static AntiRain.Tool.CheckInCD;
using MatchType = Sora.Enumeration.MatchType;

namespace AntiRain.Command
{
    [CommandGroup]
    public class HsoCommand
    {
        #region 属性

        private Dictionary<CheckUser, DateTime> Users { get; set; } = new();

        #endregion

        #region 指令响应

        /// <summary>
        /// 用于处理传入指令
        /// </summary>
        [UsedImplicitly]
        [GroupCommand(CommandExpressions = new[] {"来点色图", "来点涩图", "我要看色图"})]
        public async void HsoPic(GroupMessageEventArgs eventArgs)
        {
            eventArgs.IsContinueEventChain = false;
            if (!ConfigManager.TryGetUserConfig(eventArgs.LoginUid, out UserConfig userConfig))
            {
                Log.Error("Config", "无法获取用户配置文件");
                return;
            }

            if (CheckGroupBlock(userConfig, eventArgs)) return;
            if (Users.IsInCD(eventArgs.SourceGroup, eventArgs.Sender))
            {
                await eventArgs.SourceGroup.SendGroupMessage(CQCodes.CQAt(eventArgs.Sender) +
                                                             "你是不是只会要色图(逊欸，冲的真快)");
                return;
            }

            //刷新数据库计数
            var hsoDbHelper = new HsoDBHelper(eventArgs.LoginUid);
            if (!hsoDbHelper.AddOrUpdate(eventArgs.Sender, eventArgs.SourceGroup))
                await eventArgs.Reply("数据库错误(count)");

            await GiveMeSetu(userConfig.HsoConfig, eventArgs);
        }

        [UsedImplicitly]
        [GroupCommand(CommandExpressions = new[] {@"^让我康康[0-9]+\s[0-9]+$"}, MatchType = MatchType.Regex)]
        public async void HsoPicIndexSearch(GroupMessageEventArgs eventArgs)
        {
            eventArgs.IsContinueEventChain = false;
            var picInfos = eventArgs.Message.RawText.Split(' ');
            var picId    = picInfos[0][4..];
            var picIndex = picInfos[1];
            if (!ConfigManager.TryGetUserConfig(eventArgs.LoginUid, out UserConfig userConfig))
            {
                Log.Error("Config", "无法获取用户配置文件");
                return;
            }

            await eventArgs.Reply("什么，有好康的");
            //处理图片代理连接
            string imageUrl;
            if (!string.IsNullOrEmpty(userConfig.HsoConfig.PximyProxy))
            {
                imageUrl = $"{userConfig.HsoConfig.PximyProxy.Trim('/')}/{picId}/{picIndex}";
                Log.Debug("Hso",$"Get proxy url {imageUrl}");
            }
            else
            {
                imageUrl = $"{userConfig.HsoConfig.PximyProxy.Trim('/')}/{picId}/{picIndex}";
                Log.Warning("Hso","未找到代理服务器已使用默认代理:https://pixiv.lancercmd.cc/");
            }
            //发送图片并在一分钟后自动撤回
            var (apiStatus, messageId) = await eventArgs.Reply(CQCodes.CQImage(imageUrl),
                TimeSpan.FromSeconds(10));
            if (apiStatus.RetCode == ApiStatusType.OK)
            {
                //延迟一分钟后自动撤回
                await Task.Delay(TimeSpan.FromMinutes(1));
                await eventArgs.SoraApi.RecallMessage(messageId);
                
            }
            else await eventArgs.Reply("逊欸，图都被删了");
        }

        [UsedImplicitly]
        [GroupCommand(CommandExpressions = new[] {"来点色批"}, MatchType = MatchType.Full)]
        public async void HsoRank(GroupMessageEventArgs eventArgs)
        {
            eventArgs.IsContinueEventChain = false;
            var hsoDbHelper = new HsoDBHelper(eventArgs.LoginUid);
            if (!hsoDbHelper.GetGroupRank(eventArgs.SourceGroup, out var rankList))
            {
                await eventArgs.Reply("数据库错误(count)");
                return;
            }

            if (rankList == null || rankList.Count == 0)
            {
                await eventArgs.Reply("你群连个色批都没有");
            }
            else
            {
                var message = new MessageBody {"让我康康到底谁最能冲\r\n"};
                foreach (var count in rankList)
                {
                    message.AddRange(count.Uid.ToAt() + $"冲了{count.Count}次" + "\r\n");
                }

                //删去多余的换行
                message.RemoveAt(message.Count - 1);
                await eventArgs.Reply(message);
            }
        }

        [UsedImplicitly]
        [GroupCommand(CommandExpressions = new[] {@"^AD[0-9]+\s[0-9]+$"})]
        public async void HsoAddPic(GroupMessageEventArgs eventArgs)
        {
            eventArgs.IsContinueEventChain = false;
            //判断权限
            if (eventArgs.SenderInfo.Role != MemberRoleType.SuperUser)
            {
                await eventArgs.Reply("权限不足拒绝执行");
                return;
            }

            var picInfos = eventArgs.Message.RawText.Split(' ');
            var picId    = picInfos[0][2..];
            var picIndex = picInfos[1];
            await eventArgs.Reply($"Adding[{picId}]...");
            //读取用户配置
            if (!ConfigManager.TryGetUserConfig(eventArgs.LoginUid, out UserConfig userConfig))
            {
                Log.Error("Config", "无法获取用户配置文件");
                await eventArgs.Reply("Try load user config error");
                return;
            }

            if (string.IsNullOrEmpty(userConfig.HsoConfig.YukariApiKey))
            {
                Log.Error("apikey", "apikey is null");
                await eventArgs.Reply("apikey is null");
                return;
            }

            ReqResponse res = await Requests.PostAsync("https://api.yukari.one/setu/add_pic", new ReqParams
            {
                Params = new Dictionary<string, string>
                {
                    {"apikey", userConfig.HsoConfig.YukariApiKey},
                    {"pid", picId},
                    {"index", picIndex}
                },
                Timeout                   = 10000,
                IsThrowErrorForStatusCode = false,
                IsThrowErrorForTimeout    = false
            });

            if (res.StatusCode != HttpStatusCode.OK)
            {
                Log.Error("net", "net error");
                await eventArgs.Reply("net error");
                return;
            }

            var resData = res.Json();
            if (resData == null)
            {
                Log.Error("api error", "api error (null response)]");
                await eventArgs.Reply("api error (null response)]");
                return;
            }

            if (Convert.ToInt32(resData["code"]) != 0)
            {
                Log.Error("api error", $"api error (code:{resData["code"]})]");
                await eventArgs.Reply($"api error (code:{resData["code"]})]\r\n{resData["message"]}");
                return;
            }

            Log.Info("pic upload", "pic upload success");
            await eventArgs.Reply($"success [{picId}]");
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// <para>从色图源获取色图</para>
        /// <para>不会支持R18的哦</para>
        /// </summary>
        /// <param name="hso">hso配置实例</param>
        /// <param name="eventArgs">事件参数</param>
        private async Task GiveMeSetu(Hso hso, GroupMessageEventArgs eventArgs)
        {
            JToken response;
            Log.Debug("源", hso.Source);
            //本地模式
            if (hso.Source.Equals("Local"))
            {
                SendLocalPic(hso, eventArgs);
                return;
            }

            //网络部分
            try
            {
                Log.Info("NET", "尝试获取色图");
                await eventArgs.SourceGroup.SendGroupMessage("正在获取色图中...");
                string apiKey;
                string serverUrl;
                //源切换
                switch (hso.Source)
                {
                    case "Mix":
                        Random randSource = new Random();
                        if (randSource.Next(1, 100) > 50)
                        {
                            serverUrl = "https://api.lolicon.app/setu/";
                            apiKey    = hso.LoliconApiKey ?? string.Empty;
                        }
                        else
                        {
                            serverUrl = "https://api.yukari.one/setu/";
                            apiKey    = hso.YukariApiKey ?? string.Empty;
                        }

                        break;
                    case "Yukari":
                        serverUrl = "https://api.yukari.one/setu/";
                        apiKey    = hso.YukariApiKey ?? string.Empty;
                        break;
                    case "Lolicon":
                        serverUrl = "https://api.yukari.one/setu/";
                        apiKey    = hso.YukariApiKey ?? string.Empty;
                        break;
                    default:
                        serverUrl = hso.Source;
                        apiKey    = string.Empty;
                        break;
                }

                //向服务器发送请求
                Log.Debug("hso api server", serverUrl);
                var reqResponse = await Requests.GetAsync(serverUrl, new ReqParams
                {
                    Timeout = 3000,
                    Params = new Dictionary<string, string>
                    {
                        {"apikey", apiKey}
                    },
                    isCheckSSLCert = hso.CheckSSLCert
                });
                if (reqResponse.StatusCode != HttpStatusCode.OK)
                {
                    Log.Error("Net", $"{serverUrl} return code {(int) reqResponse.StatusCode}");
                    await eventArgs.SourceGroup
                                   .SendGroupMessage($"哇哦~发生了网络错误[{reqResponse.StatusCode}]，请联系机器人所在服务器管理员");
                    return;
                }

                response = reqResponse.Json();
            }
            catch (Exception e)
            {
                //网络错误
                await eventArgs.SourceGroup.SendGroupMessage("哇哦~发生了网络错误，请联系机器人所在服务器管理员");
                Log.Error("网络发生错误", $"{Log.ErrorLogBuilder(e)}\r\n\r\n{Utils.GetInnerExceptionMessages(e)}");
                return;
            }

            //json处理
            try
            {
                if (!int.TryParse(response["code"]?.ToString() ?? "-100", out int retCode) && retCode != 0)
                {
                    Log.Error("Hso",
                              retCode == -100
                                  ? "Server response null message"
                                  : $"Server response code {retCode}");
                    await eventArgs.SourceGroup.SendGroupMessage("哇奧色图不见了\n请联系机器人服务器管理员");
                    return;
                }

                //图片链接
                string picUrl = response["data"]?[0]?["url"]?.ToString() ?? "";
                Log.Debug("获取到图片", picUrl);
                //本地图片存储路径
                string localPicPath = $"{IOUtils.GetHsoPath()}/{Path.GetFileName(picUrl)}".Replace('\\', '/');
                if (File.Exists(localPicPath)) //检查是否已缓存过图片
                {
                    await eventArgs.SourceGroup.SendGroupMessage(HsoMessageBuilder(response["data"]?[0], hso.CardImage,
                                                                     localPicPath));
                }
                else
                {
                    //检查是否有设置代理
                    if (!string.IsNullOrEmpty(hso.PximyProxy))
                    {
                        string[]      fileNameArgs    = Regex.Split(Path.GetFileName(picUrl), "_p");
                        StringBuilder proxyUrlBuilder = new();
                        proxyUrlBuilder.Append(hso.PximyProxy);
                        //图片Pid部分
                        proxyUrlBuilder.Append(hso.PximyProxy.EndsWith("/")
                                                   ? $"{fileNameArgs[0]}"
                                                   : $"/{fileNameArgs[0]}");
                        //图片Index部分
                        proxyUrlBuilder.Append(fileNameArgs[1].Split('.')[0].Equals("0")
                                                   ? string.Empty
                                                   : $"/{fileNameArgs[1].Split('.')[0]}");
                        Log.Debug("Get Proxy Url", proxyUrlBuilder);
                        await eventArgs.SourceGroup.SendGroupMessage(HsoMessageBuilder(response["data"]?[0],
                                                                         hso.CardImage,
                                                                         proxyUrlBuilder.ToString()));
                    }
                    else
                    {
                        await eventArgs.SourceGroup.SendGroupMessage(HsoMessageBuilder(response["data"]?[0],
                                                                         hso.CardImage,
                                                                         picUrl));
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("色图下载失败", $"网络下载数据错误\n{Log.ErrorLogBuilder(e)}");
            }
        }

        private async void SendLocalPic(Hso hso, GroupMessageEventArgs eventArgs)
        {
            string[] picNames = Directory.GetFiles(IOUtils.GetHsoPath());
            if (picNames.Length == 0)
            {
                await eventArgs.SourceGroup.SendGroupMessage("机器人管理者没有在服务器上塞色图\r\n你去找他要啦!");
                return;
            }

            Random randFile     = new();
            string localPicPath = $"{picNames[randFile.Next(0, picNames.Length - 1)]}";
            Log.Debug("发送图片", localPicPath);
            await eventArgs.SourceGroup.SendGroupMessage(hso.CardImage
                                                             ? CQCodes.CQCardImage(localPicPath)
                                                             : CQCodes.CQImage(localPicPath));
        }

        /// <summary>
        /// 构建发送用的消息段
        /// </summary>
        /// <param name="picInfo">图片信息Json</param>
        /// <param name="cardImg">是否为装逼大图</param>
        /// <param name="picStr">图片路径/b64字符串</param>
        private MessageBody HsoMessageBuilder(JToken picInfo, bool cardImg, string picStr)
        {
            StringBuilder textBuilder = new();
            textBuilder.Append(picInfo["title"]);
            textBuilder.Append("\r\nid:");
            textBuilder.Append(picInfo["pid"]);
            if (picInfo["index"] != null)
            {
                textBuilder.Append(" - ");
                textBuilder.Append(picInfo["index"]);
            }

            textBuilder.Append("\r\n作者:");
            textBuilder.Append(picInfo["author"]);
            //构建消息
            MessageBody msg = new();
            msg.Add(cardImg
                        ? CQCodes.CQCardImage(picStr)
                        : CQCodes.CQImage(picStr));
            msg.Add(CQCodes.CQText(textBuilder.ToString()));
            return msg;
        }

        /// <summary>
        /// 检查群是否被屏蔽
        /// </summary>
        /// <param name="config">配置文件</param>
        /// <param name="eventArgs">事件参数</param>
        private bool CheckGroupBlock(UserConfig config, GroupMessageEventArgs eventArgs)
            => config.HsoConfig.GroupBlock.Any(gid => gid == eventArgs.SourceGroup);

        #endregion
    }
}