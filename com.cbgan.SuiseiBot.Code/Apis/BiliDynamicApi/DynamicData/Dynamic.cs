using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.cbgan.SuiseiBot.Code.Apis.BiliDynamicApi.CardEnum;
using Newtonsoft.Json.Linq;

namespace com.cbgan.SuiseiBot.Code.Apis.BiliDynamicApi.DynamicData
{
    /// <summary>
    /// 动态数据类型
    /// </summary>
    public class Dynamic
    {
        #region 属性
        /// <summary>
        /// <para>发送者ID</para>
        /// <para>[字段:JSON.data.cards[n].desc.uid]</para>
        /// </summary>
        protected long Uid { set; get; }
        /// <summary>
        /// <para>动态的ID</para>
        /// <para>[字段:JSON.data.cards[n].desc.dynamic_id_str]</para>
        /// <para>这个值实在是太长了就用字符串存吧</para>
        /// </summary>
        protected string DynamicId { set; get; }
        /// <summary>
        /// <para>动态更新时间</para>
        /// <para>[需要由时间戳转换]</para>
        /// </summary>
        public DateTime UpdateTime { private set; get; }
        /// <summary>
        /// <para>动态更新时间戳</para>
        /// <para>[字段:JSON.data.cards[n].desc.timestamp]</para>
        /// <para>仅用于设置时间，会自动转换为UpdateTime[DateTime]</para>
        /// </summary>
        protected long UpdateTimeStemp {
            set => UpdateTime = DateTimeOffset
                                .FromUnixTimeSeconds(value).AddHours(8).DateTime;
        }
        /// <summary>
        /// <para>动态所属用户名称</para>
        /// <para>[字段:JSON.data.cards[n].desc.user_profile.info.uname]</para>
        /// </summary>
        protected string UserName { set; get; }
        /// <summary>
        /// <para>用户头像的图片链接</para>
        /// <para>[字段:JSON.data.cards[n].desc.user_profile.info.face]</para>
        /// </summary>
        protected string FaceUrl { set; get; }
        /// <summary>
        /// <para>动态中Emoji的数据</para>
        /// <para>Key:EmojiText[JSON.data.cards[0].display.emoji_info.emoji_details[n].text]</para>
        /// <para>Value:EmojiUrl[JSON.data.cards[0].display.emoji_info.emoji_details[n].url]</para>
        /// </summary>
        protected Dictionary<string,string> EmojiData { set; get; }
        /// <summary>
        /// <para>动态类型</para>
        /// <para>[字段:JSON.data.cards[0].desc.type]</para>
        /// </summary>
        protected CardType CardType { set; get; }
        /// <summary>
        /// <para>动态内容</para>
        /// <para>[字段:JSON.data.cards[n].card]</para>
        /// </summary>
        protected JObject Card { set; get; }
        /// <summary>
        /// <para>额外信息</para>
        /// <para>[字段:JSON.data.cards[n].extend_json]</para>
        /// </summary>
        protected JObject ExtendJson { set; get; }
        #endregion

        #region 公有方法
        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <returns>用户信息</returns>
        public UserInfo GetUserInfo()
        {
            return new UserInfo
            {
                Uid      = this.Uid,
                UserName = this.UserName,
                FaceUrl  = this.FaceUrl
            };
        }

        public string GetDynamicUrl()
        {
            return $"https://t.bilibili.com/{DynamicId}";
        }
        #endregion

        #region 类方法
        /// <summary>
        /// 将文本中的emoji码替换为CQ码[Mirai扩展]
        /// </summary>
        /// <param name="text">消息文本</param>
        /// <returns>转换后文本</returns>
        protected string EmojiToCQCode(string text)
        {
            string ret = text;
            if (this.EmojiData.Count == 0) return ret;
            foreach (KeyValuePair<string, string> keyValuePair in this.EmojiData)
            {
                StringBuilder cqCodeBuilder = new StringBuilder();
                cqCodeBuilder.Append("[CQ:image,url=");
                cqCodeBuilder.Append(keyValuePair.Value);
                cqCodeBuilder.Append("]");
                ret = ret.Replace(keyValuePair.Key, cqCodeBuilder.ToString());
            }
            return ret;
        }
        /// <summary>
        /// 将网络图片链接转换为CQ码[Mirai扩展]
        /// </summary>
        /// <param name="url">图片链接</param>
        /// <returns>CQ码</returns>
        protected string ImgUrlToCQCode(string url)
        {
            StringBuilder cqCodeBuilder = new StringBuilder();
            cqCodeBuilder.Append("[CQ:image,url=");
            cqCodeBuilder.Append(url);
            cqCodeBuilder.Append("]");
            return cqCodeBuilder.ToString();
        }
        /// <summary>
        /// 初始化父数据
        /// </summary>
        /// <param name="root"></param>
        protected void InfoInit(JObject root)
        {
            Dictionary<string, string> emojiData = new Dictionary<string, string>();
            //判断是否存在emoji
            JToken[] emojis = root["display"]?["emoji_info"]?["emoji_details"]?.ToArray();
            if (emojis        != null &&
                emojis.Length != 0)
            {
                int unknownCount = 0;
                //写入emoji信息
                foreach (JToken emoji in emojis)
                {
                    emojiData.Add(emoji["text"]?.ToString() ?? $"unknown{unknownCount++}", emoji["url"]?.ToString());
                }
            }
            //写入数据
            Uid             = (long)root["desc"]?["uid"];
            DynamicId       = root["desc"]?["dynamic_id_str"]?.ToString();
            UpdateTimeStemp = (long)root["desc"]?["timestamp"];
            UserName        = root["desc"]?["user_profile"]?["info"]?["uname"]?.ToString();
            FaceUrl         = root["desc"]?["user_profile"]?["info"]?["face"]?.ToString();
            EmojiData       = emojiData;
            CardType        = (CardType)((int)root["desc"]?["type"]);
            Card            = JObject.Parse(root["card"]?.ToString()        ?? "{}");
            ExtendJson      = JObject.Parse(root["extend_json"]?.ToString() ?? "{}");
        }
        #endregion
    }
}
