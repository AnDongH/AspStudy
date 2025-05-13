using System;
using System.Threading;
using System.Threading.Tasks;
using AspStudy.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
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
        
        public CacheController(DataProcessService dataProcessService, ILogger<CacheController> logger, IMemoryCache memoryCache, LimitedMemoryCacheService limitedMemoryCacheService)
        {
            _dataProcessService = dataProcessService;
            _logger = logger;
            _memoryCache = memoryCache;
            _limitedMemoryCacheService = limitedMemoryCacheService;
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
        
        private void PostEvictionCallback(object cacheKey, object cacheValue, EvictionReason evictionReason, object state)
        {
            _logger.LogInformation($"Evicted {cacheKey} with value {cacheValue}. Reason: {evictionReason}");
        }
    }
}
