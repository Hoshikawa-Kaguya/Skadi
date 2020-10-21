using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sora.Tool;

namespace AntiRain.Network
{
    internal class HTTPUtils
    {
        #region HTTP工具
        /// <summary>
        /// GET请求
        /// </summary>
        /// <param name="url">URL</param>
        /// <param name="UA">UA</param>
        /// <param name="Timeout">超时</param>
        /// <returns></returns>
        public static string GetHttpResponse(string url, string UA = "Windows", int Timeout = 3000)
        {
            Dictionary<String, String> UAList = new Dictionary<String, String>
            {
                {
                    "Windows",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.121 Safari/537.36"
                },
                {
                    "IOS",
                    "Mozilla/5.0 (iPhone; CPU iPhone OS 8_3 like Mac OS X) AppleWebKit/600.1.4 (KHTML, like Gecko) Version/8.0 Mobile/12F70 Safari/600.1.4"
                },
                {
                    "Andorid",
                    "Mozilla/5.0 (Linux; Android 4.2.1; M040 Build/JOP40D) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.59 Mobile Safari/537.36"
                }
            };
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";
            request.UserAgent = UAList[UA];
            request.Timeout = Timeout;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();

            return retString;
        }

        /// <summary>
        /// 一般POST请求
        /// </summary>
        /// <param name="url">URL</param>
        /// <param name="parameters">PSOT数据</param>
        /// <param name="userAgent">UA</param>
        /// <param name="ContentType">内容类型</param>
        /// <param name="timeout">超时</param>
        /// <param name="cookies">cookies</param>
        /// <returns>String</returns>
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

            return GetResponseString(CreatePostHttpResponse(url, parameters, ref c, userAgent, ContentType,
                                                            timeout));
        }

        /// <summary>
        /// JSON_POST请求
        /// </summary>
        /// <param name="url">URL</param>
        /// <param name="parameters">PSOT数据</param>
        /// <param name="userAgent">UA</param>
        /// <param name="ContentType">内容类型</param>
        /// <param name="referer">referer</param>
        /// <param name="timeout">超时</param>
        /// <param name="CustomSource">自定义的来源</param>
        /// <param name="cookies">cookies</param>
        /// <returns>String</returns>
        public static String PostHttpResponse(string url, JObject parameters,
                                              string userAgent = "Windows",
                                              string ContentType = "application/x-www-form-urlencoded", string referer = null,
                                              int timeout = 3000, string CustomSource = null, CookieCollection cookies = null)
        {
            CookieContainer c = new CookieContainer();
            if (cookies != null)
            {
                c.Add(cookies);
            }

            return GetResponseString(CreatePostHttpResponse(url, parameters, ref c, userAgent, ContentType, referer,
                                                            timeout, CustomSource));
        }

        /// <summary>
        /// 创建POST方式的HTTP请求
        /// </summary>
        /// <param name="url">URL</param>
        /// <param name="parameters">PSOT数据</param>
        /// <param name="userAgent">UA</param>
        /// <param name="ContentType">内容类型</param>
        /// <param name="timeout">超时</param>
        /// <param name="cookies">cookies</param>
        /// <returns></returns>
        public static HttpWebResponse CreatePostHttpResponse(string url, IDictionary<string, string> parameters,
                                                             ref CookieContainer cookies, string userAgent = "Windows",
                                                             string ContentType = "application/x-www-form-urlencoded",
                                                             int timeout = 3000)
        {
            Dictionary<String, String> UAList = new Dictionary<String, String>
            {
                {
                    "Windows",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.121 Safari/537.36"
                },
                {
                    "IOS",
                    "Mozilla/5.0 (iPhone; CPU iPhone OS 8_3 like Mac OS X) AppleWebKit/600.1.4 (KHTML, like Gecko) Version/8.0 Mobile/12F70 Safari/600.1.4"
                },
                {
                    "Andorid",
                    "Mozilla/5.0 (Linux; Android 4.2.1; M040 Build/JOP40D) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.59 Mobile Safari/537.36"
                }
            };
            HttpWebRequest request;
            //如果是发送HTTPS请求  
            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                request = WebRequest.Create(url) as HttpWebRequest;
            }
            else
            {
                request = WebRequest.Create(url) as HttpWebRequest;
            }
            if(request == null) throw new NullReferenceException(nameof(request));

