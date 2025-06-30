namespace Protocol;

public class JRateLimitFixedReq() : JsonProtocolReq(ProtocolId.RateLimitFixed)
{ }

public class JRateLimitFixedRes : JsonProtocolRes
{ }

public class JRateLimitSlidingReq() : JsonProtocolReq(ProtocolId.RateLimitSliding)
{ }

public class JRateLimitSlidingRes : JsonProtocolRes
{ }

public class JRateLimitTokenBucketReq() : JsonProtocolReq(ProtocolId.RateLimitTokenBucket)
{ }

public class JRateLimitTokenBucketRes : JsonProtocolRes
{ }

public class JRateLimitConcurrencyReq() : JsonProtocolReq(ProtocolId.RateLimitConcurrency)
{ }

public class JRateLimitConcurrencyRes : JsonProtocolRes
{ }

public class JRateLimitPerUserReq() : JsonProtocolReq(ProtocolId.RateLimitPerUser)
{ }

public class JRateLimitPerUserRes : JsonProtocolRes
{ }