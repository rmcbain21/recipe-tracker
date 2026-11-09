namespace RecipeTracker.Core.Cooking;

/// <summary>
/// A timer started from a time in a Step's text. It keeps the time it ends at rather than a
/// countdown, so it survives the app reloading or closing: one that ended while the app was
/// closed shows as finished on return. It stays, flashing, until the Cook dismisses it.
/// </summary>
public sealed record CookingTimer
{
    public required string Id { get; init; }

    /// <summary>The Step whose text it was started from.</summary>
    public required string StepId { get; init; }

    /// <summary>Where in the Step's text its time is, so that chip shows as running.</summary>
    public required int TextStart { get; init; }

    /// <summary>For the timer strip, such as "Step 2 · 20 min".</summary>
    public required string Label { get; init; }

    public required DateTimeOffset EndsAt { get; init; }

    public bool IsFinished(DateTimeOffset now) => EndsAt <= now;

    /// <summary>The time left, never below zero.</summary>
    public TimeSpan Remaining(DateTimeOffset now) => EndsAt > now ? EndsAt - now : TimeSpan.Zero;

    /// <summary>The time left as a clock: "4:05", or "1:04:05" over an hour. Rounds up, so it shows 0:00 only when finished.</summary>
    public string Countdown(DateTimeOffset now)
    {
        var seconds = (long)Math.Ceiling(Remaining(now).TotalSeconds);
        var (hours, rest) = Math.DivRem(seconds, 3600);
        var (minutes, secs) = Math.DivRem(rest, 60);
        return hours > 0 ? $"{hours}:{minutes:00}:{secs:00}" : $"{minutes}:{secs:00}";
    }
}
