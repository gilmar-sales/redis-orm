using Redis.Orm.Interfaces;
using Redis.Orm.Tests.Enums;
using Redis.Orm.Tests.Models;

namespace Redis.Orm.Tests.Redis.Orm;

public class RedisExpressionVisitorTest(
    ICacheQueryProvider<TestModel> grupoUsuarioQueryProvider,
    ICacheQueryProvider<TestChildModel> grupoUsuarioModuloDiretrizQueryProvider)
{
    [Fact(DisplayName = "Query sem condicionais deve gerar '*' para consultar todos os dados")]
    [Trait("Redis.Orm.Query", "Queryable")]
    public void QuerySemCondicionais_DeveRetornarTodosDados()
    {
        var query = new CacheQuery<TestModel>(grupoUsuarioQueryProvider);

        var visitor = new RedisExpressionVisitor();

        visitor.Visit(query.Expression);

        Assert.Equal("*", visitor.RedisQuery);
    }

    [Fact(DisplayName = "Query com igualdade em uma string deve gerar a query redis '@campo:texto'")]
    [Trait("Redis.Orm.Query", "Queryable")]
    public void QueryComIgualdadeString_DeveFiltrarCampo()
    {
        var query = new CacheQuery<TestModel>(grupoUsuarioQueryProvider).AsQueryable()
            .Where(gu => gu.Code == "001");

        var visitor = new RedisExpressionVisitor();

        visitor.Visit(query.Expression);

        Assert.Equal("@Code:001", visitor.RedisQuery);
    }

    [Fact(DisplayName = "Query com igualdade em um número deve gerar a query redis '@campo:[num num]'")]
    [Trait("Redis.Orm.Query", "Queryable")]
    public void QueryComIgualdadeNumerico_DeveFiltrarCampo()
    {
        var query = new CacheQuery<TestModel>(grupoUsuarioQueryProvider)
            .AsQueryable()
            .Where(gu => gu.Id == 100)
            .Where(gu => gu.NullableNumber == 200);

        var visitor = new RedisExpressionVisitor();

        visitor.Visit(query.Expression);

        Assert.Equal("@Id:[100 100] @NullableNumber:[200 200]", visitor.RedisQuery);
    }

    [Fact(DisplayName = "Query com igualdade em um enum deve gerar a query redis de string '@campo:EnumName'")]
    [Trait("Redis.Orm.Query", "Queryable")]
    public void QueryComIgualdadeEnum_DeveFiltrarCampo()
    {
        var query = new CacheQuery<TestModel>(grupoUsuarioQueryProvider)
            .AsQueryable()
            .Where(gu => gu.Status == Status.Active);

        var visitor = new RedisExpressionVisitor();

        visitor.Visit(query.Expression);

        Assert.Equal("@Status:Active", visitor.RedisQuery);
    }

    [Fact(DisplayName = "Query com condicional 'Any'deve gerar a query redis de string '@campo:texto'")]
    [Trait("Redis.Orm.Query", "Queryable")]
    public void QueryComAny_DeveFiltrarCampo()
    {
        var query = new CacheQuery<TestModel>(grupoUsuarioQueryProvider)
            .AsQueryable()
            .Where(gu => gu.Children.Any(gum => gum.Code == "001"));

        var visitor = new RedisExpressionVisitor();

        visitor.Visit(query.Expression);

        Assert.Equal("@ChildrenCode:001", visitor.RedisQuery);
    }

    [Fact(DisplayName =
        "Query com condicional 'Contains' deve gerar a query redis OR '(@campo:[num num] | @campo:[num2 num2]...)'")]
    [Trait("Redis.Orm.Query", "Queryable")]
    public void QueryComContains_DeveGerarExpressarOr()
    {
        var gruposUsuarioIds = new List<long>
        {
            1, 2, 3, 4
        };

        var query =
            new CacheQuery<TestChildModel>(grupoUsuarioModuloDiretrizQueryProvider)
                .AsQueryable()
                .Where(gumd => gruposUsuarioIds.Contains(gumd.ParentId));

        var visitor = new RedisExpressionVisitor();

        visitor.Visit(query.Expression);

        Assert.Equal(
            "(@ParentId:[1 1] | @ParentId:[2 2] | @ParentId:[3 3] | @ParentId:[4 4])",
            visitor.RedisQuery);
    }

    [Fact(DisplayName =
        "Query com condicional 'Contains' com 'Select' deve gerar a query redis OR '(@campo:[num num] | @campo:[num2 num2]...)'")]
    [Trait("Redis.Orm.Query", "Queryable")]
    public void QueryComContainsComSelect_DeveGerarExpressarOr()
    {
        var gruposUsuarioIds = new List<TestModel>
        {
            new()
            {
                Id = 1
            },
            new()
            {
                Id = 2
            },
            new()
            {
                Id = 3
            },
            new()
            {
                Id = 4
            }
        };

        var query =
            new CacheQuery<TestChildModel>(grupoUsuarioModuloDiretrizQueryProvider)
                .AsQueryable()
                .Where(gumd => gruposUsuarioIds.Select(g => g.Id).Contains(gumd.ParentId));

        var visitor = new RedisExpressionVisitor();

        visitor.Visit(query.Expression);

        Assert.Equal("(@ParentId:[1 1] | @ParentId:[2 2] | @ParentId:[3 3] | @ParentId:[4 4])",
            visitor.RedisQuery);
    }

    [Fact(DisplayName =
        "Query com condicional 'Contains' com arranjo vazio deve gerar a query redis '(@campo:[0 0])'")]
    [Trait("Redis.Orm.Query", "Queryable")]
    public void QueryComContainsVazio_DeveGerarExpressaoZero()
    {
        var query =
            new CacheQuery<TestChildModel>(grupoUsuarioModuloDiretrizQueryProvider)
                .AsQueryable()
                .Where(gumd => Enumerable.Empty<long>().Contains(gumd.ParentId));

        var visitor = new RedisExpressionVisitor();

        visitor.Visit(query.Expression);

        Assert.Equal("(@ParentId:[0 0])",
            visitor.RedisQuery);
    }

    [Fact(DisplayName = "Query com operador AND deve gerar a query redis '@campo1 @campo2'")]
    [Trait("Redis.Orm.Query", "Queryable")]
    public void QueryComANDNumerico_DeveFiltrarCampo()
    {
        var query = new CacheQuery<TestModel>(grupoUsuarioQueryProvider)
            .AsQueryable()
            .Where(gu => gu.Id == 100 && gu.NullableNumber == 200);

        var visitor = new RedisExpressionVisitor();

        visitor.Visit(query.Expression);

        Assert.Equal("(@Id:[100 100] @NullableNumber:[200 200])", visitor.RedisQuery);
    }

    [Fact(DisplayName = "Query com operador AND e OR deve respeitar a precendencia")]
    [Trait("Redis.Orm.Query", "Queryable")]
    public void QueryComANDPrecedeORNumerico_DeveFiltrarCampo()
    {
        var query = new CacheQuery<TestModel>(grupoUsuarioQueryProvider)
            .AsQueryable()
            .Where(gu => gu.Id == 101 || (gu.Id == 100 && gu.NullableNumber == 200));

        var visitor = new RedisExpressionVisitor();

        visitor.Visit(query.Expression);

        Assert.Equal("(@Id:[101 101] | (@Id:[100 100] @NullableNumber:[200 200]))", visitor.RedisQuery);
    }

    [Fact(DisplayName = "Query com operador AND e OR deve respeitar os parentesis")]
    [Trait("Redis.Orm.Query", "Queryable")]
    public void QueryComANDParentesORNumerico_DeveFiltrarCampo()
    {
        var query = new CacheQuery<TestModel>(grupoUsuarioQueryProvider)
            .AsQueryable()
            .Where(gu => (gu.Id == 101 || gu.Id == 100) && gu.NullableNumber == 200);

        var visitor = new RedisExpressionVisitor();

        visitor.Visit(query.Expression);

        Assert.Equal("((@Id:[101 101] | @Id:[100 100]) @NullableNumber:[200 200])", visitor.RedisQuery);
    }

    [Fact(DisplayName = "Query com 'Contains' em string deve gerar @campo:*texto*")]
    [Trait("Redis.Orm.Query", "Queryable")]
    public void QueryContainsString_DeveFiltrarCampo()
    {
        var query = new CacheQuery<TestModel>(grupoUsuarioQueryProvider)
            .AsQueryable()
            .Where(gu => gu.Code!.Contains("100"));

        var visitor = new RedisExpressionVisitor();

        visitor.Visit(query.Expression);

        Assert.Equal("@Code:*100*", visitor.RedisQuery);
    }
}