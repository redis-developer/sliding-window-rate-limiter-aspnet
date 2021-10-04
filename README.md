# Sliding Window Rate Limiter ASP.NET Core Example

This example demonstrates how to set up a sliding window rate limiter for ASP.NET Core apps. This example uses [Basic Authentication](https://en.wikipedia.org/wiki/Basic_access_authentication) to limit api requests per-route per-api key.

## Implementation Details

The `RateLimitedController` has a single route called `Sliding` accessible at `http://localhost:5000/api/ratelimited/sliding`, this endpoint extracts the api key from the authorization header, and checks to see if the api key should be rate limited. If the request is rate limited, the endpoint returns a `429` error code. Otherwise it returns a `200`.

```csharp
[HttpPost]
[HttpGet]
[Route("sliding")]
public async Task<IActionResult> Sliding([FromHeader] string authorization)
{
    var encoded = string.Empty;
    if(!string.IsNullOrEmpty(authorization)) encoded = AuthenticationHeaderValue.Parse(authorization).Parameter;
    if (string.IsNullOrEmpty(encoded)) return new UnauthorizedResult();
    var apiKey = Encoding.UTF8.GetString(Convert.FromBase64String(encoded)).Split(':')[0];
    var limit = ((int) await _db.ScriptEvaluateAsync(Scripts.SlidingRateLimiterScript,
        new {key = new RedisKey($"{Request.Path}:{apiKey}"), window = 30, max_requests = 10})) == 1;
    return limit ? new StatusCodeResult(429) : Ok();
}
```

This flow uses a script run through the script preparation engine of the [StackExchange.Redis](https://stackexchange.github.io/StackExchange.Redis/Scripting.html) and is consequentially usable without the typical deference to the KEYS/ARGV arrays. This script, maintains a sorted set, which is trimmed to the current time window, adds accepted requests to it, set's the expiriation of the sorted set to the time window, and returns `0` if not rate limited and `1` if rate limited:

```text
local current_time = redis.call('TIME')
local trim_time = tonumber(current_time[1]) - @window
redis.call('ZREMRANGEBYSCORE', @key, 0, trim_time)
local request_count = redis.call('ZCARD',@key)

if request_count < tonumber(@max_requests) then
    redis.call('ZADD', @key, current_time[1], current_time[1] .. current_time[2])
    redis.call('EXPIRE', @key, @window)
    return 0
end
return 1
```

## Testing

To test simply use `dotnet run`

and then a series of API requests to the endpoint `http://localhost:5000/api/ratelimited/sliding`

You can use the following cURL command to automate this:

```
for n in {1..21}; do echo $(curl -s -w " HTTP %{http_code}, %{time_total} s" -X POST -H "Content-Length: 0" --user "foobar:password" http://localhost:5000/api/ratelimited/sliding); sleep 0.5; done
```

This will elicit 10 200 responses, and 11 429 responses, if you pause and run the test again you may see differeing results depending on where in the window you are.