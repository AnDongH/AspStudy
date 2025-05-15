using System;
using MessagePack;

namespace Protocol;

[MessagePackObject]
public class HybridCacheReq() : ProtocolReq(ProtocolId.HybridCache)
{
    
}

[MessagePackObject]
public class HybridCacheRes : ProtocolRes
{
    [Key(1)] public string MemoryCacheTime { get; set; }
    [Key(2)] public string RedisCacheTime { get; set; }
}