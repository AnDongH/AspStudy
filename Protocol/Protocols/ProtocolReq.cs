using MessagePack;

namespace Protocol;

[Union(0, typeof(MemoryCacheReq))]
[Union(1, typeof(MemoryCache2Req))]
[Union(2, typeof(MemoryCacheLimitSizeReq))]
[Union(3, typeof(MemoryCacheGroupReq))]
[Union(4, typeof(MemoryCacheTimerReq))]
[Union(5, typeof(MemoryCacheBackgroundServiceReq))]
[Union(6, typeof(DistributedCacheReq))]
[Union(7, typeof(HybridCacheReq))]
[Union(8, typeof(OutputCacheReq))]
[MessagePackObject]
public abstract class ProtocolReq(ProtocolId protocolId)
{
    [Key(0)] public ProtocolId ProtocolId { get; set; } = protocolId;
}