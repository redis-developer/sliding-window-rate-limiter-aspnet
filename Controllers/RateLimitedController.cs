using System;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace SlidingWindowRateLimiter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RateLimitedController : ControllerBase
    {
        private readonly IDatabase _db;

        public RateLimitedController(IConnectionMultiplexer muxer)
        {
            _db = muxer.GetDatabase();
        }

        [HttpPost]
        [HttpGet]
        [Route("sliding")]
        public async Task<IActionResult> Sliding([FromHeader] string authorization)
        {
            var encoded = string.Empty;
            if(!string.IsNullOrEmpty(authorization)) encoded = AuthenticationHeaderValue.Parse(authorization).Parameter;
            if (string.IsNullOrEmpty(encoded)) return new UnauthorizedResult();
            var apiKey = Encoding.UTF8.GetString(Convert.FromBase64String(encoded)).Split(':')[0];
            var limited = ((int) await _db.ScriptEvaluateAsync(Scripts.SlidingRateLimiterScript,
                new {key = new RedisKey($"{Request.Path}:{apiKey}"), window = 30, max_requests = 10})) == 1;
            return limited ? new StatusCodeResult(429) : Ok();
        }
    }
}