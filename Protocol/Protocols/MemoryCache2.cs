using MessagePack;

namespace Protocol;

[MessagePackObject]
public class MemoryCache2Req() : ProtocolReq(ProtocolId.MemoryCache2)
{
    [Key(1)] public string Message { get; set; }
}

[MessagePackObject]
public class MemoryCache2Res : ProtocolRes
{
    [Key(1)] public string Message { get; set; }
}