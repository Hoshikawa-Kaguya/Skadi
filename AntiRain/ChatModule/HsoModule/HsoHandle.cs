using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AntiRain.IO;
using AntiRain.IO.Config;
using AntiRain.IO.Config.ConfigModule;
using AntiRain.Tool;
using Newtonsoft.Json.Linq;
using PyLibSharp.Requests;
using Sora.Entities.CQCodes;
using Sora.Enumeration.ApiType;
using Sora.EventArgs.SoraEvent;
using Sora.Tool;
using Group = Sora.Entities.Group;

namespace AntiRain.ChatModule.HsoModule
{
    internal class HsoHandle
    {
        #region 属性
        public object                Sender       { private set; get; }
        public Group                 QQGroup      { private set; get; }
        public GroupMessageEventArgs HsoEventArgs { private set; get; }
        #endregion

        #region 构造函数
        public HsoHandle(object sender, GroupMessageEventArgs e)
        {
            this.HsoEventArgs = e;
            this.Sender       = sender;
            this.QQGroup      = e.SourceGroup;
        }
        #endregion

        #region 指令响应分发
        /// <summary>
        /// 用于处理传入指令
        /// </summary>
        public async void GetChat()
        {
            Config config = new Config(HsoEventArgs.LoginUid);
            config.LoadUserConfig(out UserConfig userConfig);
            if (CheckInCD.isInCD(HsoEventArgs.SourceGroup, HsoEventArgs.Sender))
            {
                await HsoEventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(HsoEventArgs.Sender), "你是不是只会要色图");
                return;
            }
            //检查色图文件夹大小
            if (IOUtils.GetHsoSize() >= userConfig.HsoConfig.SizeLimit * 1024 * 1024)
            {
                ConsoleLog.Warning("Hso","色图文件夹超出大小限制，将清空文件夹");
                Directory.Delete(IOUtils.GetHsoPath(),true);
            }
            await GiveMeSetu(userConfig.HsoConfig);
        }
        #endregion

        #region 私有方法

        /// <summary>
        /// <para>从色图源获取色图</para>
        /// <para>不会支持R18的哦</para>
        /// </summary>
        /// <param name="hso">hso配置实例</param>
        private async Task GiveMeSetu(Hso hso)
        {
            string  localPicPath;
            JToken response;
            ConsoleLog.Debug("源",hso.Source);
            //本地模式
            if (hso.Source.Equals("Local"))
            {
                string[] picNames = Directory.GetFiles(IOUtils.GetHsoPath());
                if (picNames.Length == 0)
                {
                    await QQGroup.SendGroupMessage("机器人管理者没有在服务器上塞色图\r\n你去找他要啦!");
                    return;
                }
                Random randFile = new Random();
                localPicPath = $"{picNames[randFile.Next(0, picNames.Length - 1)]}";
                ConsoleLog.Debug("发送图片",localPicPath);
                await QQGroup.SendGroupMessage(hso.CardImage
                                                   ? CQCode.CQCardImage(localPicPath)
                                                   : CQCode.CQImage(localPicPath));
                return;
            }
            //网络部分
            try
            {
                ConsoleLog.Info("NET", "尝试获取色图");
                await QQGroup.SendGroupMessage("正在获取色图中...");
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
                ConsoleLog.Debug("hso api server",serverUrl);
                ReqResponse reqResponse = await Requests.GetAsync(serverUrl, new ReqParams
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
                    ConsoleLog.Error("Net",$"{serverUrl} return code {(int)reqResponse.StatusCode}");
                    await HsoEventArgs.SourceGroup.SendGroupMessage($"哇哦~发生了网络错误[{reqResponse.StatusCode}]，请联系机器人所在服务器管理员");
                    return;
                }
                response = JToken.Parse(reqResponse.Text);
                ConsoleLog.Debug("Get Json",response);
            }
            catch (Exception e)
            {
                //网络错误
                await QQGroup.SendGroupMessage("哇哦~发生了网络错误，请联系机器人所在服务器管理员");
                ConsoleLog.Error("网络发生错误", $"{ConsoleLog.ErrorLogBuilder(e)}\r\n\r\n{PyLibSharp.Requests.Utils.GetInnerExceptionMessages(e)}");
                return;
            }
            //json处理
            try
            {
                if ((int)response["code"] == 0)
                {
                    //图片链接
                    string picUrl        = response["data"]?[0]?["url"]?.ToString() ?? "";
                    ConsoleLog.Debug("获取到图片",picUrl);
                    //本地图片存储路径
                    localPicPath = $"{IOUtils.GetHsoPath()}/{Path.GetFileName(picUrl)}".Replace('\\','/');
                    if (File.Exists(localPicPath)) //检查是否已缓存过图片
                    {
                        await QQGroup.SendGroupMessage(hso.CardImage
                                                     ? CQCode.CQCardImage(localPicPath)
                                                     : CQCode.CQImage(localPicPath));
                    }
                    else
                    {
                        //文件名处理(mirai发送网络图片时pixivcat会返回403暂时无法使用代理发送图片
                        //QQGroup.SendGroupMessage(CQApi.Mirai_UrlImage(picUrl));

                        //检查是否有设置代理
                        if (!string.IsNullOrEmpty(hso.PximyProxy))
                        {
                            string[] fileNameArgs = Regex.Split(Path.GetFileName(picUrl), "_p");
                            StringBuilder proxyUrlBuilder = new StringBuilder();
                            proxyUrlBuilder.Append(hso.PximyProxy);
                            //图片Pid部分
                            proxyUrlBuilder.Append(hso.PximyProxy.EndsWith("/") ? $"{fileNameArgs[0]}" : $"/{fileNameArgs[0]}");
                            //图片Index部分
                            proxyUrlBuilder.Append(fileNameArgs[1].Split('.')[0].Equals("0") ? string.Empty : $"/{fileNameArgs[1].Split('.')[0]}");
                            ConsoleLog.Debug("Get Proxy Url",proxyUrlBuilder);
                            DownloadFileFromURL(proxyUrlBuilder.ToString(), localPicPath, hso.UseCache, hso.CardImage);
                        }
                        else
                        {
                            DownloadFileFromURL(picUrl, localPicPath, hso.UseCache, hso.CardImage);
                        }
                    }
                    return;
                }
                if (((int) response["code"] == 401 || (int) response["code"] == 429) &&
                    hso.Source.Equals("Lolicon")) 
                    ConsoleLog.Warning("API Token 失效",$"code:{response["code"]}");
                else
                    ConsoleLog.Warning("没有找到图片信息","服务器拒绝提供信息");
                await QQGroup.SendGroupMessage("哇奧色图不见了\n请联系机器人服务器管理员");
            }
            catch (Exception e)
            {
                ConsoleLog.Error("色图下载失败", $"网络下载数据错误\n{ConsoleLog.ErrorLogBuilder(e)}");
            }
        }

