namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Test.Fakes;

internal sealed class AdjustableTimeProvider : TimeProvider
{
    private long _utcTicks;

    public AdjustableTimeProvider(DateTimeOffset? initialUtc = null)
    {
        var fallback = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        var start = (initialUtc ?? fallback).ToUniversalTime();
        _utcTicks = start.UtcTicks;
    }

    public override DateTimeOffset GetUtcNow() =>
        new(Interlocked.Read(ref _utcTicks), TimeSpan.Zero);

    public void Advance(TimeSpan delta) =>
        Interlocked.Add(ref _utcTicks, delta.Ticks);

    public void SetUtcNow(DateTimeOffset utcNow) =>
        Interlocked.Exchange(ref _utcTicks, utcNow.ToUniversalTime().UtcTicks);
}
