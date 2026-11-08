using RecipeTracker.Core.Ports;
using RecipeTracker.Core.Recipes;

namespace RecipeTracker.Core.Organizing;

/// <summary>
/// The Cook's Tags and Tag Groups. The first call on a new device creates the starter Tag Groups
/// and Tags (<see cref="StarterTags"/>).
/// </summary>
public sealed class TagCatalogue
{
    internal const string TagTable = "tags";
    internal const string GroupTable = "tagGroups";

    private readonly IRecordStore _store;
    private readonly IClock _clock;

    public TagCatalogue(IRecordStore store, IClock clock)
    {
        _store = store;
        _clock = clock;
    }

    /// <summary>The Tag Groups in the Cook's order.</summary>
    public async Task<IReadOnlyList<TagGroup>> ListTagGroupsAsync()
    {
        await EnsureStartersAsync();
        return Ordered(await _store.ListAsync<TagGroup>(GroupTable));
    }

    /// <summary>Every Tag that isn't deleted, by name.</summary>
    public async Task<IReadOnlyList<Tag>> ListTagsAsync()
    {
        await EnsureStartersAsync();
        return Live(await _store.ListAsync<Tag>(TagTable));
    }

    /// <summary>
    /// Every Tag Group in order with its Tags by name, then the ungrouped Tags (always last, and
    /// always there, even when there are none).
    /// </summary>
    public async Task<IReadOnlyList<GroupedTags<Tag>>> ListGroupedTagsAsync() =>
        Group(await ListTagGroupsAsync(), await ListTagsAsync(), tag => tag.GroupId, includeEmptyUngrouped: true);

    /// <summary>As <see cref="ListGroupedTagsAsync"/>, with how many Recipes have each Tag, for the Tags page.</summary>
    public async Task<IReadOnlyList<GroupedTags<TagUse>>> ListTagUsesAsync()
    {
        var counts = (await RecipesAsync())
            .SelectMany(recipe => recipe.TagIds.Distinct())
            .GroupBy(id => id)
            .ToDictionary(group => group.Key, group => group.Count());
        var uses = (await ListTagsAsync()).Select(tag => new TagUse(tag, counts.GetValueOrDefault(tag.Id))).ToList();
        return Group(await ListTagGroupsAsync(), uses, use => use.Tag.GroupId, includeEmptyUngrouped: true);
    }

    /// <summary>The Tag with this name (ignoring case and extra spaces), or null.</summary>
    public async Task<Tag?> FindTagByNameAsync(string name)
    {
        var tidy = TagNames.Tidy(name);
        return (await ListTagsAsync()).FirstOrDefault(tag => TagNames.Same(tag.Name, tidy));
    }

    // Tags

    /// <summary>Adds a Tag, in a Tag Group or ungrouped.</summary>
    /// <exception cref="RuleViolationException">The name is blank or another Tag has it, or the group is gone.</exception>
    public async Task<Tag> AddTagAsync(string name, string? groupId = null)
    {
        var tags = await ListTagsAsync();
        await CheckGroupAsync(groupId);
        var tag = new Tag
        {
            Id = Guid.NewGuid().ToString(),
            UpdatedAt = _clock.UtcNow,
            Name = CheckName(name, tags.Select(t => (t.Id, t.Name)), id: null, "Tag"),
            GroupId = groupId,
        };
        await _store.SaveAsync(TagTable, tag);
        return tag;
    }

    /// <summary>Renames a Tag; every Recipe with it shows the new name.</summary>
    public async Task<Tag> RenameTagAsync(string id, string name)
    {
        var tags = await ListTagsAsync();
        var tag = TagIn(tags, id);
        tag = tag with { UpdatedAt = _clock.UtcNow, Name = CheckName(name, tags.Select(t => (t.Id, t.Name)), id, "Tag") };
        await _store.SaveAsync(TagTable, tag);
        return tag;
    }

    /// <summary>Moves a Tag into a Tag Group, or out of every group (null).</summary>
    public async Task<Tag> MoveTagToGroupAsync(string id, string? groupId)
    {
        var tag = TagIn(await ListTagsAsync(), id);
        await CheckGroupAsync(groupId);
        tag = tag with { UpdatedAt = _clock.UtcNow, GroupId = groupId };
        await _store.SaveAsync(TagTable, tag);
        return tag;
    }

