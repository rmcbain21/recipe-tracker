using RecipeTracker.Core.Ports;

namespace RecipeTracker.Core.Tests.Fakes;

/// <summary>A clock that only moves when a test moves it.</summary>
public sealed class FakeClock : IClock
{
    public FakeClock(DateTimeOffset now) => UtcNow = now;

    public DateTimeOffset UtcNow { get; set; }

    /// <summary>The date of <see cref="UtcNow"/>, as if the Cook were on UTC.</summary>
    public DateOnly Today => DateOnly.FromDateTime(UtcNow.UtcDateTime);
}
