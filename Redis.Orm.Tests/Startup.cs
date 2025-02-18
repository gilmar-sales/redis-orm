using Microsoft.Extensions.DependencyInjection;
using Redis.Orm.Extensions;

namespace Redis.Orm.Tests;

public class Startup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddRedisOrm(options => { options.Connection = "127.0.0.1:6379"; });
    }
}