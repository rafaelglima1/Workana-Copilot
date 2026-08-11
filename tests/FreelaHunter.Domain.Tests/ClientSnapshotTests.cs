using FreelaHunter.Domain.ValueObjects;
using FreelaHunter.Domain.Exceptions;

namespace FreelaHunter.Domain.Tests;

public class ClientSnapshotTests
{
    [Fact]
    public void Create_WithValidValues_ReturnsSnapshot()
    {
        var client = ClientSnapshot.Create(
            displayName: "John Doe",
            country: "BR",
            publishedProjects: 10,
            paidProjects: 8,
            rating: 4.5m);

        Assert.Equal("John Doe", client.DisplayName);
        Assert.Equal("BR", client.Country);
        Assert.Equal(10, client.PublishedProjects);
        Assert.Equal(8, client.PaidProjects);
        Assert.Equal(4.5m, client.Rating);
    }

    [Fact]
    public void Create_NullValues_AreAccepted()
    {
        var client = ClientSnapshot.Create();
        Assert.Null(client.DisplayName);
        Assert.Null(client.Country);
        Assert.Null(client.PublishedProjects);
        Assert.Null(client.PaidProjects);
        Assert.Null(client.Rating);
    }

    [Fact]
    public void Create_WhitespaceStrings_NormalizedToNull()
    {
        var client = ClientSnapshot.Create(displayName: "   ", country: "  ");
        Assert.Null(client.DisplayName);
        Assert.Null(client.Country);
    }

    [Fact]
    public void Create_EmptyStrings_NormalizedToNull()
    {
        var client = ClientSnapshot.Create(displayName: "", country: "");
        Assert.Null(client.DisplayName);
        Assert.Null(client.Country);
    }

    [Fact]
    public void Create_StringsAreTrimmed()
    {
        var client = ClientSnapshot.Create(displayName: "  John  ", country: "  BR  ");
        Assert.Equal("John", client.DisplayName);
        Assert.Equal("BR", client.Country);
    }

    [Fact]
    public void Create_NegativePublishedProjects_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => ClientSnapshot.Create(publishedProjects: -1));
        Assert.Contains("negative", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_NegativePaidProjects_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => ClientSnapshot.Create(paidProjects: -1));
        Assert.Contains("negative", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_PaidProjectsGreaterThanPublished_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            ClientSnapshot.Create(publishedProjects: 5, paidProjects: 10));
        Assert.Contains("PaidProjects", ex.Message);
        Assert.Contains("PublishedProjects", ex.Message);
    }

    [Fact]
    public void Create_PaidProjectsEqualToPublished_IsValid()
    {
        var client = ClientSnapshot.Create(publishedProjects: 5, paidProjects: 5);
        Assert.Equal(5, client.PublishedProjects);
        Assert.Equal(5, client.PaidProjects);
    }

    [Fact]
    public void Create_RatingBelowZero_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => ClientSnapshot.Create(rating: -0.1m));
        Assert.Contains("Rating", ex.Message);
    }

    [Fact]
    public void Create_RatingAboveFive_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => ClientSnapshot.Create(rating: 5.1m));
        Assert.Contains("Rating", ex.Message);
    }

    [Fact]
    public void Create_RatingBoundaryZero_IsValid()
    {
        var client = ClientSnapshot.Create(rating: 0m);
        Assert.Equal(0m, client.Rating);
    }

    [Fact]
    public void Create_RatingBoundaryFive_IsValid()
    {
        var client = ClientSnapshot.Create(rating: 5m);
        Assert.Equal(5m, client.Rating);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var c1 = ClientSnapshot.Create(displayName: "John", country: "BR");
        var c2 = ClientSnapshot.Create(displayName: "John", country: "BR");
        Assert.Equal(c1, c2);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var c1 = ClientSnapshot.Create(displayName: "John");
        var c2 = ClientSnapshot.Create(displayName: "Jane");
        Assert.NotEqual(c1, c2);
    }
}
