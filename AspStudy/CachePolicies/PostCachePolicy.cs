using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;

namespace AspStudy.CachePolicies;

public class PostCachePolicy : IOutputCachePolicy
{
    public static readonly PostCachePolicy Instance = new();

    private PostCachePolicy()
    {
    }
    
    // 요청이 들어올 때 가장 먼저 호출되는 캐시 정책
    public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        var attemptOutputCaching = AttemptOutputCaching(context);
        context.EnableOutputCaching = true; // 캐시 사용할거임?
        context.AllowCacheLookup = attemptOutputCaching; // 응답 생성 전에 캐시된 곳에서 뒤져볼거임?
        context.AllowCacheStorage = attemptOutputCaching; // 생성한 응답 캐시로 저장할거임?
        context.AllowLocking = true; // 동일 요청 동시에 들어왔을 때 락 걸거임?
        
        context.CacheVaryByRules.QueryKeys = "*"; // 쿼리 매개변수에 따라서 캐시를 구분하는 규칙. *는 모든 쿼리 매개변수마다 캐시 생성
        // context.CacheVaryByRules.QueryKeys = new[] { "id", "page"}; // 이렇게 하면 id와 page라는 쿼리 매개변수들만 캐시를 구분함

        return ValueTask.CompletedTask;
    }

    // 캐시 히트한 응답을 클라이언트에게 전송하기 전, 추가 처리를 할 수 있는 메서드
    // 헤더 설정을 하거나,, 데이터를 가공하거나..
    public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        return ValueTask.CompletedTask;
    }

    // 새로 생성된 응답을 클라이언트에게 전송하기 전, 캐시하기 전에 호출되는 메서드.
    // 응답의 종류에 따라서 처리가 가능
    public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        var response = context.HttpContext.Response;

        // 쿠키 설정하는 응답이면 캐시 안함
        if (!StringValues.IsNullOrEmpty(response.Headers.SetCookie))
        {
            context.AllowCacheStorage = false;
            return ValueTask.CompletedTask;
        }

        // 200이랑, 301만 캐시
        if (response.StatusCode != StatusCodes.Status200OK && 
            response.StatusCode != StatusCodes.Status301MovedPermanently)
        {
            context.AllowCacheStorage = false;
        }

        return ValueTask.CompletedTask;
    }
    
    private static bool AttemptOutputCaching(OutputCacheContext context)
    {
        var request = context.HttpContext.Request;

        // GET, HEAD, POST 에 대한 출력만 캐시함
        if (!HttpMethods.IsGet(request.Method) && 
            !HttpMethods.IsHead(request.Method) && 
            !HttpMethods.IsPost(request.Method))
        {
            return false;
        }

        // 인증 관련 출력은 캐시 안함
        if (!StringValues.IsNullOrEmpty(request.Headers.Authorization) || 
            request.HttpContext.User?.Identity?.IsAuthenticated == true)
        {
            return false;
        }

        return true;
    }
}