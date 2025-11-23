namespace ClientBooking.Shared.Extensions;

public static class TimeExtensions
{
    public static DateTime RoundTo30Minutes(this DateTime dt)
    {
        var minutes = dt.Minute;
        var newMinutes = minutes < 15 ? 0 :
            minutes < 45 ? 30 : 0;

        var hour = minutes >= 45 ? dt.AddHours(1).Hour : dt.Hour;

        return new DateTime(dt.Year, dt.Month, dt.Day, hour, newMinutes, 0);
    }
}