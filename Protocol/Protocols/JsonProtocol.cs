namespace Protocol;

public abstract class JsonProtocolReq(ProtocolId protocolId)
{
    public ProtocolId ProtocolId { get; set; } = protocolId;
}

public abstract class JsonProtocolRes
{
    public ProtocolResult Result { get; set; }
}