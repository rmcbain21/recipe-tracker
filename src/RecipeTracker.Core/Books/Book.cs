namespace RecipeTracker.Core.Books;

/// <summary>A cookbook the Cook owns or borrowed, recorded once and picked as the Source of every Recipe from it.</summary>
public sealed record Book : StoredRecord
{
    /// <summary>Such as "Salt Fat Acid Heat". Never blank.</summary>
    public required string Title { get; init; }

    public string Author { get; init; } = "";
}

/// <summary>A Book with how many Recipes (Drafts included) have it as their Source.</summary>
public sealed record BookSummary(Book Book, int RecipeCount);
