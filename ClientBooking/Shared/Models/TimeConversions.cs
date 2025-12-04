namespace ClientBooking.Shared.Models;

//Helper methods for converting between TimeSpan and TimeOnly
public static class TimeConversions
{
    //Converts a TimeSpan to a TimeOnly, discarding the date component
    public static TimeOnly ToTimeOnly(this TimeSpan timeSpan)
    {
        return new TimeOnly(timeSpan.Hours, timeSpan.Minutes);
    }
}