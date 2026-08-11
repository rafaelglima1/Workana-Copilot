namespace FreelaHunter.Domain.ValueObjects;

public enum SkillMatchKind
{
    Exact,
    Alias,
    NoMatch
}

public sealed record SkillMatchEvidence
{
    public string RequiredSkill { get; }
    public string? MatchedProfileSkill { get; }
    public string? CanonicalSkill { get; }
    public SkillMatchKind MatchKind { get; }
    public Enums.SkillLevel? ProfileLevel { get; }
    public ScoreValue Score { get; }

    private SkillMatchEvidence(
        string requiredSkill,
        string? matchedProfileSkill,
        string? canonicalSkill,
        SkillMatchKind matchKind,
        Enums.SkillLevel? profileLevel,
        ScoreValue score)
    {
        RequiredSkill = requiredSkill;
        MatchedProfileSkill = matchedProfileSkill;
        CanonicalSkill = canonicalSkill;
        MatchKind = matchKind;
        ProfileLevel = profileLevel;
        Score = score;
    }

    public static SkillMatchEvidence Create(
        string requiredSkill,
        string? matchedProfileSkill,
        string? canonicalSkill,
        SkillMatchKind matchKind,
        Enums.SkillLevel? profileLevel,
        ScoreValue score)
    {
        return new SkillMatchEvidence(
            requiredSkill,
            matchedProfileSkill,
            canonicalSkill,
            matchKind,
            profileLevel,
            score);
    }

    public static SkillMatchEvidence NoMatch(string requiredSkill)
    {
        return new SkillMatchEvidence(
            requiredSkill,
            null,
            null,
            SkillMatchKind.NoMatch,
            null,
            ScoreValue.From(0));
    }
}
