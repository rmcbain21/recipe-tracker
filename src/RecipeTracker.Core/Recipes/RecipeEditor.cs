using RecipeTracker.Core.Ingredients;

namespace RecipeTracker.Core.Recipes;

/// <summary>
/// A Recipe being typed or edited by hand. Changes stay here until
/// <see cref="RecipeCatalogue.SaveAsync"/> checks and saves them, so cancelling an edit
/// leaves the Recipe as it was.
/// </summary>
public sealed class RecipeEditor
{
    private readonly List<EditedSection> _sections = [];
    private readonly List<EditedStep> _steps = [];

    internal RecipeEditor(Recipe? original, IReadOnlyDictionary<string, Ingredient> ingredients)
    {
        Original = original;
        _sections.Add(new EditedSection(NewId(), heading: null));
        if (original is null)
        {
            return;
        }

        Title = original.Title;
        IsDraft = original.IsDraft;
        Source = EditedSource.From(original.Source);
        TagIds.AddRange(original.TagIds);
        Description = original.Description;
        YieldAmount = original.Yield?.Amount;
        YieldLabel = original.Yield?.Label ?? "";
        PrepMinutes = original.StatedTimes.PrepMinutes;
        CookMinutes = original.StatedTimes.CookMinutes;
        TotalMinutes = original.StatedTimes.TotalMinutes;

        foreach (var section in original.Sections)
        {
            _sections.Add(new EditedSection(section.Id, section.Heading));
        }

        foreach (var line in original.Lines)
        {
            var section = _sections.FirstOrDefault(s => s.Heading is not null && s.Id == line.SectionId) ?? _sections[0];
            var edited = new EditedLine(line.Id)
            {
                QuantityText = line.Quantity?.ToString() ?? "",
                Unit = line.Unit,
                Preparation = line.Preparation,
                Optional = line.Optional,
                OriginalText = line.OriginalText,
                SuggestedName = line.SuggestedName,
            };
            var choice = ChoiceFor(line, ingredients);
            if (line.MatchUnconfirmed && choice is not null)
            {
                edited.Suggest(choice, choice.IsNew ? IngredientMatch.New
                    : IngredientNames.Same(choice.Name, line.SuggestedName) ? IngredientMatch.Matched : IngredientMatch.Alias);
            }
            else
            {
                edited.Ingredient = choice;
            }

            section.LinesList.Add(edited);
        }

        _steps.AddRange(original.Steps.Select(step => new EditedStep(step.Id) { Text = step.Text }));
    }

    /// <summary>The Recipe as it was saved, or null for a new one.</summary>
    internal Recipe? Original { get; }

    /// <summary>The Recipe's id, or null if it hasn't been saved yet.</summary>
    public string? RecipeId => Original?.Id;

    public string Title { get; set; } = "";

    /// <summary>True if the Recipe was last saved as a Draft. A new Recipe isn't a Draft until it's saved as one.</summary>
    public bool IsDraft { get; }

    /// <summary>The Source being edited; its Kind is null for none.</summary>
    public EditedSource Source { get; private set; } = new();

    /// <summary>The ids of the Tags picked for the Recipe. Ids of Tags deleted meanwhile are dropped on saving.</summary>
    public List<string> TagIds { get; } = [];

    public string Description { get; set; } = "";

    /// <summary>True if any line's Ingredient is still only a suggestion.</summary>
    public bool HasUnconfirmedMatches => _sections.SelectMany(s => s.LinesList).Any(line => line.Ingredient is not null && !line.IsConfirmed);

    /// <summary>Accepts every suggested Ingredient as it is.</summary>
    public void ConfirmAllMatches()
    {
        foreach (var line in _sections.SelectMany(s => s.LinesList))
        {
            line.Confirm();
        }
    }

    public decimal? YieldAmount { get; set; }

    public string YieldLabel { get; set; } = "";

    public int? PrepMinutes { get; set; }

    public int? CookMinutes { get; set; }

    public int? TotalMinutes { get; set; }

