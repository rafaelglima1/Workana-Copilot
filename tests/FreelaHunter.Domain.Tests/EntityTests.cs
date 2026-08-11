using FreelaHunter.Domain.Entities;

namespace FreelaHunter.Domain.Tests;

public class EntityTests
{
    [Fact]
    public void Entity_HasDefaultId()
    {
        var entity = new TestEntity();
        Assert.NotEqual(Guid.Empty, entity.Id);
    }

    [Fact]
    public void Entity_HasCreatedAt()
    {
        var before = DateTime.UtcNow;
        var entity = new TestEntity();
        var after = DateTime.UtcNow;

        Assert.InRange(entity.CreatedAt, before, after);
    }

    [Fact]
    public void Entity_UpdatedAt_IsNullByDefault()
    {
        var entity = new TestEntity();
        Assert.Null(entity.UpdatedAt);
    }

    private class TestEntity : Entity { }
}
