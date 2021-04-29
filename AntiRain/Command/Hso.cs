using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AntiRain.Config.ConfigModule;
using AntiRain.IO;
using AntiRain.Tool;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using PyLibSharp.Requests;
using Sora.Attributes.Command;
using Sora.Entities.CQCodes;
using Sora.Enumeration.ApiType;
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
                await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender), "你是不是只会要色图(请等待CD冷却)");
                return;
            }

            //检查色图文件夹大小
            if (IOUtils.GetHsoSize() >= userConfig.HsoConfig.SizeLimit * 1024 * 1024)
            {
                Log.Warning("Hso", "色图文件夹超出大小限制，将清空文件夹");
                Directory.Delete(IOUtils.GetHsoPath(), true);
            }

            await GiveMeSetu(userConfig.HsoConfig, eventArgs);
        }

        [UsedImplicitly]
        [GroupCommand(CommandExpressions = new[] {"^让我康康 [0-9]+$"}, MatchType = MatchType.Regex)]
        public async void HsoPicIndexSearch(GroupMessageEventArgs eventArgs)
        {
            eventArgs.IsContinueEventChain = false;
            string msgStr = eventArgs.Message.ToString()[5..];
            if (!ConfigManager.TryGetUserConfig(eventArgs.LoginUid, out UserConfig userConfig))
            {
                Log.Error("Config", "无法获取用户配置文件");
                return;
            }

            //TODO 支持非代理连接图片
            if (!string.IsNullOrEmpty(userConfig.HsoConfig.PximyProxy))
            {
                await eventArgs.Reply(CQCode.CQImage($"{userConfig.HsoConfig.PximyProxy.Trim('/')}/{msgStr}"));
            }
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
                        DownloadPicFromURL(proxyUrlBuilder.ToString(), response["data"]?[0], localPicPath, hso.UseCache,
                                           hso.CardImage, eventArgs);
                    }
                    else
                    {
                        DownloadPicFromURL(picUrl, response["data"]?[0], localPicPath, hso.UseCache, hso.CardImage,
                                           eventArgs);
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
                                                             ? CQCode.CQCardImage(localPicPath)
                                                             : CQCode.CQImage(localPicPath));
        }

        /// <summary>
        /// 下载图片保存到本地
        /// </summary>
        /// <param name="url">目标URL</param>
        /// <param name="picInfo">图片信息</param>
        /// <param name="receivePath">接收文件的地址</param>
        /// <param name="useCache">是否启用本地缓存</param>
        /// <param name="cardImg">使用装逼大图</param>
        /// <param name="eventArgs">事件参数</param>
        private void DownloadPicFromURL(string url, JToken picInfo, string receivePath, bool useCache, bool cardImg,
                                        GroupMessageEventArgs eventArgs)
        {
            try
            {
                int      progressPercentage = 0;
                long     bytesReceived      = 0;
                DateTime flashTime          = DateTime.Now;
                Console.WriteLine(@"开始从网络下载文件");
                WebClient client = new WebClient();
                //文件下载
                client.DownloadProgressChanged += (sender, args) =>
                                                  {
                                                      if (progressPercentage == args.ProgressPercentage) return;
                                                      progressPercentage = args.ProgressPercentage;
                                                      Log
                                                          .Debug("Download Pic",
                                                                 $"Downloading {args.ProgressPercentage}% " +
                                                                 $"({(args.BytesReceived - bytesReceived) / 1024.0 / ((DateTime.Now - flashTime).TotalMilliseconds / 1000)}KB/s) ");
                                                      flashTime     = DateTime.Now;
                                                      bytesReceived = args.BytesReceived;
                                                  };
                //文件下载完成
                client.DownloadFileCompleted += async (sender, args) =>
                                                {
                                                    //检查是否出现空文件
                                                    if (!File.Exists(receivePath)) return;
                                                    FileInfo file = new FileInfo(receivePath);
                                                    Log.Debug("File Size Check", file.Length);
                                                    if (file.Length != 0)
                                                    {
                                                        Log.Info("Hso", "下载数据成功,发送图片");
                                                        //发送消息
                                                        var (code, _) =
                                                            await eventArgs.SourceGroup
                                                                           .SendGroupMessage(HsoMessageBuilder(picInfo,
                                                                               cardImg, receivePath));
                                                        Log.Debug("file", Path.GetFileName(receivePath));
                                                        if (code == APIStatusType.OK)
                                                        {
                                                            Log.Info("Hso", "色图发送成功");
                                                        }
                                                        else
                                                        {
                                                            Log.Error("Hso", $"色图发送失败 code={(int) code}");
                                                            if (code != APIStatusType.TimeOut)
                                                                await eventArgs.SourceGroup
                                                                               .SendGroupMessage($"哇奧色图不见了\r\n色图发送失败了\r\nAPI ERROR [{code}]");
                                                        }

                                                        return;
                                                    }

                                                    await eventArgs.SourceGroup
                                                                   .SendGroupMessage($"哇奧色图不见了\r\nAPI ERROR [可能是画师把图删了.jpg]");
                                                    Log.Error("Hso", "色图下载失败");
                                                    //删除下载失败的文件
                                                    try
                                                    {
                                                        file.Delete();
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        Log.Error("IO", Log.ErrorLogBuilder(e));
                                                    }
                                                };
                client.DownloadDataCompleted += async (sender, args) =>
                                                {
                                                    Log.Info("Hso", "下载数据成功,发送图片");
                                                    try
                                                    {
                                                        StringBuilder ImgBase64Str = new StringBuilder();
                                                        ImgBase64Str.Append("base64://");
                                                        ImgBase64Str.Append(Convert.ToBase64String(args.Result));
                                                        var (code, _) =
                                                            await eventArgs.SourceGroup
                                                                           .SendGroupMessage(HsoMessageBuilder(picInfo,
                                                                               cardImg,
                                                                               ImgBase64Str.ToString()));
                                                        if (code == APIStatusType.OK) Log.Info("Hso", "色图发送成功");
                                                        else
                                                        {
                                                            Log.Error("Hso", $"色图发送失败 code={(int) code}");
                                                            if (code != APIStatusType.TimeOut)
                                                                await eventArgs.SourceGroup
                                                                               .SendGroupMessage($"哇奧色图不见了\r\n色图发送失败了\r\nAPI ERROR [{code}]");
                                                        }

                                                        Log.Debug("base64 length", ImgBase64Str.Length);
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        Log.Error("pic send error", Log.ErrorLogBuilder(e));
                                                    }
                                                };
                if (useCache)
                    client.DownloadFileAsync(new Uri(url), receivePath);
                else
                    client.DownloadDataAsync(new Uri(url));
            }
            catch (Exception e)
            {
                Log.Error("色图下载失败", $"网络下载数据错误\n{Log.ErrorLogBuilder(e)}");
            }
        }

        /// <summary>
        /// 构建发送用的消息段
        /// </summary>
        /// <param name="picInfo">图片信息Json</param>
        /// <param name="cardImg">是否为装逼大图</param>
        /// <param name="picStr">图片路径/b64字符串</param>
        private List<CQCode> HsoMessageBuilder(JToken picInfo, bool cardImg, string picStr)
        {
            StringBuilder textBuilder = new StringBuilder();
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
            List<CQCode> msg = new();
            msg.Add(cardImg
                        ? CQCode.CQCardImage(picStr)
                        : CQCode.CQImage(picStr));
            msg.Add(CQCode.CQText(textBuilder.ToString()));
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