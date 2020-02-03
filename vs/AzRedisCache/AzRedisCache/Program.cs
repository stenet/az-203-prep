using StackExchange.Redis;
using System;

namespace AzRedisCache
{
    class Program
    {
        const string CONNECTION_STRING = "";

        static void Main(string[] args)
        {
            var redis = ConnectionMultiplexer.Connect(CONNECTION_STRING);
            var cache = redis.GetDatabase();

            cache.StringSet("mykey", "WERT", expiry: TimeSpan.FromMinutes(30));
            var value = cache.StringGet("mykey");
        }
    }
}
