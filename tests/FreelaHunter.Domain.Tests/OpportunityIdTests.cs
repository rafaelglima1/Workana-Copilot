using FreelaHunter.Domain.ValueObjects;
using FreelaHunter.Domain.Exceptions;

namespace FreelaHunter.Domain.Tests;

public class OpportunityIdTests
{
    [Fact]
    public void New_GeneratesValidId()
    {
        var id = OpportunityId.New();
        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void New_GeneratesUniqueIds()
    {
        var id1 = OpportunityId.New();
        var id2 = OpportunityId.New();
        Assert.NotEqual(id1.Value, id2.Value);
    }

    [Fact]
    public void From_WithValidGuid_ReturnsId()
    {
        var guid = Guid.NewGuid();
        var id = OpportunityId.From(guid);
        Assert.Equal(guid, id.Value);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => OpportunityId.From(Guid.Empty));
        Assert.Contains("empty", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var guid = Guid.NewGuid();
        var id1 = OpportunityId.From(guid);
        var id2 = OpportunityId.From(guid);
        Assert.Equal(id1, id2);
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        var id1 = OpportunityId.New();
        var id2 = OpportunityId.New();
        Assert.NotEqual(id1, id2);
    }
}
