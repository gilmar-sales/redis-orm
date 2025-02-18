using System.Reflection;
using NRedisStack.Search;

namespace Redis.Orm.Interfaces;

public interface IRedisSchemaProvider
{
    Schema Generate(Type type, string jsonPath = "", string alias = "");

    string GetJsonPath(string jsonPath, PropertyInfo propertyInfo);
}