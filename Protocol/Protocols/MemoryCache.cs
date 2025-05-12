using MessagePack;

namespace Protocol;

[MessagePackObject]
public class MemoryCacheReq() : ProtocolReq(ProtocolId.MemoryCache)
{
    [Key(1)] public string Message { get; set; }
}

[MessagePackObject]
public class MemoryCacheRes : ProtocolRes
{
    [Key(1)] public string Message { get; set; }
}