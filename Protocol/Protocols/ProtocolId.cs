namespace Protocol;

public enum ProtocolId
{
    None = 0,
    MemoryCache,
    MemoryCache2,
    MemoryCacheLimitSize,
    MemoryCacheGroup,
    MemoryCacheGroupCancel,
    MemoryCacheTimer,
    MemoryCacheBackgroundService,
    DistributedCache,
    HybridCache,

    #region json protocol

    OutputCache = 101,

    #endregion
}