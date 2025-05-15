using System;
using MessagePack;

namespace Protocol;

[MessagePackObject]
public class DistributedCacheReq() : ProtocolReq(ProtocolId.DistributedCache)
{
    
}

[MessagePackObject]
public class DistributedCacheRes : ProtocolRes
{
    [Key(1)] public DateTime CacheTime { get; set; }
}