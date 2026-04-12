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
}
