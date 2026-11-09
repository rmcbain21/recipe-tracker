namespace RecipeTracker.Core.Cooking;

/// <summary>
/// Where the Cook is in Cooking Mode for one Recipe: the Scale, the screen they're on, what
/// they've ticked on Gather and their timers. Kept on this device only and never synced, so that leaving Cooking
/// Mode (a phone call) returns them to the same place. At most one per Recipe: its id is the Recipe's.
/// </summary>
public sealed record CookingSession : StoredRecord
{
    /// <summary>The Scale's factor; 1 for the Recipe as written.</summary>
    public decimal ScaleFactor { get; init; } = 1;

    /// <summary>0 for Gather, then 1 for the first Step, 2 for the second and so on.</summary>
    public int Position { get; init; }

    /// <summary>The Ingredient Lines ticked on Gather.</summary>
    public IReadOnlyList<string> GatherTicks { get; init; } = [];

    /// <summary>The running and finished (not yet dismissed) timers, in the order they were started.</summary>
    public IReadOnlyList<CookingTimer> Timers { get; init; } = [];

    public DateTimeOffset StartedAt { get; init; }

    public string RecipeId => Id;

    public bool OnGather => Position == 0;

    /// <summary>The index of the Step shown (0 for the first), or null on Gather.</summary>
    public int? StepIndex => OnGather ? null : Position - 1;
}
