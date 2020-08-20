namespace com.cbgan.SuiseiBot.Code.Apis.BiliDynamicApi.DynamicData
{
    public class UserInfo
    {
        /// <summary>
        /// <para>发送者ID</para>
        /// <para>[字段:JSON.data.cards[n].desc.uid]</para>
        /// </summary>
        public long Uid { set; get; }
        /// <summary>
        /// <para>动态所属用户名称</para>
        /// <para>[字段:JSON.data.cards[n].desc.user_profile.info.uname]</para>
        /// </summary>
        public string UserName { set; get; }
        /// <summary>
        /// <para>用户头像的图片链接</para>
        /// <para>[字段:JSON.data.cards[n].desc.user_profile.info.face]</para>
        /// </summary>
        public string FaceUrl { set; get; }
    }
}
