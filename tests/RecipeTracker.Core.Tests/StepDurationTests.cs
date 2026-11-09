using RecipeTracker.Core.Parsing;

namespace RecipeTracker.Core.Tests;

public class StepDurationTests
{
    // step text, the time found as written, low in seconds, high in seconds (0 for none)
    [Theory]
    [InlineData("Simmer 20 minutes.", "20 minutes", 1200, 0)]
    [InlineData("Bake 25-30 min until golden.", "25-30 min", 1500, 1800)]
    [InlineData("Bake 25–30 mins.", "25–30 mins", 1500, 1800)]
    [InlineData("Cook for 10 to 15 minutes, stirring.", "10 to 15 minutes", 600, 900)]
    [InlineData("Rest 2 or 3 minutes.", "2 or 3 minutes", 120, 180)]
    [InlineData("Roast 1½ hours.", "1½ hours", 5400, 0)]
    [InlineData("Roast 1 1/2 hours.", "1 1/2 hours", 5400, 0)]
    [InlineData("Roast 1-1/2 hours.", "1-1/2 hours", 5400, 0)]
    [InlineData("Braise 2.5 hrs.", "2.5 hrs", 9000, 0)]
    [InlineData("Leave for an hour.", "an hour", 3600, 0)]
    [InlineData("Proof for half an hour.", "half an hour", 1800, 0)]
    [InlineData("Proof for an hour and a half.", "an hour and a half", 5400, 0)]
    [InlineData("Proof for 1 and a half hours.", "1 and a half hours", 5400, 0)]
    [InlineData("Whisk for 30 seconds.", "30 seconds", 30, 0)]
    [InlineData("Microwave 45 secs.", "45 secs", 45, 0)]
    [InlineData("Boil for ten minutes.", "ten minutes", 600, 0)]
    [InlineData("Wait a couple of minutes.", "a couple of minutes", 120, 0)]
    [InlineData("Give it a 20-minute rest.", "20-minute", 1200, 0)]
    [InlineData("Bake 1 hour 15 minutes.", "1 hour 15 minutes", 4500, 0)]
    [InlineData("Bake 1 hr, 20 min.", "1 hr, 20 min", 4800, 0)]
    [InlineData("Microwave 2 minutes and 30 seconds.", "2 minutes and 30 seconds", 150, 0)]
    [InlineData("Bake 1h at 180C.", "1h", 3600, 0)]
    [InlineData("SIMMER 5 MINUTES", "5 MINUTES", 300, 0)]
    public void Finds_one_time(string text, string written, int lowSeconds, int highSeconds)
    {
        var found = Assert.Single(StepDurations.Find(text));

        Assert.Equal(written, text.Substring(found.Start, found.Length));
        Assert.Equal(TimeSpan.FromSeconds(lowSeconds), found.Low);
        Assert.Equal(highSeconds == 0 ? null : TimeSpan.FromSeconds(highSeconds), found.High);
    }

    [Theory]
    [InlineData("Preheat the oven to 350°F.")]
    [InlineData("Add 2 minced garlic cloves.")]
    [InlineData("Add a second egg.")]
    [InlineData("Heat a hot pan.")]
    [InlineData("Use 13 cups of stock, often stirring.")]
    [InlineData("Chill for 5 days.")]
    [InlineData("Marinate 100 hours.")]
    [InlineData("")]
    public void Finds_nothing(string text)
    {
        Assert.Empty(StepDurations.Find(text));
    }

    [Fact]
    public void Finds_several_times_in_order()
    {
        var text = "Sear 3 minutes per side, then roast 20-25 minutes and rest 10 minutes.";

        var found = StepDurations.Find(text);

        Assert.Equal(["3 minutes", "20-25 minutes", "10 minutes"], found.Select(d => text.Substring(d.Start, d.Length)));
    }

    [Theory]
    [InlineData("Simmer 20 minutes.", "20 min")]
    [InlineData("Bake 25-30 min.", "25–30 min")]
    [InlineData("Roast 1½ hours.", "1 hr 30 min")]
    [InlineData("Whisk 30 seconds.", "30 sec")]
    [InlineData("Roast 1-2 hours.", "1 hr – 2 hr")]
    public void Labels_a_time_for_showing(string text, string label)
    {
        Assert.Equal(label, Assert.Single(StepDurations.Find(text)).Label);
    }
}
