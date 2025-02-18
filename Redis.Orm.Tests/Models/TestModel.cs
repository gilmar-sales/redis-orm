using Redis.Orm.Tests.Enums;

namespace Redis.Orm.Tests.Models;

public class TestModel
{
    public long Id { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public long? NullableNumber { get; set; }
    public Status? Status { get; set; }
    public ICollection<TestChildModel> Children { get; set; } = [];
}