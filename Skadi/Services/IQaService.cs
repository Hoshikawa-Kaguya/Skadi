using System.Collections.Generic;
using Skadi.Entities;
using Sora.Entities;

namespace Skadi.Services;

public interface IQaService
{
    public long LoginUid { get; }

    /// <summary>
    /// 添加QA
    /// </summary>
    /// <returns>
    /// <para>-1 有相同QA</para>
    /// <para>-2 错误</para>
    /// </returns>
    public int AddNewQA(QaData newQA);

    /// <summary>
    /// 删除QA
    /// </summary>
    /// <returns>
    /// <para>-1 没有QA</para>
    /// </returns>
    public int DeleteQA(MessageBody qMsg, long groupId);

    public List<MessageBody> GetAllQA(long groupId);
}