        /// <summary>
        /// 下载图片保存到本地
        /// </summary>
        /// <param name="url">目标URL</param>
        /// <param name="receivePath">接收文件的地址</param>
        /// <param name="useCache">是否启用本地缓存</param>
        /// <param name="cardImg">使用装逼大图</param>
        private void DownloadFileFromURL(string url, string receivePath, bool useCache, bool cardImg)
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
                                                      ConsoleLog
                                                          .Debug("Download Pic",$"Downloading {args.ProgressPercentage}% " +
                                                                 $"({(args.BytesReceived - bytesReceived) / 1024.0 / ((DateTime.Now - flashTime).TotalMilliseconds / 1000)}KB/s) ");
                                                      flashTime     = DateTime.Now;
                                                      bytesReceived = args.BytesReceived;
                                                  };
                //文件下载完成
                client.DownloadFileCompleted += async (sender, args) =>
                                                {
                                                    ConsoleLog.Info("Hso","下载数据成功,发送图片");
                                                    var (code, _) = await QQGroup.SendGroupMessage(cardImg
                                                        ? CQCode.CQCardImage(receivePath)
                                                        : CQCode.CQImage(receivePath));
                                                    ConsoleLog.Debug("file",Path.GetFileName(receivePath));
                                                    if(code == APIStatusType.OK) ConsoleLog.Info("Hso","色图发送成功");
                                                    else
                                                    {
                                                        ConsoleLog.Error("Hso", $"色图发送失败 code={(int) code}");
                                                        if(code != APIStatusType.TimeOut)
                                                            await QQGroup.SendGroupMessage($"哇奧色图不见了\r\n色图发送失败了\r\nAPI ERROR [{code}]");
                                                    }
                                                    //检查是否出现空文件
                                                    if (code == APIStatusType.OK || !File.Exists(receivePath)) return;
                                                    FileInfo file = new FileInfo(receivePath);
                                                    ConsoleLog.Debug("File Size Check", file.Length);
                                                    if (file.Length != 0) return;
                                                    ConsoleLog.Error("Hso", "色图下载失败");
                                                    //删除下载失败的文件
                                                    try
                                                    {
                                                        file.Delete();
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        ConsoleLog.Error("IO",ConsoleLog.ErrorLogBuilder(e));
                                                    }
                                                };
                client.DownloadDataCompleted += async (sender, args) =>
                                                {
                                                    ConsoleLog.Info("Hso","下载数据成功,发送图片");
                                                    try
                                                    {
                                                        StringBuilder ImgBase64Str = new StringBuilder();
                                                        ImgBase64Str.Append("base64://");
                                                        ImgBase64Str.Append(Convert.ToBase64String(args.Result));
                                                        var (code, _) = await QQGroup.SendGroupMessage(cardImg
                                                            ? CQCode.CQCardImage(ImgBase64Str.ToString())
                                                            : CQCode.CQImage(ImgBase64Str.ToString()));
                                                        if(code == APIStatusType.OK) ConsoleLog.Info("Hso","色图发送成功");
                                                        else
                                                        {
                                                            ConsoleLog.Error("Hso", $"色图发送失败 code={(int) code}");
                                                            if(code != APIStatusType.TimeOut)
                                                                await QQGroup.SendGroupMessage($"哇奧色图不见了\r\n色图发送失败了\r\nAPI ERROR [{code}]");
                                                        }
                                                        ConsoleLog.Debug("base64 length",ImgBase64Str.Length);
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        Console.WriteLine(e);
                                                    }
                                                };
                if(useCache)
                    client.DownloadFileAsync(new Uri(url), receivePath);
                else
                    client.DownloadDataAsync(new Uri(url));
            }
            catch (Exception e)
            {
                ConsoleLog.Error("色图下载失败",$"网络下载数据错误\n{ConsoleLog.ErrorLogBuilder(e)}");
            }
        }
        #endregion
    }
}
