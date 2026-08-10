using Microsoft.Extensions.Logging;

namespace EventTicketing.Tests.Fakes;

/// <summary>
/// In-memory logger that records every log call so tests can assert on
/// what was logged without spinning up a real logging pipeline.
/// </summary>
public class CapturingLogger<T> : ILogger<T>
{
    public record LogEntry(LogLevel Level, string Message, Exception? Exception);

    public List<LogEntry> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
        => Entries.Add(new(logLevel, formatter(state, exception), exception));
}
