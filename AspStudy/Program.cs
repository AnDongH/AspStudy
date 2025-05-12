using AspStudy.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace AspStudy;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 메모리 캐시 서비스 지원
        builder.Services.AddMemoryCache();
        
        // 컨트롤러 추가
        builder.Services.AddControllers();

        // 직렬화 역직렬화
        builder.Services.AddTransient<DataProcessService>();
        
        // 캐시에 사이즈를 제한할 때는 일반 IMemoryCache는 서비스 전체적으로 공유되기에, 여러 컨트롤러에서 사용하기에는 잠재적 위협이 있다.
        // 따라서 사이즈를 정할 때는 이렇게 컨텐츠마다 새로운 캐시 서비스를 구성하는 것이 좋음
        builder.Services.AddSingleton<LimitedMemoryCacheService>();
        
        var app = builder.Build();
        
        app.MapControllers();
        
        app.Run();
    }
}