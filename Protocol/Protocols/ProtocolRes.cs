using MessagePack;

namespace Protocol;

[Union(0, typeof(MemoryCacheRes))]
[Union(1, typeof(MemoryCache2Res))]
[Union(2, typeof(MemoryCacheLimitSizeRes))]
[Union(3, typeof(MemoryCacheGroupRes))]
[Union(4, typeof(MemoryCacheTimerRes))]
[Union(5, typeof(MemoryCacheBackgroundServiceRes))]
[Union(7, typeof(HybridCacheRes))]
[Union(8, typeof(OutputCacheRes))]
[MessagePackObject]
public abstract class ProtocolRes
{ 
   [Key(0)] public ProtocolResult ProtocolResult { get; set; }
}