// EventTicketing.Scheduler — demo date refresher
//
// Manages 8 showcase events so the app always shows upcoming events regardless
// of when it is opened.
//
// ┌──────────────────────────────────────────────────────┬────────────────────────────────────────────────┐
// │                    Before day 20                     │                On/after day 20                 │
// ├──────────────────────────────────────────────────────┼────────────────────────────────────────────────┤
// │ 4 events in current month (conflict pair + 2 others) │ 2 events in current month (conflict pair only) │
// ├──────────────────────────────────────────────────────┼────────────────────────────────────────────────┤
// │ 4 events in next month                               │ 6 events in next month                         │
// └──────────────────────────────────────────────────────┴────────────────────────────────────────────────┘
//
// The conflict pair (Jazz Night Id=1 + Blues & Soul Evening Id=30) always land
// on the same date with overlapping times, so John and Jane's existing bookings
// always trigger the conflict alert on the events list.
//
// Usage:
//   dotnet run --project EventTicketing.Scheduler -- /path/to/events.db
//   DB_PATH=/path/to/events.db dotnet run --project EventTicketing.Scheduler
//
// Cron job (daily at midnight):
//   0 0 * * * cd /path/to/dotnet_mvc && dotnet run --project EventTicketing.Scheduler -- EventTicketing.Api/events.db >> scheduler.log 2>&1
//
// Or publish first for faster startup:
//   dotnet publish EventTicketing.Scheduler -c Release -o scheduler-bin
//   0 0 * * * /path/to/scheduler-bin/EventTicketing.Scheduler /path/to/events.db

using Microsoft.Data.Sqlite;

// ── Configuration ─────────────────────────────────────────────────────────────
// DB path: first CLI arg → DB_PATH env var → default "events.db" next to the API
string dbPath = args.Length > 0
    ? args[0]
    : Environment.GetEnvironmentVariable("DB_PATH") ?? "events.db";

// ── Event schedule ────────────────────────────────────────────────────────────
// Each entry maps an event Id to its fixed time-of-day slot (independent of date).
// Ids 1 (Jazz Night) and 30 (Blues & Soul Evening) are the conflict pair — they
// are always placed on the same date, with overlapping times, so that users who
// have a booking for Jazz Night see the conflict warning on Blues & Soul Evening.
var schedules = new Dictionary<int, (TimeSpan Start, TimeSpan End)>
{
    [1] = (new(19, 0, 0), new(22, 0, 0)),  // Jazz Night
    [30] = (new(20, 30, 0), new(23, 0, 0)),  // Blues & Soul Evening  ← overlaps Jazz Night
    [2] = (new(9, 0, 0), new(18, 0, 0)),  // Tech Conference
    [3] = (new(20, 0, 0), new(22, 30, 0)),  // Comedy Showcase
    [7] = (new(12, 0, 0), new(20, 0, 0)),  // Food & Wine Festival
    [8] = (new(10, 0, 0), new(18, 0, 0)),  // Modern Art Exhibition
    [13] = (new(19, 30, 0), new(22, 0, 0)),  // Symphony Orchestra
    [22] = (new(20, 0, 0), new(23, 0, 0)),  // Salsa Dance Night
};
int[] otherIds = [2, 3, 7, 8, 13, 22];

// ── Date distribution ─────────────────────────────────────────────────────────
var today = DateTime.Today;
bool isLate = today.Day >= 20;
var tomorrow = today.AddDays(1);
var endCurrent = EndOfMonth(today);
var nextStart = new DateTime(today.Year, today.Month, 1).AddMonths(1);
var endNext = EndOfMonth(nextStart);
bool hasDaysLeft = tomorrow <= endCurrent;

DateTime conflictDate;
DateTime[] otherDates;

if (!isLate && hasDaysLeft)
{
    // Before 20th: conflict pair + 2 others in current month (4 events),
    //              4 others in next month
    var cur = Spread(tomorrow, endCurrent, 3);  // 3 slots: [conflict, other, other]
    var next = Spread(nextStart, endNext, 4);
    conflictDate = cur[0];
    otherDates = [cur[1], cur[2], .. next];
}
else if (isLate && hasDaysLeft)
{
    // On/after 20th: conflict pair only in current month (2 events),
    //                6 others in next month
    conflictDate = Spread(tomorrow, endCurrent, 1)[0];
    otherDates = Spread(nextStart, endNext, 6);
}
else
{
    // No days left in current month: everything moves to next month
    var next = Spread(nextStart, endNext, 7);  // 7 slots: [conflict, other×6]
    conflictDate = next[0];
    otherDates = next[1..];
}

// ── Apply updates ─────────────────────────────────────────────────────────────
using var conn = new SqliteConnection($"Data Source={dbPath}");
conn.Open();

// Conflict pair — same date, overlapping times
UpdateEvent(conn, 1, conflictDate, schedules[1].Start, schedules[1].End);
UpdateEvent(conn, 30, conflictDate, schedules[30].Start, schedules[30].End);

// Other demo events
for (int i = 0; i < otherIds.Length; i++)
{
    var id = otherIds[i];
    UpdateEvent(conn, id, otherDates[i], schedules[id].Start, schedules[id].End);
}

// Keep orders consistent: BookedAt = 14 days before the updated Jazz Night date
using var orderCmd = conn.CreateCommand();
orderCmd.CommandText = """
    UPDATE Orders SET BookedAt = @b
    WHERE EventId = 1 AND UserId IN (
        SELECT Id FROM Users WHERE Email IN ('john@example.com', 'jane@example.com')
    )
    """;
orderCmd.Parameters.AddWithValue("@b", conflictDate.AddDays(-14).ToString("yyyy-MM-dd HH:mm:ss"));
orderCmd.ExecuteNonQuery();

var mode = !isLate ? "before-20th (4 current + 4 next)" : "on/after-20th (2 current + 6 next)";
Console.WriteLine($"[{DateTime.Now:u}] Scheduler complete.");
Console.WriteLine($"  Mode          : {mode}");
Console.WriteLine($"  Conflict date : {conflictDate:yyyy-MM-dd}  (Jazz Night 19:00–22:00 / Blues & Soul 20:30–23:00)");
Console.WriteLine($"  Other dates   : {string.Join(", ", otherDates.Select(d => d.ToString("yyyy-MM-dd")))}");

// ── Helpers ───────────────────────────────────────────────────────────────────

static void UpdateEvent(SqliteConnection conn, int id, DateTime date, TimeSpan start, TimeSpan end)
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "UPDATE Events SET StartTime = @s, EndTime = @e WHERE Id = @id";
    cmd.Parameters.AddWithValue("@s", date.Add(start).ToString("yyyy-MM-dd HH:mm:ss"));
    cmd.Parameters.AddWithValue("@e", date.Add(end).ToString("yyyy-MM-dd HH:mm:ss"));
    cmd.Parameters.AddWithValue("@id", id);
    cmd.ExecuteNonQuery();
}

// Returns `count` evenly-spaced dates between `start` and `end` (inclusive).
static DateTime[] Spread(DateTime start, DateTime end, int count)
{
    if (count <= 0) return [];
    int totalDays = Math.Max(count, (end - start).Days + 1);
    int step = Math.Max(1, totalDays / (count + 1));
    var result = new DateTime[count];
    for (int i = 0; i < count; i++)
        result[i] = start.AddDays(step * (i + 1) - 1) is var d && d <= end ? d : end;
    return result;
}

static DateTime EndOfMonth(DateTime d) =>
    new(d.Year, d.Month, DateTime.DaysInMonth(d.Year, d.Month));
