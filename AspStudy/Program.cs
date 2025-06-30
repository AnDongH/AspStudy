using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using AspStudy.BackGroundServices;
using AspStudy.CachePolicies;
using AspStudy.CompressionProvider;
using AspStudy.Options;
using AspStudy.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;

namespace AspStudy;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        #region Cache Services

                // 메모리 캐시 서비스 지원
        builder.Services.AddMemoryCache();
        builder.Services.AddHybridCache(); // IMemoryCache + IDistributedCache
        builder.Services.AddHostedService<CacheUpdateService>();
        
        // 분산 캐시 서비스. Redis
        // 이거는 IDistributedCache 인터페이스를 구현함
        // 권장되지 않음..
        // 태그 지원이 없음
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = builder.Configuration.GetConnectionString("MyRedisConStr"); // 레디스 연결 구성
            options.InstanceName = "NormalCache"; // 캐시에 사용할 레디스 키 접두사. SampleInstance + key 이렇게 저장됨..
        });

        // 이거는 레디스를 이용한 출력 캐시 서비스
        // 이거를 하면 메모리 캐시 말고 레디스 캐시를 사용함
        // 이를 이용해 출력 캐시도 분산 캐시가 가능
        builder.Services.AddStackExchangeRedisOutputCache(options =>
        {
            options.Configuration = builder.Configuration.GetConnectionString("MyRedisConStr"); // 레디스 연결 구성
            options.InstanceName = "OutputCache"; // 캐시에 사용할 레디스 키 접두사. SampleInstance + key 이렇게 저장됨..
        });
        
        // 출력 캐시 서비스
        builder.Services.AddOutputCache(options =>
        {
            options.AddBasePolicy(builder =>
            {
                // 기본 캐싱 규칙을 앱의 모든 엔드포인트에 적용
                //builder.Cache();
                
                builder.Expire(TimeSpan.FromSeconds(5));
                
                // /tag로 시작하는 엔드포인트에 캐시 tag를 추가하는 규칙
                builder
                    .With(c => c.HttpContext.Request.Path.StartsWithSegments("/tag"))
                    .Tag("tag-test");
            });
            
            options.AddPolicy("Expire2", builder => 
                builder.Expire(TimeSpan.FromSeconds(2)));
            options.AddPolicy("Expire3", builder => 
                builder.Expire(TimeSpan.FromSeconds(3)));
                
            // 커스텀 캐시 정책 추가
            options.AddPolicy("CachePost", PostCachePolicy.Instance);
                
            // DI를 통해 커스텀 캐시 정책을 추가할 수도 있음
            //options.AddPolicy("CachePost", builder => builder.AddPolicy<PostCachePolicy>(), true);
                
            // 기본적으로 출력 캐시에 사용되는 키는 요청 전체임. ex - https://localhost:5179/api/values?id=1&page=2
            // 이때 키를 제어하는 것이 가능함. 아래는 쿼리값에 따라서 캐시를 구분하는 규칙을 추가함
            options.AddPolicy("Query", builder => builder.SetVaryByQuery("id"));
            // 헤더에 따라서 캐시를 구분하는 규칙을 추가함
            options.AddPolicy("Header", builder => builder.SetVaryByHeader("test-header"));
            // 사용자 로직에 따라 캐시를 구분하는 규칙을 추가함 (밑에는 서버 시간이 홀수냐 짝수냐에 따라 캐시)
            options.AddPolicy("Value", builder => builder.VaryByValue((context) => 
                new KeyValuePair<string, string>(
                    "time", (DateTime.UtcNow.Second %  2).ToString())));
            
            // 리소스 잠금 비활성화 캐시
            // 기본값은 true. 동일한 요청이 들어왔을 때, 캐시가 생성되는 동안 다른 요청은 대기함
            // false로 설정하면, 캐시가 생성되는 동안 다른 요청들도 캐시를 생성함 (무의미한 작업이 될 수 있음)
            // 웬만해서는 true로 설정하는게 좋음
            // 응답이 정~말 빨라야 하거나, 리소스 생산 비용이 적으면 false로 고려할 수 있음
            options.AddPolicy("NoLock", builder => builder.SetLocking(false));
           
            // 200MB로 설정 => 앱 전체 캐시 스토리지 크기 기본은 100MB
            // 이거 넘어가면 오래된거 없애고 생성함
            options.SizeLimit = 200 * 1024 * 1024;
            
            // 32MB로 설정 => 개별 응답 최대 크기 기본은 64MB
            // 이거 넘어가면 캐시 저장 안함
            options.MaximumBodySize = 32 * 1024 * 1024;
        });

        // 캐시에 사이즈를 제한할 때는 일반 IMemoryCache는 서비스 전체적으로 공유되기에, 여러 컨트롤러에서 사용하기에는 잠재적 위협이 있다.
        // 따라서 사이즈를 정할 때는 이렇게 컨텐츠마다 새로운 캐시 서비스를 구성하는 것이 좋음
        builder.Services.AddSingleton<LimitedMemoryCacheService>();
        
        #endregion

        #region Rate Limitter Services

        // 커스텀 옵션 구성(구성을 외부로 뺄 때 사용)
        builder.Services.Configure<CustomRateLimitOptions>(
            builder.Configuration.GetSection(CustomRateLimitOptions.CustomRateLimit));
        
        var customOptions = new CustomRateLimitOptions();
        builder.Configuration.GetSection(CustomRateLimitOptions.CustomRateLimit).Bind(customOptions);
        
        builder.Services.AddRateLimiter(options =>
        {
            // 사용자ID 혹은 전역적으로 분당 10개의 요청 허용
            // 이거 설정하면 자동으로 모든 엔드포인트에 적용됨
            // 이거를 설정할 시 명명된 정책을 사용해도, 이걸 먼저 만족해야 하기에
            // 보통 관대하게 설정함. (최후 방어선?)
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true, // 자동으로 윈도우 허용량 초기화.
                        PermitLimit = 1000, // 1000개 요청 허용
                        QueueLimit = 0, // 대기열에 요청을 넣지 않음. 1분 안에 1000개 요청이 넘어가면 429 too many requests 응답
                        Window = TimeSpan.FromMinutes(1) // 1분동안
                    }));
            
            // 명명된 정책, 고정창 알고리즘
            // 트래픽 버스트가 있을 수 있음. 12초에 4개 요청 -> 13초에 4개 요청시 2초에 걸쳐 8개 요청이 올 수 있음
            options.AddFixedWindowLimiter("fixed", opt =>
            {
                opt.PermitLimit = 4; // 4개 요청 허용
                opt.Window = TimeSpan.FromSeconds(12); // 12초 동안
                // opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst; // 대기열에 요청이 들어오면 먼저 들어온 요청부터 처리함
                // opt.QueueLimit = 2; // 대기열에 최대 2개 요청을 허용함
            });
            
            // 슬라이딩 윈도우 알고리즘
            // 항상 현재 시점에서 지난 설정된 시간의 과거를 추적해 요청 상태 확인
            // 만약 제한값까지 도달했다면 버리거나, 대기열에 추가 (트래픽 버스트 X, 진정한 속도 제한)
            options.AddSlidingWindowLimiter("sliding", opt =>
            {
                opt.PermitLimit = customOptions.PermitLimit; // 허용량
                opt.Window = TimeSpan.FromSeconds(customOptions.Window); // 윈도우 시간
                opt.SegmentsPerWindow = customOptions.SegmentsPerWindow; // 윈도우를 몇 개의 세그먼트로 나눌지
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst; // 대기열에 요청이 들어오면 먼저 들어온 요청부터 처리함
                opt.QueueLimit = customOptions.QueueLimit; // 대기열 허용량
            });
            
            // 토큰 버킷 알고리즘
            // 요청이 들어오면 토큰을 사용하고, 토큰은 주기적으로 재충전됨
            options.AddTokenBucketLimiter("token", opt =>
            {
                opt.TokenLimit = customOptions.TokenLimit; // 토큰 최대 개수
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst; // 대기열에 요청이 들어오면 먼저 들어온 요청부터 처리함
                opt.QueueLimit = customOptions.QueueLimit; // 대기열 허용량
                opt.ReplenishmentPeriod = TimeSpan.FromSeconds(customOptions.ReplenishmentPeriod); // 토큰 재충전 주기
                opt.TokensPerPeriod = customOptions.TokensPerPeriod; // 토큰 재충전 주기마다 재충전되는 토큰 개수
                opt.AutoReplenishment = customOptions.AutoReplenishment; // 자동으로 토큰 재충전
            });
            
            // 동시성 제한기 알고리즘
            // 서버가 동시에 처리할 수 있는 요청 수를 제한함
            options.AddConcurrencyLimiter("concurrency", opt =>
            {
                opt.PermitLimit = customOptions.PermitLimit; // 동시에 처리할 수 있는 요청 개수
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst; // 대기열에 요청이 들어오면 먼저 들어온 요청부터 처리함
                opt.QueueLimit = customOptions.QueueLimit; // 대기열 허용량
            });
            
            // 파티션 나누기 (유저별)
            // 유저별로 속도 제한을 적용할 수 있음
            // 공정성: 한 사용자가 모든 사용자에 대해 전체 속도 제한을 사용할 수 없습니다.
            // 세분성: 사용자/리소스별로 제한이 다르다
            // 보안: 대상 남용에 대한 더 나은 보호
            // 계층화된 서비스: 제한이 다른 서비스 계층에 대한 지원
            // ip로 파티션 나누는건 좋지 않은 방법.
            // ip 스푸핑을 사용한 Dos 공격에 취약함
            // 1. 서비스 이용에 장애가 갈 수 있고
            // 2. 여러 가짜 ip에 의한 파티션 생성으로 메모리가 고갈됨
            options.AddPolicy("per-user", httpContext => 
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.User.Identity?.Name ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 50,
                        Window = TimeSpan.FromMinutes(1),
                    }));
            
            // 거절시 콜백
            options.OnRejected = async (context, cancellationToken) =>
            {
                // Custom rejection handling logic
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.Headers["Retry-After"] = customOptions.Window.ToString();

                await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please try again later.", cancellationToken);
            };
        });

        #endregion

        #region TimeOut Services

        builder.Services.AddRequestTimeouts(options =>
        {
            // 전역 기본 설정 (길게)
            options.DefaultPolicy = new RequestTimeoutPolicy
            {
                Timeout = TimeSpan.FromMinutes(1), // 기본 타임아웃 1분
                TimeoutStatusCode = 504 // 타임아웃 시 응답 코드
            };

            // 정책별 설정
            options.AddPolicy("Timeout2", new RequestTimeoutPolicy
            {
                Timeout = TimeSpan.FromSeconds(2), // 2초 타임아웃
                TimeoutStatusCode = 504, // 타임아웃 시 응답 코드
                WriteTimeoutResponse = async (HttpContext context) => // 타임아웃시 콜백 응답
                {
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync("Timeout from MyPolicy2!");
                }
            });

        });

        #endregion

        #region Object Pool Services

        // ms에서 제공하는 객체 풀링 서비스
        
        builder.Services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();

        builder.Services.TryAddSingleton<ObjectPool<ReusableBuffer>>(serviceProvider =>
        {
            var provider = serviceProvider.GetRequiredService<ObjectPoolProvider>();
            var policy = new DefaultPooledObjectPolicy<ReusableBuffer>();
            return provider.Create(policy);
        });

        #endregion

        #region Response Compression Services

        // ?? 왜 응답 크기에 따라 압축 하도록 하는 기능이 없는 것이냐??
        
        builder.Services.AddResponseCompression(options =>
        {
            // 기본 값은 false => 보안상 위험이 있기에 https에서는 기본적으로 압축 못함
            // EnableForHttps를 true로 설정하면 https에서도 압축을 허용함
            // 방어 방법이 있을 때만 사용
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>(); // 브로틀리 압축
            options.Providers.Add<GzipCompressionProvider>(); // Gzip 압축
            options.Providers.Add<CustomCompressionProvider>(); // 커스텀 압축 제공자
        });
        
        builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        builder.Services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.SmallestSize;
        });

        #endregion
        
        // 컨트롤러 추가
        builder.Services.AddControllers();

        // 직렬화 역직렬화
        builder.Services.AddTransient<DataProcessService>();
        
        var app = builder.Build();
        
        // 응답 압축 미들웨어 => 왜 어떤 경우든 전부 압축하도록 만든거지??
        // 차라리 커스텀 압축 미들웨어 만드는게 더 나을듯;;
        // app.UseResponseCompression();
        
        // 요청 타임아웃 미들웨어
        app.UseRequestTimeouts();
        
        // 출력 캐싱 미들웨어
        app.UseOutputCache();
        
        // 속도 제한 미들웨어
        app.UseRateLimiter();
        
        // tag로 묶인 캐시를 제거하는 엔드포인트
        // tag로 캐시를 묶으면 특정 컨텐츠에 관련된 캐시들을 일관적으로 한번에 정리하는게 가능함
        app.MapPost("/purge/{tag}", async (IOutputCacheStore cache, string tag) =>
        {
            await cache.EvictByTagAsync(tag, default);
        });
        
        // 이미 시작된 시간 제한 취소
        // ?? 이걸 어따 쓰냐?
        // 특수한 상황에서 -> 파일 다운로드를 하는데 용량이 거어어어업나 크다던지?
        app.MapGet("/canceltimeout", async (HttpContext context) => {
            var timeoutFeature = context.Features.Get<IHttpRequestTimeoutFeature>();
            timeoutFeature?.DisableTimeout();

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), context.RequestAborted);
            }
            catch (TaskCanceledException)
            {
                return Results.Content("Timeout!", "text/plain");
            }

            return Results.Content("No timeout!", "text/plain");
        }).WithRequestTimeout(TimeSpan.FromSeconds(1));
        
        // 객체 풀링을 이용한 연산
        app.MapGet("/hash/{name}", (string name, ObjectPool<ReusableBuffer> bufferPool) =>
        {

            var buffer = bufferPool.Get();
            try
            {
                // Set the buffer data to the ASCII values of a word
                for (var i = 0; i < name.Length; i++)
                {
                    buffer.Data[i] = (byte)name[i];
                }

                Span<byte> hash = stackalloc byte[32];
                SHA256.HashData(buffer.Data.AsSpan(0, name.Length), hash);
                return "Hash: " + Convert.ToHexString(hash);
            }
            finally
            {
                // Data is automatically reset because this type implemented IResettable
                bufferPool.Return(buffer); 
            }
        });
        
        app.MapControllers();
        // 모든 컨트롤러의 엔드포인트에 fixed 속도 제한 정책 적용
        // app.MapControllers().RequireRateLimiting("fixed");
        
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
        
        app.Run("http://127.0.0.1:5179");
    }
}