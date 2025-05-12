using MessagePack;

namespace Protocol;

[Union(0, typeof(MemoryCacheRes))]
[Union(1, typeof(MemoryCache2Res))]
[Union(2, typeof(MemoryCacheLimitSizeRes))]
[Union(3, typeof(MemoryCacheGroupRes))]
[MessagePackObject]
public abstract class ProtocolRes
{ 
   [Key(0)] public ProtocolResult ProtocolResult { get; set; }
}