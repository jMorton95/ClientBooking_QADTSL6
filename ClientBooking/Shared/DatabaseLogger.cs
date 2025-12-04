using ClientBooking.Data.Entities;

namespace ClientBooking.Shared;

//Custom logger that writes logs to the database
//This is used to write logs to the database from the background worker in DatabaseLoggerProvider
public class DatabaseLogger(string categoryName, DatabaseLoggerProvider provider) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) => null;

    //Only log messages from ClientBooking namespace
    //This is to prevent logging sensitive information from other namespaces and libraries used in the application
    public bool IsEnabled(LogLevel logLevel)
    {
        if (!categoryName.StartsWith("ClientBooking", StringComparison.OrdinalIgnoreCase))
            return false;

        return logLevel >= LogLevel.Information;
    }

    //Invokes the global logging provider to write the log to the database
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);

        provider.Channel.Writer.TryWrite(new ErrorLog
        {
            LogLevel = logLevel.ToString(),
            Message = message,
            Exception = exception?.ToString(),
            TimestampUtc = DateTime.UtcNow,
            Source = categoryName
        });
    }
}