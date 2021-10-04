using StackExchange.Redis;

namespace SlidingWindowRateLimiter
{
    public static class Scripts
    {
        public static LuaScript SlidingRateLimiterScript => LuaScript.Prepare(SlidingRateLimiter);
        private const string SlidingRateLimiter = @"
            local current_time = tonumber(redis.call('TIME')[1])
            local trim_time = current_time - @window
            redis.call('ZREMRANGEBYSCORE', @key, 0, trim_time)
            local request_count = redis.call('ZCARD',@key)

            if request_count < tonumber(@max_requests) then
                redis.call('ZADD', @key, current_time, current_time)
                redis.call('EXPIRE', @key, @window)
                return 0
            end
            return 1
            ";
    }
}