    /// <summary>
    /// The ingredient groups in order. The first has no heading and holds the lines before the
    /// first Ingredient Section; it's always there, even when empty.
    /// </summary>
    public IReadOnlyList<EditedSection> Sections => _sections;

    public IReadOnlyList<EditedStep> Steps => _steps;

    /// <summary>Adds an Ingredient Section at the end.</summary>
    public EditedSection AddSection(string heading = "")
    {
        var section = new EditedSection(NewId(), heading);
        _sections.Add(section);
        return section;
    }

    /// <summary>Removes a heading. Its lines stay, joining the end of the group above.</summary>
    public void RemoveSection(EditedSection section)
    {
        var index = IndexOfHeaded(section);
        _sections[index - 1].LinesList.AddRange(section.LinesList);
        _sections.RemoveAt(index);
    }

    /// <summary>Moves a section, with its lines, up (-1) or down (+1) among the other headed sections.</summary>
    public void MoveSection(EditedSection section, int offset)
    {
        var index = IndexOfHeaded(section);
        var target = Math.Clamp(index + offset, 1, _sections.Count - 1);
        _sections.RemoveAt(index);
        _sections.Insert(target, section);
    }

    /// <summary>Adds a blank Ingredient Line at the end of a group.</summary>
    public EditedLine AddLine(EditedSection section)
    {
        var line = new EditedLine(NewId());
        GroupOf(section).LinesList.Add(line);
        return line;
    }

    public void RemoveLine(EditedLine line) => SectionContaining(line).LinesList.Remove(line);

    /// <summary>
    /// Moves a line up (-1) or down (+1). Past the top or bottom of its group it moves into the
    /// end of the group above or the start of the group below.
    /// </summary>
    public void MoveLine(EditedLine line, int offset)
    {
        var section = SectionContaining(line);
        var sectionIndex = _sections.IndexOf(section);
        var index = section.LinesList.IndexOf(line);
        var target = index + Math.Sign(offset);

        if (target >= 0 && target < section.LinesList.Count)
        {
            section.LinesList.RemoveAt(index);
            section.LinesList.Insert(target, line);
        }
        else if (target < 0 && sectionIndex > 0)
        {
            section.LinesList.RemoveAt(index);
            _sections[sectionIndex - 1].LinesList.Add(line);
        }
        else if (target >= section.LinesList.Count && sectionIndex < _sections.Count - 1)
        {
            section.LinesList.RemoveAt(index);
            _sections[sectionIndex + 1].LinesList.Insert(0, line);
        }
    }

    /// <summary>Adds a Step at the end.</summary>
    public EditedStep AddStep(string text = "")
    {
        var step = new EditedStep(NewId()) { Text = text };
        _steps.Add(step);
        return step;
    }

    public void RemoveStep(EditedStep step) => _steps.Remove(step);

    /// <summary>Moves a Step up (-1) or down (+1).</summary>
    public void MoveStep(EditedStep step, int offset)
    {
        var index = _steps.IndexOf(step);
        var target = Math.Clamp(index + Math.Sign(offset), 0, _steps.Count - 1);
        _steps.RemoveAt(index);
        _steps.Insert(target, step);
    }

    private EditedSection GroupOf(EditedSection section) =>
        _sections.Contains(section) ? section : throw new ArgumentException("That section isn't in this Recipe.", nameof(section));

    private int IndexOfHeaded(EditedSection section)
    {
        var index = _sections.IndexOf(GroupOf(section));
        return index > 0 ? index : throw new ArgumentException("The lines before the first heading aren't a section.", nameof(section));
    }

    private EditedSection SectionContaining(EditedLine line) =>
        _sections.FirstOrDefault(s => s.LinesList.Contains(line))
        ?? throw new ArgumentException("That line isn't in this Recipe.", nameof(line));

    private static string NewId() => Guid.NewGuid().ToString();

