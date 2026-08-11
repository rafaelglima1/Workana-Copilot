using System.Text.RegularExpressions;

namespace FreelaHunter.Domain.ValueObjects;

public static partial class SkillNameNormalizer
{
    private static readonly Regex WhitespaceCollapseRegex = WhiteSpaceCollapsePattern();

    public static string Normalize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var trimmed = name.Trim();
        var collapsed = WhitespaceCollapseRegex.Replace(trimmed, " ");
        return collapsed;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhiteSpaceCollapsePattern();
}
