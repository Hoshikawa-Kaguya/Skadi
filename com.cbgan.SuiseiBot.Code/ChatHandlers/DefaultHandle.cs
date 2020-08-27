using System;
using System.Text;
using System.Threading.Tasks;
using com.cbgan.SuiseiBot.Code.Network;
using com.cbgan.SuiseiBot.Code.Resource.TypeEnum;
using com.cbgan.SuiseiBot.Code.Resource.TypeEnum.CmdType;
using com.cbgan.SuiseiBot.Code.Tool.Log;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.EventArgs;
using Newtonsoft.Json.Linq;

namespace com.cbgan.SuiseiBot.Code.ChatHandlers
{
    internal class DefaultHandle
    {
        #region 属性

        public object                  sender    { private set; get; }
        public CQGroupMessageEventArgs eventArgs { private set; get; }

        #endregion

        #region 构造函数

        public DefaultHandle(object sender, CQGroupMessageEventArgs e)
        {
            this.eventArgs = e;
            this.sender    = sender;
        }

        #endregion

        #region 消息响应函数
        /// <summary>
        /// 消息接收函数
        /// 并匹配相应指令
        /// </summary>
        /// <param name="keywordType"></param>
        public void GetChat(WholeMatchCmdType keywordType) //消息接收并判断是否响应
        {
            if (eventArgs == null || sender == null) return;
            switch (keywordType)
            {
                case WholeMatchCmdType.Debug:
                    Test();
                    break;
            }
        }
        #endregion

        #region DEBUG
        /// <summary>
        /// 响应函数
        /// </summary>
        public async void Test() //功能响应
        {
            //测试用代码
            await GiveMeSetu(SetuSourceType.Mix);
        }

        /// <summary>
        /// <para>从色图源获取色图</para>
        /// <para>不会支持R18的哦</para>
        /// </summary>
        /// <param name="setuSource">源类型</param>
        /// <param name="loliconToken">lolicon token</param>
        /// <param name="yukariToken">yukari token</param>
        private Task GiveMeSetu(SetuSourceType setuSource, string loliconToken = null, string yukariToken = null)
        {
            StringBuilder urlBuilder = new StringBuilder();
            //源选择
            switch (setuSource)
            {
                //混合源
                case SetuSourceType.Mix:
                    Random rand = new Random();
                    if (rand.Next(1, 100) > 50)
                    {
                        urlBuilder.Append("https://api.lolicon.app/setu/");
                        if (!string.IsNullOrEmpty(loliconToken)) urlBuilder.Append($"?token={loliconToken}");
                        ConsoleLog.Debug("色图源","Lolicon");
                    }
                    else
                    {
                        urlBuilder.Append("https://api.yukari.one/setu/");
                        if (!string.IsNullOrEmpty(yukariToken)) urlBuilder.Append($"?token={yukariToken}");
                        ConsoleLog.Debug("色图源", "Yukari");
                    }
                    break;
                //lolicon
                case SetuSourceType.Lolicon:
                    urlBuilder.Append("https://api.lolicon.app/setu/");
                    if (!string.IsNullOrEmpty(loliconToken)) urlBuilder.Append($"?token={loliconToken}");
                    ConsoleLog.Debug("色图源", "Lolicon");
                    break;
                //Yukari
                case SetuSourceType.Yukari:
                    urlBuilder.Append("https://api.yukari.one/setu/");
                    if (!string.IsNullOrEmpty(yukariToken)) urlBuilder.Append($"?token={yukariToken}");
                    ConsoleLog.Debug("色图源", "Yukari");
                    break;
            }
            string response;
            //网络部分
            try
            {
                ConsoleLog.Info("NET", "尝试获取色图");
                //eventArgs.FromGroup.SendGroupMessage("查询中...");
                response = HTTPUtils.GetHttpResponse(urlBuilder.ToString());
                ConsoleLog.Debug("Get Json",response);
                if (string.IsNullOrEmpty(response))
                {
                    ConsoleLog.Error("网络错误","获取到的响应数据为空");
                    eventArgs.FromGroup.SendGroupMessage("哇哦~发生了网络错误，请联系机器人所在服务器管理员");
                    return Task.CompletedTask;
                }
            }
            catch (Exception e)
            {
                eventArgs.FromGroup.SendGroupMessage("哇哦~发生了网络错误，请联系机器人所在服务器管理员");
                ConsoleLog.Error("网络发生错误", ConsoleLog.ErrorLogBuilder(e));
                return Task.CompletedTask;
            }
            //json处理
            try
            {
                JObject picJson = JObject.Parse(response);
                if ((int)picJson["code"] == 0)
                {
                    string picUrl = picJson["data"]?[0]?["url"]?.ToString();
                    if (!string.IsNullOrEmpty(picUrl)) eventArgs.FromGroup.SendGroupMessage(CQApi.Mirai_UrlImage(picUrl));
                    ConsoleLog.Debug("Setu Url", picUrl);
                }
                else
                {
                    eventArgs.FromGroup.SendGroupMessage("哇哦~没有色图了，请联系机器人所在服务器管理员");
                    ConsoleLog.Warning("Setu API Token",$"图片源Token可能已经失效,目前色图模式为{setuSource}");
                    return Task.CompletedTask;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            return Task.CompletedTask;
        }
        #endregion
    }
}