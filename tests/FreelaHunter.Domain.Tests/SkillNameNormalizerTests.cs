using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Domain.Tests;

public class SkillNameNormalizerTests
{
    [Theory]
    [InlineData("C#", "C#")]
    [InlineData("  C#  ", "C#")]
    [InlineData("c#", "c#")]
    [InlineData("CSharp", "CSharp")]
    [InlineData("React.js", "React.js")]
    [InlineData("Node.js", "Node.js")]
    [InlineData("C++", "C++")]
    public void Normalize_PreservesOriginalValue(string input, string expected)
    {
        var result = SkillNameNormalizer.Normalize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("  C#  ", "C#")]
    [InlineData("   React   ", "React")]
    [InlineData("  Node.js  ", "Node.js")]
    public void Normalize_TrimWhitespace(string input, string expected)
    {
        var result = SkillNameNormalizer.Normalize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("C#", "C#")]
    [InlineData("REACT", "REACT")]
    [InlineData("Node.js", "Node.js")]
    public void Normalize_CasePreserved(string input, string expected)
    {
        var result = SkillNameNormalizer.Normalize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("C  Sharp", "C Sharp")]
    [InlineData("React    JS", "React JS")]
    [InlineData("Node    JS", "Node JS")]
    public void Normalize_CollapsesWhitespace(string input, string expected)
    {
        var result = SkillNameNormalizer.Normalize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Normalize_EmptyOrWhitespace_ReturnsEmpty(string? input)
    {
        var result = SkillNameNormalizer.Normalize(input!);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Normalize_PreservesPunctuation()
    {
        Assert.Equal("C#", SkillNameNormalizer.Normalize("C#"));
        Assert.Equal("C++", SkillNameNormalizer.Normalize("C++"));
        Assert.Equal("ASP.NET Core", SkillNameNormalizer.Normalize("ASP.NET Core"));
        Assert.Equal(".NET", SkillNameNormalizer.Normalize(".NET"));
    }
}
