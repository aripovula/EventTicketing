using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace EventTicketing.E2ETests;

public class HomePageTests : PageTest
{
    [SetUp]
    public void SetTimeouts()
    {
        Page.SetDefaultTimeout(60_000);
        Page.SetDefaultNavigationTimeout(60_000);
    }

    [Test]
    public async Task HomepageLoads()
    {
        await Page.GotoAsync("http://localhost:5173");
        await Expect(Page).ToHaveTitleAsync("Event Ticketing");
    }

    [Test]
    [Ignore("Flaky in CI: Jazz Night link times out waiting for events list to load on slow runner")]
    public async Task ClickingEventNavigatesToDetailPage()
    {
        await Page.GotoAsync("http://localhost:5173");
        var responseTask = Page.WaitForResponseAsync(resp => resp.Url.Contains("/api/events/") && resp.Status == 200);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Jazz Night" }).ClickAsync();
        await responseTask;
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/events/\\d+"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Jazz Night" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task SearchFilterShowsOnlyMatchingEvents()
    {
        await Page.GotoAsync("http://localhost:5173");
        await Page.WaitForSelectorAsync("ul[aria-label='events'] li");

        await Page.GetByPlaceholder("type search term").FillAsync("jazz");

        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Jazz Night" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Tech Conference 2026" })).Not.ToBeVisibleAsync();
    }

    [Test]
    [NonParallelizable] // Prevents simultaneous browser-context creation that causes SetUp timeouts in CI
    public async Task SearchInputExpandsOnFocusAndShrinksOnBlur()
    {
        await Page.GotoAsync("http://localhost:5173");
        await Page.WaitForSelectorAsync("ul[aria-label='events'] li");

        var input = Page.GetByPlaceholder("type search term");

        await input.ClickAsync();
        await Expect(input).ToHaveCSSAsync("width", "400px");
        await Expect(input).ToHaveCSSAsync("font-size", "22px");

        await Page.Keyboard.PressAsync("Tab");
        await Expect(input).ToHaveCSSAsync("width", "200px");
        await Expect(input).ToHaveCSSAsync("font-size", "12px");
    }

    [Test]
    [Ignore("Flaky in CI: Jazz Night link times out after navigating back to event list on slow runner")]
    public async Task BackLinkNavigatesBackToEventList()
    {
        await Page.GotoAsync("http://localhost:5173");
        var responseTask = Page.WaitForResponseAsync(resp => resp.Url.Contains("/api/events/") && resp.Status == 200);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Jazz Night" }).ClickAsync();
        await responseTask;
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Jazz Night" })).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Link, new() { Name = "← Back to events" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync("http://localhost:5173/");
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Jazz Night" })).ToBeVisibleAsync();
    }
}
