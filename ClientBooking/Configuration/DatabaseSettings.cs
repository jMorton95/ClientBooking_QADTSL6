namespace ClientBooking.Configuration;

public class DatabaseSettings()
{
    public string? Host { get; init; }
    public string? Port { get; init; }
    public string? Database { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
}