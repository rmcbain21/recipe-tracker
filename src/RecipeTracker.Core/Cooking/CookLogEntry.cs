using System.Globalization;

namespace RecipeTracker.Core.Cooking;

/// <summary>
/// A record that the Cook made a Recipe on a given day, added when they tap Done in Cooking Mode.
/// A top-level synced record, so the history follows the Cook between devices. The Retro later
/// adds Actual Time, Rating and a "next time" thought to the same record.
/// </summary>
public sealed record CookLogEntry : StoredRecord
{
    public required string RecipeId { get; init; }

    /// <summary>The day it was cooked, where the Cook was.</summary>
    public required DateOnly Date { get; init; }
}

/// <summary>A Recipe's cooking history in brief: "Cooked 3 times · last on 4 Nov 2026".</summary>
public sealed record CookSummary(int TimesCooked, DateOnly? LastCooked)
{
    public static CookSummary Never { get; } = new(0, null);

    /// <summary>"Never cooked", "Cooked once · last on 4 Nov 2026" or "Cooked 3 times · last on 4 Nov 2026".</summary>
    public override string ToString() => LastCooked is not { } last
        ? "Never cooked"
        : $"Cooked {(TimesCooked == 1 ? "once" : $"{TimesCooked} times")} · last on {last.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}";
}
