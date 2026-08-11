using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Services;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Domain.Tests;

public class SkillMatcherTests
{
    [Fact]
    public void Match_ExactMatch_ReturnsExactKind()
    {
        var profileSkills = new[]
        {
            SkillCapability.Create("C#", SkillLevel.Expert),
            SkillCapability.Create("JavaScript", SkillLevel.Expert)
        };

        var result = SkillMatcher.Match("C#", profileSkills);

        Assert.NotNull(result);
        Assert.Equal(SkillMatchKind.Exact, result.MatchKind);
        Assert.Equal("C#", result.MatchedProfileSkill);
    }

    [Fact]
    public void Match_AliasMatch_ReturnsAliasKind()
    {
        var profileSkills = new[]
        {
            SkillCapability.Create("C#", SkillLevel.Expert),
            SkillCapability.Create("JavaScript", SkillLevel.Expert)
        };

        var result = SkillMatcher.Match("CSharp", profileSkills);

        Assert.NotNull(result);
        Assert.Equal(SkillMatchKind.Alias, result.MatchKind);
        Assert.Equal("C#", result.MatchedProfileSkill);
    }

    [Fact]
    public void Match_CaseInsensitiveExact_ReturnsExactKind()
    {
        var profileSkills = new[]
        {
            SkillCapability.Create("C#", SkillLevel.Expert),
            SkillCapability.Create("JavaScript", SkillLevel.Expert)
        };

        var result = SkillMatcher.Match("c#", profileSkills);

        Assert.NotNull(result);
        Assert.Equal(SkillMatchKind.Exact, result.MatchKind);
    }

    [Fact]
    public void Match_Unknown_ReturnsNoMatch()
    {
        var profileSkills = new[]
        {
            SkillCapability.Create("C#", SkillLevel.Expert),
            SkillCapability.Create("JavaScript", SkillLevel.Expert)
        };

        var result = SkillMatcher.Match("Python", profileSkills);

        Assert.NotNull(result);
        Assert.Equal(SkillMatchKind.NoMatch, result.MatchKind);
        Assert.Null(result.MatchedProfileSkill);
    }

    [Fact]
    public void Match_EmptySkills_ReturnsNoMatch()
    {
        var result = SkillMatcher.Match("C#", []);

        Assert.NotNull(result);
        Assert.Equal(SkillMatchKind.NoMatch, result.MatchKind);
    }

    [Fact]
    public void Match_WhitespaceInSkillName_CollapsesCorrectly()
    {
        var profileSkills = new[]
        {
            SkillCapability.Create("React JS", SkillLevel.Expert)
        };

        var result = SkillMatcher.Match("React   JS", profileSkills);

        Assert.NotNull(result);
        Assert.Equal(SkillMatchKind.Exact, result.MatchKind);
    }

    [Fact]
    public void Match_SkillLevelAffectsScore()
    {
        var strongSkills = new[]
        {
            SkillCapability.Create("C#", SkillLevel.Strong)
        };

        var expertSkills = new[]
        {
            SkillCapability.Create("C#", SkillLevel.Expert)
        };

        var strongResult = SkillMatcher.Match("C#", strongSkills);
        var expertResult = SkillMatcher.Match("C#", expertSkills);

        Assert.True(strongResult.Score.Value < expertResult.Score.Value);
    }

    [Fact]
    public void MatchAll_ReturnsEvidenceForEachSkill()
    {
        var profileSkills = new[]
        {
            SkillCapability.Create("C#", SkillLevel.Expert),
            SkillCapability.Create("JavaScript", SkillLevel.Expert),
            SkillCapability.Create("React", SkillLevel.Expert)
        };

        var results = SkillMatcher.MatchAll(
            ["C#", "Python", "React.js"],
            profileSkills);

        Assert.Equal(3, results.Count);
        var resultsList = results.ToList();
        Assert.Equal(SkillMatchKind.Exact, resultsList[0].MatchKind);
        Assert.Equal(SkillMatchKind.NoMatch, resultsList[1].MatchKind);
        Assert.Equal(SkillMatchKind.Alias, resultsList[2].MatchKind);
    }

    [Fact]
    public void MatchAll_EmptyInput_ReturnsEmpty()
    {
        var results = SkillMatcher.MatchAll([], [SkillCapability.Create("C#", SkillLevel.Expert)]);

        Assert.Empty(results);
    }

    [Fact]
    public void Match_AliasMatch_ReactsJs()
    {
        var profileSkills = new[]
        {
            SkillCapability.Create("React", SkillLevel.Expert)
        };

        var result = SkillMatcher.Match("React.js", profileSkills);

        Assert.NotNull(result);
        Assert.Equal(SkillMatchKind.Alias, result.MatchKind);
    }

    [Fact]
    public void Match_AliasMatch_K8s()
    {
        var profileSkills = new[]
        {
            SkillCapability.Create("Kubernetes", SkillLevel.Expert)
        };

        var result = SkillMatcher.Match("K8s", profileSkills);

        Assert.NotNull(result);
        Assert.Equal(SkillMatchKind.Alias, result.MatchKind);
    }

    [Fact]
    public void Match_AliasMatch_Postgres()
    {
        var profileSkills = new[]
        {
            SkillCapability.Create("PostgreSQL", SkillLevel.Expert)
        };

        var result = SkillMatcher.Match("Postgres", profileSkills);

        Assert.NotNull(result);
        Assert.Equal(SkillMatchKind.Alias, result.MatchKind);
    }
}
