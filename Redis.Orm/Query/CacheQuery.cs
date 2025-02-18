using System.Collections;
using System.Linq.Expressions;
using Redis.Orm.Interfaces;

namespace Redis.Orm;

public class CacheQuery<T> : ICacheQuery<T>, IAsyncEnumerable<T>
{
    public CacheQuery(ICacheQueryProvider<T> cacheQueryProvider, Expression? expression = null)
    {
        Provider = cacheQueryProvider;
        Expression = expression ?? Expression.Constant(this);
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        if (Provider is not ICacheQueryProvider<T> provider)
            throw new InvalidOperationException();

        return provider.ExecuteAsync<IAsyncEnumerator<T>>(Expression, cancellationToken);
    }

    public Expression Expression { get; }

    public IQueryProvider Provider { get; }

    public Type ElementType => typeof(T);


    public IEnumerator<T> GetEnumerator()
    {
        return Provider.Execute<IEnumerator<T>>(Expression);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}