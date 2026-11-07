using RecipeTracker.Core.Parsing;
using RecipeTracker.Core.Recipes;

namespace RecipeTracker.Core.Tests;

/// <summary>
/// Whole-recipe samples shaped like real captures: a recipe website copied from Chrome, a YouTube
/// description, a book page read by Google Lens, and a messy note with no headings.
/// </summary>
public class RecipeParserTests
{
    private static readonly RuleBasedRecipeParser Parser = new();

    private static string[] Names(ParsedRecipe recipe) =>
        recipe.Sections.SelectMany(section => section.Lines).Select(line => line.Name).ToArray();

    private const string WebsiteCopy = """
        Jump to Recipe
        Print Recipe
        Easy Skillet Lasagna
        4.92 from 213 votes
        This one-pan lasagna is ready in about 40 minutes and makes very few dishes.
        Prep Time
        10 minutes
        Cook Time
        30 minutes
        Total Time
        40 minutes
        Servings
        6 servings
        Author: A Food Blogger
        Ingredients
        US Customary Metric
        1x 2x 3x
        ▢ 1 lb ground Italian sausage
        ▢ 1 medium onion, diced
        ▢ 3 cloves garlic, minced
        ▢ 1 (24 oz) jar marinara sauce
        ▢ 2 cups water
        ▢ 8 oz lasagna noodles, broken into pieces
        For the cheese layer:
        ▢ 1 cup ricotta cheese
        ▢ 1 ½ cups shredded mozzarella cheese
        ▢ ¼ cup grated Parmesan cheese
        ▢ Fresh basil, for serving (optional)
        Instructions
        Brown the sausage and onion in a large deep skillet over medium-high heat, about 6 minutes. Add the garlic and cook 1 minute.
        Stir in the marinara and water and bring to a simmer.
        Add the noodles, cover, and simmer 15-18 minutes, stirring now and then, until tender.
        Dollop the ricotta over the top, sprinkle with mozzarella and Parmesan, cover and cook 3 minutes until melted.
        Notes
        Leftovers keep for 4 days in the fridge.
        Nutrition
        Calories: 520kcal
        Course: Dinner
        """;

    [Fact]
    public void A_recipe_website_copy()
    {
        var recipe = Parser.Parse(WebsiteCopy);

        Assert.Equal("Easy Skillet Lasagna", recipe.Title);
        Assert.Equal(new StatedTimes(10, 30, 40), recipe.StatedTimes);
        Assert.Equal(new Yield(6, "servings"), recipe.Yield);

        Assert.Equal([null, "For the cheese layer"], recipe.Sections.Select(s => s.Heading));
        Assert.Equal(
            ["ground Italian sausage", "onion", "garlic", "marinara sauce", "water", "lasagna noodles",
             "ricotta cheese", "mozzarella cheese", "Parmesan cheese", "Fresh basil"],
            Names(recipe));

        var marinara = recipe.Sections[0].Lines[3];
        Assert.Equal("(24 oz) jar", marinara.Unit);
        var onion = recipe.Sections[0].Lines[1];
        Assert.Equal(("medium", "diced"), (onion.Unit, onion.Preparation));
        var mozzarella = recipe.Sections[1].Lines[1];
        Assert.Equal(("1½", "cups", "shredded"), (mozzarella.Quantity!.ToString(), mozzarella.Unit, mozzarella.Preparation));
        var basil = recipe.Sections[1].Lines[3];
        Assert.True(basil.Optional);
        Assert.Equal("for serving", basil.Preparation);

        Assert.Equal(4, recipe.Steps.Count);
        Assert.StartsWith("Brown the sausage", recipe.Steps[0]);
        Assert.StartsWith("Dollop the ricotta", recipe.Steps[3]);
    }

    private const string YouTubeDescription = """
        The crispiest roast potatoes you'll ever make! Full method below 👇

        INGREDIENTS:
        - 1kg Maris Piper potatoes
        - 1 tsp salt
        - 4 tbsp olive oil
        - 4 cloves garlic, smashed
        - 2 sprigs rosemary
        - Salt and pepper to taste

        METHOD:
        1. Preheat the oven to 220°C / 200°C fan.
        2. Peel and quarter the potatoes, then boil in salted water for 10 minutes.
        3. Drain and shake in the pan to rough up the edges.
        4. Heat the oil in a roasting tin for 5 minutes, add the potatoes and roast for 45 minutes,
        turning halfway, adding the garlic and rosemary for the last 15 minutes.

        0:00 Intro
        0:45 Boiling
        Follow me on Instagram: https://instagram.com/somecook
        #roastpotatoes #sundayroast
        """;

    [Fact]
    public void A_youtube_description()
    {
        var recipe = Parser.Parse(YouTubeDescription);

        Assert.Equal("", recipe.Title);
        var section = Assert.Single(recipe.Sections);
        Assert.Null(section.Heading);
        Assert.Equal(["Maris Piper potatoes", "salt", "olive oil", "garlic", "rosemary", "Salt", "pepper"], Names(recipe));
        Assert.Equal(("1", "kg"), (section.Lines[0].Quantity!.ToString(), section.Lines[0].Unit));
        Assert.Equal("smashed", section.Lines[3].Preparation);

        Assert.Equal(4, recipe.Steps.Count);
        Assert.Equal("Preheat the oven to 220°C / 200°C fan.", recipe.Steps[0]);
        Assert.EndsWith("turning halfway, adding the garlic and rosemary for the last 15 minutes.", recipe.Steps[3]);
        Assert.DoesNotContain(recipe.Steps, step => step.Contains("Instagram"));
    }

