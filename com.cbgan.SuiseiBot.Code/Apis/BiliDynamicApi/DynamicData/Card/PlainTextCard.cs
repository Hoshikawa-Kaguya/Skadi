using Newtonsoft.Json.Linq;

namespace com.cbgan.SuiseiBot.Code.Apis.BiliDynamicApi.DynamicData.Card
{
    public class PlainTextCard : Dynamic
    {
        #region 属性
        /// <summary>
        /// <para>动态内容</para>
        /// <para>[字段:JSON.item.content]</para>
        /// </summary>
        private string Content { get; }
        #endregion

        #region 构造函数
        /// <summary>
        /// 从json转换为卡片子类
        /// </summary>
        /// <param name="root"></param>
        public PlainTextCard(JObject root)
        {
            //写入动态信息
            //父属性初始化
            InfoInit(root);
            //子属性
            Content = Card["item"]?["content"]?.ToString();
        }
        #endregion

        #region 公有方法
        /// <summary>
        /// 获取动态的文本
        /// </summary>
        /// <returns>将数据转换为含有CQ码的文本</returns>
        public override string ToString()
        {
            return EmojiToCQCode(Content);
        }
        #endregion
    }
}
