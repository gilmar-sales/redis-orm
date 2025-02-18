using System.Collections;
using System.Linq.Expressions;
using System.Text;

namespace Redis.Orm;

public class RedisExpressionVisitor : ExpressionVisitor
{
    private readonly StringBuilder _stringBuilder = new();

    public string RedisQuery => _stringBuilder.Length > 0 ? _stringBuilder.ToString().Trim() : "*";

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name.Contains("First"))
            foreach (var item in node.Arguments)
            {
                var visitor = new RedisExpressionVisitor();
                visitor.Visit(item);
                _stringBuilder.Append($"{visitor._stringBuilder}");
            }

        if (node.Method.Name == "Where")
            foreach (var item in node.Arguments)
            {
                var visitor = new RedisExpressionVisitor();
                visitor.Visit(item);
                _stringBuilder.Append($"{visitor._stringBuilder}");
            }

        if (node.Method.Name == "Any")
        {
            foreach (var item in node.Arguments)
            {
                var visitor = new RedisExpressionVisitor();
                visitor.Visit(item);

                var query = visitor._stringBuilder.ToString();

                if (item != node.Arguments.First())
                    query = query.Replace("@", "");

                _stringBuilder.Append($"{query.Trim()}");
            }

            _stringBuilder.Append(' ');
        }

        if (node.Method.Name == "Contains")
            try
            {
                var value = node.Object is not null
                    ? Expression.Lambda(node.Object).Compile().DynamicInvoke()
                    : Expression.Lambda(node.Arguments.First()).Compile().DynamicInvoke();

                if (value is IEnumerable<long> values)
                {
                    var visitor = new RedisExpressionVisitor();
                    visitor.Visit(node.Arguments.Last());

                    var enumerable = values as long[] ?? values.ToArray();

                    if (enumerable.Length == 0)
                    {
                        _stringBuilder.Append($"({visitor.RedisQuery}:[0 0])");
                        return node;
                    }

                    var queries = enumerable.Select(val => $"{visitor.RedisQuery}:[{val} {val}]");

                    _stringBuilder.Append($"({string.Join(" | ", queries)})");

                    return node;
                }
            }
            catch (Exception)
            {
                var visitor = new RedisExpressionVisitor();
                visitor.Visit(node.Object);

                var value = Expression.Lambda(node.Arguments.First()).Compile().DynamicInvoke();

                _stringBuilder.Append($"{visitor._stringBuilder}:*{value}*");
            }

        return node;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (node.Value is string value)
            _stringBuilder.Append($"{value}");
        else if (node.Value is Enum)
            _stringBuilder.Append($"{Enum.GetName(node.Value.GetType(), node.Value)}");
        else if (node.Value is long || node.Value is int || node.Value is decimal || node.Value is float)
            _stringBuilder.Append($"{node.Value}");
        else if (node.Value is IEnumerable enumerable and not IQueryable)
            _stringBuilder.Append(string.Join("|", enumerable));

        return base.VisitConstant(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression is ParameterExpression)
            _stringBuilder.Insert(0, $"@{node.Member.Name}");
        else
            try
            {
                if (node.Member.DeclaringType is { IsClass: false })
                {
                    var value = Expression.Lambda(node).Compile().DynamicInvoke();

                    _stringBuilder.Append(value);
                    return node;
                }
            }
            catch (Exception)
            {
                _stringBuilder.Insert(0, $"{node.Member.Name}");
            }

        return base.VisitMember(node);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        var visitorL = new RedisExpressionVisitor();

        visitorL.Visit(node.Left);

        var visitorR = new RedisExpressionVisitor();

        visitorR.Visit(node.Right);

        switch (node.NodeType)
        {
            case ExpressionType.Equal:
            {
                if (node.Left.Type.IsAssignableTo(typeof(long?)))
                    _stringBuilder.Append(
                        $"{visitorL._stringBuilder}:[{visitorR._stringBuilder} {visitorR._stringBuilder}] ");
                else
                    _stringBuilder.Append($"{visitorL._stringBuilder}:{visitorR._stringBuilder} ");

                break;
            }
            case ExpressionType.And:
            case ExpressionType.AndAlso:
            {
                _stringBuilder.Append(
                    $"({visitorL._stringBuilder.ToString().Trim()} {visitorR._stringBuilder.ToString().Trim()}) ");
                break;
            }
            case ExpressionType.Or:
            case ExpressionType.OrElse:
            {
                _stringBuilder.Append(
                    $"({visitorL._stringBuilder.ToString().Trim()} | {visitorR._stringBuilder.ToString().Trim()}) ");
                break;
            }
        }

        return node;
    }
}