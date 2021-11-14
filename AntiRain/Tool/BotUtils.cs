using AntiRain.IO;
using AntiRain.TypeEnum;
using PyLibSharp.Requests;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Sora.Entities;
using Sora.Entities.Segment;
using Sora.EventArgs.SoraEvent;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using YukariToolBox.FormatLog;
using Font = AntiRain.Resource.Font;

namespace AntiRain.Tool;

internal static class BotUtils
{
    #region 时间戳处理

    /// <summary>
    /// 获取游戏刷新的时间戳
    /// 时间戳单位(秒)
    /// </summary>
    public static long GetPcrUpdateStamp()
    {
        if (DateTime.Now > DateTime.Today.Add(new TimeSpan(5, 0, 0)))
            return (long) (DateTime.Today - new DateTime(1970, 1, 1, 8, 0, 0, 0)).Add(new TimeSpan(5, 0, 0))
                .TotalSeconds;
        else
            return (long) (DateTime.Today.AddDays(-1) - new DateTime(1970, 1, 1, 8, 0, 0, 0))
                          .Add(new TimeSpan(5, 0, 0)).TotalSeconds;
    }

    #endregion

    #region 字符串处理

    /// <summary>
    /// 获取字符串在QQ上显示的长度（用于PadQQ函数）
    /// </summary>
    /// <param name="input">要计算长度的字符串</param>
    /// <returns>长度（不要问为啥是Double，0.5个字符真的存在）</returns>
    public static double GetQQStrLength(string input)
    {
        double strLength = 0;
        foreach (var i in input)
            if (char.IsLetter(i))
                strLength += 2.5;
            else if (char.IsNumber(i))
                strLength += 2;
            else if (char.IsSymbol(i))
                strLength += 2;
            else
                strLength += 3;

        return strLength;
    }

    /// <summary>
    /// 对字符串进行PadRight，但符合QQ上的对齐标准
    /// </summary>
    /// <param name="input">要补齐的字符串</param>
    /// <param name="padNums">补齐的长度（请使用getQQStrLength进行计算）</param>
    /// <param name="paddingChar">用来对齐的字符（强烈建议用默认的空格，其他字符请手动计算后用String类原生的PadRight进行操作）</param>
    /// <returns>补齐长度后的字符串</returns>
    public static string PadRightQQ(string input, double padNums, char paddingChar = ' ')
    {
        var sb = new StringBuilder();

        var toPadNum = (int) Math.Floor(padNums - GetQQStrLength(input));
        if (toPadNum <= 0) return input;

        sb.Append(input);
        for (var i = 0; i < toPadNum; i++) sb.Append(paddingChar);

        return sb.ToString();
    }

    /// <summary>
    /// 检查参数数组长度
    /// </summary>
    /// <param name="args">指令数组</param>
    /// <param name="len">至少需要的参数个数</param>
    /// <param name="qGroup">（可选，不给的话就不发送错误信息）\n报错信息要发送到的QQ群对象</param>
    /// <param name="fromQQid">（可选，但QQgroup给了的话本参数必填）\n要通知的人的QQ Id</param>
    /// <returns>Illegal不符合 Legitimate符合 Extra超出</returns>
    public static async ValueTask<LenType> CheckForLength(string[] args, int len, Group qGroup = null,
                                                          long fromQQid = 0)
    {
        if (args.Length >= len + 1) return args.Length == len + 1 ? LenType.Legitimate : LenType.Extra;

        if (qGroup is not null)
            await qGroup.SendGroupMessage(SoraSegment.At(fromQQid) + " 命令参数不全，请补充。");
        return LenType.Illegal;
    }

    #endregion

    #region crash处理

    /// <summary>
    /// bot崩溃日志生成
    /// </summary>
    /// <param name="e">错误</param>
    public static void BotCrash(Exception e)
    {
        //生成错误报告
        IOUtils.CrashLogGen(Log.ErrorLogBuilder(e));
    }

    #endregion

    #region 重复的消息提示

    /// <summary>
    /// 数据库发生错误时的消息提示
    /// </summary>
    public static async ValueTask DatabaseFailedTips(GroupMessageEventArgs groupEventArgs)
    {
        await groupEventArgs.SourceGroup.SendGroupMessage(SoraSegment.At(groupEventArgs.Sender.Id) +
                                                          "\r\nERROR"                              +
                                                          "\r\n数据库错误");
        Log.Error("database", "database error");
    }

    #endregion

    #region 图片处理

    // public Image ow(Image[] imgs)
    // {
    //     if (imgs == null || imgs.Length < 9) return null;
    //     var width = imgs.First().Width * 3;
    //     var height = imgs.First().Height * 2 +
    //                  Math.Max(Math.Max(imgs[6].Height, imgs[7].Height), imgs[8].Height);
    //     var       cursor    = (x: 0, y: 0);
    //     var       imgeIndex = 0;
    //     using var ret       = new Bitmap(width,height);
    //     using var canvas    = Graphics.FromImage(ret);
    //     canvas.InterpolationMode = InterpolationMode.HighQualityBicubic;
    //     canvas.DrawImage(imgs[imgeIndex], cursor.x, cursor.y, imgs[imgeIndex].Width, imgs[imgeIndex].Height);
    //     
    // } 

    /// <summary>
    /// 绘制文字图片
    /// </summary>
    public static string DrawTextImage(string text, Color fontColor, Color backColor)
    {
        //加载字体
        using var fontMs = new MemoryStream(Font.JetBrainsMono);

        var fontCollection = new FontCollection();
        var fontFamily     = fontCollection.Install(fontMs);
        var font           = fontFamily.CreateFont(24);

        //计算图片大小
        var strRect = TextMeasurer.Measure(text, new RendererOptions(font));
        //边缘距离
        var frameSize = 5;
        //图片大小
        var (width, height) = ((int) strRect.Width + frameSize * 2, (int) strRect.Height + frameSize * 2);
        //创建图片
        var img = new Image<Rgba32>(width, height);
        //绘制
        img.Mutate(x => x.Fill(backColor)
                         .DrawText(text, font, fontColor,
                                   new PointF(frameSize, frameSize / 2 - 1)));
        //转换base64
        var b64 = img.ToBase64String(PngFormat.Instance);
        img.Dispose();
        return b64.Split(',')[1];
    }

    #endregion

    #region R18图片拦截

    public static SoraSegment GetPixivImg(long pid, string proxyUrl)
    {
        var pixApiReq =
            Requests.Get($"https://pixiv.yukari.one/api/illust/{pid}",
                         new ReqParams
                         {
                             Timeout                   = 5000,
                             IsThrowErrorForTimeout    = false,
                             IsThrowErrorForStatusCode = false
                         });
        var imgSegment = SoraSegment.Image(proxyUrl);

        if (pixApiReq.StatusCode == HttpStatusCode.OK)
        {
            var infoJson = pixApiReq.Json();
            if (Convert.ToBoolean(infoJson["error"]))
                return SoraSegment.Text($"[ERROR:网络错误，无法获取图片详细信息({infoJson["message"]})]");
            return Convert.ToBoolean(infoJson["body"]?["xRestrict"])
                ? SoraSegment.Text("[H是不行的]")
                :imgSegment;
        }

        return SoraSegment.Text($"[ERROR:网络错误，无法获取图片详细信息({pixApiReq.StatusCode})]");
    }

    #endregion
}