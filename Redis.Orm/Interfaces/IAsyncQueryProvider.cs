using System.Linq.Expressions;

namespace Redis.Orm.Interfaces;

public interface IAsyncQueryProvider : IQueryProvider
{
    TResult ExecuteAsync<TResult>(Expression expression,
        CancellationToken cancellationToken = default);
}