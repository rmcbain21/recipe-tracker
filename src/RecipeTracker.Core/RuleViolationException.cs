namespace RecipeTracker.Core;

/// <summary>
/// Thrown when an action would break one of the app's rules. The message is written
/// for the Cook, so the UI can show it as it is.
/// </summary>
public sealed class RuleViolationException : Exception
{
    public RuleViolationException(string message) : base(message)
    {
    }
}
