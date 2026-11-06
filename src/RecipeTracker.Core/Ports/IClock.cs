namespace RecipeTracker.Core.Ports;

public interface IClock
{
    DateTimeOffset UtcNow { get; }

    /// <summary>Today's date where the Cook is, for dates such as a Cook Log Entry's.</summary>
    DateOnly Today { get; }
}
