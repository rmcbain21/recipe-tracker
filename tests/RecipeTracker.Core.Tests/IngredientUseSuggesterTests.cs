using RecipeTracker.Core.Parsing;

namespace RecipeTracker.Core.Tests;

public class IngredientUseSuggesterTests
{
    private static string[] Suggest(string step, params string[] lines) =>
        IngredientUseSuggester.Suggest(step, lines.Select(names =>
        {
            var parts = names.Split('|');
            return new UseCandidate(parts[0], parts);
        })).ToArray();

    [Theory]
    // Plain names, ignoring case
    [InlineData("Cream the Butter and sugar.", new[] { "butter", "sugar" }, "butter", "flour", "sugar")]
    // Plurals both ways
    [InlineData("Beat in the eggs one at a time.", new[] { "egg" }, "egg")]
    [InlineData("Add the tomato.", new[] { "tomatoes" }, "tomatoes")]
    [InlineData("Fold in the blueberries.", new[] { "blueberry" }, "blueberry")]
    [InlineData("Top with a blueberry.", new[] { "blueberries" }, "blueberries")]
    [InlineData("Slice the onions.", new[] { "onion" }, "onion")]
    // Multi-word names, also across a line break
    [InlineData("Add the brown sugar.", new[] { "brown sugar" }, "brown sugar", "sugar")]
    [InlineData("Add the brown\nsugar.", new[] { "brown sugar" }, "brown sugar", "sugar")]
    [InlineData("Add the brown sugar, then the rest of the sugar.", new[] { "brown sugar", "sugar" }, "brown sugar", "sugar")]
    [InlineData("Whisk the all-purpose flour with the baking powder.", new[] { "all-purpose flour", "baking powder" }, "all-purpose flour", "baking powder", "baking soda")]
    // Whole words only
    [InlineData("Bring to a boil.", new string[0], "oil")]
    [InlineData("Use salted water.", new string[0], "salt")]
    [InlineData("Add the buttermilk.", new[] { "buttermilk" }, "butter", "buttermilk")]
    [InlineData("Add the peanut butter.", new[] { "peanut butter" }, "butter", "peanut butter")]
    // Aliases
    [InlineData("Scatter over the scallions.", new[] { "green onion" }, "green onion|scallion")]
    // Nothing named
    [InlineData("Preheat the oven to 350°F.", new string[0], "butter", "sugar")]
    [InlineData("", new string[0], "butter")]
    public void Suggests_the_lines_a_step_names(string step, string[] expected, params string[] lines)
    {
        Assert.Equal(expected, Suggest(step, lines));
    }

    [Fact]
    public void Two_lines_of_the_same_ingredient_are_both_suggested()
    {
        var lines = new[] { new UseCandidate("dough butter", ["butter"]), new UseCandidate("frosting butter", ["butter"]) };

        Assert.Equal(["dough butter", "frosting butter"], IngredientUseSuggester.Suggest("Melt the butter.", lines));
    }

    [Fact]
    public void Suggestions_come_in_ingredient_list_order()
    {
        Assert.Equal(["flour", "sugar"], Suggest("Add the sugar, then the flour.", "flour", "sugar"));
    }
}
