using MessagePack;

namespace Protocol;

[MessagePackObject]
public class OutputCacheReq() : ProtocolReq(ProtocolId.OutputCache)
{
    
}

[MessagePackObject]
public class OutputCacheRes : ProtocolRes
{
    [Key(1)] public string CacheTime { get; set; }
}