    private static IngredientChoice? ChoiceFor(IngredientLine line, IReadOnlyDictionary<string, Ingredient> ingredients)
    {
        if (line.IngredientId is not null)
        {
            var name = ingredients.TryGetValue(line.IngredientId, out var ingredient) ? ingredient.Name : "Unknown Ingredient";
            return IngredientChoice.Existing(line.IngredientId, name);
        }

        return line.SuggestedName.Length > 0 ? IngredientChoice.New(line.SuggestedName) : null;
    }
}

/// <summary>An ingredient group in a <see cref="RecipeEditor"/>: an Ingredient Section, or the lines before the first one.</summary>
public sealed class EditedSection
{
    internal EditedSection(string id, string? heading)
    {
        Id = id;
        Heading = heading;
    }

    public string Id { get; }

    /// <summary>The heading, or null for the group before the first Ingredient Section.</summary>
    public string? Heading { get; set; }

    public bool IsHeaded => Heading is not null;

    public IReadOnlyList<EditedLine> Lines => LinesList;

    internal List<EditedLine> LinesList { get; } = [];
}

/// <summary>An Ingredient Line in a <see cref="RecipeEditor"/>, with its quantity still as typed.</summary>
public sealed class EditedLine
{
    internal EditedLine(string id)
    {
        Id = id;
    }

    public string Id { get; }

    /// <summary>The quantity as typed, such as "1½" or "2-3"; checked when the Recipe is saved.</summary>
    public string QuantityText { get; set; } = "";

    public string Unit { get; set; } = "";

    private IngredientChoice? _ingredient;

    /// <summary>
    /// The Ingredient picked, or null if none has been picked yet. Picking one, even the
    /// suggested one, confirms it.
    /// </summary>
    public IngredientChoice? Ingredient
    {
        get => _ingredient;
        set
        {
            _ingredient = value;
            IsConfirmed = true;
            Match = null;
        }
    }

    /// <summary>False while the Ingredient is the capture's suggestion and the Cook hasn't confirmed it.</summary>
    public bool IsConfirmed { get; private set; } = true;

    /// <summary>How the suggested Ingredient matched, shown until the Cook confirms or changes it; null otherwise.</summary>
    public IngredientMatch? Match { get; private set; }

    /// <summary>Accepts the suggested Ingredient.</summary>
    public void Confirm()
    {
        IsConfirmed = true;
        Match = null;
    }

    internal void Suggest(IngredientChoice choice, IngredientMatch match)
    {
        _ingredient = choice;
        IsConfirmed = false;
        Match = match;
    }

    public string Preparation { get; set; } = "";

    public bool Optional { get; set; }

    public string OriginalText { get; set; } = "";

    /// <summary>On a captured line, the Ingredient name the parser read, kept so that a Draft can show how it matched.</summary>
    public string SuggestedName { get; internal set; } = "";

    internal bool IsBlank =>
        string.IsNullOrWhiteSpace(QuantityText) && string.IsNullOrWhiteSpace(Unit) &&
        Ingredient is null && string.IsNullOrWhiteSpace(Preparation);
}

/// <summary>A Step in a <see cref="RecipeEditor"/>.</summary>
public sealed class EditedStep
{
    internal EditedStep(string id)
    {
        Id = id;
    }

    public string Id { get; }

    public string Text { get; set; } = "";
}

/// <summary>
/// The Ingredient picked for a line: an existing one, or a new one to create with this name
/// when the Recipe is saved.
/// </summary>
public sealed record IngredientChoice
{
    private IngredientChoice(string? ingredientId, string name)
    {
        IngredientId = ingredientId;
        Name = name;
    }

    /// <summary>The existing Ingredient's id, or null for a new one.</summary>
    public string? IngredientId { get; }

    public string Name { get; }

    public bool IsNew => IngredientId is null;

    public static IngredientChoice Existing(Ingredient ingredient) => new(ingredient.Id, ingredient.Name);

    public static IngredientChoice Existing(string ingredientId, string name) => new(ingredientId, name);

    public static IngredientChoice New(string name) => new(null, IngredientNames.Tidy(name));

    public override string ToString() => IsNew ? $"New Ingredient \"{Name}\"" : Name;
}
