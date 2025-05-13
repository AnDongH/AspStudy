using System;
using MessagePack;

namespace Protocol;

[MessagePackObject]
public class MemoryCacheBackgroundServiceReq() : ProtocolReq(ProtocolId.MemoryCacheBackgroundService)
{
    
}

[MessagePackObject]
public class MemoryCacheBackgroundServiceRes : ProtocolRes
{
    [Key(1)] public DateTime CacheTime { get; set; }
}

