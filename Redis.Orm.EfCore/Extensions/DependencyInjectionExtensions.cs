using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Redis.Orm.EfCore.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection UseDbContext<TDbContext>(this IServiceCollection services,
        Action<RedisOptions> setupAction) where TDbContext : DbContext
    {
        return services;
    }
}