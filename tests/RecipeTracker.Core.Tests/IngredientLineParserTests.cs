using RecipeTracker.Core.Parsing;
using RecipeTracker.Core.Quantities;

namespace RecipeTracker.Core.Tests;

public class IngredientLineParserTests
{
    // text, quantity ("" for none), unit, name, preparation, optional
    [Theory]
    [InlineData("1 1/2 cups all-purpose flour, sifted", "1½", "cups", "all-purpose flour", "sifted", false)]
    [InlineData("1½ cups all-purpose flour", "1½", "cups", "all-purpose flour", "", false)]
    [InlineData("½ c sugar", "½", "c", "sugar", "", false)]
    [InlineData("2 T butter", "2", "T", "butter", "", false)]
    [InlineData("1 t vanilla extract", "1", "t", "vanilla extract", "", false)]
    [InlineData("1/2 tsp. salt", "½", "tsp", "salt", "", false)]
    [InlineData("3 tbsp olive oil", "3", "tbsp", "olive oil", "", false)]
    [InlineData("8 oz cream cheese, softened", "8", "oz", "cream cheese", "softened", false)]
    [InlineData("1 lb ground beef", "1", "lb", "ground beef", "", false)]
    [InlineData("500g strong white flour", "500", "g", "strong white flour", "", false)]
    [InlineData("1 kg potatoes, peeled and cubed", "1", "kg", "potatoes", "peeled and cubed", false)]
    [InlineData("250 ml whole milk", "250", "ml", "whole milk", "", false)]
    [InlineData("1 l chicken stock", "1", "l", "chicken stock", "", false)]
    [InlineData("3-4 cloves garlic, minced", "3–4", "cloves", "garlic", "minced", false)]
    [InlineData("2 to 3 cans kidney beans", "2–3", "cans", "kidney beans", "", false)]
    [InlineData("1 (15-ounce) can black beans, drained and rinsed", "1", "(15-ounce) can", "black beans", "drained and rinsed", false)]
    [InlineData("1 cup (240 ml) milk", "1", "cup (240 ml)", "milk", "", false)]
    [InlineData("1 stick butter, melted", "1", "stick", "butter", "melted", false)]
    [InlineData("2 large eggs", "2", "large", "eggs", "", false)]
    [InlineData("1 onion, diced", "1", "", "onion", "diced", false)]
    [InlineData("pinch of salt", "", "pinch", "salt", "", false)]
    [InlineData("a pinch of nutmeg", "1", "pinch", "nutmeg", "", false)]
    [InlineData("dash hot sauce", "", "dash", "hot sauce", "", false)]
    [InlineData("salt to taste", "", "", "salt", "to taste", false)]
    [InlineData("2 tbsp chopped fresh parsley", "2", "tbsp", "fresh parsley", "chopped", false)]
    [InlineData("1 tsp freshly ground black pepper", "1", "tsp", "black pepper", "freshly ground", false)]
    [InlineData("1 cup finely chopped onion", "1", "cup", "onion", "finely chopped", false)]
    [InlineData("1/4 cup butter (softened)", "¼", "cup", "butter", "softened", false)]
    [InlineData("1 cup walnuts, chopped (optional)", "1", "cup", "walnuts", "chopped", true)]
    [InlineData("1/2 cup raisins, optional", "½", "cup", "raisins", "", true)]
    [InlineData("Optional: 1 tsp chili flakes", "1", "tsp", "chili flakes", "", true)]
    [InlineData("- 2 cups flour", "2", "cups", "flour", "", false)]
    [InlineData("▢ 1 tbsp honey", "1", "tbsp", "honey", "", false)]
    [InlineData("• 2cups water", "2", "cups", "water", "", false)]
    [InlineData("vegetable oil for frying", "", "", "vegetable oil", "for frying", false)]
    [InlineData("3 Roma tomatoes", "3", "", "Roma tomatoes", "", false)]
    [InlineData("4 fl oz cream", "4", "fl oz", "cream", "", false)]
    public void Lines_are_split_into_their_parts(string text, string quantity, string unit, string name, string preparation, bool optional)
    {
        var line = Assert.Single(IngredientLineParser.Parse(text));

        Assert.Equal(quantity, line.Quantity?.ToString() ?? "");
        Assert.Equal(unit, line.Unit);
        Assert.Equal(name, line.Name);
        Assert.Equal(preparation, line.Preparation);
        Assert.Equal(optional, line.Optional);
        Assert.Equal(text.Trim(), line.OriginalText);
    }

    [Fact]
    public void Salt_and_pepper_to_taste_becomes_two_lines()
    {
        var lines = IngredientLineParser.Parse("Salt and pepper to taste");

        Assert.Equal(["Salt", "pepper"], lines.Select(l => l.Name));
        Assert.All(lines, line =>
        {
            Assert.Null(line.Quantity);
            Assert.Equal("to taste", line.Preparation);
            Assert.Equal("Salt and pepper to taste", line.OriginalText);
        });
    }

    [Fact]
    public void A_range_keeps_both_ends()
    {
        var line = Assert.Single(IngredientLineParser.Parse("2-3 tbsp lemon juice"));

        Assert.Equal(new Quantity(2, 3), line.Quantity);
    }

    [Theory]
    [InlineData("2 cups flour", true)]
    [InlineData("½ tsp salt", true)]
    [InlineData("pinch of salt", true)]
    [InlineData("salt and pepper to taste", true)]
    [InlineData("- 1 egg", true)]
    [InlineData("Preheat the oven to 400°F.", false)]
    [InlineData("For the dough:", false)]
    [InlineData("Weeknight chili", false)]
    public void Ingredient_shaped_lines_are_recognised(string text, bool expected)
    {
        Assert.Equal(expected, IngredientLineParser.LooksLikeIngredient(text));
    }
}
