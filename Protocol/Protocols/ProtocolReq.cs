using MessagePack;

namespace Protocol;

[Union(0, typeof(MemoryCacheReq))]
[Union(1, typeof(MemoryCache2Req))]
[Union(2, typeof(MemoryCacheLimitSizeReq))]
[Union(3, typeof(MemoryCacheGroupReq))]
[MessagePackObject]
public abstract class ProtocolReq(ProtocolId protocolId)
{
    [Key(0)] public ProtocolId ProtocolId { get; set; } = protocolId;
}