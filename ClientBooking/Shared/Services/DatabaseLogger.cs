using ClientBooking.Data.Entities;

namespace ClientBooking.Shared.Services;

public class DatabaseLogger : ILogger
{
    private readonly string _categoryName;
    private readonly DatabaseLoggerProvider _provider;

    public DatabaseLogger(string categoryName, DatabaseLoggerProvider provider)
    {
        _categoryName = categoryName;
        _provider = provider;
    }

    public IDisposable? BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel)
    {
        if (!_categoryName.StartsWith("ClientBooking", StringComparison.OrdinalIgnoreCase))
            return false;

        return logLevel >= LogLevel.Information;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);

        _provider.Channel.Writer.TryWrite(new ErrorLog
        {
            LogLevel = logLevel.ToString(),
            Message = message,
            Exception = exception?.ToString(),
            TimestampUtc = DateTime.UtcNow,
            Source = _categoryName
        });
    }
}