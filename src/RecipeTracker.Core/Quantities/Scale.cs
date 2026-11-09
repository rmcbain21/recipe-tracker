using RecipeTracker.Core.Recipes;

namespace RecipeTracker.Core.Quantities;

/// <summary>
/// A temporary multiplier on a Recipe's quantities while viewing or cooking it, such as ×2 or
/// "make 6 servings". Never saved. Lines with no quantity stay as they are.
/// </summary>
public sealed record Scale
{
    private Scale(decimal factor)
    {
        Factor = factor;
    }

    public decimal Factor { get; }

    public static Scale One { get; } = new(1);

    /// <summary>The quick choices on the recipe page: ×½, ×1, ×1½, ×2 and ×3.</summary>
    public static IReadOnlyList<Scale> Presets { get; } = [new(0.5m), One, new(1.5m), new(2), new(3)];

    public bool IsOne => Factor == 1;

    /// <exception cref="ArgumentOutOfRangeException">The factor isn't more than zero.</exception>
    public static Scale By(decimal factor) =>
        factor > 0 ? new(factor) : throw new ArgumentOutOfRangeException(nameof(factor), "A Scale must be more than zero.");

    /// <summary>The largest custom Scale, so a slip of the thumb ("×1000") doesn't produce nonsense.</summary>
    public const decimal MaxFactor = 100;

    /// <summary>
    /// Reads a custom Scale typed by the Cook: "4", "×4", "x4", "2½", "2 1/2" or "1.25". A blank,
    /// zero, a range, or anything over <see cref="MaxFactor"/> isn't one.
    /// </summary>
    public static bool TryParse(string? text, out Scale? scale)
    {
        scale = null;
        var trimmed = (text ?? "").Trim().TrimStart('×', 'x', 'X').Trim();
        if (trimmed.Length == 0 || !Quantity.TryParse(trimmed, out var quantity) || quantity is not { IsRange: false } || quantity.Low > MaxFactor)
        {
            return false;
        }

        scale = new(quantity.Low);
        return true;
    }

    /// <summary>The Scale that makes <paramref name="target"/> of what the Yield counts ("6 servings" from 4).</summary>
    /// <exception cref="RuleViolationException">The Yield has no number, or the target isn't more than zero.</exception>
    public static Scale ToYield(Yield? yield, decimal target)
    {
        if (yield?.Amount is not > 0)
        {
            throw new RuleViolationException("This Recipe's Yield has no number to scale from.");
        }

        return target > 0 ? new(target / yield.Amount.Value) : throw new RuleViolationException("Make at least some.");
    }

    /// <summary>"×1½", "×2" or "×1.667".</summary>
    public string Label => $"×{Quantity.FormatNumber(Math.Round(Factor, 3))}";

    /// <summary>
    /// The quantity at this Scale, as the Cook would measure it. At ×1 it's shown exactly as
    /// written. Otherwise both ends of a range are multiplied and rounded (<see cref="Round"/>),
    /// and an amount that rounds to nothing shows as "a pinch".
    /// </summary>
    public string Format(Quantity quantity)
    {
        if (IsOne)
        {
            return quantity.ToString();
        }

        var low = Round(quantity.Low * Factor);
        if (quantity.High is null)
        {
            return low == 0 ? "a pinch" : Quantity.FormatNumber(low);
        }

        var high = Round(quantity.High.Value * Factor);
        if (high == 0)
        {
            return "a pinch";
        }

        low = low == 0 ? 0.125m : low;
        return low == high ? Quantity.FormatNumber(low) : $"{Quantity.FormatNumber(low)}–{Quantity.FormatNumber(high)}";
    }

    /// <summary>The Yield at this Scale, such as "6 servings (×1½)", or as written at ×1.</summary>
    public string Format(Yield yield)
    {
        if (IsOne)
        {
            return yield.ToString();
        }

        var amount = yield.Amount is null ? yield.Label : $"{Quantity.FormatNumber(Round(yield.Amount.Value * Factor))} {yield.Label}".Trim();
        return $"{amount} ({Label})";
    }

    /// <summary>
    /// Rounds to a cook-friendly amount: the nearest ⅛ or ⅓ below 1, and the nearest ¼ or ⅓
    /// above 1. Thirds are allowed too, so that ⅓ cup doubled is ⅔ cup rather than ⅝.
    /// </summary>
    public static decimal Round(decimal value)
    {
        var whole = decimal.Truncate(value);
        var steps = value < 1 ? BelowOne : AboveOne;
        var rest = value - whole;
        var nearest = steps.MinBy(step => Math.Abs(rest - step));
        return whole + nearest;
    }

    private static readonly decimal[] BelowOne =
        [0, 0.125m, 0.25m, 1m / 3, 0.375m, 0.5m, 0.625m, 2m / 3, 0.75m, 0.875m, 1];

    private static readonly decimal[] AboveOne = [0, 0.25m, 1m / 3, 0.5m, 2m / 3, 0.75m, 1];
}