            request.Method = "POST";
            request.ContentType = ContentType;

            //设置代理UserAgent和超时
            request.UserAgent = UAList[userAgent];
            request.Timeout = timeout;

            request.CookieContainer = cookies;

            //发送POST数据  
            if (!(parameters == null || parameters.Count == 0))
            {
                StringBuilder buffer = new StringBuilder();
                int i = 0;
                foreach (string key in parameters.Keys)
                {
                    if (i > 0)
                    {
                        buffer.AppendFormat("&{0}={1}", WebUtility.UrlEncode(key),
                                            WebUtility.UrlEncode(parameters[key]));
                    }
                    else
                    {
                        buffer.AppendFormat("{0}={1}", WebUtility.UrlEncode(key),
                                            WebUtility.UrlEncode(parameters[key]));
                        i++;
                    }
                }

                byte[]       data   = Encoding.UTF8.GetBytes(buffer.ToString());
                using Stream stream = request.GetRequestStream();
                stream.Write(data, 0, data.Length);
            }

            return request.GetResponse() as HttpWebResponse;
        }

        /// <summary>
        /// 创建POST方式的HTTP请求
        /// </summary>
        /// <param name="url">URL</param>
        /// <param name="parameters">PSOT数据</param>
        /// <param name="userAgent">UA</param>
        /// <param name="ContentType">内容类型</param>
        /// <param name="referer">referer</param>
        /// <param name="timeout">超时</param>
        /// <param name="cookies">cookies</param>
        /// <param name="CustomSource">自定义的来源</param>
        /// <returns>HttpWebResponse</returns>
        public static HttpWebResponse CreatePostHttpResponse(string url, JObject parameters,
                                                             ref CookieContainer cookies, string userAgent = "Windows",
                                                             string ContentType = "application/x-www-form-urlencoded", string referer = null,
                                                             int timeout = 3000,string CustomSource = null)
        {
            Dictionary<String, String> UAList = new Dictionary<String, String>
            {
                {
                    "Windows",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.121 Safari/537.36"
                },
                {
                    "IOS",
                    "Mozilla/5.0 (iPhone; CPU iPhone OS 8_3 like Mac OS X) AppleWebKit/600.1.4 (KHTML, like Gecko) Version/8.0 Mobile/12F70 Safari/600.1.4"
                },
                {
                    "Andorid",
                    "Mozilla/5.0 (Linux; Android 4.2.1; M040 Build/JOP40D) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.59 Mobile Safari/537.36"
                }
            };
            HttpWebRequest request;
            //如果是发送HTTPS请求  
            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                request = WebRequest.Create(url) as HttpWebRequest;
            }
            else
            {
                request = WebRequest.Create(url) as HttpWebRequest;
            }
            if(request == null) throw new NullReferenceException(nameof(request));

            request.Method = "POST";
            request.ContentType = ContentType;

            //设置代理UserAgent和超时
            request.UserAgent = UAList[userAgent];
            request.Timeout = timeout;

            request.CookieContainer = cookies;
            request.Referer = referer ?? string.Empty;
            //镜华查询在不知道什么时候加了一个这个字段否则就403
            if (CustomSource != null)
            {
                request.Headers.Set("Custom-Source", CustomSource);
            }

            //发送POST数据  
            if (!(parameters == null || parameters.Count == 0))
            {
                string buffer = JsonConvert.SerializeObject(parameters);

                byte[]       data   = Encoding.UTF8.GetBytes(buffer);
                using Stream stream = request.GetRequestStream();
                stream.Write(data, 0, data.Length);
            }

            foreach (string requestHeader in request.Headers)
            {
                ConsoleLog.Debug("HTTP-Headers",$"{requestHeader}={request.Headers.GetValues(requestHeader)?[0]}");
            }

            return request.GetResponse() as HttpWebResponse;
        }


        /// <summary>
        /// 获取请求的数据
        /// </summary>
        public static string GetResponseString(HttpWebResponse webresponse)
        {
            using Stream s      = webresponse.GetResponseStream();
            StreamReader reader = new StreamReader(s, Encoding.UTF8);
            return reader.ReadToEnd();
        }
        #endregion
    }
}
