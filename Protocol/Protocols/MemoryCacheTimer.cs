using System;
using MessagePack;

namespace Protocol;

[MessagePackObject]
public class MemoryCacheTimerReq() : ProtocolReq(ProtocolId.MemoryCacheTimer)
{
        
}

[MessagePackObject]
public class MemoryCacheTimerRes : ProtocolRes
{
    [Key(1)] public DateTime CacheTime { get; set; }
}