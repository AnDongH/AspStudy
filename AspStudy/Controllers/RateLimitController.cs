using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AspStudy.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class RateLimitController : ControllerBase
    {

        [HttpGet("rate-limit/fixed")]
        [EnableRateLimiting("fixed")]
        public IActionResult FixedTest()
        {
            var result = (DateTime.Now.Ticks & 0x11111).ToString("00000");
            return Ok(result);
        }
        
        [HttpGet("rate-limit/sliding")]
        [EnableRateLimiting("sliding")]
        public IActionResult SlidingTest()
        {
            var result = (DateTime.Now.Ticks & 0x11111).ToString("00000");
            return Ok(result);
        }
        
        [HttpGet("rate-limit/token")]
        [EnableRateLimiting("token")]
        public IActionResult TokenTest()
        {
            var result = (DateTime.Now.Ticks & 0x11111).ToString("00000");
            return Ok(result);
        }
        
        [HttpGet("rate-limit/concurrency")]
        [EnableRateLimiting("concurrency")]
        public IActionResult ConcurrencyTest()
        {
            var result = (DateTime.Now.Ticks & 0x11111).ToString("00000");
            return Ok(result);
        }
        
        [HttpGet("rate-limit/per-user")]
        [EnableRateLimiting("per-user")]
        public IActionResult PartitionTest()
        {
            var result = (DateTime.Now.Ticks & 0x11111).ToString("00000");
            return Ok(result);
        }
    }
}
