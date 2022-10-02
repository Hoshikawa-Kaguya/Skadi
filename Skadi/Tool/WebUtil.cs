using BeetleX.FastHttpApi;

namespace Skadi.Tool;

public static class WebUtil
{
    /// <summary>
    /// API数据返回构造器
    /// </summary>
    /// <param name="data">数据数组</param>
    /// <param name="code">返回代码</param>
    /// <param name="message">API消息</param>
    public static JsonResult GenResult(object data, int code = 0, string message = "OK")
    {
        return new JsonResult(new
                              {
                                  code,
                                  message,
                                  data
                              },
                              true);
    }

    /// <summary>
    /// 检查请求来源
    /// </summary>
    /// <param name="context">请求信息</param>
    /// <param name="expectAddress">期望地址</param>
    public static bool CheckRequestAddress(IHttpContext context, string expectAddress = "127.0.0.1")
    {
        return context.Request.RemoteIPAddress.Equals(expectAddress);
    }
}