    /// <summary>
    /// Merges one Tag into another: every Recipe with it gets the other one instead, and it's
    /// deleted, its tombstone recording what it was merged into.
    /// </summary>
    /// <returns>How many Recipes were changed.</returns>
    public async Task<int> MergeTagAsync(string id, string intoId)
    {
        if (id == intoId)
        {
            throw new RuleViolationException("Pick a different Tag to merge into.");
        }

        var tags = await ListTagsAsync();
        var tag = TagIn(tags, id);
        TagIn(tags, intoId);
        var now = _clock.UtcNow;

        var recipes = (await RecipesAsync()).Where(recipe => recipe.TagIds.Contains(id)).ToList();
        var records = recipes
            .Select(recipe => new RecordToSave(RecipeCatalogue.Table, recipe with
            {
                UpdatedAt = now,
                TagIds = recipe.TagIds.Select(t => t == id ? intoId : t).Distinct().ToList(),
            }))
            .Append(new RecordToSave(TagTable, tag with { UpdatedAt = now, Deleted = true, MergedInto = intoId }))
            .ToList();
        await _store.SaveAllAsync(records);
        return recipes.Count;
    }

    /// <summary>Deletes a Tag and takes it off every Recipe.</summary>
    /// <returns>How many Recipes it was taken off.</returns>
    public async Task<int> DeleteTagAsync(string id)
    {
        var tag = (await ListTagsAsync()).FirstOrDefault(t => t.Id == id);
        if (tag is null)
        {
            return 0;
        }

        var now = _clock.UtcNow;
        var recipes = (await RecipesAsync()).Where(recipe => recipe.TagIds.Contains(id)).ToList();
        var records = recipes
            .Select(recipe => new RecordToSave(RecipeCatalogue.Table, recipe with { UpdatedAt = now, TagIds = recipe.TagIds.Where(t => t != id).ToList() }))
            .Append(new RecordToSave(TagTable, tag with { UpdatedAt = now, Deleted = true }))
            .ToList();
        await _store.SaveAllAsync(records);
        return recipes.Count;
    }

    // Tag Groups

    /// <summary>Adds a Tag Group at the end.</summary>
    public async Task<TagGroup> AddTagGroupAsync(string name)
    {
        var groups = await ListTagGroupsAsync();
        var group = new TagGroup
        {
            Id = Guid.NewGuid().ToString(),
            UpdatedAt = _clock.UtcNow,
            Name = CheckName(name, groups.Select(g => (g.Id, g.Name)), id: null, "Tag Group"),
            SortOrder = groups.Count == 0 ? 0 : groups.Max(g => g.SortOrder) + 1,
        };
        await _store.SaveAsync(GroupTable, group);
        return group;
    }

    public async Task<TagGroup> RenameTagGroupAsync(string id, string name)
    {
        var groups = await ListTagGroupsAsync();
        var group = GroupIn(groups, id);
        group = group with { UpdatedAt = _clock.UtcNow, Name = CheckName(name, groups.Select(g => (g.Id, g.Name)), id, "Tag Group") };
        await _store.SaveAsync(GroupTable, group);
        return group;
    }

    /// <summary>Moves a Tag Group up (-1) or down (+1) in the order.</summary>
    public async Task MoveTagGroupAsync(string id, int offset)
    {
        var groups = (await ListTagGroupsAsync()).ToList();
        var index = groups.IndexOf(GroupIn(groups, id));
        var target = Math.Clamp(index + Math.Sign(offset), 0, groups.Count - 1);
        if (target == index)
        {
            return;
        }

        var moved = groups[index];
        groups.RemoveAt(index);
        groups.Insert(target, moved);

        // Renumber, saving only the groups whose position changed.
        var now = _clock.UtcNow;
        var records = groups
            .Select((group, order) => (group, order))
            .Where(g => g.group.SortOrder != g.order)
            .Select(g => new RecordToSave(GroupTable, g.group with { UpdatedAt = now, SortOrder = g.order }))
            .ToList();
        await _store.SaveAllAsync(records);
    }

    /// <summary>Deletes a Tag Group. Its Tags stay, ungrouped.</summary>
    public async Task DeleteTagGroupAsync(string id)
    {
        var group = (await ListTagGroupsAsync()).FirstOrDefault(g => g.Id == id);
        if (group is null)
        {
            return;
        }

        var now = _clock.UtcNow;
        var records = (await ListTagsAsync())
            .Where(tag => tag.GroupId == id)
            .Select(tag => new RecordToSave(TagTable, tag with { UpdatedAt = now, GroupId = null }))
            .Append(new RecordToSave(GroupTable, group with { UpdatedAt = now, Deleted = true }))
            .ToList();
        await _store.SaveAllAsync(records);
    }

