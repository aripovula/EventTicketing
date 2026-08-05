using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace EventTicketing.E2ETests;

public class AdminPageTests : PageTest
{
    [SetUp]
    public void SetTimeouts()
    {
        Page.SetDefaultTimeout(60_000);
        Page.SetDefaultNavigationTimeout(60_000);
    }

    [SetUp]
    public async Task GoToAdmin()
    {
        await Page.GotoAsync("http://localhost:5173/login");
        var adminPanel = Page.Locator("form").Filter(new() { HasText = "Admin" });
        await adminPanel.GetByLabel("Admin").ClickAsync();

        // Wait for the login API response before proceeding — more reliable than
        // waiting on a React Router pushState navigation in a loaded CI environment.
        var loginResponse = Page.WaitForResponseAsync(
            resp => resp.Url.Contains("/api/auth/login"));
        await adminPanel.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
        await loginResponse;

        await Page.GotoAsync("http://localhost:5173/admin");
        await Page.WaitForSelectorAsync("ul[aria-label='admin events'] li");
    }

    [Test]
    public async Task AdminPageLoads()
    {
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Admin" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "New event" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task AdminPageShowsAtLeastOneEvent()
    {
        var listItems = Page.GetByRole(AriaRole.List, new() { Name = "admin events" }).Locator("li");
        await Expect(listItems.First).ToBeVisibleAsync();
    }

    [Test]
    public async Task NewEventButtonNavigatesToCreateForm()
    {
        await Page.GetByRole(AriaRole.Link, new() { Name = "New event" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync("http://localhost:5173/admin/new");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "New event" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task CanCreateNewEvent()
    {
        var title = $"Playwright Event {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        var listItems = Page.GetByRole(AriaRole.List, new() { Name = "admin events" }).Locator("li");
        var initialCount = await listItems.CountAsync();

        await Page.GetByRole(AriaRole.Link, new() { Name = "New event" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync("http://localhost:5173/admin/new");

        await Page.GetByLabel("Title").FillAsync(title);
        await Page.GetByLabel("Venue").FillAsync("Test Venue");
        await Page.GetByLabel("Description").FillAsync("Created by Playwright.");
        await Page.GetByLabel("Start time").FillAsync("2026-12-01T19:00");
        await Page.GetByLabel("End time").FillAsync("2026-12-01T22:00");
        await Page.GetByLabel("Total seats").FillAsync("50");
        await Page.GetByLabel("Available seats").FillAsync("50");
        await Page.GetByLabel("Price ($)").FillAsync("30");

        var responseTask = Page.WaitForResponseAsync(resp => resp.Url.Contains("/api/events") && resp.Status == 201);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create event" }).ClickAsync();
        await responseTask;

        await Expect(Page).ToHaveURLAsync("http://localhost:5173/admin");
        await Expect(listItems).ToHaveCountAsync(initialCount + 1);

        // Clean up
        await listItems.Last.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
        var dialog = Page.GetByRole(AriaRole.Dialog);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
        await Expect(listItems).ToHaveCountAsync(initialCount);
    }

    [Test]
    public async Task EditLinkNavigatesToEditForm()
    {
        await Page.GetByRole(AriaRole.List, new() { Name = "admin events" })
            .Locator("li").First
            .GetByRole(AriaRole.Link, new() { Name = "Edit" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/admin/\\d+/edit"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Edit event" })).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Title")).Not.ToBeEmptyAsync();
    }

    [Test]
    public async Task CancelOnEditFormNavigatesBackToAdmin()
    {
        await Page.GetByRole(AriaRole.List, new() { Name = "admin events" })
            .Locator("li").First
            .GetByRole(AriaRole.Link, new() { Name = "Edit" }).ClickAsync();

        // Wait for URL then for the heading — the heading only renders after the
        // event fetch completes, so give it a generous timeout for slow CI runners.
        await Expect(Page).ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex("/admin/\\d+/edit"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Edit event" }))
            .ToBeVisibleAsync(new() { Timeout = 15_000 });

        await Page.GetByRole(AriaRole.Link, new() { Name = "Cancel" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync("http://localhost:5173/admin");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Admin" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task CanDeleteEvent()
    {
        var title = $"Delete Me {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        var listItems = Page.GetByRole(AriaRole.List, new() { Name = "admin events" }).Locator("li");

        await Page.GetByRole(AriaRole.Link, new() { Name = "New event" }).ClickAsync();
        await Page.GetByLabel("Title").FillAsync(title);
        await Page.GetByLabel("Venue").FillAsync("Nowhere");
        await Page.GetByLabel("Description").FillAsync("Temporary.");
        await Page.GetByLabel("Start time").FillAsync("2026-11-01T10:00");
        await Page.GetByLabel("End time").FillAsync("2026-11-01T12:00");
        await Page.GetByLabel("Total seats").FillAsync("10");
        await Page.GetByLabel("Available seats").FillAsync("10");
        await Page.GetByLabel("Price ($)").FillAsync("1");

        var createTask = Page.WaitForResponseAsync(resp => resp.Url.Contains("/api/events") && resp.Status == 201);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create event" }).ClickAsync();
        await createTask;

        await Expect(Page).ToHaveURLAsync("http://localhost:5173/admin");
        await Expect(listItems.First).ToBeVisibleAsync();
        var countAfterCreate = await listItems.CountAsync();

        await listItems.Last.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
        var dialog = Page.GetByRole(AriaRole.Dialog);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
        await Expect(listItems).ToHaveCountAsync(countAfterCreate - 1);
    }
}
