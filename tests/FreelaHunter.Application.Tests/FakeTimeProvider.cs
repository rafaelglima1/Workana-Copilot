namespace FreelaHunter.Application.Tests;

internal sealed class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public FakeTimeProvider(DateTimeOffset? initialTime = null)
    {
        _now = initialTime ?? DateTimeOffset.UtcNow;
    }

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan timeSpan)
    {
        _now = _now.Add(timeSpan);
    }

    public void SetUtcNow(DateTimeOffset value)
    {
        _now = value;
    }
}
