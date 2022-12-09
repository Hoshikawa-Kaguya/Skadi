using System;
using Sora.Entities;

namespace Skadi.Entities;

public struct QABuf
{
    internal MessageBody qMsg;
    internal Guid        cmdId;
    internal long        gid;
}