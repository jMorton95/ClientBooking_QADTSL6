namespace ClientBooking.Shared.Models;

public static class TimeConversions
{
    public static TimeSpan ToTimeSpan(this TimeOnly timeOnly)
    {
        return new TimeSpan(timeOnly.Hour, timeOnly.Minute, 0);
    }

    public static TimeOnly ToTimeOnly(this TimeSpan timeSpan)
    {
        return new TimeOnly(timeSpan.Hours, timeSpan.Minutes);
    }
}