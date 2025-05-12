using Microsoft.Extensions.Caching.Memory;

namespace AspStudy.Services;

public class LimitedMemoryCacheService
{
    // 캐시 사이즈 설정. 이렇게 따로 서비스 만들어서 사용하는게 권장됨
    public static long CacheLimitSize { get; private set; } = 3;
    public MemoryCache Cache { get; } = new MemoryCache(
        new MemoryCacheOptions
        {
            SizeLimit = CacheLimitSize
        });
}