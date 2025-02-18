using System.Collections;
using System.Reflection;
using NRedisStack.Search;
using Redis.Orm.Interfaces;

namespace Redis.Orm;

public class RedisSchemaProvider : IRedisSchemaProvider
{
    public Schema Generate(Type type, string jsonPath, string alias)
    {
        var schema = new Schema();

        foreach (var item in type.GetProperties())
            switch (item.PropertyType)
            {
                case var _ when item.PropertyType.IsEnum:
                case var _ when item.PropertyType.GenericTypeArguments.FirstOrDefault()?.IsEnum == true:
                case var _ when item.PropertyType == typeof(string):
                {
                    schema.AddTextField(new FieldName(GetJsonPath(jsonPath, item), $"{alias}{item.Name}"));
                    break;
                }
                case var _ when item.PropertyType.IsAssignableTo(typeof(long?)):
                {
                    schema.AddNumericField(new FieldName(GetJsonPath(jsonPath, item), $"{alias}{item.Name}"));
                    break;
                }
                case var _ when item.PropertyType.IsAssignableTo(typeof(IEnumerable)):
                {
                    var nestedSchema = Generate(item.PropertyType.GenericTypeArguments[0], GetJsonPath(jsonPath, item),
                        $"{alias}{item.Name}");

                    foreach (var nestedField in nestedSchema.Fields)
                        schema.AddField(nestedField);

                    break;
                }
                case var _ when item.PropertyType.IsClass &&
                                !item.PropertyType.IsAssignableTo(typeof(IEnumerable)):
                {
                    var nestedSchema = Generate(item.PropertyType, GetJsonPath(jsonPath, item), $"{alias}{item.Name}");

                    foreach (var nestedField in nestedSchema.Fields)
                        schema.AddField(nestedField);

                    break;
                }
            }

        return schema;
    }

    public string GetJsonPath(string jsonPath, PropertyInfo propertyInfo)
    {
        if (!jsonPath.StartsWith("$."))
            jsonPath = $"${jsonPath}";

        jsonPath = $"{jsonPath}.{propertyInfo.Name}";

        if (propertyInfo.PropertyType.IsAssignableTo(typeof(IEnumerable)) &&
            propertyInfo.PropertyType != typeof(string))
            jsonPath = $"{jsonPath}[*]";

        return jsonPath;
    }
}