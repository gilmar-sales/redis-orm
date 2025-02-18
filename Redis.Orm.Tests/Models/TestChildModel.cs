using Redis.Orm.Tests.Enums;

namespace Redis.Orm.Tests.Models;

public class TestChildModel
{
    public long ParentId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public Status? Status { get; set; }
}