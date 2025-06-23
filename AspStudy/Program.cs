using System;
using AspStudy.BackGroundServices;
using AspStudy.CachePolicies;
using AspStudy.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AspStudy;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 메모리 캐시 서비스 지원
        builder.Services.AddMemoryCache();
        builder.Services.AddHybridCache(); // IMemoryCache + IDistributedCache
        builder.Services.AddHostedService<CacheUpdateService>();
        
        // 분산 캐시 서비스. Redis
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = builder.Configuration.GetConnectionString("MyRedisConStr"); // 레디스 연결 구성
            options.InstanceName = "SampleInstance"; // 캐시에 사용할 레디스 키 접두사. SampleInstance + key 이렇게 저장됨..
        });
        
        // 출력 캐시 서비스
        builder.Services.AddOutputCache(options =>
        {
            options.AddBasePolicy(builder =>
            {
                builder.Expire(TimeSpan.FromSeconds(5));
                options.AddPolicy("Expire2", builder => 
                    builder.Expire(TimeSpan.FromSeconds(2)));
                options.AddPolicy("Expire3", builder => 
                    builder.Expire(TimeSpan.FromSeconds(3)));
                
                // 커스텀 캐시 정책 추가
                options.AddPolicy("CachePost", PostCachePolicy.Instance);
                
            });
        });
        
        // 컨트롤러 추가
        builder.Services.AddControllers();

        // 직렬화 역직렬화
        builder.Services.AddTransient<DataProcessService>();
        
        // 캐시에 사이즈를 제한할 때는 일반 IMemoryCache는 서비스 전체적으로 공유되기에, 여러 컨트롤러에서 사용하기에는 잠재적 위협이 있다.
        // 따라서 사이즈를 정할 때는 이렇게 컨텐츠마다 새로운 캐시 서비스를 구성하는 것이 좋음
        builder.Services.AddSingleton<LimitedMemoryCacheService>();
        
        var app = builder.Build();
        
        // 출력 캐싱 미들웨어
        app.UseOutputCache();
        
        app.MapControllers();
        
        // 앱 시작할 때 분산 캐싱
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var currentTimeUTC = DateTime.UtcNow.ToString();
            byte[] encodedCurrentTimeUTC = System.Text.Encoding.UTF8.GetBytes(currentTimeUTC);
            var options = new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromSeconds(20));
            app.Services.GetService<IDistributedCache>()
                .Set("cachedTimeUTC", encodedCurrentTimeUTC, options);
        });
        
        app.Run();
    }
}