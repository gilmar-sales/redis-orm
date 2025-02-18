using System.Collections.Concurrent;
using Redis.Orm.Interfaces;

namespace Redis.Orm;

public class CacheRegister(ConcurrentDictionary<Type, Type[]> cacheRegistryDictionary) : ICacheRegister
{
    public bool Contains(Type? entityType)
    {
        if (entityType is null) return false;

        return cacheRegistryDictionary.ContainsKey(entityType);
    }

    public bool Contains<TEntity>()
    {
        return Contains(typeof(TEntity));
    }

    public Type[]? GetDtoTypes(Type entityType)
    {
        cacheRegistryDictionary.TryGetValue(entityType, out var dtos);

        return dtos;
    }

    public Type[]? GetDtoTypes<TEntity>()
    {
        return GetDtoTypes(typeof(TEntity));
    }
}