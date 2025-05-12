using MessagePack;

namespace Protocol;

[MessagePackObject]
public class MemoryCacheLimitSizeReq() : ProtocolReq(ProtocolId.MemoryCacheLimitSize)
{
    [Key(1)] public string Message { get; set; }
}

[MessagePackObject]
public class MemoryCacheLimitSizeRes : ProtocolRes
{
    [Key(1)] public string LimitedMessage { get; set; }
}