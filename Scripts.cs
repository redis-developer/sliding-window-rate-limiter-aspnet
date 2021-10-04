using StackExchange.Redis;

namespace SlidingWindowRateLimiter
{
    public static class Scripts
    {
        public static LuaScript SlidingRateLimiterScript => LuaScript.Prepare(SlidingRateLimiter);
        private const string SlidingRateLimiter = @"
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
            ";
    }
}