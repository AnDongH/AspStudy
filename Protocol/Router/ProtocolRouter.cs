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
    };
}