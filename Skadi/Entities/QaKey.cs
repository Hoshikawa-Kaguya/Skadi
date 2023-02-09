using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using ProtoBuf;
using Sora.Entities;
using Sora.Serializer;
using PbSerializer = ProtoBuf.Serializer;

namespace Skadi.Entities;

[ProtoContract]
public record QaKey
{
    [ProtoMember(1)]
    public long GroupId { get; init; }

    [ProtoMember(2)]
    public MessageBody ReqMsg { get; init; }

    public byte[] GetQaKeyMd5()
    {
        MD5 md5 = MD5.Create();

        using MemoryStream ms = new();
        PbSerializer.Serialize(ms, this);
        ms.Position = 0;

        return md5.ComputeHash(ms.ToArray());
    }

    public virtual bool Equals(QaKey other)
    {
        byte[] reqBytes = ReqMsg.SerializeToPb().ToArray();
        return other.GroupId == GroupId && reqBytes.SequenceEqual(other.ReqMsg.SerializeToPb().ToArray());
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GetQaKeyMd5);
    }
}