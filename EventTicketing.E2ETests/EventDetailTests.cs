using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace EventTicketing.E2ETests;

[Parallelizable(ParallelScope.Self)]
public class EventDetailTests : PageTest
{
    [Test]
    public async Task BuyTicketButton_IsVisibleOnEventDetailPage()
    {
        await Page.GotoAsync("http://localhost:5173");
        await Page.GetByRole(AriaRole.Link, new() { Name = "Jazz Night" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/events/\\d+"));

        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Buy ticket" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task BuyTicketButton_OpensPlaceOrderModal()
    {
        await Page.GotoAsync("http://localhost:5173");
        await Page.GetByRole(AriaRole.Link, new() { Name = "Jazz Night" }).ClickAsync();
        await Page.WaitForSelectorAsync("button:has-text('Buy ticket')");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Buy ticket" }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Place order" })).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Email")).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Card number")).ToBeVisibleAsync();
    }

    [Test]
    public async Task BookingModal_ShowsEventDateAndVenue()
    {
        await Page.GotoAsync("http://localhost:5173");
        await Page.GetByRole(AriaRole.Link, new() { Name = "Jazz Night" }).ClickAsync();
        await Page.WaitForSelectorAsync("button:has-text('Buy ticket')");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Buy ticket" }).ClickAsync();

        var dialog = Page.GetByRole(AriaRole.Dialog);
        await Expect(dialog.GetByText(new System.Text.RegularExpressions.Regex("Blue Note Club"))).ToBeVisibleAsync();
        await Expect(dialog.GetByText(new System.Text.RegularExpressions.Regex("2026"))).ToBeVisibleAsync();
    }

    [Test]
    public async Task BookingModal_CancelClosesModal()
    {
        await Page.GotoAsync("http://localhost:5173");
        await Page.GetByRole(AriaRole.Link, new() { Name = "Jazz Night" }).ClickAsync();
        await Page.WaitForSelectorAsync("button:has-text('Buy ticket')");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Buy ticket" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Dialog)).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task BookingModal_ConfirmDecrementsAvailableSeats()
    {
        await Page.GotoAsync("http://localhost:5173");
        await Page.GetByRole(AriaRole.Link, new() { Name = "Jazz Night" }).ClickAsync();
        await Page.WaitForSelectorAsync("button:has-text('Buy ticket')");

        var seatsText = await Page.Locator("text=/\\d+ of \\d+ seats available/").TextContentAsync();
        var before = int.Parse(seatsText!.Split(' ')[0]);

        await Page.GetByRole(AriaRole.Button, new() { Name = "Buy ticket" }).ClickAsync();
        await Page.GetByLabel("Email").FillAsync("test@example.com");
        await Page.GetByLabel("Card number").FillAsync("1234567890123456");
        await Page.GetByLabel("Expiry (MM/YY)").FillAsync("12/27");
        await Page.GetByLabel("CVV").FillAsync("123");

        var responseTask = Page.WaitForResponseAsync(resp => resp.Url.Contains("/book") && resp.Status == 200);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Place order" }).ClickAsync();
        await responseTask;

        await Expect(Page.GetByRole(AriaRole.Dialog)).Not.ToBeVisibleAsync();

        var updatedText = await Page.Locator("text=/\\d+ of \\d+ seats available/").TextContentAsync();
        var after = int.Parse(updatedText!.Split(' ')[0]);
        Assert.That(after, Is.EqualTo(before - 1));

        // Restore seat count to avoid DB state pollution
        var api = await Playwright.APIRequest.NewContextAsync(new() { BaseURL = "http://localhost:5017" });
        await api.PutAsync("/api/events/1", new() { DataObject = new { Id = 1, Title = "Jazz Night", Description = "An evening of live jazz music.", Date = "2026-08-15T20:00:00", Venue = "Blue Note Club", TotalSeats = 100, AvailableSeats = before, Price = 30 } });
        await api.DisposeAsync();
    }
}
