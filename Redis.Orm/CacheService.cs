using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using NRedisStack.Search.Literals.Enums;
using Redis.Orm.Extensions;
using Redis.Orm.Interfaces;
using StackExchange.Redis;

namespace Redis.Orm;

public partial class CacheService(
    IConnectionMultiplexer redis,
    IOptions<RedisOptions> redisOptions,
    ILogger<CacheService> logger,
    IRedisSchemaProvider redisSchemaProvider,
    ICacheRegister cacheRegister) : ICacheService
{
    private readonly JsonSerializerOptions _jsonSerializerOptions =
        new()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

    public async Task SetMaxMemory(long maxMemory)
    {
        await redis.GetDatabase().ExecuteAsync("CONFIG", "SET", "maxmemory", maxMemory * 1024 * 1024);
    }

    public bool IsCacheable(Type? entityType)
    {
        return cacheRegister.Contains(entityType);
    }

    public bool IsCacheable<TEntity>()
    {
        return IsCacheable(typeof(TEntity));
    }

    public (long maxMemory, long memoryUsed) GetMemoryStats()
    {
        var memoryStatsString = redis.GetDatabase().Execute("INFO", "memory").ToString();

        (long maxMemory, long memoryUsed) memoryStats = (0, 0);

        if (string.IsNullOrEmpty(memoryStatsString))
            return memoryStats;

        var usedMemoryString = UsedMemoryRegex().Match(memoryStatsString);

        if (usedMemoryString.Success)
        {
            var valores = usedMemoryString.Value.Split(':');

            if (valores.Length > 1)
                memoryStats.memoryUsed = Convert.ToInt64(valores[1]);
        }

        var maxMemoryString = MaxMemoryRegex().Match(memoryStatsString);

        if (maxMemoryString.Success)
        {
            var valores = maxMemoryString.Value.Split(':');

            if (valores.Length > 1)
                memoryStats.maxMemory = Convert.ToInt64(valores[1]);
        }

        return memoryStats;
    }

    public async Task SetCachedDataAsync<TEntity>(TEntity? data)
    {
        // await SetCachedDataAsync(typeof(TEntity), data);
    }

    public Task WarmUp(Type dtoType)
    {
        var method = GetType()
            .GetMethod(nameof(WarmUpImpl), BindingFlags.Instance | BindingFlags.NonPublic)?
            .MakeGenericMethod(dtoType.BaseType!.GenericTypeArguments.First(), dtoType);

        if (method is not null && method.Invoke(this, []) is Task task)
            return task;

        return Task.CompletedTask;
    }

    private string GetSchemaHash(Type type)
    {
        var schema = redisSchemaProvider.Generate(type);

        var propsHash = string.Join('|', schema.Fields.Select(f => f.FieldName)).GetDeterministicHashCode();

        return propsHash.ToString();
    }

    private void SetCollectionWarmUp(Type type)
    {
        redis.GetDatabase()
            .StringSet(
                $"{GetCachePattern(type)}WarmUp",
                DateTime.Now.Ticks.ToString(),
                redisOptions.Value.ExpirationTime);
    }

    private async Task WarmUpImpl<TEntity, TDto>()
    {
        await RebuildIndex<TDto>();

        try
        {
            var oldData = await redis.GetDatabase().FT().SearchAsync(
                GetCacheIndex(typeof(TDto)),
                new Query("*")
                    .Limit(0, 10000)
            );

            var keys = oldData.Documents.Select(d => new RedisKey(d.Id)).ToArray();
            await redis.GetDatabase().KeyDeleteAsync(keys);
        }
        catch (Exception)
        {
            // ignored
        }

        // var query = unitOfWork.GetRepository<TEntity>().GetQueryable();
        // var projection = mapper.ProjectTo(query, typeof(TDto));
        //
        // var newDtos = projection.Cast<IDtoBase>().ToList();
        //
        // var batches = newDtos
        //     .Select(dto => new KeyPathValue(GetCacheKey<TDto>(dto.Id), "$",
        //         JsonSerializer.Serialize(dto, typeof(TDto), _jsonSerializerOptions)))
        //     .Chunk(redisOptions.Value.MaxBatchSize);
        //
        // foreach (var batch in batches)
        //     await redis.GetDatabase().JSON().MSetAsync(batch);
    }

    private async Task RebuildIndex<TDto>()
    {
        try
        {
            await redis.GetDatabase()
                .FT()
                .DropIndexAsync(GetCacheIndex(typeof(TDto)));
        }
        catch (Exception error)
        {
            logger.LogWarning("{error}", error);
        }

        try
        {
            await redis.GetDatabase()
                .FT()
                .CreateAsync(
                    GetCacheIndex(typeof(TDto)),
                    new FTCreateParams().On(IndexDataType.JSON).AddPrefix(GetCachePattern<TDto>()),
                    redisSchemaProvider.Generate(typeof(TDto)));
        }
        catch (Exception error)
        {
            logger.LogWarning("{error}", error);
        }
    }

    private IQueryable CastQuery(IQueryable query, Type type)
    {
        var method = typeof(Queryable)
            .GetMethods()
            .FirstOrDefault(m => m.Name.StartsWith("Cast") && m.ReturnType.Name.StartsWith("IQuery"));

        if (method is not null)
            query = (IQueryable)method.MakeGenericMethod(type).Invoke(null, [query])!;

        return query;
    }

    private string GetCacheKey<T>(object? identifier)
    {
        return GetCacheKey(typeof(T), identifier);
    }

    private string GetCacheKey(Type type, object? identifier)
    {
        return $"{GetCachePattern(type)}{identifier}";
    }

    private string? GetCollectionWarmUp(Type type)
    {
        var warmUpString = redis.GetDatabase().StringGet($"{GetCachePattern(type)}WarmUp").ToString();

        if (string.IsNullOrEmpty(warmUpString))
            return null;

        return warmUpString;
    }

    private string GetCacheIndex(Type type)
    {
        return $"{GetCachePattern(type)}Idx";
    }

    private string GetCachePattern<T>()
    {
        return GetCachePattern(typeof(T));
    }

    private string GetCachePattern(Type type)
    {
        return $"{type.FullName}:{GetSchemaHash(type)}:";
    }

    [GeneratedRegex(@"used_memory:\d+")]
    private static partial Regex UsedMemoryRegex();

    [GeneratedRegex(@"maxmemory:\d+")]
    private static partial Regex MaxMemoryRegex();
}