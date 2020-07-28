using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.cbgan.SuiseiBot.Code.Tool
{
    class JsonUtils
    {
        #region 进行Json文件的数据解析
        /// <summary>
        /// 在JObject中找到KeyNames所有字段的数据，并以string数组形式返回
        /// </summary>
        /// <param name="jsonData"></param>
        /// <param name="keyNames"></param>
        /// <returns></returns>
        public static string[] GetKeysData(JObject jsonData,string[] keyNames)
        {
            try
            {
                List<string> resultData = new List<string>();
                foreach(var key in keyNames)
                {
                    resultData.Add(jsonData[key]?.ToString());
                }
                return resultData.ToArray();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// 在JObject中找到KeyName字段数据
        /// </summary>
        /// <param>输入的JObject
        ///     <name>jsonData</name>
        /// </param>
        /// <param>目标字段
        ///     <name>keyName</name>
        /// </param>
        /// <returns>返回目标字段字符串</returns>
        public static Func<JObject,string, string> GetKeyData = (jsonData, keyName) => jsonData[keyName]?.ToString();

        /// <summary>
        /// 将string转为JObject
        /// </summary>
        /// <param>输入的string
        ///     <name>jsonString</name>
        /// </param>
        /// <returns>返回一个JObject</returns>
        public static Func<string,JObject> ConvertJson = (jsonString) => (JObject)JsonConvert.DeserializeObject(jsonString);
        #endregion
    }
}
