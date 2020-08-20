using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace com.cbgan.SuiseiBot.Code.Apis.BiliDynamicApi.DynamicData.Card
{
    public class TextAndPicCard : Dynamic
    {
        #region 属性
        /// <summary>
        /// <para>动态内容</para>
        /// <para>[字段:JSON.item.description]</para>
        /// </summary>
        private string Description { set; get; }
        /// <summary>
        /// <para>图片链接</para>
        /// <para>[字段:JSON.item.pictures]</para>
        /// </summary>
        private List<string> ImgList { set; get; }
        #endregion

        #region 构造方法
        public TextAndPicCard(JObject root)
        {
            //写入动态信息
            //父属性初始化
            InfoInit(root);
            //描述
            Description = Card["item"]?["description"]?.ToString();
            //图片
            ImgList = new List<string>();
            JToken[] pictures = Card["item"]?["pictures"]?.ToArray() ?? JArray.Parse("[]").ToArray();
            foreach (JToken url in pictures)
            {
                ImgList.Add(url["img_src"]?.ToString() ?? "");
            }
        }
        #endregion

        #region 公有方法
        /// <summary>
        /// 获取动态的文本
        /// </summary>
        /// <returns>将数据转换为含有CQ码的文本</returns>
        public override string ToString()
        {
            StringBuilder messageBuilder = new StringBuilder();
            messageBuilder.Append(EmojiToCQCode(Description));
            messageBuilder.Append('\n');
            foreach (string url in ImgList)
            {
                messageBuilder.Append(ImgUrlToCQCode(url));
                messageBuilder.Append('\n');
            }
            return messageBuilder.ToString();
        }
        #endregion
    }
}
