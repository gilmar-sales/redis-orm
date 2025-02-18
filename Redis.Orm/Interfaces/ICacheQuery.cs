namespace Redis.Orm.Interfaces;

internal interface ICacheQuery<out T> : IQueryable<T>
{
}