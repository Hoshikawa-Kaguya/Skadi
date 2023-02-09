using System.Collections.Generic;
using System.Threading.Tasks;
using Skadi.Entities;
using Sora.Entities;

namespace Skadi.Interface;

public interface IQaService
{
    /// <summary>
    /// 添加QA
    /// </summary>
    /// <returns>
    /// <para>-1 有相同QA</para>
    /// <para>-2 错误</para>
    /// </returns>
    ValueTask<int> AddNewQA(long loginUid, long groupId, MessageBody message);

    /// <summary>
    /// 删除QA
    /// </summary>
    /// <returns>
    /// <para>-1 没有QA</para>
    /// </returns>
    int DeleteQA(long loginUid, long groupId, MessageBody question);

    /// <summary>
    /// 获取回答
    /// </summary>
    MessageBody GetAnswer(long loginUid, long groupId, MessageBody question);

    /// <summary>
    /// 获取所有的问题
    /// </summary>
    /// <returns></returns>
    List<MessageBody> GetAllQA(long loginUid, long groupId);
}