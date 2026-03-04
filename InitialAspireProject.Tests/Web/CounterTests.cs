using Bunit;
using InitialAspireProject.Web.Components.Pages;

namespace InitialAspireProject.Tests.Web;

public class CounterTests : Bunit.TestContext
{
    [Fact]
    public void Counter_InitialState_DisplaysZero()
    {
        var cut = RenderComponent<Counter>();

        var display = cut.Find("[role=status]");

        Assert.Equal("0", display.TextContent.Trim());
    }

    [Fact]
    public void IncrementCount_Click_IncrementsDisplay()
    {
        var cut = RenderComponent<Counter>();

        cut.Find(".btn-primary.btn-lg").Click();

        Assert.Equal("1", cut.Find("[role=status]").TextContent.Trim());
    }

    [Fact]
    public void DecrementCount_Click_DecrementsDisplay()
    {
        var cut = RenderComponent<Counter>();

        cut.Find(".btn-outline-secondary").Click();

        Assert.Equal("-1", cut.Find("[role=status]").TextContent.Trim());
    }

    [Fact]
    public void ResetCount_AfterIncrements_ResetsToZero()
    {
        var cut = RenderComponent<Counter>();
        cut.Find(".btn-primary.btn-lg").Click();
        cut.Find(".btn-primary.btn-lg").Click();

        cut.Find(".btn-outline-warning").Click();

        Assert.Equal("0", cut.Find("[role=status]").TextContent.Trim());
    }

    [Fact]
    public void IncrementCount_UpdatesTotalClicks()
    {
        var cut = RenderComponent<Counter>();

        cut.Find(".btn-primary.btn-lg").Click();
        cut.Find(".btn-primary.btn-lg").Click();

        Assert.Equal("2", cut.Find(".h4.text-success").TextContent.Trim());
    }

    [Fact]
    public void IncrementCount_UpdatesMaxValue()
    {
        var cut = RenderComponent<Counter>();

        cut.Find(".btn-primary.btn-lg").Click();
        cut.Find(".btn-primary.btn-lg").Click();
        cut.Find(".btn-primary.btn-lg").Click();

        Assert.Equal("3", cut.Find(".h4.text-info").TextContent.Trim());
    }

    [Fact]
    public void DecrementCount_UpdatesMinValue()
    {
        var cut = RenderComponent<Counter>();

        cut.Find(".btn-outline-secondary").Click();
        cut.Find(".btn-outline-secondary").Click();

        Assert.Equal("-2", cut.Find(".h4.text-warning").TextContent.Trim());
    }

    [Fact]
    public void ResetCount_IncrementsTotalClicks()
    {
        var cut = RenderComponent<Counter>();

        cut.Find(".btn-outline-warning").Click();

        Assert.Equal("1", cut.Find(".h4.text-success").TextContent.Trim());
    }

    [Fact]
    public void MixedOperations_TracksMaxAndMinCorrectly()
    {
        var cut = RenderComponent<Counter>();

        cut.Find(".btn-primary.btn-lg").Click();   // +1
        cut.Find(".btn-primary.btn-lg").Click();   // +2
        cut.Find(".btn-outline-secondary").Click(); // +1
        cut.Find(".btn-outline-secondary").Click(); // 0
        cut.Find(".btn-outline-secondary").Click(); // -1

        Assert.Equal("2", cut.Find(".h4.text-info").TextContent.Trim());   // max
        Assert.Equal("-1", cut.Find(".h4.text-warning").TextContent.Trim()); // min
    }
}