    // Shared with the Recipe page and editor.

    /// <summary>
    /// The given Tags grouped as the recipe page shows them: in Tag Group order, by name within a
    /// group, ungrouped last. Ids of deleted Tags are skipped.
    /// </summary>
    internal static IReadOnlyList<GroupedTags<Tag>> GroupForRecipe(IEnumerable<string> tagIds, IReadOnlyList<Tag> allTags, IReadOnlyList<TagGroup> allGroups)
    {
        var ids = tagIds.ToHashSet();
        return Group(Ordered(allGroups), Live(allTags).Where(tag => ids.Contains(tag.Id)).ToList(), tag => tag.GroupId, includeEmptyUngrouped: false)
            .Where(group => group.Tags.Count > 0)
            .ToList();
    }

    private static IReadOnlyList<GroupedTags<T>> Group<T>(IReadOnlyList<TagGroup> groups, IReadOnlyList<T> tags, Func<T, string?> groupOf, bool includeEmptyUngrouped)
    {
        var groupIds = groups.Select(g => g.Id).ToHashSet();
        var result = groups.Select(group => new GroupedTags<T>(group, tags.Where(t => groupOf(t) == group.Id).ToList())).ToList();

        // A Tag whose group was deleted on another device counts as ungrouped.
        var ungrouped = tags.Where(t => groupOf(t) is not { } g || !groupIds.Contains(g)).ToList();
        if (includeEmptyUngrouped || ungrouped.Count > 0)
        {
            result.Add(new GroupedTags<T>(null, ungrouped));
        }

        return result;
    }

    private static IReadOnlyList<TagGroup> Ordered(IEnumerable<TagGroup> groups) =>
        groups.Where(group => !group.Deleted)
            .OrderBy(group => group.SortOrder)
            // Two devices can each add a group at the same position before syncing.
            .ThenBy(group => group.Id, StringComparer.Ordinal)
            .ToList();

    private static IReadOnlyList<Tag> Live(IEnumerable<Tag> tags) =>
        tags.Where(tag => !tag.Deleted).OrderBy(tag => tag.Name, StringComparer.CurrentCultureIgnoreCase).ToList();

    // Only on first run: once any Tag or Tag Group has existed, its tombstone stays in the store.
    private async Task EnsureStartersAsync()
    {
        if ((await _store.ListAsync<TagGroup>(GroupTable)).Count > 0 || (await _store.ListAsync<Tag>(TagTable)).Count > 0)
        {
            return;
        }

        var records = StarterTags.GroupRecords().Select(group => new RecordToSave(GroupTable, group))
            .Concat(StarterTags.TagRecords().Select(tag => new RecordToSave(TagTable, tag)))
            .ToList();
        await _store.SaveAllAsync(records);
    }

    private async Task CheckGroupAsync(string? groupId)
    {
        if (groupId is not null)
        {
            GroupIn(await ListTagGroupsAsync(), groupId);
        }
    }

    private static Tag TagIn(IEnumerable<Tag> tags, string id) =>
        tags.FirstOrDefault(tag => tag.Id == id) ?? throw new RuleViolationException("That Tag has been deleted.");

    private static TagGroup GroupIn(IEnumerable<TagGroup> groups, string id) =>
        groups.FirstOrDefault(group => group.Id == id) ?? throw new RuleViolationException("That Tag Group has been deleted.");

    private static string CheckName(string name, IEnumerable<(string Id, string Name)> existing, string? id, string what)
    {
        var tidy = TagNames.Tidy(name);
        if (tidy.Length == 0)
        {
            throw new RuleViolationException($"Give the {what} a name.");
        }

        if (existing.Any(e => e.Id != id && TagNames.Same(e.Name, tidy)))
        {
            throw new RuleViolationException($"You already have a {what} called \"{tidy}\".");
        }

        return tidy;
    }

    private async Task<IEnumerable<Recipe>> RecipesAsync() =>
        (await _store.ListAsync<Recipe>(RecipeCatalogue.Table)).Where(recipe => !recipe.Deleted);
}

/// <summary>How Tag and Tag Group names are cleaned up and compared: trimmed, inner spaces collapsed, ignoring case.</summary>
public static class TagNames
{
    public static string Tidy(string? name) =>
        string.Join(' ', (name ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    public static bool Same(string a, string b) =>
        string.Equals(Tidy(a), Tidy(b), StringComparison.CurrentCultureIgnoreCase);
}
