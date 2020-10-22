using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AntiRain.IO;
using AntiRain.IO.Config;
using AntiRain.IO.Config.ConfigModule;
using AntiRain.Network;
using AntiRain.Resource.TypeEnum;
using Newtonsoft.Json.Linq;
using Sora.Entities.CQCodes;
using Sora.EventArgs.SoraEvent;
using Sora.Tool;
using Group = Sora.Entities.Group;

namespace AntiRain.ChatModule.HsoModule
{
    internal class HsoHandle
    {
        #region 属性
        public object                  Sender       { private set; get; }
        public Group                   QQGroup      { private set; get; }
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
            string localPicPath;
            string response;
            StringBuilder urlBuilder = new StringBuilder();
            ConsoleLog.Debug("源",hso.Source);
            //源选择
            switch (hso.Source)
            {
                //混合源
                case SetuSourceType.Mix:
                    Random randSource = new Random();
                    if (randSource.Next(1, 100) > 50)
                    {
                        urlBuilder.Append("https://api.lolicon.app/setu/");
                        if (!string.IsNullOrEmpty(hso.LoliconToken)) urlBuilder.Append($"?token={hso.LoliconToken}");
                        ConsoleLog.Debug("色图源","Lolicon");
                    }
                    else
                    {
                        urlBuilder.Append("https://api.yukari.one/setu/");
                        if (!string.IsNullOrEmpty(hso.YukariToken)) urlBuilder.Append($"?token={hso.YukariToken}");
                        ConsoleLog.Debug("色图源", "Yukari");
                    }
                    break;
                //lolicon
                case SetuSourceType.Lolicon:
                    urlBuilder.Append("https://api.lolicon.app/setu/");
                    if (!string.IsNullOrEmpty(hso.LoliconToken)) urlBuilder.Append($"?token={hso.LoliconToken}");
                    ConsoleLog.Debug("色图源", "Lolicon");
                    break;
                //Yukari
                case SetuSourceType.Yukari:
                    urlBuilder.Append("https://api.yukari.one/setu/");
                    if (!string.IsNullOrEmpty(hso.YukariToken)) urlBuilder.Append($"?token={hso.YukariToken}");
                    ConsoleLog.Debug("色图源", "Yukari");
                    break;
                case SetuSourceType.Local:
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
                response = HTTPUtils.GetHttpResponse(urlBuilder.ToString());
                ConsoleLog.Debug("Get Json",response);
                if (string.IsNullOrEmpty(response))//没有获取到任何返回
                {
                    ConsoleLog.Error("网络错误","获取到的响应数据为空");
                    await HsoEventArgs.SourceGroup.SendGroupMessage("哇哦~发生了网络错误，请联系机器人所在服务器管理员");
                    return;
                }
            }
            catch (Exception e)
            {
                //网络错误
                await QQGroup.SendGroupMessage("哇哦~发生了网络错误，请联系机器人所在服务器管理员");
                ConsoleLog.Error("网络发生错误", ConsoleLog.ErrorLogBuilder(e));
                return;
            }
            //json处理
            try
            {
                JObject picJson = JObject.Parse(response);
                if ((int)picJson["code"] == 0)
                {
                    //图片链接
                    string picUrl        = picJson["data"]?[0]?["url"]?.ToString() ?? "";
                    ConsoleLog.Debug("获取到图片",picUrl);
                    //本地图片存储路径
                    localPicPath = $"{IOUtils.GetHsoPath()}/{Path.GetFileName(picUrl)}";
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
                if (((int) picJson["code"] == 401 || (int) picJson["code"] == 429) &&
                    hso.Source == SetuSourceType.Lolicon) 
                    ConsoleLog.Warning("API Token 失效",$"code:{picJson["code"]}");
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
                                                    await QQGroup.SendGroupMessage(cardImg
                                                                                 ? CQCode.CQCardImage(receivePath.Replace('/','\\'))
                                                                                 : CQCode.CQImage(receivePath.Replace('/','\\')));
                                                    ConsoleLog.Debug("file",Path.GetFileName(receivePath));
                                                };
                client.DownloadDataCompleted += async (sender, args) =>
                                                {
                                                    ConsoleLog.Info("Hso","下载数据成功,发送图片");
                                                    try
                                                    {
                                                        StringBuilder ImgBase64Str = new StringBuilder();
                                                        ImgBase64Str.Append("base64://");
                                                        ImgBase64Str.Append(Convert.ToBase64String(args.Result));
                                                        await QQGroup.SendGroupMessage(cardImg
                                                            ? CQCode.CQCardImage(ImgBase64Str.ToString())
                                                            : CQCode.CQImage(ImgBase64Str.ToString()));
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
