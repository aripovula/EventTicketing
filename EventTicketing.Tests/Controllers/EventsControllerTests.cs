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

    [Fact]
    public void GetById_ExistingId_ReturnsEvent()
    {
        var result = _controller.GetById(1);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var ev = Assert.IsType<Event>(ok.Value);
        Assert.Equal(1, ev.Id);
    }

    [Fact]
    public void GetById_NonExistingId_ReturnsNotFound()
    {
        var result = _controller.GetById(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }
}
