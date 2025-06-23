using System;

namespace Protocol;

public class JOutputCacheReq() : JsonProtocolReq(ProtocolId.OutputCache)
{
    
}

public class JOutputCacheRes : JsonProtocolRes
{
    public DateTime ResponseTime { get; set; }
}