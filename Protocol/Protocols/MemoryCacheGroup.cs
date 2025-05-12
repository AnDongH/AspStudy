using MessagePack;

namespace Protocol;

[MessagePackObject]
public class MemoryCacheGroupReq() : ProtocolReq(ProtocolId.MemoryCacheGroup)
{
    [Key(1)] public string Message { get; set; }
}

[MessagePackObject]
public class MemoryCacheGroupRes : ProtocolRes
{
    [Key(1)] public string Message { get; set; }
}

[MessagePackObject]
public class MemoryCacheGroupCancelReq() : ProtocolReq(ProtocolId.MemoryCacheGroupCancel)
{ }

[MessagePackObject]
public class MemoryCacheGroupCancelRes : ProtocolRes
{ }