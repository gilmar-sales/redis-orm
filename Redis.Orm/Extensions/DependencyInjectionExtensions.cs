using Microsoft.Extensions.DependencyInjection;
using Redis.Orm.Interfaces;

namespace Redis.Orm.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddRedisOrm(this IServiceCollection services, Action<RedisOptions> setupAction)
    {
        services.AddTransient(typeof(ICacheQuery<>), typeof(CacheQuery<>));
        services.AddTransient(typeof(ICacheQueryProvider<>), typeof(CacheQueryProvider<>));
        services.AddScoped<IRedisSchemaProvider, RedisSchemaProvider>();

        return services;
    }
}