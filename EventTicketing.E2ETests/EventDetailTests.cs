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
    public async Task BuyTicketButton_DecrementsAvailableSeats()
    {
        await Page.GotoAsync("http://localhost:5173");
        await Page.GetByRole(AriaRole.Link, new() { Name = "Jazz Night" }).ClickAsync();
        await Page.WaitForSelectorAsync("button:has-text('Buy ticket')");

        var seatsText = await Page.Locator("text=/\\d+ of \\d+ seats available/").TextContentAsync();
        var before = int.Parse(seatsText!.Split(' ')[0]);

        var responseTask = Page.WaitForResponseAsync(resp => resp.Url.Contains("/book") && resp.Status == 200);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Buy ticket" }).ClickAsync();
        await responseTask;

        var updatedText = await Page.Locator("text=/\\d+ of \\d+ seats available/").TextContentAsync();
        var after = int.Parse(updatedText!.Split(' ')[0]);

        Assert.That(after, Is.EqualTo(before - 1));

        // Restore: book the seat back via admin edit to avoid DB state pollution
        // (reset AvailableSeats back by updating the event via PUT)
        await Page.APIRequestContext.PutAsync(
            $"http://localhost:5017/api/events/1",
            new() { DataObject = new { Id = 1, Title = "Jazz Night", Description = "An evening of live jazz music.", Date = "2026-08-15T20:00:00", Venue = "Blue Note Club", TotalSeats = 100, AvailableSeats = before, Price = 30 } }
        );
    }
}
