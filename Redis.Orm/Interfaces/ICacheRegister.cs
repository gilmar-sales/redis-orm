namespace Redis.Orm.Interfaces;

public interface ICacheRegister
{
    public bool Contains(Type? entityType);
    public bool Contains<TEntity>();

    public Type[]? GetDtoTypes(Type entityType);
    public Type[]? GetDtoTypes<TEntity>();
}