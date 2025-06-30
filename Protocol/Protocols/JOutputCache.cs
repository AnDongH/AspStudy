using System;
using Protocol.DataAttribute;

namespace Protocol;

// 일반 출력 캐시
public class JOutputCacheReq() : JsonProtocolReq(ProtocolId.OutputCache)
{
    
}

public class JOutputCacheRes : JsonProtocolRes
{
    public DateTime ResponseTime { get; set; }
}

// 출력 캐시 쿼리
public class JOutputCacheQueryReq() : JsonProtocolReq(ProtocolId.OutputCacheQuery)
{
    [Query("id")] public int Id { get; set; }
}

public class JOutputCacheQueryRes : JsonProtocolRes
{
    public string Message { get; set; }
}

// 출력 캐시 헤더
public class JOutputCacheHeaderReq() : JsonProtocolReq(ProtocolId.OutputCacheHeader)
{
    [Header("test-header")] public string HeaderMessage { get; set; }
}

public class JOutputCacheHeaderRes : JsonProtocolRes
{
    public string Message { get; set; }
}

// 출력 캐시 value
public class JOutputCacheValueReq() : JsonProtocolReq(ProtocolId.OutputCacheValue)
{
}

public class JOutputCacheValueRes : JsonProtocolRes
{
    public string Message { get; set; }
}

// 캐시 검증
public class JOutputCacheValidateReq() : JsonProtocolReq(ProtocolId.OutputCacheValidate)
{
}

public class JOutputCacheValidateRes : JsonProtocolRes
{
    public string Message { get; set; }
}

// 태그를 통한 캐시 관리
public class JOutputCacheTag1Req() : JsonProtocolReq(ProtocolId.OutputCacheTag1)
{
}

public class JOutputCacheTag1Res : JsonProtocolRes
{
    public string Message { get; set; }
}

public class JOutputCacheTag2Req() : JsonProtocolReq(ProtocolId.OutputCacheTag2)
{
}

public class JOutputCacheTag2Res : JsonProtocolRes
{
    public string Message { get; set; }
}

