namespace RecipeTracker.Core.Organizing;

/// <summary>
/// The Tag Groups and Tags created on first run. They have fixed ids (rather than a new GUID per
/// device) so that every device creates the same records and sync combines them instead of
/// duplicating them, and an <c>UpdatedAt</c> of the Unix epoch so sync can tell an untouched
/// starter from one edited on another device.
/// </summary>
public static class StarterTags
{
    public static readonly IReadOnlyList<(string Id, string Name, IReadOnlyList<(string Id, string Name)> Tags)> Groups =
    [
        (GroupId(1), "Meal",
        [
            (TagId(1, 1), "breakfast"), (TagId(1, 2), "lunch"), (TagId(1, 3), "dinner"),
            (TagId(1, 4), "side"), (TagId(1, 5), "dessert"), (TagId(1, 6), "snack"),
        ]),
        (GroupId(2), "Cuisine", []),
        (GroupId(3), "Equipment",
        [
            (TagId(3, 1), "sheet pan"), (TagId(3, 2), "crockpot"), (TagId(3, 3), "instant pot"),
            (TagId(3, 4), "air fryer"), (TagId(3, 5), "grill"), (TagId(3, 6), "dutch oven"),
        ]),
        (GroupId(4), "Diet",
        [
            (TagId(4, 1), "vegetarian"), (TagId(4, 2), "vegan"), (TagId(4, 3), "gluten-free"), (TagId(4, 4), "dairy-free"),
        ]),
        (GroupId(5), "Occasion",
        [
            (TagId(5, 1), "weeknight"), (TagId(5, 2), "meal prep"), (TagId(5, 3), "holiday"), (TagId(5, 4), "want to try"),
        ]),
    ];

    internal static IEnumerable<TagGroup> GroupRecords() =>
        Groups.Select((group, index) => new TagGroup
        {
            Id = group.Id,
            UpdatedAt = DateTimeOffset.UnixEpoch,
            Name = group.Name,
            SortOrder = index,
        });

    internal static IEnumerable<Tag> TagRecords() =>
        Groups.SelectMany(group => group.Tags.Select(tag => new Tag
        {
            Id = tag.Id,
            UpdatedAt = DateTimeOffset.UnixEpoch,
            Name = tag.Name,
            GroupId = group.Id,
        }));

    private static string GroupId(int group) => $"00000000-0000-0000-0002-{group:D12}";

    private static string TagId(int group, int tag) => $"00000000-0000-0000-0003-{group:D6}{tag:D6}";
}
