using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace com.cbgan.SuiseiBot.Code.Tool
{
    class Utils
    {
        public static string GetHttpResponse(string url, string UA = "Windows", int Timeout = 3000)
        {
            Dictionary<String, String> UAList = new Dictionary<String, String>();
            UAList.Add("Windows",
                       "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.121 Safari/537.36");
            UAList.Add("IOS",
                       "Mozilla/5.0 (iPhone; CPU iPhone OS 8_3 like Mac OS X) AppleWebKit/600.1.4 (KHTML, like Gecko) Version/8.0 Mobile/12F70 Safari/600.1.4");
            UAList.Add("Andorid",
                       "Mozilla/5.0 (Linux; Android 4.2.1; M040 Build/JOP40D) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.59 Mobile Safari/537.36");
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
            request.Method      = "GET";
            request.ContentType = "text/html;charset=UTF-8";
            request.UserAgent   = UAList[UA];
            request.Timeout     = Timeout;

            HttpWebResponse response         = (HttpWebResponse) request.GetResponse();
            Stream          myResponseStream = response.GetResponseStream();
            StreamReader    myStreamReader   = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string          retString        = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();

            return retString;
        }

        public static String PostHttpResponse(string url, IDictionary<string, string> parameters,
                                              string userAgent = "Windows",
                                              string ContentType = "application/x-www-form-urlencoded",
                                              int timeout = 3000, CookieCollection cookies = null)
        {
            CookieContainer c = new CookieContainer();
            if (cookies != null)
            {
                c.Add(cookies);
            }

            return GetResponseString(CreatePostHttpResponse(url, parameters, ref c, userAgent, ContentType, timeout));
        }

        /// 创建POST方式的HTTP请求  
        public static HttpWebResponse CreatePostHttpResponse(string url, IDictionary<string, string> parameters,
                                                             ref CookieContainer cookies, string userAgent = "Windows",
                                                             string ContentType = "application/x-www-form-urlencoded",
                                                             int timeout = 3000)
        {
            Dictionary<String, String> UAList = new Dictionary<String, String>();
            UAList.Add("Windows",
                       "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.121 Safari/537.36");
            UAList.Add("IOS",
                       "Mozilla/5.0 (iPhone; CPU iPhone OS 8_3 like Mac OS X) AppleWebKit/600.1.4 (KHTML, like Gecko) Version/8.0 Mobile/12F70 Safari/600.1.4");
            UAList.Add("Andorid",
                       "Mozilla/5.0 (Linux; Android 4.2.1; M040 Build/JOP40D) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.59 Mobile Safari/537.36");
            HttpWebRequest request = null;
            //如果是发送HTTPS请求  
            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                request = WebRequest.Create(url) as HttpWebRequest;
            }
            else
            {
                request = WebRequest.Create(url) as HttpWebRequest;
            }

            request.Method      = "POST";
            request.ContentType = ContentType;

            //设置代理UserAgent和超时
            request.UserAgent = UAList[userAgent];
            request.Timeout   = timeout;

            request.CookieContainer = cookies;

            //发送POST数据  
            if (!(parameters == null || parameters.Count == 0))
            {
                StringBuilder buffer = new StringBuilder();
                int           i      = 0;
                foreach (string key in parameters.Keys)
                {
                    if (i > 0)
                    {
                        buffer.AppendFormat("&{0}={1}", key, parameters[key]);
                    }
                    else
                    {
                        buffer.AppendFormat("{0}={1}", key, parameters[key]);
                        i++;
                    }
                }

                byte[] data = Encoding.ASCII.GetBytes(buffer.ToString());
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
            }

            string[] values = request.Headers.GetValues("Content-Type");
            return request.GetResponse() as HttpWebResponse;
        }


        /// <summary>
        /// 获取请求的数据
        /// </summary>
        public static string GetResponseString(HttpWebResponse webresponse)
        {
            using (Stream s = webresponse.GetResponseStream())
            {
                StreamReader reader = new StreamReader(s, Encoding.UTF8);
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// 打开控制台
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AllocConsole();

        /// <summary>
        /// 获取当前时间戳
        /// </summary>
        public static Func<long> GetNowTimeStamp = () => (DateTime.Now - new DateTime(1970, 1, 1, 8, 0, 0, 0)).Ticks;

        /// <summary>
        /// 获取今天零点的时间戳
        /// </summary>
        public static Func<long> GetTodayStamp = () => (DateTime.Today - new DateTime(1970, 1, 1, 8, 0, 0, 0)).Ticks;

        /// <summary>
        /// 将long类型时间戳转换为DateTime
        /// </summary>
        public static Func<long, System.DateTime> TimeStampToDateTime = (TimeStamp) => new System.DateTime(1970, 1, 1, 8, 0, 0, 0).AddTicks(TimeStamp);

        /// <summary>
        /// 将DateTime转换为long时间戳
        /// </summary>
        public static Func<System.DateTime, long> DateTimeToTimeStamp = dateTime => (dateTime - (new System.DateTime(1970, 1, 1, 8, 0, 0, 0))).Ticks;
    }
}