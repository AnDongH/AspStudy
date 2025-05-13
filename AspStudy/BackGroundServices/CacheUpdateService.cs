using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AspStudy.BackGroundServices;

// 이런식으로 백그라운드 서비스에서 캐시를 업데이트 관리하는 방법도 있음
public class CacheUpdateService : BackgroundService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<CacheUpdateService> _logger;

    public CacheUpdateService(IMemoryCache memoryCache, ILogger<CacheUpdateService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 주기적인 업데이트 루프
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(3000, stoppingToken);

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromSeconds(5))
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(20));
                
                _memoryCache.Set<DateTime>("CacheTime", DateTime.Now, cacheEntryOptions);
            }
            catch (OperationCanceledException)
            {
                // 서비스가 중지될 때 예상되는 예외
                break;
            }
            catch (Exception ex)
            {
                // 예외 발생 시 로깅하고 계속 진행
                _logger.LogError(ex, "캐시 업데이트 중 오류 발생");
            }
        }
    }
}