using System.Collections.Generic;
using Skadi.Entities;
using Sora.Entities;

namespace Skadi.Interface;

public interface IQaService
{
    long LoginUid { get; }

    /// <summary>
    /// 添加QA
    /// </summary>
    /// <returns>
    /// <para>-1 有相同QA</para>
    /// <para>-2 错误</para>
    /// </returns>
    int AddNewQA(QaData newQA);

    /// <summary>
    /// 删除QA
    /// </summary>
    /// <returns>
    /// <para>-1 没有QA</para>
    /// </returns>
    int DeleteQA(MessageBody qMsg, long groupId);

    List<MessageBody> GetAllQA(long groupId);
}