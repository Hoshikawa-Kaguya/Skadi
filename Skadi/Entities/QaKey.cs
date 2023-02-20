using System.IO;
using System.Security.Cryptography;
using ProtoBuf;
using Skadi.Tool;
using Sora.Entities;
using PbSerializer = ProtoBuf.Serializer;

namespace Skadi.Entities;

[ProtoContract]
public record QaKey
{
    [ProtoMember(1)]
    public long GroupId { get; init; }

    [ProtoMember(2)]
    public long SourceId { get; init; }

    [ProtoMember(3)]
    public MessageBody ReqMsg { get; init; }

    public string GetQaKeyMd5()
    {
        MD5 md5 = MD5.Create();

        using MemoryStream ms = new();
        PbSerializer.Serialize(ms, this);
        ms.Position = 0;
        byte[] md5buf = md5.ComputeHash(ms.ToArray());

        return md5buf.ToHexString();
    }
}