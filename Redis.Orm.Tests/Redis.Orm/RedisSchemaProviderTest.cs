using Redis.Orm.Interfaces;
using Redis.Orm.Tests.Models;
using static NRedisStack.Search.Schema;

namespace Redis.Orm.Tests.Redis.Orm;

public class RedisSchemaProviderTest(IRedisSchemaProvider redisSchemaProvider)
{
    [Fact(DisplayName = "SchemaProvider deve gerar campos simples no formato: '$.Name' e 'Name'")]
    [Trait("Infra.Data.Caching", "RedisSchema")]
    public void RedisSchemaProvider_DeveGerarCamposSimples()
    {
        var schema = redisSchemaProvider.Generate(typeof(TestModel));

        Assert.Contains(schema.Fields, field =>
        {
            var (name, alias) = GetFieldValues(field);

            return name == "$.Id" && alias == "Id";
        });

        Assert.Contains(schema.Fields, field =>
        {
            var (name, alias) = GetFieldValues(field);

            return name == "$.Code" && alias == "Code";
        });
    }


    [Fact(DisplayName = "SchemaProvider deve gerar campos enums no formato: '$.Name' e 'Name'")]
    [Trait("Infra.Data.Caching", "RedisSchema")]
    public void RedisSchemaProvider_DeveGerarCamposEnums()
    {
        var schema = redisSchemaProvider.Generate(typeof(TestModel));

        Assert.Contains(schema.Fields, field =>
        {
            var (name, alias) = GetFieldValues(field);

            return name == "$.Status" && alias == "Status";
        });
    }


    private (string? name, string? alias) GetFieldValues(Field field)
    {
        return (field.FieldName.Name, field.FieldName.Alias);
    }
}