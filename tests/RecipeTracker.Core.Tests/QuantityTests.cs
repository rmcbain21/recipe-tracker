using RecipeTracker.Core.Quantities;

namespace RecipeTracker.Core.Tests;

public class QuantityTests
{
    private static Quantity? Parse(string text)
    {
        Assert.True(Quantity.TryParse(text, out var quantity), $"\"{text}\" should parse");
        return quantity;
    }

    [Theory]
    [InlineData("2", 2)]
    [InlineData("1.5", 1.5)]
    [InlineData(".5", 0.5)]
    [InlineData("½", 0.5)]
    [InlineData("¾", 0.75)]
    [InlineData("1½", 1.5)]
    [InlineData("1 ½", 1.5)]
    [InlineData("1/2", 0.5)]
    [InlineData("1 1/2", 1.5)]
    [InlineData("2 3/4", 2.75)]
    [InlineData("1⁄2", 0.5)]
    [InlineData("1-1/2", 1.5)]
    [InlineData("  3  ", 3)]
    public void Exact_quantities_are_read(string text, double expected)
    {
        Assert.Equal(new Quantity((decimal)expected), Parse(text));
    }

    [Theory]
    [InlineData("2-3", 2, 3)]
    [InlineData("2 - 3", 2, 3)]
    [InlineData("2–3", 2, 3)]
    [InlineData("2 to 3", 2, 3)]
    [InlineData("1½-2", 1.5, 2)]
    [InlineData("1/2 to 1", 0.5, 1)]
    [InlineData("1 1/2 - 2", 1.5, 2)]
    public void Ranges_are_read(string text, double low, double high)
    {
        Assert.Equal(new Quantity((decimal)low, (decimal)high), Parse(text));
    }

    [Fact]
    public void A_range_with_the_same_ends_is_exact()
    {
        Assert.Equal(new Quantity(2), Parse("2-2"));
    }

    [Fact]
    public void Thirds_are_read_close_enough_to_show_as_thirds()
    {
        Assert.Equal("⅓", Parse("1/3")!.ToString());
        Assert.Equal("1⅔", Parse("1 2/3")!.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Blank_means_no_quantity(string? text)
    {
        Assert.True(Quantity.TryParse(text, out var quantity));
        Assert.Null(quantity);
    }

    [Theory]
    [InlineData("a few")]
    [InlineData("to taste")]
    [InlineData("2 cups")]
    [InlineData("0")]
    [InlineData("3-2")]
    [InlineData("1/0")]
    [InlineData("11/2 3")]
    [InlineData("1..5")]
    public void Text_that_is_not_a_quantity_is_rejected(string text)
    {
        Assert.False(Quantity.TryParse(text, out _));
    }

    [Theory]
    [InlineData(2, "2")]
    [InlineData(0.5, "½")]
    [InlineData(1.5, "1½")]
    [InlineData(0.25, "¼")]
    [InlineData(2.75, "2¾")]
    [InlineData(0.125, "⅛")]
    [InlineData(0.375, "⅜")]
    [InlineData(1.2, "1⅕")]
    [InlineData(0.15, "0.15")]
    [InlineData(1.999, "2")]
    public void Numbers_are_shown_with_fraction_glyphs(double value, string expected)
    {
        Assert.Equal(expected, Quantity.FormatNumber((decimal)value));
    }

    [Fact]
    public void A_range_is_shown_with_a_dash()
    {
        Assert.Equal("1½–2", new Quantity(1.5m, 2m).ToString());
    }
}
