using Microsoft.AspNetCore.SignalR;

namespace EventTicketing.Api.Hubs;

/// <summary>
/// Real-time hub. Server pushes BookingMade(eventId) to all clients after a ticket is booked.
/// </summary>
public class TicketingHub : Hub { }