    // Google Lens keeps the page's line breaks, so sentences wrap mid-line and steps have no numbers.
    private const string LensBookPage = """
        BROWN BUTTER
        CHOCOLATE CHIP COOKIES
        Makes 24 cookies
        For the dough:
        1 cup (2 sticks) unsalted butter
        1¼ cups packed light brown sugar
        ½ cup granulated sugar
        2 large eggs, at room
        temperature
        2 tsp vanilla extract
        2¼ cups all-purpose flour
        1 tsp baking soda
        1 tsp kosher salt
        To finish:
        2 cups dark chocolate chunks
        Flaky sea salt
        Brown the butter in a saucepan over medium heat, swirling, until it smells
        nutty, about 6 minutes. Pour into a large bowl and cool for 15 minutes.
        Whisk in both sugars, then the eggs and vanilla, until glossy.
        Stir in the flour, baking soda and salt, then fold in the chocolate.
        Chill the dough for at least 1 hour. Scoop into balls, bake at 375°F for 10 to 12
        minutes, and sprinkle with flaky salt.
        """;

    [Fact]
    public void A_google_lens_book_page()
    {
        var recipe = Parser.Parse(LensBookPage);

        Assert.Equal("BROWN BUTTER CHOCOLATE CHIP COOKIES", recipe.Title);
        Assert.Equal(new Yield(24, "cookies"), recipe.Yield);
        Assert.Equal(["For the dough", "To finish"], recipe.Sections.Select(s => s.Heading));
        Assert.Equal(
            ["unsalted butter", "light brown sugar", "granulated sugar", "eggs", "vanilla extract",
             "all-purpose flour", "baking soda", "kosher salt", "dark chocolate chunks", "Flaky sea salt"],
            Names(recipe));

        var eggs = recipe.Sections[0].Lines[3];
        Assert.Equal("at room temperature", eggs.Preparation);
        Assert.Equal("2 large eggs, at room temperature", eggs.OriginalText);
        Assert.Equal("1 cup (2 sticks)", $"{recipe.Sections[0].Lines[0].Quantity} {recipe.Sections[0].Lines[0].Unit}");

        Assert.Equal(4, recipe.Steps.Count);
        Assert.Equal("Brown the butter in a saucepan over medium heat, swirling, until it smells nutty, about 6 minutes. Pour into a large bowl and cool for 15 minutes.", recipe.Steps[0]);
        Assert.Equal("Chill the dough for at least 1 hour. Scoop into balls, bake at 375°F for 10 to 12 minutes, and sprinkle with flaky salt.", recipe.Steps[3]);
    }

    // Typed into a phone note: no headings, mixed bullets, a numbered method with a step on its own line.
    private const string MessyNote = """
        Mum's dal

        1 cup red lentils, rinsed
        3 cups water
        1/2 tsp turmeric
        2 T ghee
        1 tsp cumin seeds
        1 onion finely chopped
        salt to taste
        handful cilantro (optional)

        Step 1
        Simmer the lentils in the water with the turmeric for 20-25 minutes until soft
        Step 2
        Heat the ghee, fry the cumin seeds until they pop, then add the onion and cook until golden
        Step 3
        Stir it all into the lentils and season with salt
        """;

    [Fact]
    public void A_messy_note_with_no_headings()
    {
        var recipe = Parser.Parse(MessyNote);

        Assert.Equal("Mum's dal", recipe.Title);
        Assert.Equal(["red lentils", "water", "turmeric", "ghee", "cumin seeds", "onion finely chopped", "salt", "cilantro"], Names(recipe));
        var cilantro = recipe.Sections[0].Lines[^1];
        Assert.True(cilantro.Optional);
        Assert.Equal("handful", cilantro.Unit);

        Assert.Equal(
            ["Simmer the lentils in the water with the turmeric for 20-25 minutes until soft",
             "Heat the ghee, fry the cumin seeds until they pop, then add the onion and cook until golden",
             "Stir it all into the lentils and season with salt"],
            recipe.Steps);
    }

    [Fact]
    public void Steps_lose_their_numbers_and_bullets()
    {
        var recipe = Parser.Parse("""
            Toast
            Ingredients
            2 slices bread
            Directions
            1) Toast the bread.
            Step 2: Butter it.
            • Eat it.
            """);

        Assert.Equal(["Toast the bread.", "Butter it.", "Eat it."], recipe.Steps);
    }

    [Fact]
    public void Ingredients_with_a_heading_and_unheaded_steps_are_split_by_shape()
    {
        var recipe = Parser.Parse("""
            Garlic bread
            Ingredients:
            1 baguette
            4 tbsp butter, softened
            2 cloves garlic, minced
            Mix the butter and garlic together in a small bowl.
            Spread it on the halved baguette and bake for 10 minutes.
            """);

        Assert.Equal(["baguette", "butter", "garlic"], Names(recipe));
        Assert.Equal(2, recipe.Steps.Count);
    }

    [Fact]
    public void Empty_text_gives_an_empty_recipe()
    {
        var recipe = Parser.Parse("  \n\n ");

        Assert.Equal("", recipe.Title);
        Assert.Empty(recipe.Sections);
        Assert.Empty(recipe.Steps);
    }
}
