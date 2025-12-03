using ClientBooking.Data;
using ClientBooking.Data.Entities;
using ClientBooking.Shared.Services;
using System.Threading.Channels;

namespace ClientBooking.Shared;


public class DatabaseLoggerProvider : ILoggerProvider
{
    public Channel<ErrorLog> Channel { get; } =
        System.Threading.Channels.Channel.CreateUnbounded<ErrorLog>();

    private readonly IServiceProvider _services;
    private readonly CancellationTokenSource _cts = new();

    public DatabaseLoggerProvider(IServiceProvider services)
    {
        _services = services;

        Task.Run(BackgroundWorker);
    }
    

    public ILogger CreateLogger(string categoryName)
        => new DatabaseLogger(categoryName, this);

    public void Dispose()
    {
        _cts.Cancel();
        GC.SuppressFinalize(this);
    }

    private async Task BackgroundWorker()
    {
        while (!_cts.IsCancellationRequested)
        {
            var log = await Channel.Reader.ReadAsync(_cts.Token);

            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();

            db.ErrorLogs.Add(log);
            await db.SaveChangesAsync();
        }
    }
}
