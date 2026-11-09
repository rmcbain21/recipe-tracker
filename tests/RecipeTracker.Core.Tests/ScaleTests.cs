using RecipeTracker.Core.Quantities;
using RecipeTracker.Core.Recipes;
using RecipeTracker.Core.Tests.Fakes;

namespace RecipeTracker.Core.Tests;

public class ScaleTests
{
    private static string Scaled(string quantity, decimal factor)
    {
        Assert.True(Quantity.TryParse(quantity, out var parsed));
        return Scale.By(factor).Format(parsed!);
    }

    [Theory]
    // The issue's edge cases
    [InlineData("⅓", 2, "⅔")]
    [InlineData("1½", 1.0 / 3, "½")]
    [InlineData("2-3", 1.5, "3–4½")]
    [InlineData("1-2", 0.5, "½–1")]
    // Below 1: nearest ⅛ (or ⅓)
    [InlineData("¾", 0.5, "⅜")]
    [InlineData("⅓", 0.5, "⅛")]
    [InlineData("¼", 3, "¾")]
    [InlineData("⅛", 3, "⅜")]
    // Above 1: nearest ¼ (or ⅓)
    [InlineData("1", 1.5, "1½")]
    [InlineData("¾", 1.5, "1")] // 1.125 is nearer 1 than 1¼
    [InlineData("1⅓", 2, "2⅔")]
    [InlineData("1⅛", 3, "3⅓")] // 3.375 is nearer 3⅓ than 3½
    [InlineData("3", 3, "9")]
    [InlineData("500", 1.5, "750")]
    // Too little to measure
    [InlineData("⅛", 0.25, "a pinch")]
    // A range whose ends meet after rounding is one amount
    [InlineData("1-1¼", 0.5, "½–⅝")]
    public void Scaling_multiplies_and_rounds_to_cook_friendly_fractions(string quantity, double factor, string expected)
    {
        Assert.Equal(expected, Scaled(quantity, (decimal)factor));
    }

    [Theory]
    [InlineData("⅓")]
    [InlineData("0.3")]
    [InlineData("2-3")]
    public void At_times_one_quantities_show_exactly_as_written(string quantity)
    {
        Assert.True(Quantity.TryParse(quantity, out var parsed));
        Assert.Equal(parsed!.ToString(), Scale.One.Format(parsed));
    }

    [Fact]
    public void Scaling_to_a_yield_works_out_the_factor()
    {
        var scale = Scale.ToYield(new Yield(4, "servings"), 6);

        Assert.Equal(1.5m, scale.Factor);
        Assert.Equal("6 servings (×1½)", scale.Format(new Yield(4, "servings")));
    }

    [Fact]
    public void A_yield_without_a_number_cannot_be_scaled_to()
    {
        Assert.Throws<RuleViolationException>(() => Scale.ToYield(new Yield(null, "one 9×13 pan"), 2));
        Assert.Throws<RuleViolationException>(() => Scale.ToYield(null, 2));
        Assert.Throws<RuleViolationException>(() => Scale.ToYield(new Yield(4, "servings"), 0));
    }

    [Fact]
    public void Labels_show_the_factor()
    {
        Assert.Equal(["×½", "×1", "×1½", "×2", "×3"], Scale.Presets.Select(s => s.Label));
        Assert.Equal("×1⅔", Scale.ToYield(new Yield(6, "servings"), 10).Label);
        Assert.Equal("×1.143", Scale.ToYield(new Yield(7, "servings"), 8).Label);
    }

    [Fact]
    public async Task The_recipe_page_scales_lines_and_ingredient_uses_and_leaves_lines_with_no_quantity()
    {
        var app = new AppCore(new InMemoryRecordStore(), new FakeClock(new DateTimeOffset(2026, 11, 8, 18, 0, 0, TimeSpan.Zero)));
        var editor = await app.Recipes.CaptureAsync("""
            Brownies
            Serves 4

            Ingredients
            1 cup sugar
            2-3 eggs
            Salt to taste

            Method
            1. Beat half the sugar with the eggs.
            """);
        editor.ConfirmAllMatches();
        var sugarUse = editor.Steps[0].Uses.First(u => editor.LineFor(u.LineId)!.Label.EndsWith("sugar"));
        sugarUse.QuantityText = "½";
        sugarUse.Unit = "cup";
        var recipe = await app.Recipes.SaveAsync(editor);

        var view = (await app.Recipes.GetRecipeAsync(recipe.Id))!.AtScale(Scale.By(1.5m));

        Assert.Equal(["1½ cup sugar", "3–4½ eggs", "Salt, to taste"], view.Ingredients.SelectMany(g => g.Lines).Select(l => l.Text));
        Assert.Equal(["sugar, ¾ cup (of 1½ cup)", "eggs, 3–4½"], view.UsesFor(view.Recipe.Steps[0]).Select(u => u.Text));
        Assert.Equal("6 servings (×1½)", view.YieldText);
    }
}
