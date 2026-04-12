using EventTicketing.Api.Controllers;
using EventTicketing.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventTicketing.Tests.Controllers;

public class EventsControllerTests
{
    private readonly EventsController _controller = new();

    [Fact]
    public void GetAll_ReturnsOk()
    {
        var result = _controller.GetAll();

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public void GetAll_ReturnsEvents()
    {
        var result = _controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var events = Assert.IsAssignableFrom<IEnumerable<Event>>(ok.Value);
        Assert.NotEmpty(events);
    }
}
