using System.Collections.Generic;

namespace Protocol.Router;

public class ProtocolRouter
{
    public static Dictionary<ProtocolId, string> RouterMap = new Dictionary<ProtocolId, string>
    {
        { ProtocolId.MemoryCache , "cache/memory-cache"},
        { ProtocolId.MemoryCache2 , "cache/memory-cache2"},
        { ProtocolId.MemoryCacheLimitSize , "cache/memory-cache-limit-size"},
        { ProtocolId.MemoryCacheGroup , "cache/memory-cache-group"},
        { ProtocolId.MemoryCacheGroupCancel , "cache/memory-cache-group-cancel"},
        { ProtocolId.MemoryCacheTimer , "cache/memory-cache-timer"},
        { ProtocolId.MemoryCacheBackgroundService , "cache/memory-cache-background-service"},
        { ProtocolId.DistributedCache , "cache/distributed-cache"},
        { ProtocolId.HybridCache , "cache/hybrid-cache"},
        
        { ProtocolId.OutputCache , "cache/output-cache"},
        { ProtocolId.OutputCacheQuery , "cache/output-cache-query" },
        { ProtocolId.OutputCacheHeader , "cache/output-cache-header" },
        { ProtocolId.OutputCacheValue , "cache/output-cache-value" },
        { ProtocolId.OutputCacheValidate , "cache/output-cache-validate" },
        { ProtocolId.OutputCacheTag1 , "tag/output-cache-tag-test1" },
        { ProtocolId.OutputCacheTag2 , "tag/output-cache-tag-test2" },
        
        { ProtocolId.RateLimitFixed , "ratelimit/rate-limit/fixed" },
        { ProtocolId.RateLimitSliding , "ratelimit/rate-limit/sliding" },
        { ProtocolId.RateLimitTokenBucket , "ratelimit/rate-limit/token" },
        { ProtocolId.RateLimitConcurrency , "ratelimit/rate-limit/concurrency" },
        { ProtocolId.RateLimitPerUser , "ratelimit/rate-limit/per-user" },
        
        { ProtocolId.Timeout1 , "requesttimeout/timeout1" },
        { ProtocolId.Timeout2 , "requesttimeout/timeout2" }
    };
}