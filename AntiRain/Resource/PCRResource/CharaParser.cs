using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AntiRain.DatabaseUtils;
using AntiRain.DatabaseUtils.Helpers.PCRDataDB;
using YukariToolBox.FormatLog;

namespace AntiRain.Resource.PCRResource
{
    /// <summary>
    /// 角色别名处理
    /// </summary>
    internal class CharaParser
    {
        #region 属性

        private CharaDBHelper CharaDBHelper { get; }

        #endregion

        #region 构造函数

        internal CharaParser()
        {
            this.CharaDBHelper = new CharaDBHelper();
        }

        #endregion

        #region 公有方法

        /// <summary>
        /// <para>从云端获取数据更新</para>
        /// <para>会覆盖原有数据</para>
        /// </summary>
        internal async ValueTask<bool> UpdateCharaNameByCloud()
        {
            string res;
            //从服务器获取数据
            Log.Info("角色数据更新", "尝试从云端获取更新");
            try
            {
                HttpClient client = new HttpClient
                {
                    //设置超时
                    Timeout = TimeSpan.FromSeconds(5)
                };
                //下载信息
                res = await client.GetStringAsync("https://api.yukari.one/pcr/unit_data.py");
                // res = Requests.Get("https://api.yukari.one/pcr/unit_data.py",
                //                    new ReqParams {Timeout = 5000});
            }
            catch (Exception e)
            {
                Log.Error("角色数据更新", $"发生了网络错误\n{PyLibSharp.Requests.Utils.GetInnerExceptionMessages(e)}");
                return false;
            }

            //python字典数据匹配正则
            Regex PyDict = new(@"\d+:\s*\[\"".+\""\],", RegexOptions.IgnoreCase);

            //匹配所有名称数据
            MatchCollection DictMatchRes = PyDict.Matches(res.Substring(res.IndexOf('{'),
                                                                        res.IndexOf('}') - res.IndexOf('{') + 1));
            Log.Info("角色数据更新", $"角色数据获取成功({DictMatchRes.Count})");
            //名称数据列表
            List<PCRChara> chareNameList = new();
            //处理匹配结果
            foreach (Match chara in DictMatchRes)
            {
                string[] charaData = chara.Value.Split(':');
                //角色ID
                int.TryParse(charaData[0], out int charaId);
                //角色名
                string charaNames = charaData[1].Substring(2, charaData[1].Length - 4)
                                                .Replace("\"", string.Empty)
                                                .Replace(" ", string.Empty);
                //添加至列表
                chareNameList.Add(new PCRChara
                {
                    CharaId = charaId,
                    Name    = charaNames
                });
            }

            return CharaDBHelper.UpdateCharaData(chareNameList);
        }

        /// <summary>
        /// 查找角色
        /// </summary>
        /// <param name="keyWord">关键词</param>
        internal PCRChara FindChara(string keyWord)
            => CharaDBHelper.FindChara(keyWord);

        /// <summary>
        /// 查找角色
        /// </summary>
        /// <param name="charaId">id</param>
        internal PCRChara FindChara(int charaId)
            => CharaDBHelper.FindChara(charaId);

        #endregion
    }
}