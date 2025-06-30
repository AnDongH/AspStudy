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
    OutputCacheQuery,
    OutputCacheHeader,
    OutputCacheValue,
    OutputCacheValidate,
    OutputCacheTag1,
    OutputCacheTag2,
    
    RateLimitFixed,
    RateLimitSliding,
    RateLimitTokenBucket,
    RateLimitConcurrency,
    RateLimitPerUser,
    
    Timeout1,
    Timeout2,
    #endregion
}