using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AspStudy.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class RequestTimeoutController : ControllerBase
    {
        // RequestTimeout은 자동으로 응답을 취소하는 기능이 없고,
        // 요청이 취소되면 HttpContext.RequestAborted 라는 cancellation token이 트리거되는 것 뿐임
        [HttpGet("timeout1")]
        [RequestTimeout(1000)]
        public async Task<IActionResult> TimeoutTest()
        {
            await Task.Delay(2000, HttpContext.RequestAborted);
            return Ok("1. This response should not be returned due to timeout.");
        }
        
        [HttpGet("timeout2")]
        [RequestTimeout("Timeout2")]
        public async Task<IActionResult> Timeout2Test()
        {
            await Task.Delay(3000, HttpContext.RequestAborted);
            return Ok("2. This response should not be returned due to timeout.");
        }
    }
}
