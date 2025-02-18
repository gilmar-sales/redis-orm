using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Redis.Orm.Interfaces;

namespace Redis.Orm;

public class CacheQueryProvider<T>(ILogger<CacheQueryProvider<T>> logger)
    : ICacheQueryProvider<T>
{
    public IQueryable CreateQuery(Expression expression)
    {
        return CreateQuery<T>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        var query = new CacheQuery<TElement>((ICacheQueryProvider<TElement>)this, expression);

        return query;
    }

    public object Execute(Expression expression)
    {
        var visitor = new RedisExpressionVisitor();

        visitor.Visit(expression);

        var query = string.IsNullOrEmpty(visitor.RedisQuery) ? "*" : visitor.RedisQuery;

        logger.LogInformation("Query Redis Gerada: {redisQuery}", query);

        var results = Enumerable.Empty<T>();

        return results.GetEnumerator();
    }

    public TResult Execute<TResult>(Expression expression)
    {
        var results = (TResult?)Execute(expression);

        return results!;
    }

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var visitor = new RedisExpressionVisitor();

        visitor.Visit(expression);

        var query = string.IsNullOrEmpty(visitor.RedisQuery) ? "*" : visitor.RedisQuery;

        logger.LogInformation("Query Redis Gerada: {redisQuery}", query);

        try
        {
            var results = Enumerable.Empty<T>();

            if (typeof(TResult).IsGenericType &&
                typeof(TResult).GetGenericTypeDefinition().Name.Contains("Task") &&
                typeof(TResult).GenericTypeArguments.First() == typeof(T))
                return (TResult)(object)Task.FromResult(results.FirstOrDefault());

            var enumerator =
                (TResult)(object)new AsyncEnumeratorWrapper<T>(results.GetEnumerator());

            return enumerator;
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);

            return (TResult)(object)new AsyncEnumeratorWrapper<T>(Enumerable.Empty<T>().GetEnumerator());
        }
    }

    private sealed class AsyncEnumeratorWrapper<TSource>(IEnumerator<TSource>? enumerator) : IAsyncEnumerator<TSource>
    {
        private readonly IEnumerator<TSource> _enumerator = enumerator ?? throw new ArgumentNullException();

        public TSource Current => _enumerator.Current;

        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(_enumerator.MoveNext());
        }

        public ValueTask DisposeAsync()
        {
            _enumerator.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}