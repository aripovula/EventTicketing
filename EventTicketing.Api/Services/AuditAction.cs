namespace EventTicketing.Api.Services;

public static class AuditAction
{
    public const string EventCreated = "EventCreated";
    public const string EventUpdated = "EventUpdated";
    public const string EventDeleted = "EventDeleted";
    public const string TicketBooked = "TicketBooked";
    public const string UserLoggedIn = "UserLoggedIn";
    public const string UserLoginFailed = "UserLoginFailed";
}
