using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace EventTicketing.E2ETests;

[Parallelizable(ParallelScope.Self)]
public class HomePageTests : PageTest
{
    [Test]
    public async Task HomepageLoads()
    {
        await Page.GotoAsync("http://localhost:5173");
        await Expect(Page).ToHaveTitleAsync("Event Ticketing");
    }

    [Test]
    public async Task ClickingEventNavigatesToDetailPage()
    {
        await Page.GotoAsync("http://localhost:5173");
        var responseTask = Page.WaitForResponseAsync(resp => resp.Url.Contains("/api/events/") && resp.Status == 200);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Jazz Night" }).ClickAsync();
        await responseTask;
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/events/\\d+"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Jazz Night" })).ToBeVisibleAsync();
    }
}
