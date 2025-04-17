using System.Collections.Generic;
using JetBrains.Annotations;

namespace Skadi.Entities.ConfigModule;

[UsedImplicitly]
public class Hso
{
    /// <summary>
    /// 色图源类型
    /// </summary>
    public string Source { set; get; }

    /// <summary>
    /// 检查源证书
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public bool CheckSSLCert { set; get; }

    /// <summary>
    /// 是否启用本地缓存
    /// </summary>
    public bool UseCache { set; get; }

    /// <summary>
    /// 是否使用装逼大图
    /// </summary>
    public bool CardImage { set; get; }

    /// <summary>
    /// 色图文件夹大小限制[MB]
    /// </summary>
    public long SizeLimit { set; get; }

    /// <summary>
    /// YukariToken
    /// </summary>
    public string YukariApiKey { set; get; }

    /// <summary>
    /// 群组屏蔽
    /// </summary>
    // ReSharper disable once CollectionNeverUpdated.Global
    public List<long> GroupBlock { get; set; }
}