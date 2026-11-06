namespace RecipeTracker.Core;

/// <summary>
/// The fields every stored record carries, so records can be combined and synced between devices.
/// </summary>
public abstract record StoredRecord
{
    /// <summary>A GUID string generated on the device.</summary>
    public required string Id { get; init; }

    /// <summary>When the record was last changed, in UTC, from the clock.</summary>
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>Tombstone flag: deleted records are kept so that deletions sync.</summary>
    public bool Deleted { get; init; }
}
