using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AspStudy.Services;
using MessagePack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Protocol;

namespace AspStudy.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CacheController : ControllerBase
    {

        private readonly DataProcessService _dataProcessService;
        private readonly ILogger<CacheController> _logger;
        
        // 해당 캐시는 시스템 전체에 걸쳐있음
        private readonly IMemoryCache _memoryCache;
        private readonly LimitedMemoryCacheService _limitedMemoryCacheService;
        
        // 분산 캐시
        private readonly IDistributedCache _distributedCache;
        
        // 하이브리드 캐시
        private readonly HybridCache _hybridCache;
        
        public CacheController(
            DataProcessService dataProcessService, 
            ILogger<CacheController> logger, 
            IMemoryCache memoryCache, 
            LimitedMemoryCacheService limitedMemoryCacheService,
            IDistributedCache distributedCache,
            HybridCache hybridCache)
        {
            _dataProcessService = dataProcessService;
            _logger = logger;
            _memoryCache = memoryCache;
            _limitedMemoryCacheService = limitedMemoryCacheService;
            _distributedCache = distributedCache;
            _hybridCache = hybridCache;
        }
        
        // 메모리 캐시 테스트
        [HttpPost("memory-cache")]
        public async Task MemoryCacheTest()
        {
            try
            {
                var req = await _dataProcessService.DeSerializeAsync<MemoryCacheReq>(Request);
                string msg;
                
                // 캐시 있는지 확인
                if (_memoryCache.TryGetValue<string>(nameof(MemoryCacheReq), out var value))
                {
                    msg = value;
                }
                else
                {
                    // 옵션 설정.
                    // SetSlidingExpiration -> 3초 내에 엑세스 하면 유지
                    // SetAbsoluteExpiration -> 무조건 20초 내에는 만료 
                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromSeconds(3))
                        .SetAbsoluteExpiration(TimeSpan.FromSeconds(20));
                    
                    msg = Random.Shared.Next().ToString();
                    _memoryCache.Set(nameof(MemoryCacheReq), msg, cacheEntryOptions);
                    
                    // 하루 내에 무조건 만료
                    //_memoryCache.Set("hello", msg, TimeSpan.FromDays(1));
                }
                
                var res = new MemoryCacheRes() { Message = req.Message + " - " + msg, ProtocolResult = ProtocolResult.Success};
                await _dataProcessService.SerializeAndSendAsync(Response, res);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                var res = new MemoryCacheRes() { ProtocolResult = ProtocolResult.Error};
                await _dataProcessService.SerializeAndSendAsync(Response, res);
            }
        }
        
        // 메모리 캐시 테스트2 -> 이게 더 편한듯?
        [HttpPost("memory-cache2")]
        public async Task MemoryCacheTest2()
        {
            try
            {
                var req = await _dataProcessService.DeSerializeAsync<MemoryCache2Req>(Request);
                
                string msg = await _memoryCache.GetOrCreateAsync(nameof(MemoryCache2Req), entry =>
                {
                    entry.SlidingExpiration = TimeSpan.FromSeconds(3); // 3초 안에 엑세스 안하면 만료
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20); // 무조건 20초 후에는 만료 (같이 설정해주는 것이 좋음)
                    entry.SetPriority(CacheItemPriority.Normal); // 메모리가 부족해지는 메모리 압박 상황에서 삭제 우선 순위 정해줌
                    entry.RegisterPostEvictionCallback(PostEvictionCallback); // 캐시 만료시 콜백
                    return Task.FromResult(Random.Shared.Next().ToString());
                });
                
                var res = new MemoryCache2Res() { Message = req.Message + " - " + msg, ProtocolResult = ProtocolResult.Success};
                await _dataProcessService.SerializeAndSendAsync(Response, res);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                var res = new MemoryCache2Res() { ProtocolResult = ProtocolResult.Error};
                await _dataProcessService.SerializeAndSendAsync(Response, res);
            }
        }
        
        // 메모리 캐시 테스트3 -> 캐시 크기 제한
        [HttpPost("memory-cache-limit-size")]
        public async Task MemoryCacheSizeLimitTest()
        {
            try
            {
                var req = await _dataProcessService.DeSerializeAsync<MemoryCacheLimitSizeReq>(Request);
                
                string msg;
                
                if (_limitedMemoryCacheService.Cache.TryGetValue<string>(nameof(MemoryCacheLimitSizeReq), out var value))
                {
                    _logger.LogInformation("cache success");
                    msg = value;
                }
                else
                {
                    msg = req.Message + " - " + Random.Shared.Next().ToString();
                    
                    // 캐시 사이즈의 기준은 정해져있지 않음. 개발자가 직접 규칙을 정해야함 여기서는 메시지의 길이를 기준으로 하고있음
                    // 컨텐츠의 개수로도 정하는 것이 가능함
                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromSeconds(3))
                        .SetAbsoluteExpiration(TimeSpan.FromSeconds(20))
                        .SetSize(req.Message.Length);
                    
                    _limitedMemoryCacheService.Cache.Set(nameof(MemoryCacheLimitSizeReq), msg, cacheEntryOptions);
                }
                
                var res = new MemoryCacheLimitSizeRes() { LimitedMessage = msg, ProtocolResult = ProtocolResult.Success};
                await _dataProcessService.SerializeAndSendAsync(Response, res);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                var res = new MemoryCacheLimitSizeRes() { ProtocolResult = ProtocolResult.Error};
                
                // 캐시 삭제.
                _limitedMemoryCacheService.Cache.Remove(nameof(MemoryCacheLimitSizeReq));
                
                // 만료된 모든 항목.
                // 우선 순위별 항목. 우선 순위가 가장 낮은 항목이 먼저 제거.
                // 오래 사용하지 않은 개체.
                // 절대 만료가 가장 빠른 항목.
                // 가장 빠른 슬라이딩 만료가 있는 항목.
                // 위 항목 순위로 상위 ~~ 퍼센트 캐시 삭제.
                // 이때 CacheItemPriority.NeverRemove 설정이 되어있는건 Compact로 삭제 안됨 
                _limitedMemoryCacheService.Cache.Compact(.25);
                
                await _dataProcessService.SerializeAndSendAsync(Response, res);
            }
        }
        
        // 메모리 캐시 테스트4 -> 여러 관련 캐시 동시에 만료시키는 법
        [HttpPost("memory-cache-group")]
        public async Task MemoryCacheGroupTest()
        {
            try
            {
                var req = await _dataProcessService.DeSerializeAsync<MemoryCacheGroupReq>(Request);

                if (!_memoryCache.TryGetValue<DateTime>("unit1", out var value1) ||
                    !_memoryCache.TryGetValue<DateTime>("unit2", out var value2) ||
                    !_memoryCache.TryGetValue<DateTime>("unit3", out var value3))
                {
                    // 이런식으로 캐시를 취소 토큰으로 묶으면 cancellationTokenSource가 Cancel 될 때 캐시들이 만료된다.
                    var cancellationTokenSource = new CancellationTokenSource();

                    _memoryCache.Set("GroupCancel", cancellationTokenSource);
                    _memoryCache.Set("unit1", DateTime.Now, new CancellationChangeToken(cancellationTokenSource.Token));
                    _memoryCache.Set("unit2", DateTime.Now, new CancellationChangeToken(cancellationTokenSource.Token));
                    _memoryCache.Set("unit3", DateTime.Now, new CancellationChangeToken(cancellationTokenSource.Token));    
                }
                else
                {
                    _logger.LogInformation(value1.ToString());
                    _logger.LogInformation(value2.ToString());
                    _logger.LogInformation(value3.ToString());
                }
                
                var res = new MemoryCacheGroupRes() { Message = "cached", ProtocolResult = ProtocolResult.Success};
                await _dataProcessService.SerializeAndSendAsync(Response, res);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                var res = new MemoryCacheGroupRes() { ProtocolResult = ProtocolResult.Error};
                await _dataProcessService.SerializeAndSendAsync(Response, res);
            }
        }
        
        // 메모리 캐시 테스트4 -> 여러 관련 캐시 동시에 만료시키는 법
        [HttpPost("memory-cache-group-cancel")]
        public async Task MemoryCacheGroupCancelTest()
        {
            try
            {
                var req = await _dataProcessService.DeSerializeAsync<MemoryCacheGroupCancelReq>(Request);

                if (_memoryCache.TryGetValue<CancellationTokenSource>("GroupCancel", out var value))
                {
                    await value.CancelAsync();
                }
                
                var res = new MemoryCacheGroupCancelRes() { ProtocolResult = ProtocolResult.Success};
                await _dataProcessService.SerializeAndSendAsync(Response, res);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                var res = new MemoryCacheGroupCancelRes() { ProtocolResult = ProtocolResult.Error};
                await _dataProcessService.SerializeAndSendAsync(Response, res);
            }
        }
        
        // 메모리 캐시 테스트5 -> cancellationTokenSource를 이용한 캐시 자동 만료
        [HttpPost("memory-cache-timer")]
        public async Task MemoryCacheTimerTest()
        {
            try
            {
                var req = await _dataProcessService.DeSerializeAsync<MemoryCacheTimerReq>(Request);
                
                if (!_memoryCache.TryGetValue("timer", out DateTime cacheValue))
                {
                    cacheValue = DateTime.Now;

                    // memoryCache 기능은 백그라운드에서 캐시 만료를 확인하지 않음.
                    // TryGetValue 등 메서드를 사용해야 그때 스캐닝이 일어남.
                    // 따라서 자동으로 캐시 만료를 관리하고싶으면 이렇게 cancellationTokenSource과 타이머를 이용해야함
                    var cancellationTokenSource = new CancellationTokenSource(
                        TimeSpan.FromSeconds(3));

                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                        .AddExpirationToken(new CancellationChangeToken(cancellationTokenSource.Token))
                        .RegisterPostEvictionCallback((key, value, reason, state) =>
                        {
                            // 만료가 일어나면 자동 콜백
                            // 이러한 콜백에서 캐시 항목을 다시 채우면 좀 위험함
                            // 콜백으로 캐시를 다시 채우기 전, 여러 스레드에서 동시에 캐시에 접근해 캐시 미스로
                            // 캐시를 채우려고 시도하기 때문
                            ((CancellationTokenSource)state).Dispose();
                        }, cancellationTokenSource);

                    _memoryCache.Set("timer", cacheValue, cacheEntryOptions);
                }
                
                var res = new MemoryCacheTimerRes() { CacheTime = cacheValue, ProtocolResult = ProtocolResult.Success};
                await _dataProcessService.SerializeAndSendAsync(Response, res);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                var res = new MemoryCacheTimerRes() { ProtocolResult = ProtocolResult.Error};
                await _dataProcessService.SerializeAndSendAsync(Response, res);
            }
        }
        
        // 메모리 캐시 테스트 6 -> background service를 이용한 캐시 자동 업데이트
        [HttpPost("memory-cache-background-service")]
        public async Task MemoryCacheBackgroundServiceTest()
        {
            try
            {
                var req = await _dataProcessService.DeSerializeAsync<MemoryCacheBackgroundServiceReq>(Request);

                if (_memoryCache.TryGetValue<DateTime>("CacheTime", out var value))
                {
                    var res = new MemoryCacheBackgroundServiceRes() { CacheTime = value, ProtocolResult = ProtocolResult.Success};
                    await _dataProcessService.SerializeAndSendAsync(Response, res);   
                }
                else
                {
                    throw new Exception();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                var res = new MemoryCacheTimerRes() { ProtocolResult = ProtocolResult.Error};
                await _dataProcessService.SerializeAndSendAsync(Response, res);
            }
        }
        
        // 분산 캐시 테스트 -> redis 사용
        [HttpPost("distributed-cache")]
        public async Task DistributedCacheTest()
        {
            try
            {
                var req = await _dataProcessService.DeSerializeAsync<DistributedCacheReq>(Request);

                var data = await _distributedCache.GetAsync("cachedTimeUTC");
                var msg = String.Empty;
                if (data != null)
                {
                    msg = Encoding.UTF8.GetString(data);
                }
                
                var res = new DistributedCacheRes() { CacheTime = DateTime.Parse(msg), ProtocolResult = ProtocolResult.Success};
                await _dataProcessService.SerializeAndSendAsync(Response, res);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                var res = new MemoryCacheRes() { ProtocolResult = ProtocolResult.Error};
                await _dataProcessService.SerializeAndSendAsync(Response, res);
            }
        }
        
        // 하이브리드 캐시 테스트
        [HttpPost("hybrid-cache")]
        public async Task HybridCacheTest()
        {
            try
            {
                var req = await _dataProcessService.DeSerializeAsync<HybridCacheReq>(Request);
                
                // 캐시 옵션. 메모리 캐시만 사용
                var entryOptions1 = new HybridCacheEntryOptions
                {
                    LocalCacheExpiration = TimeSpan.FromMinutes(1),
                    Flags = HybridCacheEntryFlags.DisableDistributedCache
                };
                
                // 캐시 옵션. 분산 캐시만 사용
                var entryOptions2 = new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromMinutes(1),
                    Flags = HybridCacheEntryFlags.DisableLocalCache
                };
                
                var mTime = await _hybridCache.GetOrCreateAsync("memory", 
                    async token => {
                        // 비동기 작업 수행
                        await Task.Delay(100, token); // 예제용 지연
                        return DateTime.Now; // 실제 값 반환
                    }, 
                    entryOptions1);
                
                var rTime = await _hybridCache.GetOrCreateAsync("redis", 
                    async token => {
                        // 비동기 작업 수행
                        await Task.Delay(100, token); // 예제용 지연
                        return DateTime.UtcNow; // 실제 값 반환
                    }, 
                    entryOptions2);
                
                // MessagePack은 DateTime을 무조건 UTC 타임으로 저장함...왜 이따구임?
                var res = new HybridCacheRes()
                {
                    MemoryCacheTime = mTime.ToString(),
                    RedisCacheTime = rTime.ToString(), 
                    ProtocolResult = ProtocolResult.Success
                };
                
                await _dataProcessService.SerializeAndSendAsync(Response, res);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                var res = new HybridCacheRes() { ProtocolResult = ProtocolResult.Error};
                await _dataProcessService.SerializeAndSendAsync(Response, res);
            }
        }
        
        // 기본 캐시 설정
        // 출력값을 캐시함. GET, HEAD 요청만 가능
        // HTTP 200만 캐시함
        // 쿠키 설정은 캐시 안함
        // [Authorize] 가 붙는 인증이 필요한 응답은 캐시 안함
        // 물론 커스텀 정책 만들어서 바꿀 수 있음
        [HttpGet("output-cache")]
        [OutputCache(PolicyName = "Expire2")]
        public IActionResult OutputCacheTest()
        {
            return Ok(DateTime.Now);
        }
        
        // 커스텀 캐시 정책 적용
        // Post 응답도 캐시해줌
        [HttpPost("output-cache")]
        [OutputCache(PolicyName = "CachePost")]
        public IActionResult CustomOutputCacheTest()
        {
            var res = new JOutputCacheRes()
            {
                ResponseTime = DateTime.Now,
            };
            
            return Ok(res);
        }

        [HttpGet("output-cache-query")]
        [OutputCache(PolicyName = "Query")]
        public IActionResult OutputCacheQueryTest(int id)
        {
            var res = new JOutputCacheQueryRes()
            {
                Message = id + Random.Shared.Next().ToString(),
            };
            
            return Ok(res);
        }
        
        [HttpGet("output-cache-header")]
        [OutputCache(PolicyName = "Header")]
        public IActionResult OutputCacheHeaderTest([FromHeader(Name = "test-header")] string testHeader)
        {
            var res = new JOutputCacheQueryRes()
            {
                Message = testHeader + " - " + Random.Shared.Next(),
            };
            
            return Ok(res);
        }
        
        [HttpGet("output-cache-value")]
        [OutputCache(PolicyName = "Value")]
        public IActionResult OutputCacheValueTest()
        {
            var res = new JOutputCacheQueryRes()
            {
                Message = Random.Shared.Next().ToString(),
            };
            
            return Ok(res);
        }
        
        // ETag을 이용한 캐시 검증
        // ETag는 리소스의 고유한 식별자를 의미함. 리소스의 내용이 변경될 때마다 달라지는 값임
        // 이를 이용해 클라이언트는 캐시된 값이 변경되었는지 그대로인지 확인이 가능함
        // 서버는 클라이언트에게 새 값을 줄 때마다, ETag헤더의 값을 변경해줌
        // 클라이언트는 서버에게 요청을 보낼때마다 서버에게 받은 ETag 값을 If-None-Match 헤더에 담아 보냄
        // 서버는 클라이언트가 보낸 If-None-Match 헤더의 값과 현재 리소스의 ETag 값을 비교함
        // 만약 같다면, 클라이언트가 가지고 있는 캐시된 값이 최신임을 의미함. 이때 서버는 304 Not Modified 응답을 보내줌
        // 만약 다르다면, 클라이언트가 가지고 있는 캐시된 값이 최신이 아님을 의미함. 이때 서버는 200 OK 응답과 함께 새로운 리소스를 보내줌
        
        // 이것 말고도 응답 헤더의 Last-Modified 헤더와 요청 헤더의 If-Modified-Since 헤더를 이용한 캐시 검증도 있음
        // Last-Modified 헤더는 리소스가 마지막으로 수정된 시간을 나타내고, If-Modified-Since 헤더는 클라이언트가 가지고 있는 캐시된 리소스의 마지막 수정 시간을 나타냄
        [HttpGet("output-cache-validate")]
        [OutputCache]
        public IActionResult OutputCacheValidateTest()
        {
            var res = new JOutputCacheValidateRes()
            {
                Message = Random.Shared.Next().ToString(),
            };
         
            var etag = $"\"{Guid.NewGuid():n}\"";
            HttpContext.Response.Headers.ETag = etag;
            
            return Ok(res);
        }

        // /tag로 시작하는 엔드포인트
        // 캐시 설정에서 /tag로 시작하는 엔드포인트에 tag-test라는 태그를 추가했음
        [HttpGet("/tag/output-cache-tag-test1")]
        [OutputCache]
        public IActionResult OutputCacheTagTest1()
        {
            var res = new JOutputCacheTag1Res
            {
                Message = Random.Shared.Next().ToString(),
            };
            return Ok(res);
        }
        
        // /tag로 시작하는 엔드포인트
        // 캐시 설정에서 /tag로 시작하는 엔드포인트에 tag-test라는 태그를 추가했음
        [HttpGet("/tag/output-cache-tag-test2")]
        [OutputCache]
        public IActionResult OutputCacheTagTest2()
        {
            var res = new JOutputCacheTag2Res()
            {
                Message = Random.Shared.Next().ToString(),
            };
            return Ok(res);
        }
        
        private void PostEvictionCallback(object cacheKey, object cacheValue, EvictionReason evictionReason, object state)
        {
            _logger.LogInformation($"Evicted {cacheKey} with value {cacheValue}. Reason: {evictionReason}");
        }
    }
}
